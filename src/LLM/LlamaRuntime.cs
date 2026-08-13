using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Cathedral.LLM;

/// <summary>Which compute device the llama.cpp server should use.</summary>
public enum LlamaComputeDevice
{
    /// <summary>Decided by the first-run probe: a GPU backend if one measured faster, else CPU.</summary>
    Auto,
    /// <summary>Force the discovered GPU backend, even if the probe preferred the CPU.</summary>
    Gpu,
    /// <summary>Force CPU inference. Also where the fallback ladder lands after a GPU failure.</summary>
    Cpu
}

/// <summary>One GPU backend found under <c>models/llama/backends/</c>.</summary>
/// <param name="Name">The folder name — <c>vulkan</c>, <c>cuda</c>. Shown in the Settings screen.</param>
/// <param name="DllPath">The backend library itself, handed to ggml through <c>GGML_BACKEND_PATH</c>.</param>
/// <param name="Directory">Its folder, prepended to <c>PATH</c> so co-located dependencies resolve.</param>
public sealed record LlamaBackend(string Name, string DllPath, string Directory);

/// <summary>
/// Everything about <i>where</i> the llama.cpp runtime lives and <i>what hardware</i> it can use.
/// The server manager owns the conversation with the server; this owns the files underneath it.
///
/// <para><b>The model has no name.</b> It is always <see cref="ModelFileName"/>. There is no setting,
/// no alias table and no configurable path — swapping models means replacing that file. A GGUF
/// carries its own architecture, tokenizer and chat template, so the file name is inert to
/// llama.cpp; <see cref="GgufMetadata"/> recovers the real identity for display.</para>
///
/// <para><b>Backends are resolved at runtime, not at build time.</b> ggml scans the server's own
/// directory on startup and loads whichever <c>ggml-*.dll</c> initialise — that is how the fifteen
/// <c>ggml-cpu-*.dll</c> variants beside the executable pick themselves by host ISA, with no help
/// from us. A GPU backend joins the same mechanism through <c>GGML_BACKEND_PATH</c>.</para>
///
/// <para><b>That variable names one file, not a directory.</b> Pointing it at a folder is answered
/// with <c>load_backend: failed to load</c>. This suits us: exactly one GPU backend should ever be
/// live, so the launcher names the DLL it chose and nothing else is loaded. It is also what lets
/// each candidate be verified in isolation — a backend built against a different llama.cpp revision
/// crashes on load, and in a probe subprocess that costs nothing.</para>
/// </summary>
public static class LlamaRuntime
{
    /// <summary>
    /// The one model file name the game will ever load. Deliberately generic: this constant is the
    /// entire interface for changing models.
    /// </summary>
    public const string ModelFileName = "model.gguf";

    public const string ServerExecutableName = "llama-server.exe";
    public const string BenchExecutableName  = "llama-bench.exe";
    private const string LlamaFolderName     = "llama";
    private const string BackendsFolderName  = "backends";
    private const string BuildFileName       = "BUILD.txt";

    /// <summary>
    /// Backend libraries that are not GPU backends and must never be offered as one.
    /// The CPU variants already load themselves from beside the executable, and loading a second
    /// copy through <c>GGML_BACKEND_PATH</c> would register a duplicate device.
    /// </summary>
    private static readonly string[] NonBackendPrefixes = { "ggml-cpu", "ggml-base", "ggml-rpc" };

    /// <summary><c>models/llama</c>, or null when the models directory is missing.</summary>
    public static string? LlamaDirectory => ModelsDirectory.PathTo(LlamaFolderName);

    /// <summary>The server executable. Null when the models directory is missing.</summary>
    public static string? ServerPath => Combine(LlamaDirectory, ServerExecutableName);

    /// <summary>The benchmark executable used by the first-run probe.</summary>
    public static string? BenchPath => Combine(LlamaDirectory, BenchExecutableName);

    /// <summary>The model file. Null when the models directory is missing.</summary>
    public static string? ModelPath => ModelsDirectory.PathTo(ModelFileName);

    private static string? Combine(string? dir, string file)
        => dir == null ? null : Path.Combine(dir, file);

    /// <summary>
    /// The llama.cpp build this runtime folder was taken from (e.g. <c>b8851</c>), read from
    /// <c>BUILD.txt</c>. Returns null when the file is absent or unparseable — it is documentation
    /// for the player and a diagnostic in log messages, never a gate.
    /// </summary>
    public static string? BuildId
    {
        get
        {
            var path = Combine(LlamaDirectory, BuildFileName);
            if (path == null || !File.Exists(path)) return null;
            try
            {
                foreach (var line in File.ReadLines(path).Take(5))
                {
                    var m = Regex.Match(line, @"\bb\d{3,6}\b");
                    if (m.Success) return m.Value;
                }
            }
            catch { /* documentation only — never fatal */ }
            return null;
        }
    }

