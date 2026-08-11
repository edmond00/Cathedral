using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Cathedral.LLM;

/// <summary>What the probe decided, and enough of its reasoning to print.</summary>
/// <param name="Device">The device the game should launch with.</param>
/// <param name="BackendName">The backend folder chosen, or null for CPU.</param>
/// <param name="Summary">One line for the log and the Settings screen.</param>
public sealed record LlamaProbeResult(LlamaComputeDevice Device, string? BackendName, string Summary);

/// <summary>
/// Decides once, on first run, whether to serve the model on the GPU or the CPU.
///
/// <para><b>It measures rather than identifies.</b> Reading the hardware — a PCI id, a vendor
/// string, a driver version — answers "is there a GPU in this machine", which is not the question.
/// The question is "will llama.cpp go faster on it today", and the cases that get that wrong are
/// ordinary: hybrid laptop graphics, a driver too old for the backend, an integrated GPU slower
/// than the CPU beside it, a remote session with no device at all. Loading the backend and timing
/// it answers all of them the same way, and it is the only answer that cannot be wrong about the
/// machine it is running on.</para>
///
/// <para><b>Nothing runs in this process.</b> Every step is a subprocess, so a backend that
/// crashes on load — the documented consequence of mixing llama.cpp build numbers — takes down a
/// probe that was expected to fail sometimes, instead of the game.</para>
///
/// <para><b>The common case is free.</b> With no GPU backend installed there is nothing to choose
/// between, and the probe settles on the CPU without loading the model or benchmarking anything.
/// A stock install pays a few milliseconds. Only a machine that actually has a backend pays for
/// the measurement, once, and the result is cached against the model file.</para>
///
/// <para>What it deliberately does <b>not</b> decide is how many layers to offload. llama.cpp sizes
/// that itself: <c>--fit</c> defaults to on and adjusts unset arguments to fit device memory. The
/// game's job is to leave <c>-ngl</c> unset and let it.</para>
/// </summary>
public static class LlamaProbe
{
    /// <summary>
    /// Cleared by <c>--no-llm-probe</c>. The probe is also skipped in playground mode, which never
    /// starts a server at all.
    /// </summary>
    public static bool Enabled { get; set; } = true;

    /// <summary>Long enough for a cold driver to initialise; short enough that a hung one is not fatal.</summary>
    private const int DeviceListTimeoutMs = 20_000;

    /// <summary>
    /// A bench loads the full model, so this covers a cold read of two gigabytes from a slow disk
    /// as well as the run itself.
    /// </summary>
    private const int BenchTimeoutMs = 120_000;

    // A deliberately small workload: enough tokens to be measurable, few enough to stay quick.
    private const int BenchPromptTokens = 32;
    private const int BenchGenTokens    = 16;

    /// <summary>
    /// Runs the probe if the persisted result is missing or was measured against a different model,
    /// and writes the outcome to <see cref="UserSettings"/>. Returns null when nothing was done.
    /// <para>Blocking and slow by nature — call it from the server startup path, which is already
    /// asynchronous and already behind a loading screen.</para>
    /// </summary>
    public static LlamaProbeResult? EnsureProbed()
    {
        if (!Enabled || Cathedral.Game.PlaygroundMode.IsActive) return null;

        var signature = LlamaRuntime.ModelSignature();
        if (signature == null) return null;   // no model; the server manager reports that properly

        if (UserSettings.LlmProbeSignature == signature) return null;

        Console.WriteLine(UserSettings.LlmProbeSignature.Length == 0
            ? "First run: choosing a compute device for the language model..."
            : "The model file changed: re-checking the compute device...");

        var result = Run();

        UserSettings.LlmProbedDevice   = result.Device;
        UserSettings.LlmProbedBackend  = result.BackendName;
        UserSettings.LlmProbeSignature = signature;
        UserSettings.LlmProbeSummary   = result.Summary;
        UserSettings.Save();

        Console.WriteLine($"Compute device: {result.Summary}");
        return result;
    }

    /// <summary>
    /// Measures without touching settings. Also what the Settings screen's re-detect button calls.
    /// Always returns a usable answer — the CPU always works.
    /// </summary>
    public static LlamaProbeResult Run()
    {
        var backends = LlamaRuntime.DiscoverBackends();
        if (backends.Count == 0)
            return new LlamaProbeResult(LlamaComputeDevice.Cpu, null, "CPU (no GPU backend installed)");

        // Which of the installed backends actually load and report a device.
        var usable = new List<(LlamaBackend Backend, string Device)>();
        foreach (var backend in backends)
        {
            var device = FirstDeviceOf(backend);
            if (device != null) usable.Add((backend, device));
            else Console.WriteLine($"  {backend.Name}: no usable device (skipped)");
        }

        if (usable.Count == 0)
            return new LlamaProbeResult(LlamaComputeDevice.Cpu, null, "CPU (no GPU backend loaded successfully)");

        double cpuRate = Benchmark(null);
        var best = usable
            .Select(u => (u.Backend, u.Device, Rate: Benchmark(u.Backend)))
            .OrderByDescending(x => x.Rate)
            .First();

        if (best.Rate <= 0)
            return new LlamaProbeResult(LlamaComputeDevice.Cpu, null, "CPU (GPU benchmark failed)");

        // A GPU that cannot beat the CPU is a slower path with more ways to fail. Integrated
        // graphics sharing system memory with the CPU land here regularly, which is the whole
        // reason this is measured rather than assumed.
        if (cpuRate > 0 && best.Rate < cpuRate)
            return new LlamaProbeResult(LlamaComputeDevice.Cpu, null,
                $"CPU ({Rate(cpuRate)} tok/s, faster than {best.Backend.Name} at {Rate(best.Rate)})");

        var comparison = cpuRate > 0 ? $", CPU {Rate(cpuRate)}" : "";
        return new LlamaProbeResult(LlamaComputeDevice.Gpu, best.Backend.Name,
            $"{best.Backend.Name} — {best.Device} ({Rate(best.Rate)} tok/s{comparison})");
    }

