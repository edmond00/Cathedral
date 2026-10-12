using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Cathedral;

/// <summary>
/// The demo's run allowance: how many runs a demo build has started, kept where a player cannot
/// casually wind it back.
///
/// <para><b>What it is.</b> A counter of runs <i>started</i> — not finished, not saved — written in
/// two copies, each signed with an HMAC over a per-installation id and the count. Neither copy lives
/// in the game's folder (reinstalling resets nothing) and neither is the save (deleting or restoring
/// a save resets nothing). The counter is spent by <see cref="TryConsumeRun"/>, which persists the
/// new count <b>before</b> a run is built, so a crash, a kill or a restored save cannot refund it.</para>
///
/// <para><b>What it is not.</b> Security. The HMAC key is a constant in this assembly, and anyone
/// willing to open the executable can extract it and sign any count they like; anyone willing to
/// back up and restore both copies together can rewind them; anyone who deletes both copies is a new
/// installation, because offline that is indistinguishable from one. The point is that the counter
/// cannot be reset by editing a text file or deleting a folder, which is the effort a casual player
/// will spend. There is no server and no DRM, deliberately: this ships as a plain itch.io download.</para>
///
/// <para><b>Failing safe.</b> Every ambiguity resolves against the player getting another run:</para>
/// <list type="bullet">
/// <item>both copies missing — a fresh installation, the only state that starts at zero;</item>
/// <item>one copy missing — the other is trusted and the missing one rewritten from it;</item>
/// <item>both valid with different counts — the higher one, which is also what an interrupted write
/// between the two copies leaves behind;</item>
/// <item>both valid with different installation ids, or either copy present but unreadable or
/// wrongly signed — <see cref="DemoState.Locked"/>: no new run starts, and the menu says why.
/// Locked is never cleared by the game. A copy that is present cannot be told apart from one that was
/// edited, and writes are atomic (temp file, then rename), so honest corruption is not expected.</item>
/// </list>
///
/// <para>Inert outside a demo: <see cref="IsActive"/> is compiled on by the DEMO constant
/// (<c>package.ps1 -Demo</c>), and a development build turns it on with <c>--demo</c>.</para>
/// </summary>
public static class DemoLimit
{
    /// <summary>Whether this build limits runs at all.</summary>
    public static bool IsActive { get; private set; } =
#if DEMO
        true;
#else
        false;
#endif

    /// <summary>How many runs a demo build may start. <c>--demo-max-runs</c> overrides it in development.</summary>
    public static int MaxRuns { get; private set; } = Config.Demo.MaxRuns;

    private static DemoStore? _store;

    /// <summary>The store the game uses: the two default locations unless <c>--demo-state-dir</c> moved them.</summary>
    public static DemoStore Store => _store ??= DemoStore.Default();

    /// <summary>
    /// Development switches, parsed from the command line. A shipped build never sees them —
    /// <see cref="ShipArguments"/> strips everything not on its allow-list — so a demo build cannot be
    /// talked out of its limit by its own command line.
    /// </summary>
    public static void Configure(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--demo") IsActive = true;
            if (args[i] == "--demo-max-runs" && i + 1 < args.Length && int.TryParse(args[i + 1], out int n))
            {
                IsActive = true;
                MaxRuns = Math.Max(0, n);
            }
            if (args[i] == "--demo-state-dir" && i + 1 < args.Length)
            {
                IsActive = true;
                _store = DemoStore.InDirectory(args[i + 1]);
            }
        }

        // --demo-reset wipes a --demo-state-dir store, so a CLI script starts from a fresh
        // installation every time it runs. Never the default store: a stray flag must not be able to
        // erase the real counter, even in development.
        if (System.Array.IndexOf(args, "--demo-reset") >= 0)
        {
            if (_store is null)
                Console.WriteLine("DemoLimit: --demo-reset ignored without --demo-state-dir");
            else
                foreach (var p in new[] { _store.PrimaryPath, _store.SecondaryPath })
                    if (File.Exists(p)) File.Delete(p);
        }
    }

    /// <summary>The state as the menu should describe it. Repairs a missing copy as a side effect.</summary>
    public static DemoStatus Status() => Store.Load(MaxRuns);

    /// <summary>
    /// Spends one run, or refuses. Call immediately before a new run is built, and build nothing on
    /// false. Always true outside a demo.
    /// </summary>
    public static bool TryConsumeRun(out DemoStatus status)
    {
        if (!IsActive) { status = DemoStatus.Unlimited; return true; }
        return Store.TryConsume(MaxRuns, out status);
    }
}