    /// <summary>
    /// GPU backends present under <c>backends/</c>, one per subfolder holding a loadable-looking
    /// <c>ggml-*.dll</c>. Presence here means "worth trying", not "works" — only
    /// <see cref="LlamaProbe"/> establishes that, by loading it in a subprocess.
    /// </summary>
    public static IReadOnlyList<LlamaBackend> DiscoverBackends()
    {
        var llamaDir = LlamaDirectory;
        if (llamaDir == null) return Array.Empty<LlamaBackend>();

        var backendsRoot = Path.Combine(llamaDir, BackendsFolderName);
        if (!Directory.Exists(backendsRoot)) return Array.Empty<LlamaBackend>();

        var found = new List<LlamaBackend>();
        foreach (var dir in Directory.EnumerateDirectories(backendsRoot))
        {
            string? dll;
            try
            {
                dll = Directory.EnumerateFiles(dir, "ggml-*.dll")
                               .FirstOrDefault(f => !NonBackendPrefixes.Any(p =>
                                   Path.GetFileName(f).StartsWith(p, StringComparison.OrdinalIgnoreCase)));
            }
            catch { continue; }

            if (dll != null)
                found.Add(new LlamaBackend(Path.GetFileName(dir), dll, dir));
        }

        // Stable order so a tie between two installed backends does not depend on the file system.
        return found.OrderBy(b => b.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// Points a child process at one GPU backend, or at none. Call before starting
    /// <c>llama-server</c> or <c>llama-bench</c>; requires <c>UseShellExecute = false</c>.
    /// <para>
    /// Two variables, for two different loaders. <c>GGML_BACKEND_PATH</c> is read by ggml and names
    /// the backend to load. <c>PATH</c> is read by Windows when that DLL pulls in its own
    /// dependencies — CUDA's <c>cudart64_12.dll</c> and <c>cublas64_12.dll</c> sit beside it, and
    /// the directory of a dynamically loaded library is not otherwise searched, so a CUDA backend
    /// would fail to load with nothing to say why.
    /// </para>
    /// </summary>
    public static void ApplyBackend(ProcessStartInfo startInfo, LlamaBackend? backend)
    {
        if (backend == null)
        {
            // Inherited from the game's own environment, this would silently re-enable a backend
            // the player or the fallback ladder just turned off.
            startInfo.Environment.Remove("GGML_BACKEND_PATH");
            return;
        }

        startInfo.Environment["GGML_BACKEND_PATH"] = backend.DllPath;

        var existingPath = startInfo.Environment.TryGetValue("PATH", out var p) && !string.IsNullOrEmpty(p)
            ? p
            : Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        startInfo.Environment["PATH"] = string.IsNullOrEmpty(existingPath)
            ? backend.Directory
            : $"{backend.Directory}{Path.PathSeparator}{existingPath}";
    }

    /// <summary>
    /// Identifies the current model file by length and write time, cheaply and without hashing two
    /// gigabytes. Recorded with a probe result so that replacing <c>model.gguf</c> — the documented
    /// way to change models — invalidates hardware settings that were measured against the old one.
    /// Null when the model is missing.
    /// </summary>
    public static string? ModelSignature()
    {
        var path = ModelPath;
        if (path == null || !File.Exists(path)) return null;
        try
        {
            var info = new FileInfo(path);
            return $"{info.Length}:{info.LastWriteTimeUtc.Ticks}";
        }
        catch { return null; }
    }

    private static string? _modelDisplayName;

    /// <summary>
    /// What the model calls itself, from its GGUF header — "qwen2.5-3b-instruct" rather than
    /// "model.gguf". Written to the startup log, and deliberately <b>not</b> shown in the Settings
    /// screen: with a generic file name this is the only way to tell which model an install is
    /// actually running, which is a diagnostic need rather than something to put in front of a
    /// player.
    /// <para>Cached, since it costs a file read and cannot change while the game is running.</para>
    /// </summary>
    public static string ModelDisplayName
        => _modelDisplayName ??= GgufMetadata.Read(ModelPath)?.DisplayName ?? ModelFileName;

    /// <summary>
    /// A one-line description of the hardware situation, for the log and the Settings screen:
    /// the build, and which backends were found.
    /// </summary>
    public static string DescribeInstallation()
    {
        var build = BuildId ?? "unknown build";
        var backends = DiscoverBackends();
        // "installed", not the device in use. Without that word the startup line reads
        // "Using model: … ; vulkan" and looks like a statement about this run — which it is not:
        // a run started with --cpu prints the same thing, because this describes what is on disk.
        // Which device the run actually uses is the separate "Starting llama server on …" line.
        var backendText = backends.Count == 0
            ? "no GPU backend installed (CPU only)"
            : "backends installed: " + string.Join(", ", backends.Select(b => b.Name));
        return $"llama.cpp {build}; {backendText}";
    }
}
