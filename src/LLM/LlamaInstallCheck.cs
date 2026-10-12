using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Cathedral.LLM;

/// <summary>
/// Does the language-model install on this machine match the one this build was made against?
///
/// <para><c>models/</c> is not in git — the llama.cpp binaries and the model file are copied between
/// machines by hand — so nothing stops a machine running an older llama.cpp, a backend pack from a
/// different build, or another <c>model.gguf</c>. None of those fails cleanly: a server too old for
/// the model or for one of our flags never comes up and the game carries on without its narrator; a
/// backend from another build crashes inside its DLL and the game silently drops to the CPU, and
/// saves that choice; a different model simply writes differently. This turns each of them into a
/// line the player can read, on the loading screen and the main menu, and into the log.</para>
///
/// <para><b>Checked, never selected.</b> The expected model below does not choose anything: the model
/// is still whatever <c>models/model.gguf</c> is (see <see cref="LlamaRuntime"/>). A mismatch is
/// reported and the game runs anyway, because a deliberate experiment with another model is a
/// legitimate thing to do.</para>
///
/// <para><b>When llama.cpp or the model is updated, update the four constants here</b> in the same
/// commit — the <c>models</c> skill carries the procedure.</para>
/// </summary>
public static class LlamaInstallCheck
{
    // ── What this build was made against ─────────────────────────────────────

    /// <summary>The llama.cpp build number, as <c>llama-server --version</c> reports it.</summary>
    public const int ExpectedBuild = 11515;

    /// <summary>The short commit of that build, from the same line.</summary>
    public const string ExpectedCommit = "3d65c90d0";

    /// <summary>The model's <c>general.name</c>, from its GGUF header.</summary>
    public const string ExpectedModelName = "qwen2.5-3b-instruct";

    /// <summary>
    /// The model file's exact size. The name alone cannot tell two quantisations of one model apart,
    /// nor a truncated copy from a whole one; hashing 2 GB at every launch would cost more than the
    /// check is worth, and the size catches both.
    /// </summary>
    public const long ExpectedModelBytes = 2_104_932_768;

    /// <summary>
    /// The tracked file in each backend pack: its build, and the SHA-256 of its DLL as
    /// <c>&lt;dll name&gt; sha256: &lt;hex&gt;</c>.
    /// </summary>
    public const string BackendBuildFileName = "BUILD.txt";

    private static IReadOnlyList<string>? _problems;
    private static readonly object Gate = new();

    /// <summary>
    /// Every mismatch found, one short line each, or empty when the install matches. Empty until
    /// <see cref="Run"/> has been called — which only happens when the server is actually started, so
    /// a <c>--playground</c> run neither pays for the check nor shows its result.
    /// </summary>
    public static IReadOnlyList<string> Problems => _problems ?? Array.Empty<string>();

    /// <summary>Runs the check once and caches the result. Safe to call from any thread.</summary>
    public static IReadOnlyList<string> Run()
    {
        lock (Gate)
        {
            if (_problems != null) return _problems;
            _problems = Collect();

            // Logged here, where it is computed, rather than by a caller: the server start is not a
            // reliable place, since it is skipped outright when a server is already answering.
            if (_problems.Count == 0)
                Console.WriteLine($"Install check: llama.cpp b{ExpectedBuild} and {ExpectedModelName} as expected.");
            foreach (var problem in _problems)
                Console.WriteLine($"WARNING: Install check: {problem}");
            return _problems;
        }
    }

    private static List<string> Collect()
    {
        var problems = new List<string>();
        try
        {
            CheckServer(problems);
            CheckBackends(problems);
            CheckModel(problems);
        }
        catch (Exception ex)
        {
            // A check that throws must not take the launch down with it — it would be the only
            // fatal thing about an install the game can otherwise run on.
            problems.Add($"The install check itself failed: {ex.Message}");
        }
        return problems;
    }

    // ── llama.cpp ────────────────────────────────────────────────────────────

    private static readonly Regex VersionLine = new(@"build\s+(\d+),\s*commit\s+([0-9a-f]+)", RegexOptions.IgnoreCase);

    /// <summary>
    /// Asks the server itself. BUILD.txt is what someone wrote down; this is what was compiled, and
    /// it also catches a BUILD.txt left behind by an upgrade that only half happened.
    /// </summary>
    private static void CheckServer(List<string> problems)
    {
        var server = LlamaRuntime.ServerPath;
        if (server == null || !File.Exists(server)) return;   // fatal elsewhere, with its own message

        string? output = RunForOutput(server, "--version", TimeSpan.FromSeconds(10));
        var m = output == null ? null : VersionLine.Match(output);
        if (m == null || !m.Success)
        {
            problems.Add($"llama.cpp did not report its version (expected build {ExpectedBuild}).");
            return;
        }

        int build = int.Parse(m.Groups[1].Value);
        string commit = m.Groups[2].Value;
        if (build != ExpectedBuild || !commit.StartsWith(ExpectedCommit, StringComparison.OrdinalIgnoreCase))
            problems.Add($"llama.cpp is build {build} ({commit}); this game expects build {ExpectedBuild} ({ExpectedCommit}).");

        // The paper record disagreeing with the binary is its own fault: the next person to read it
        // will be told the wrong thing.
        var written = LlamaRuntime.BuildId;
        if (written != null && written != $"b{build}")
            problems.Add($"models/llama/BUILD.txt says {written}, but the llama.cpp beside it is build {build}.");
    }