/// <summary>What the two copies amount to.</summary>
public enum DemoState
{
    /// <summary>Not a demo.</summary>
    Unlimited,
    /// <summary>Runs remain.</summary>
    Available,
    /// <summary>Every run has been started.</summary>
    Exhausted,
    /// <summary>A copy is damaged, wrongly signed, or belongs to another installation. No run starts.</summary>
    Locked,
}

public readonly record struct DemoStatus(DemoState State, int RunsUsed, int MaxRuns, string Detail)
{
    public static readonly DemoStatus Unlimited = new(DemoState.Unlimited, 0, 0, "");

    public int RunsLeft => Math.Max(0, MaxRuns - RunsUsed);

    public bool CanStartRun => State is DemoState.Unlimited or DemoState.Available;

    /// <summary>The one line the main menu draws. Kept under 90 columns.</summary>
    public string MenuLine => State switch
    {
        DemoState.Available => RunsLeft == 1
            ? $"Demo  ·  1 run left of {MaxRuns}"
            : $"Demo  ·  {RunsLeft} runs left of {MaxRuns}",
        DemoState.Exhausted => $"The demo's {MaxRuns} runs are spent  ·  the full game is on {Config.Demo.StorePage}",
        DemoState.Locked    => "The demo's run record is damaged or was edited  ·  no new run can start",
        _                   => "",
    };
}

/// <summary>
/// The two signed copies and the rules for reconciling them. Instance-based so the self-test can run
/// it against throwaway folders; the game only ever uses <see cref="DemoLimit.Store"/>.
/// </summary>
public sealed class DemoStore
{
    public string PrimaryPath   { get; }
    public string SecondaryPath { get; }

    public DemoStore(string primaryPath, string secondaryPath)
    {
        PrimaryPath   = primaryPath;
        SecondaryPath = secondaryPath;
    }

    /// <summary>
    /// Roaming and Local application data: two folders a player clears separately, if at all, and
    /// neither the save's file nor the install folder. The secondary sits under a name that does not
    /// announce itself next to the game's own folder.
    /// </summary>
    public static DemoStore Default() => new(
        Path.Combine(AppData.Directory, "demo.state"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                     AppData.FolderName + "-cache", "index.bin"));

    /// <summary>Both copies under one folder, in separate subfolders. Tests and <c>--demo-state-dir</c>.</summary>
    public static DemoStore InDirectory(string dir) => new(
        Path.Combine(dir, "a", "demo.state"),
        Path.Combine(dir, "b", "index.bin"));

    // ── The signature ────────────────────────────────────────────────────────

    // Embedded, and therefore extractable by anyone with a decompiler — see the class comment on
    // DemoLimit. It exists to make the file not editable by hand, not to make it secret.
    private static readonly byte[] Key = Convert.FromHexString(
        "6B1F0E8A4C2D97B35E10F6A9C7D24B8E31A05FD2689C4E17B0D3A6F95C28E741");

    private const string Magic = "PPDEMO1";

    internal static string Sign(string installId, int runs)
    {
        using var hmac = new HMACSHA256(Key);
        byte[] mac = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{Magic}|{installId}|{runs}"));
        return Convert.ToHexString(mac);
    }

    internal static string Serialize(string installId, int runs) =>
        $"{Magic}\ninstall={installId}\nruns={runs.ToString(CultureInfo.InvariantCulture)}\nmac={Sign(installId, runs)}\n";

    // ── Reading one copy ─────────────────────────────────────────────────────

    internal enum CopyKind { Missing, Invalid, Valid }

    internal readonly record struct Copy(CopyKind Kind, string InstallId, int Runs, string Why);

    internal static Copy ReadCopy(string path)
    {
        string text;
        try
        {
            if (!File.Exists(path)) return new(CopyKind.Missing, "", 0, "missing");
            text = File.ReadAllText(path);
        }
        catch (Exception e)
        {
            // Present but unreadable is not missing: treating it as missing would let a player reset
            // the counter by locking the file.
            return new(CopyKind.Invalid, "", 0, "unreadable: " + e.GetType().Name);
        }
        return Parse(text);
    }

    internal static Copy Parse(string text)
    {
        var lines = text.Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length != 4 || lines[0] != Magic) return new(CopyKind.Invalid, "", 0, "malformed");

        string? install = Field(lines[1], "install");
        string? runsText = Field(lines[2], "runs");
        string? mac = Field(lines[3], "mac");
        if (install is null || runsText is null || mac is null) return new(CopyKind.Invalid, "", 0, "malformed");
        if (install.Length != 32 || !IsHex(install)) return new(CopyKind.Invalid, "", 0, "bad installation id");
        if (!int.TryParse(runsText, NumberStyles.None, CultureInfo.InvariantCulture, out int runs))
            return new(CopyKind.Invalid, "", 0, "bad count");

