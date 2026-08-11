using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Cathedral;

/// <summary>
/// Reconnects a GUI-subsystem build to the console it was launched from, when there is one.
///
/// <para><b>Why this exists.</b> A shipped build is compiled <c>WinExe</c> so that double-clicking
/// it opens a game rather than a black window filling with diagnostic logging. But that flag is
/// absolute: Windows gives the process no console at all, so a shipped build launched deliberately
/// from a terminal is silent too. That costs two things worth keeping — <c>--cli</c> against the
/// packaged artifact, and any chance of reading a crash out of a player's install.</para>
///
/// <para>Verifying the release package meant driving <c>dist\Cathedral\Cathedral.exe</c> with a
/// <c>--cli</c> script and reading its output. Without this, that check goes dark on the very build
/// that most needs checking — the one about to be uploaded.</para>
///
/// <para><b>What it does.</b> Asks to join the parent process's console. Launched from a terminal
/// there is one, and stdout/stderr/stdin are rebound to it. Double-clicked from Explorer there is
/// none, the call fails, and nothing happens — which is the quiet behaviour the GUI subsystem was
/// chosen for. In a console build (<c>Exe</c>) a console already exists and the call is a no-op.</para>
///
/// <para>Every failure is swallowed. This is plumbing for diagnostics; it must never be the reason
/// a game does not start.</para>
/// </summary>
public static class ConsoleAttach
{
    /// <summary>Use the console of the parent process, if it has one.</summary>
    private const int AttachParentProcess = -1;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(int processId);

    /// <summary>
    /// Call once, before anything writes to the console. Safe to call from any build: it does
    /// nothing when a console is already present and nothing when none is available.
    /// </summary>
    public static void AttachToParentIfPresent()
    {
        // Nothing to attach to on any other platform, and the P/Invoke would not resolve.
        if (!OperatingSystem.IsWindows()) return;

        try
        {
            if (!AttachConsole(AttachParentProcess)) return;

            // The streams were bound to the null device when the process started without a
            // console. They do not re-point themselves, so writes would still go nowhere: the
            // attach succeeds and the output silently disappears, which is worse than not
            // attaching at all. Rebind all three to the console we just joined.
            var stdout = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
            var stderr = new StreamWriter(Console.OpenStandardError()) { AutoFlush = true };
            Console.SetOut(stdout);
            Console.SetError(stderr);
            Console.SetIn(new StreamReader(Console.OpenStandardInput()));
        }
        catch
        {
            // No console, a redirected handle, or a locked-down environment. The game runs either
            // way; only the log is lost.
        }
    }
}
