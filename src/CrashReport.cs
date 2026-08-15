using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Cathedral;

/// <summary>
/// Captures a diagnostic snapshot when a phase fails, and preserves the run's log so the evidence
/// outlives the session.
///
/// <para><b>What problem this solves.</b> <see cref="GameLog"/> is truncated on every launch, which
/// is right for "what happened *this* time" but means a tester who relaunches before sending the
/// file has destroyed the only record. A failure that ends a run is exactly the moment that file
/// stops being disposable, so it is copied aside under a name no later launch will touch.</para>
///
/// <para><b>The snapshot exists because a message is not a diagnosis.</b> The failure this was
/// written for reported <c>An error occurred while sending the request</c> and nothing else: no
/// socket error code, no indication whether the server was even alive, no way to tell a poisoned
/// connection from a dead process. Everything here answers a question that could not be answered
/// from the log we had.</para>
///
/// <para><b>Subsystems contribute their own sections</b> through <see cref="AddProvider"/>, so this
/// file knows nothing about the LLM, audio or rendering. A provider that throws costs its own
/// section and nothing else — the report is written on the worst path there is, and must never be
/// the reason a failure gets worse.</para>
/// </summary>
public static class CrashReport
{
    /// <summary>
    /// Cap on preserved copies per run. Without one, a failure that repeats every frame would copy a
    /// multi-megabyte log until the disk filled. Three is enough to see whether the second and third
    /// failures look like the first, which is the question a repeat raises.
    /// </summary>
    private const int MaxReportsPerRun = 3;

    private static readonly object Gate = new();
    private static readonly List<(string Name, Func<string> Provider)> Providers = new();
    private static int _written;

    /// <summary>
    /// Registers a section written into every later report. Call once, at construction — the report
    /// is built long after, on a thread that must not be made to wait on anything slow, so a
    /// provider is expected to bound its own work.
    ///
    /// <para>Registering <paramref name="name"/> twice replaces the first rather than adding a second
    /// section: a subsystem rebuilt mid-run (a restarted LLM server) would otherwise have the report
    /// interrogating the dead instance alongside the live one, with nothing to say which was which.</para>
    /// </summary>
    public static void AddProvider(string name, Func<string> provider)
    {
        lock (Gate)
        {
            int existing = Providers.FindIndex(p => p.Name == name);
            if (existing >= 0) Providers[existing] = (name, provider);
            else Providers.Add((name, provider));
        }
    }

    /// <summary>
    /// Writes a full report to the log, then copies the log aside.
    ///
    /// <para>The report goes to <see cref="Console.Error"/> first and the copy is taken after, so the
    /// preserved file ends with the diagnosis rather than requiring the two to be read together.</para>
    /// </summary>
    /// <param name="what">What was being attempted, in the caller's words ("generating observations").</param>
    /// <param name="ex">The failure, if there was one.</param>
    /// <returns>
    /// The file name of the preserved copy, for showing the player, or null when nothing could be
    /// preserved (no log open, disk unwritable, or the per-run cap reached).
    /// </returns>
    public static string? Capture(string what, Exception? ex)
    {
        lock (Gate)
        {
            if (_written >= MaxReportsPerRun)
            {
                Console.Error.WriteLine(
                    $"CrashReport: {what} failed again; not preserving a {MaxReportsPerRun + 1}th copy of the log.");
                return null;
            }
            _written++;
        }

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("════════════════════════════════════════════════════════════════════════");
        sb.AppendLine($"CRASH REPORT — {what}");
        sb.AppendLine($"  at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} (local), {DateTime.UtcNow:HH:mm:ss.fff} UTC");
        sb.AppendLine("════════════════════════════════════════════════════════════════════════");

        AppendExceptionChain(sb, ex);
        AppendEnvironment(sb);

        List<(string Name, Func<string> Provider)> providers;
        lock (Gate) providers = new List<(string, Func<string>)>(Providers);

        foreach (var (name, provider) in providers)
        {
            sb.AppendLine();
            sb.AppendLine($"── {name} ──");
            try
            {
                sb.AppendLine(provider().TrimEnd());
            }
            catch (Exception pex)
            {
                sb.AppendLine($"  (section unavailable: {pex.GetType().Name}: {pex.Message})");
            }
        }

        sb.AppendLine("════════════════════════════════════════════════════════════════════════");

        // Console.Error is teed into log.txt, so this lands in the file that is about to be copied.
        Console.Error.WriteLine(sb.ToString());

        var stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        return GameLog.PreserveCopy($"log-crash-{stamp}.txt");
    }