    /// <summary>
    /// A backend pack cannot be asked its build — the DLLs carry no version — and a pack from another
    /// build crashes inside the backend instead of failing. So each pack has a <b>tracked</b> BUILD.txt
    /// recording its build and the SHA-256 of its DLL, and the DLL on disk is hashed against it.
    ///
    /// <para>The hash is what makes tracking the file safe. A bare build number travelling through git
    /// would arrive on every machine whatever DLL sat beside it, and the check would pass exactly where
    /// it should fail; a hash can only be matched by the right DLL. Hashing ~60 MB costs a fraction of a
    /// second, once per launch.</para>
    /// </summary>
    private static void CheckBackends(List<string> problems)
    {
        foreach (var backend in LlamaRuntime.DiscoverBackends())
        {
            var path = Path.Combine(backend.Directory, BackendBuildFileName);
            if (!File.Exists(path))
            {
                problems.Add($"The {backend.Name} backend has no {BackendBuildFileName}, so its build cannot be checked.");
                continue;
            }

            var lines = File.ReadLines(path).Take(10).ToList();

            string? build = lines
                .Select(l => Regex.Match(l, @"\bb(\d{3,6})\b"))
                .FirstOrDefault(x => x.Success)?.Groups[1].Value;
            if (build != ExpectedBuild.ToString())
            {
                problems.Add($"The {backend.Name} backend is recorded as build {build ?? "unknown"}; this game expects build {ExpectedBuild}.");
                continue;
            }

            // "<dll name> sha256: <hex>" — keyed by the file name, so the record says which DLL it vouches for.
            string dllName = Path.GetFileName(backend.DllPath);
            string? recorded = lines
                .Select(l => Regex.Match(l, $@"^\s*{Regex.Escape(dllName)}\s+sha256:\s*([0-9a-fA-F]{{64}})", RegexOptions.IgnoreCase))
                .FirstOrDefault(x => x.Success)?.Groups[1].Value;
            if (recorded == null)
            {
                problems.Add($"The {backend.Name} backend's {BackendBuildFileName} records no hash for {dllName}.");
                continue;
            }

            string actual = Sha256(backend.DllPath);
            if (!string.Equals(actual, recorded, StringComparison.OrdinalIgnoreCase))
                problems.Add($"The {backend.Name} backend's {dllName} is not the build {ExpectedBuild} one (its hash differs).");
        }
    }

    private static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(stream));
    }

    // ── The model ────────────────────────────────────────────────────────────

    private static void CheckModel(List<string> problems)
    {
        var path = LlamaRuntime.ModelPath;
        if (path == null || !File.Exists(path)) return;       // fatal elsewhere, with its own message

        string name = GgufMetadata.Read(path)?.Name ?? "an unnamed model";
        if (!string.Equals(name, ExpectedModelName, StringComparison.OrdinalIgnoreCase))
        {
            problems.Add($"models/model.gguf is {name}; this game expects {ExpectedModelName}.");
            return;   // a different model is obviously a different size — one line says it all
        }

        long bytes = new FileInfo(path).Length;
        if (bytes != ExpectedModelBytes)
            problems.Add($"models/model.gguf is {ExpectedModelName} but {bytes:N0} bytes, not {ExpectedModelBytes:N0} " +
                         "— another quantisation, or an incomplete copy.");
    }

    // ── Plumbing ─────────────────────────────────────────────────────────────

    /// <summary>Runs an executable and returns stdout and stderr together, or null if it did not finish.</summary>
    private static string? RunForOutput(string exe, string args, TimeSpan timeout)
    {
        try
        {
            using var p = new Process
            {
                StartInfo = new ProcessStartInfo(exe, args)
                {
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    WorkingDirectory       = Path.GetDirectoryName(exe) ?? "",
                },
            };
            p.Start();
            // Both streams read asynchronously: llama.cpp prints its version on stderr, and reading
            // one stream to the end while the other fills its pipe is the classic deadlock.
            var stdout = p.StandardOutput.ReadToEndAsync();
            var stderr = p.StandardError.ReadToEndAsync();
            if (!p.WaitForExit((int)timeout.TotalMilliseconds))
            {
                try { p.Kill(entireProcessTree: true); } catch { }
                return null;
            }
            return stdout.Result + "\n" + stderr.Result;
        }
        catch
        {
            return null;
        }
    }
}