        byte[] expected = Encoding.ASCII.GetBytes(Sign(install, runs));
        byte[] actual   = Encoding.ASCII.GetBytes(mac.ToUpperInvariant());
        if (!CryptographicOperations.FixedTimeEquals(expected, actual))
            return new(CopyKind.Invalid, "", 0, "signature mismatch");

        return new(CopyKind.Valid, install, runs, "");
    }

    private static string? Field(string line, string name) =>
        line.StartsWith(name + "=", StringComparison.Ordinal) ? line[(name.Length + 1)..] : null;

    private static bool IsHex(string s)
    {
        foreach (char c in s) if (!Uri.IsHexDigit(c)) return false;
        return true;
    }

    // ── Reconciling two copies ───────────────────────────────────────────────

    /// <summary>
    /// The rules in <see cref="DemoLimit"/>'s comment, as a pure function. <paramref name="locked"/>
    /// is set when the pair cannot be trusted; otherwise the result is the installation and its count,
    /// with an empty id meaning a fresh installation.
    /// </summary>
    internal static (string InstallId, int Runs, bool Locked, string Why) Reconcile(Copy a, Copy b)
    {
        if (a.Kind == CopyKind.Invalid) return ("", 0, true, "primary copy " + a.Why);
        if (b.Kind == CopyKind.Invalid) return ("", 0, true, "secondary copy " + b.Why);

        if (a.Kind == CopyKind.Missing && b.Kind == CopyKind.Missing) return ("", 0, false, "fresh");
        if (a.Kind == CopyKind.Missing) return (b.InstallId, b.Runs, false, "primary missing");
        if (b.Kind == CopyKind.Missing) return (a.InstallId, a.Runs, false, "secondary missing");

        if (!string.Equals(a.InstallId, b.InstallId, StringComparison.OrdinalIgnoreCase))
            return ("", 0, true, "copies belong to different installations");

        return (a.InstallId, Math.Max(a.Runs, b.Runs), false, a.Runs == b.Runs ? "" : "copies disagreed");
    }

    // ── The two operations ───────────────────────────────────────────────────

    public DemoStatus Load(int maxRuns)
    {
        var a = ReadCopy(PrimaryPath);
        var b = ReadCopy(SecondaryPath);
        var (id, runs, locked, why) = Reconcile(a, b);
        if (locked) return new(DemoState.Locked, 0, maxRuns, why);

        // Put back whichever copy was missing or behind, so the two never drift further apart than
        // one interrupted write. Best effort: a failure here changes nothing about what is allowed.
        if (id.Length > 0 && (a.Kind != CopyKind.Valid || b.Kind != CopyKind.Valid || a.Runs != b.Runs))
            WriteBoth(id, runs);

        return Describe(runs, maxRuns, why);
    }

    public bool TryConsume(int maxRuns, out DemoStatus status)
    {
        var (id, runs, locked, why) = Reconcile(ReadCopy(PrimaryPath), ReadCopy(SecondaryPath));
        if (locked) { status = new(DemoState.Locked, 0, maxRuns, why); return false; }
        if (runs >= maxRuns) { status = Describe(runs, maxRuns, why); return false; }

        // A fresh installation gets its id now, at its first run, not at its first look at the menu.
        // Not GameRng, on purpose: the id must differ between installations, and GameRng is the seed,
        // which every player shares for a given moon and which --seed pins outright.
        if (id.Length == 0) id = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

        int next = runs + 1;
        int written = WriteBoth(id, next);

        // One copy is enough: the next load takes the higher count from whichever survived. Neither
        // means the spend did not happen, so the run must not start either.
        if (written == 0)
        {
            status = new(DemoState.Locked, runs, maxRuns, "could not record the run");
            return false;
        }

        status = Describe(next, maxRuns, why);
        return true;
    }

    private static DemoStatus Describe(int runs, int maxRuns, string why) =>
        new(runs >= maxRuns ? DemoState.Exhausted : DemoState.Available, runs, maxRuns, why);

    /// <summary>Writes both copies atomically. Returns how many landed.</summary>
    private int WriteBoth(string installId, int runs)
    {
        string text = Serialize(installId, runs);
        int ok = 0;
        foreach (var path in new[] { PrimaryPath, SecondaryPath })
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                string tmp = path + ".tmp";
                File.WriteAllText(tmp, text);
                File.Move(tmp, path, overwrite: true);
                ok++;
            }
            catch (Exception e)
            {
                Console.WriteLine($"DemoLimit: could not write {path}: {e.Message}");
            }
        }
        return ok;
    }
}