    /// <summary>
    /// Formats a token rate for the summary line. Invariant, so a French or German locale does not
    /// write "123,2" into a string that is persisted to JSON and read back by whoever is diagnosing
    /// a machine — the same reason the server's own decimal arguments are formatted invariantly.
    /// </summary>
    private static string Rate(double tokensPerSecond)
        => tokensPerSecond.ToString("F1", CultureInfo.InvariantCulture);

    /// <summary>
    /// The first device a backend reports, or null if it loaded nothing usable. Cheap: this asks
    /// the server to enumerate devices and exit, with no model involved.
    /// </summary>
    private static string? FirstDeviceOf(LlamaBackend backend)
    {
        var server = LlamaRuntime.ServerPath;
        if (server == null) return null;

        var output = RunCaptured(server, "--list-devices", backend, DeviceListTimeoutMs);
        if (output == null) return null;

        // The output that matters is a short block at the end:
        //
        //     Available devices:
        //       Vulkan0: NVIDIA GeForce RTX 3060 (12288 MiB, 11800 MiB free)
        //
        // Only lines after that header are read. The rest of the stream is llama.cpp's loading
        // trace, and much of it is "prefix: text" shaped — "ggml_vulkan: Found 1 Vulkan devices:"
        // would otherwise be mistaken for a device named "Found 1 Vulkan devices:".
        bool inDeviceList = false;
        foreach (var raw in output.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;

            if (line.StartsWith("Available devices", StringComparison.OrdinalIgnoreCase))
            {
                inDeviceList = true;
                continue;
            }
            if (!inDeviceList) continue;

            if (line.Equals("(none)", StringComparison.OrdinalIgnoreCase)) return null;

            int colon = line.IndexOf(':');
            if (colon <= 0) continue;

            var description = line[(colon + 1)..].Trim();
            if (description.Length == 0) continue;

            // Trim the trailing memory report — "NVIDIA GeForce RTX 3060 (12288 MiB, …)".
            int paren = description.IndexOf('(');
            return (paren > 0 ? description[..paren] : description).Trim();
        }
        return null;
    }

    /// <summary>
    /// Generation rate in tokens/second for one backend, or 0 if the run failed. A null backend
    /// measures the CPU.
    /// <para>Generation is timed rather than prompt processing: narration is streamed into the
    /// preview box a token at a time, so tokens/second is what a player actually waits on.</para>
    /// </summary>
    private static double Benchmark(LlamaBackend? backend)
    {
        var bench = LlamaRuntime.BenchPath;
        var model = LlamaRuntime.ModelPath;
        if (bench == null || model == null) return 0;

        // -ngl 0 keeps the CPU measurement on the CPU even when a backend is loaded.
        var layers = backend == null ? " -ngl 0" : "";
        var args = $"-m \"{model}\" -p {BenchPromptTokens} -n {BenchGenTokens} -r 1 -o json{layers}";

        var output = RunCaptured(bench, args, backend, BenchTimeoutMs);
        if (output == null) return 0;

        try
        {
            int start = output.IndexOf('[');
            int end = output.LastIndexOf(']');
            if (start < 0 || end <= start) return 0;

            using var doc = JsonDocument.Parse(output[start..(end + 1)]);
            double best = 0;
            foreach (var entry in doc.RootElement.EnumerateArray())
            {
                // Skip the prompt-processing row; n_gen > 0 identifies the generation row.
                if (entry.TryGetProperty("n_gen", out var gen) && ReadNumber(gen) <= 0) continue;
                if (entry.TryGetProperty("avg_ts", out var ts))
                    best = Math.Max(best, ReadNumber(ts));
            }
            return best;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>llama-bench writes numbers as JSON numbers in some versions and strings in others.</summary>
    private static double ReadNumber(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Number => element.GetDouble(),
        JsonValueKind.String => double.TryParse(element.GetString(), NumberStyles.Float,
                                                CultureInfo.InvariantCulture, out var v) ? v : 0,
        _ => 0
    };

    /// <summary>
    /// Runs a llama.cpp tool to completion and returns stdout+stderr, or null if it failed, timed
    /// out or crashed. Both streams are wanted: <c>--list-devices</c> prints its list on stdout and
    /// its backend-loading trace on stderr, and a crashing backend says so on stderr only.
    /// </summary>
    private static string? RunCaptured(string executable, string arguments, LlamaBackend? backend, int timeoutMs)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        LlamaRuntime.ApplyBackend(startInfo, backend);

        try
        {
            using var process = Process.Start(startInfo);
            if (process == null) return null;

            var captured = new StringBuilder();
            // Read both streams asynchronously: a process that fills a redirected pipe blocks
            // forever if nobody drains it, and llama.cpp is verbose on stderr.
            process.OutputDataReceived += (_, e) => { if (e.Data != null) lock (captured) captured.AppendLine(e.Data); };
            process.ErrorDataReceived  += (_, e) => { if (e.Data != null) lock (captured) captured.AppendLine(e.Data); };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (!process.WaitForExit(timeoutMs))
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                return null;
            }

            // Drains whatever the async handlers had not delivered when the process exited.
            process.WaitForExit();

            if (process.ExitCode != 0) return null;
            lock (captured) return captured.ToString();
        }
        catch
        {
            return null;
        }
    }
}
