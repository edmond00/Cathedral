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

    /// <summary>The file a backend pack carries to say which build it came from.</summary>
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
            return _problems ??= Collect();
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
    /// build crashes inside the backend instead of failing. So each pack carries a BUILD.txt, written
    /// when it is installed, and that is what is compared.
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

            string? found = File.ReadLines(path).Take(5)
                .Select(l => Regex.Match(l, @"\bb(\d{3,6})\b"))
                .FirstOrDefault(x => x.Success)?.Groups[1].Value;
            if (found != ExpectedBuild.ToString())
                problems.Add($"The {backend.Name} backend is build {found ?? "unknown"}; this game expects build {ExpectedBuild}.");
        }
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
