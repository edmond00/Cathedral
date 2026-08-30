using System;

namespace Cathedral.Game.Cli;

/// <summary>
/// Headless-driving mode: the game still opens its normal window and renders as usual, but every
/// interaction can also be driven from stdin and every screen can be observed as text.
/// Activated by <c>--cli</c>.
///
/// <para>
/// The point is automated verification. An agent (or a shell script) issues semantic commands —
/// <c>click keyword hearth</c>, <c>travel "Oakhollow"</c> — and reads back the terminal grid or a
/// structured state line, instead of computing pixel coordinates and screenshotting.
/// See <see cref="CliDriver"/> for the command vocabulary.
/// </para>
///
/// <para>
/// Combine with <c>--playground</c> (no LLM server), <c>--seed &lt;n&gt;</c> (deterministic world and
/// dice) and <c>--debug</c> (forced action outcomes) for reproducible runs.
/// </para>
/// </summary>
public static class CliMode
{
    /// <summary>Whether CLI driving is active.</summary>
    public static bool IsActive { get; set; } = false;

    /// <summary>
    /// Optional path to a newline-separated command script executed at startup. Commands from the
    /// script run before (and then alongside) anything typed on stdin.
    /// </summary>
    public static string? ScriptPath { get; set; } = null;

    /// <summary>
    /// True when at least one <c>expect</c> assertion has failed. The process exits non-zero on
    /// <c>quit</c> so a failing script fails its build step.
    /// </summary>
    public static bool HasFailedAssertion { get; set; } = false;

    /// <summary>
    /// Hard wall-clock limit for the whole run, after which the game closes itself. Without this an
    /// unattended run can hang forever on a screen nobody scripted a way out of — which is exactly
    /// the failure mode that makes an automated harness useless. Set with <c>--cli-timeout</c>.
    /// </summary>
    public static TimeSpan RunTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Emit a CLI response line. Everything the driver prints is prefixed so it can be separated
    /// from the game's own (very chatty) diagnostic logging on the same stdout.
    /// </summary>
    public static void Emit(string line) => Console.WriteLine($"[cli] {line}");

    /// <summary>Emit a multi-line block, prefixing every line.</summary>
    public static void EmitBlock(string text)
    {
        foreach (var line in text.Split('\n'))
            Console.WriteLine($"[cli] {line.TrimEnd('\r')}");
    }
}
