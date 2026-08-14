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
/// <para><b>It measures both halves of inference.</b> Prompt processing and token generation are
/// different workloads on different code paths, and a device can be good at one and hopeless at the
/// other. A Qualcomm Adreno X1 shipped exactly that split: 6.6 tok/s generated against the CPU's
/// 4.3 — a clear win — while ingesting a prompt at <b>1.9 tok/s</b>, some thirty times slower than
/// the same machine's CPU. Scoring on generation alone picked Vulkan, and the game then sat on its
/// loading bar for minutes because its prompts are hundreds of tokens long and its replies are
/// often a dozen. Both rates are measured, and the two are combined into the only figure that
/// decides anything: how long one representative request takes end to end.</para>
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
    /// <para>Sized so the pathological case is <b>measured</b> rather than timed out. A Qualcomm
    /// Adreno X1-45 reads a prompt at 1.1 tok/s, so <see cref="BenchPromptTokens"/> costs it about a
    /// minute, on top of a model load that is itself slow under x64 emulation. A tighter limit would
    /// score that device zero and report "benchmark failed" — the right device reached by the wrong
    /// route, with the one number that explains the choice thrown away. A device slower even than
    /// this still times out and still loses, harmlessly.</para>
    /// <para>Paid once per backend, on first run, behind the loading screen, and cached against the
    /// model file thereafter.</para>
    /// </summary>
    private const int BenchTimeoutMs = 180_000;

    // A deliberately small workload: enough tokens to be measurable, few enough to stay quick.
    //
    // The prompt sample is the larger of the two because a handful of tokens measures kernel launch
    // overhead rather than throughput, which would understate a fast GPU on the very axis this probe
    // was rewritten to take seriously. It stops at 64 because the cost of raising it is paid by the
    // slowest device on the machine: an Adreno X1-45 needs about a minute for 64 tokens and two for
    // 128, and that minute is spent on first launch behind the loading screen.
    //
    // Both ends of that trade are real and they pull opposite ways, so neither is a safety argument:
    // a short sample understates a fast GPU (launch overhead is a bigger fraction), and it also
    // understates a bad one, because the degradation is not linear — the same Adreno measures
    // 2.9 tok/s over 64 tokens and 1.1 over 128, and the game's prompts are longer than either.
    // What actually carries the decision is WorkloadPromptTokens weighting the prompt rate, not the
    // fidelity of the sample: on that Adreno the CPU wins by 3.6x measured this way and by 8.3x
    // measured at 128, and the answer is the CPU either way.
    //
    // Generation is sampled harder than prompt for the same reason, read the other way round: it is
    // the CHEAP axis. Every device here generates at 5-12 tok/s where the worst reads at 1, so
    // doubling the generated tokens costs about a second and doubling the prompt tokens costs a
    // minute. And the noise was real — two runs of the same CPU measured 7.3 and 5.4 tok/s, a 36%
    // swing at 16 tokens, wider than most margins this probe has to judge.
    private const int BenchPromptTokens = 64;
    private const int BenchGenTokens    = 32;

    /// <summary>
    /// The shape of a typical request, used to weigh the two measured rates against each other.
    /// <para>Cathedral is prompt-heavy and generation-light, and not by a little. Persona choices
    /// (<c>PersonaMatchCritic</c>, 4 tokens), item-use criticism (20–60) and the rewriter (64) all
    /// answer several hundred tokens of scene with a phrase; only narration itself reaches
    /// <c>Config.LLM.GenerationMaxTokens</c>. Observed prompts in a live run ran 220–495 tokens.
    /// These two numbers encode that: they are not a benchmark, they are the ratio that decides
    /// which measured rate matters more.</para>
    /// </summary>
    private const int WorkloadPromptTokens = 400;
    private const int WorkloadGenTokens    = 80;

    /// <summary>
    /// How much faster than the CPU a GPU must be before the game will move onto it.
    /// <para>The GPU path is strictly more ways to fail — a driver, a backend DLL, a memory
    /// allocator — so a dead heat is not worth taking. The Adreno that prompted this was accepted on
    /// a 1.5× margin measured on the wrong axis; requiring a real margin on the right one costs a
    /// genuinely fast card nothing, since those win by ten or twenty times.</para>
    /// </summary>
    private const double RequiredGpuSpeedup = 1.25;

    /// <summary>
    /// Runs the probe if the persisted result is missing or was measured against a different model,
    /// and writes the outcome to <see cref="UserSettings"/>. Returns null when nothing was done.
    /// <para>Blocking and slow by nature — call it from the server startup path, which is already
    /// asynchronous and already behind a loading screen.</para>
    /// </summary>
    /// <summary>
    /// Whether <see cref="EnsureProbed"/> would actually measure anything, answered without
    /// measuring. Cheap: a file length and a timestamp against the persisted signature.
    /// <para>The caller that matters is the server manager, which holds the probe back until the
    /// loading screen is up so the benchmark happens in front of a player who has been told what it
    /// is. Waiting costs nothing on the runs that <b>would</b> probe and would delay the server on
    /// every run that would not, so it has to be possible to ask first.</para>
    /// </summary>
    public static bool IsProbeNeeded()
    {
        if (!Enabled || Cathedral.Game.PlaygroundMode.IsActive) return false;
        if (Config.Debug.ForcedLlmDevice != null) return false;

        var signature = LlamaRuntime.ModelSignature();
        return signature != null && UserSettings.LlmProbeSignature != signature;
    }

    /// <param name="onStatus">
    /// Optional running commentary — one short line per stage, called from whatever thread the probe
    /// is on. The loading screen shows it, because a first run spends minutes here and a screen that
    /// only says "Loading language model" is describing something that has not started yet.
    /// </param>
    public static LlamaProbeResult? EnsureProbed(Action<string>? onStatus = null)
    {
        if (!Enabled || Cathedral.Game.PlaygroundMode.IsActive) return null;

        // --cpu / --gpu have already decided. Measuring would spend a minute — and, on the GPU
        // rung, a full benchmark — to produce an answer that BuildDeviceLadder then ignores,
        // because the forced device outranks both the setting and the probe.
        //
        // This is not merely wasteful. Every package re-stages model.gguf, which changes its
        // timestamp and so invalidates the probe signature; the publish smoke test therefore
        // re-probed on every release, running GPU inference on a machine whose whole reason for
        // passing --cpu was to avoid exactly that.
        if (Config.Debug.ForcedLlmDevice != null)
        {
            Console.WriteLine($"Compute device: {Config.Debug.ForcedLlmDevice} (forced on the command line; detection skipped).");
            return null;
        }

        var signature = LlamaRuntime.ModelSignature();
        if (signature == null) return null;   // no model; the server manager reports that properly

        if (UserSettings.LlmProbeSignature == signature) return null;

        bool firstRun = UserSettings.LlmProbeSignature.Length == 0;
        Console.WriteLine(firstRun
            ? "First run: choosing a compute device for the language model..."
            : "The model file changed: re-checking the compute device...");

        onStatus?.Invoke(firstRun
            ? "First run: checking what this machine runs the model fastest on"
            : "The model changed — re-checking what runs it fastest");

        var result = Run(onStatus);

        UserSettings.LlmProbedDevice   = result.Device;
        UserSettings.LlmProbedBackend  = result.BackendName;
        UserSettings.LlmProbeSignature = signature;
        UserSettings.LlmProbeSummary   = result.Summary;
        UserSettings.Save();

        Console.WriteLine($"Compute device: {result.Summary}");
        return result;
    }

    /// <summary>
    /// Re-measures and prints the whole comparison — both rates per device, the weighted cost of a
    /// representative request, and which device wins and by how much. Ignores the cached result and
    /// writes nothing back, so it can be run on a machine that has already decided.
    /// <para>This exists because the decision was previously invisible. A machine that picked the
    /// wrong device said so in one summary line that reported a single rate, and the symptom the
    /// player saw was a loading bar that never finished.</para>
    /// </summary>
    public static string BuildAuditReport()
    {
        var report = new StringBuilder();
        report.AppendLine("=== Compute Device Probe ===");
        report.AppendLine();

        if (LlamaRuntime.ModelPath == null)
        {
            report.AppendLine("No model found — nothing to measure.");
            return report.ToString();
        }

        report.AppendLine($"Model:    {LlamaRuntime.ModelPath}");
        report.AppendLine($"Bench:    -p {BenchPromptTokens} -n {BenchGenTokens}");
        report.AppendLine($"Workload: {WorkloadPromptTokens} prompt + {WorkloadGenTokens} generated tokens per request");
        report.AppendLine($"Rule:     a GPU must be {RequiredGpuSpeedup.ToString("0.##", CultureInfo.InvariantCulture)}x faster than the CPU to be chosen");
        report.AppendLine();
        report.AppendLine("Measuring — this loads the model once per device and takes a few minutes...");
        report.AppendLine();

        var m = Measure(verbose: false);
        var result = Decide(m);

        if (m.Settled != null)
        {
            report.AppendLine(m.Settled.Summary);
            return report.ToString();
        }

        report.AppendLine($"  {"device",-42} {"read",10} {"write",10} {"per request",14}");
        report.AppendLine($"  {new string('-', 42)} {new string('-', 10)} {new string('-', 10)} {new string('-', 14)}");
        AppendRow(report, "CPU", m.Cpu);
        foreach (var c in m.Candidates)
            AppendRow(report, $"{c.Backend.Name} — {c.Device}", c.Rates);

        report.AppendLine();
        report.AppendLine($"Chosen: {result.Summary}");

        // The line that would have caught the Adreno: naming the axis on which the loser lost.
        var fastestGen = m.Candidates.OrderByDescending(c => c.Rates.GenRate).FirstOrDefault();
        if (result.Device == LlamaComputeDevice.Cpu && m.Cpu.GenRate > 0
            && fastestGen.Rates.GenRate > m.Cpu.GenRate)
            report.AppendLine(
                $"Note:   {fastestGen.Backend.Name} generates faster than the CPU "
              + $"({Rate(fastestGen.Rates.GenRate)} vs {Rate(m.Cpu.GenRate)} tok/s) but reads prompts slower "
              + $"({Rate(fastestGen.Rates.PromptRate)} vs {Rate(m.Cpu.PromptRate)} tok/s). "
              + "Scoring generation alone would pick it, and the game would stall.");

        return report.ToString();
    }

    private static void AppendRow(StringBuilder report, string label, BenchRates rates)
    {
        double seconds = SecondsPerRequest(rates);
        string cost = double.IsPositiveInfinity(seconds) ? "failed" : $"{Rate(seconds)}s";
        string read  = rates.PromptRate > 0 ? Rate(rates.PromptRate) : "-";
        string write = rates.GenRate    > 0 ? Rate(rates.GenRate)    : "-";
        report.AppendLine($"  {Truncate(label, 42),-42} {read,10} {write,10} {cost,14}");
    }

    private static string Truncate(string text, int width)
        => text.Length <= width ? text : text[..(width - 1)] + "…";

    /// <summary>One measured GPU candidate: the backend, the device it reported, and its two rates.</summary>
    private readonly record struct Candidate(LlamaBackend Backend, string Device, BenchRates Rates);

    /// <summary>
    /// Everything a run measured, before anything is concluded from it. <paramref name="Settled"/>
    /// is non-null when there was nothing to measure — no backend installed, or none that loaded —
    /// in which case the other fields are empty and the answer is already the CPU.
    /// </summary>
    private sealed record Measurements(
        BenchRates Cpu,
        IReadOnlyList<Candidate> Candidates,
        LlamaProbeResult? Settled);

    /// <summary>
    /// Measures without touching settings. Also what the Settings screen's re-detect button calls.
    /// Always returns a usable answer — the CPU always works.
    /// </summary>
    public static LlamaProbeResult Run(Action<string>? onStatus = null)
        => Decide(Measure(verbose: true, onStatus));

    /// <summary>
    /// Runs both benchmarks against every installed backend. Split from <see cref="Decide"/> so the
    /// audit can show the same measurements the decision was made on rather than repeating them:
    /// a probe run costs minutes, and two runs could disagree.
    /// </summary>
    private static Measurements Measure(bool verbose, Action<string>? onStatus = null)
    {
        var none = Array.Empty<Candidate>();

        var backends = LlamaRuntime.DiscoverBackends();
        if (backends.Count == 0)
            return new Measurements(default, none,
                new LlamaProbeResult(LlamaComputeDevice.Cpu, null, "CPU (no GPU backend installed)"));

        // Which of the installed backends actually load and report a device.
        var usable = new List<(LlamaBackend Backend, string Device)>();
        foreach (var backend in backends)
        {
            var device = FirstDeviceOf(backend);
            if (device != null) usable.Add((backend, device));
            else if (verbose) Console.WriteLine($"  {backend.Name}: no usable device (skipped)");
        }

        if (usable.Count == 0)
            return new Measurements(default, none,
                new LlamaProbeResult(LlamaComputeDevice.Cpu, null, "CPU (no GPU backend loaded successfully)"));

        // One line per device, counted, because this is the part that takes minutes and the player
        // is looking at it. "1 of 2" is the only honest progress signal available: a benchmark
        // reports nothing until it finishes.
        int total = usable.Count + 1;

        onStatus?.Invoke($"Measuring the processor (1 of {total})");
        var cpu = Benchmark(null);
        if (verbose) Console.WriteLine($"  CPU: {Describe(cpu)}");

        // Materialised rather than left lazy so every candidate is reported, not just the winner.
        // When a machine picks something surprising, this is the line that explains it.
        var candidates = new List<Candidate>();
        foreach (var (backend, device) in usable)
        {
            onStatus?.Invoke($"Measuring {device} ({candidates.Count + 2} of {total})");
            var rates = Benchmark(backend);
            if (verbose) Console.WriteLine($"  {backend.Name} ({device}): {Describe(rates)}");
            candidates.Add(new Candidate(backend, device, rates));
        }

        return new Measurements(cpu, candidates, null);
    }

    /// <summary>Turns measurements into the device the game will launch with.</summary>
    private static LlamaProbeResult Decide(Measurements m)
    {
        if (m.Settled != null) return m.Settled;

        // Lowest seconds-per-request wins: the two rates only mean something together.
        var best = m.Candidates.OrderBy(c => SecondsPerRequest(c.Rates)).First();

        double cpuSeconds = SecondsPerRequest(m.Cpu);
        double gpuSeconds = SecondsPerRequest(best.Rates);

        if (double.IsPositiveInfinity(gpuSeconds))
            return new LlamaProbeResult(LlamaComputeDevice.Cpu, null, "CPU (GPU benchmark failed)");

        // A GPU that cannot clearly beat the CPU is a slower path with more ways to fail. Integrated
        // graphics sharing system memory with the CPU land here regularly, which is the whole
        // reason this is measured rather than assumed — and a device that reads prompts slowly lands
        // here however well it generates.
        if (!double.IsPositiveInfinity(cpuSeconds) && cpuSeconds < gpuSeconds * RequiredGpuSpeedup)
            return new LlamaProbeResult(LlamaComputeDevice.Cpu, null,
                $"CPU ({Describe(m.Cpu)} — beats {best.Backend.Name} at {Short(best.Rates)})");

        var comparison = double.IsPositiveInfinity(cpuSeconds) ? "" : $" — CPU {Short(m.Cpu)}";
        return new LlamaProbeResult(LlamaComputeDevice.Gpu, best.Backend.Name,
            $"{best.Backend.Name} — {best.Device} ({Describe(best.Rates)}{comparison})");
    }

    /// <summary>
    /// What one device did on each half of inference, in tokens per second. Zero means the run
    /// failed; <see cref="SecondsPerRequest"/> treats that as unusable rather than as infinitely
    /// fast.
    /// </summary>
    private readonly record struct BenchRates(double PromptRate, double GenRate);

    /// <summary>
    /// Seconds to serve one <see cref="WorkloadPromptTokens"/>/<see cref="WorkloadGenTokens"/>
    /// request — the single number the choice is made on, and the one a player experiences as the
    /// length of the loading bar. Infinity when either half was not measurable, so a device that
    /// failed can never sort ahead of one that worked.
    /// </summary>
    private static double SecondsPerRequest(BenchRates rates)
        => rates.PromptRate <= 0 || rates.GenRate <= 0
            ? double.PositiveInfinity
            : WorkloadPromptTokens / rates.PromptRate + WorkloadGenTokens / rates.GenRate;

    /// <summary>
    /// Formats a token rate for the summary line. Invariant, so a French or German locale does not
    /// write "123,2" into a string that is persisted to JSON and read back by whoever is diagnosing
    /// a machine — the same reason the server's own decimal arguments are formatted invariantly.
    /// </summary>
    private static string Rate(double tokensPerSecond)
        => tokensPerSecond.ToString("F1", CultureInfo.InvariantCulture);

    /// <summary>
    /// Both rates, named. "read" and "write" rather than "prompt eval" and "token generation":
    /// this line reaches the Settings screen, where a player is owed plain words.
    /// </summary>
    private static string Describe(BenchRates rates)
        => SecondsPerRequest(rates) is double.PositiveInfinity
            ? "measurement failed"
            : $"read {Rate(rates.PromptRate)}, write {Rate(rates.GenRate)} tok/s";

    /// <summary>The same pair as a bare ratio, for the losing half of a comparison.</summary>
    private static string Short(BenchRates rates)
        => SecondsPerRequest(rates) is double.PositiveInfinity
            ? "no measurement"
            : $"{Rate(rates.PromptRate)}/{Rate(rates.GenRate)}";

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

            // Trim the trailing memory report — "NVIDIA GeForce RTX 3060 (12288 MiB, 11800 MiB free)".
            // That one only, identified by its units. A vendor's own name can contain parentheses,
            // and cutting at the first of them reduced "Qualcomm(R) Adreno(TM) X1-45 GPU" to
            // "Qualcomm" — in the log, in the settings file and on the Settings screen, which is
            // most of what someone diagnosing a machine has to go on.
            int paren = description.LastIndexOf('(');
            if (paren > 0 && description[paren..].Contains("MiB", StringComparison.OrdinalIgnoreCase))
                description = description[..paren];
            return description.Trim();
        }
        return null;
    }

    /// <summary>
    /// Prompt-processing and generation rates in tokens/second for one backend, either of them 0 if
    /// that half did not run. A null backend measures the CPU.
    /// <para><c>-p</c> and <c>-n</c> make llama-bench emit one row for each half, told apart by
    /// <c>n_gen</c>: the generation row carries a positive one, the prompt row a zero. Reading only
    /// the generation row is what put a game with 400-token prompts onto a device that ingested them
    /// at walking pace.</para>
    /// </summary>
    private static BenchRates Benchmark(LlamaBackend? backend)
    {
        var bench = LlamaRuntime.BenchPath;
        var model = LlamaRuntime.ModelPath;
        if (bench == null || model == null) return default;

        // -ngl 0 keeps the CPU measurement on the CPU even when a backend is loaded.
        var layers = backend == null ? " -ngl 0" : "";
        var args = $"-m \"{model}\" -p {BenchPromptTokens} -n {BenchGenTokens} -r 1 -o json{layers}";

        var output = RunCaptured(bench, args, backend, BenchTimeoutMs);
        if (output == null) return default;

        try
        {
            int start = output.IndexOf('[');
            int end = output.LastIndexOf(']');
            if (start < 0 || end <= start) return default;

            using var doc = JsonDocument.Parse(output[start..(end + 1)]);
            double promptRate = 0, genRate = 0;
            foreach (var entry in doc.RootElement.EnumerateArray())
            {
                if (!entry.TryGetProperty("avg_ts", out var ts)) continue;
                double rate = ReadNumber(ts);

                bool isGeneration = entry.TryGetProperty("n_gen", out var gen) && ReadNumber(gen) > 0;
                if (isGeneration) genRate = Math.Max(genRate, rate);
                else promptRate = Math.Max(promptRate, rate);
            }
            return new BenchRates(promptRate, genRate);
        }
        catch
        {
            return default;
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