    // ── Sections ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// The whole exception chain, with the fields that actually discriminate between causes.
    ///
    /// <para><see cref="Exception.Message"/> alone is close to useless for a transport failure: every
    /// one of them reads "An error occurred while sending the request". The <see cref="System.Net.Sockets.SocketError"/>
    /// underneath separates a connection reset by the peer (a connection reused after the server
    /// closed it) from a refusal (the server is gone) from a timeout (it is alive but wedged) — three
    /// different bugs behind one sentence.</para>
    /// </summary>
    private static void AppendExceptionChain(StringBuilder sb, Exception? ex)
    {
        sb.AppendLine();
        sb.AppendLine("── Exception ──");
        if (ex == null)
        {
            sb.AppendLine("  (none — reported without an exception)");
            return;
        }

        int depth = 0;
        for (var e = ex; e != null; e = e.InnerException, depth++)
        {
            string indent = new string(' ', 2 + depth * 2);
            sb.AppendLine($"{indent}{(depth == 0 ? "" : "inner: ")}{e.GetType().FullName}: {e.Message}");

            if (e is System.Net.Http.HttpRequestException httpEx)
            {
                sb.AppendLine($"{indent}  HttpRequestError: {httpEx.HttpRequestError}");
                if (httpEx.StatusCode != null)
                    sb.AppendLine($"{indent}  StatusCode:      {(int)httpEx.StatusCode} {httpEx.StatusCode}");
            }

            if (e is System.Net.Sockets.SocketException sockEx)
            {
                sb.AppendLine($"{indent}  SocketErrorCode: {sockEx.SocketErrorCode}");
                sb.AppendLine($"{indent}  NativeErrorCode: {sockEx.NativeErrorCode}");
            }

            if (e is IOException ioEx && ioEx.HResult != 0)
                sb.AppendLine($"{indent}  HResult:         0x{ioEx.HResult:X8}");

            if (e is OperationCanceledException)
                sb.AppendLine($"{indent}  (cancellation — check whether a timeout or a caller cancelled)");
        }

        sb.AppendLine();
        sb.AppendLine("  Stack trace:");
        foreach (var line in (ex.StackTrace ?? "(none)").Split('\n'))
            sb.AppendLine($"    {line.TrimEnd()}");
    }

    /// <summary>
    /// The machine the failure happened on.
    ///
    /// <para><b>Wine is called out explicitly</b> because it changes what a networking or windowing
    /// failure means, and because it is otherwise only inferable from incidental details — a
    /// <c>Z:\home\…</c> path in a startup line, a PipeWire audio device — which is a poor way to
    /// learn the single most important fact about a report.</para>
    /// </summary>
    private static void AppendEnvironment(StringBuilder sb)
    {
        sb.AppendLine();
        sb.AppendLine("── Environment ──");
        try
        {
            sb.AppendLine($"  OS:            {Environment.OSVersion} ({RuntimeInformation.OSDescription})");
            sb.AppendLine($"  Runtime:       {RuntimeInformation.FrameworkDescription}, {RuntimeInformation.ProcessArchitecture}");
            sb.AppendLine($"  Wine:          {DetectWine() ?? "not detected (running on Windows)"}");
            sb.AppendLine($"  Processors:    {Environment.ProcessorCount}");
            sb.AppendLine($"  Working dir:   {Environment.CurrentDirectory}");
            sb.AppendLine($"  Process up:    {(DateTime.Now - System.Diagnostics.Process.GetCurrentProcess().StartTime).TotalMinutes:F1} min");
            sb.AppendLine($"  Managed heap:  {GC.GetTotalMemory(false) / (1024 * 1024)} MB");
            sb.AppendLine($"  Threads:       {System.Diagnostics.Process.GetCurrentProcess().Threads.Count}");
        }
        catch (Exception ex)
        {
            sb.AppendLine($"  (partially unavailable: {ex.GetType().Name}: {ex.Message})");
        }
    }

    /// <summary>
    /// Wine exports <c>wine_get_version</c> from its own ntdll and Windows does not, which is the
    /// documented way to ask. Resolved through <c>GetProcAddress</c> rather than a direct P/Invoke so
    /// the ordinary Windows answer costs a null pointer instead of a thrown
    /// <c>EntryPointNotFoundException</c>.
    /// </summary>
    private static string? DetectWine()
    {
        try
        {
            IntPtr ntdll = GetModuleHandleW("ntdll.dll");
            if (ntdll == IntPtr.Zero) return null;

            IntPtr proc = GetProcAddress(ntdll, "wine_get_version");
            if (proc == IntPtr.Zero) return null;

            var getVersion = Marshal.GetDelegateForFunctionPointer<WineGetVersionDelegate>(proc);
            string version = Marshal.PtrToStringAnsi(getVersion()) ?? "unknown version";
            return $"YES — Wine {version} (a Linux/macOS host; networking and windowing differ from Windows)";
        }
        catch
        {
            return null;
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr WineGetVersionDelegate();

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandleW(string moduleName);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr module, string procName);
}
