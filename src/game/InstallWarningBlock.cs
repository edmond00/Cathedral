using System.Collections.Generic;
using Cathedral.LLM;
using Cathedral.Terminal;

namespace Cathedral.Game;

/// <summary>
/// Draws <see cref="LlamaInstallCheck.Problems"/> as a short warning block. Shared by the loading
/// screen, where the problem usually shows itself, and the main menu, which every run passes through
/// and which stays up long enough to be read — the loading screen leaves the moment the server is up.
/// Draws nothing when the install matches.
/// </summary>
public static class InstallWarningBlock
{
    /// <summary>How many problem lines are shown before the rest are summarised.</summary>
    private const int MaxLines = 5;

    /// <summary>Draws the block with its heading on <paramref name="row"/>; returns the row after it.</summary>
    public static int Draw(TerminalHUD terminal, int row)
    {
        var problems = LlamaInstallCheck.Problems;
        if (problems.Count == 0) return row;

        int width = terminal.Width - 8;
        terminal.CenteredText(row++, "The language model install does not match this build:",
            Config.Colors.BrightRed, Config.Colors.Black);
        row++;

        int shown = 0;
        foreach (var problem in problems)
        {
            if (shown == MaxLines) break;
            foreach (var line in Wrap(problem, width))
                terminal.CenteredText(row++, line, Config.Colors.Gray, Config.Colors.Black);
            shown++;
        }
        if (problems.Count > MaxLines)
            terminal.CenteredText(row++, $"... and {problems.Count - MaxLines} more (see log.txt)",
                Config.Colors.DarkGray35, Config.Colors.Black);

        row++;
        terminal.CenteredText(row++, "The game will run, but narration may fail or read differently. See models/README.md.",
            Config.Colors.DarkGray35, Config.Colors.Black);
        return row;
    }

    private static IEnumerable<string> Wrap(string text, int width)
    {
        while (text.Length > width)
        {
            int cut = text.LastIndexOf(' ', width);
            if (cut <= 0) cut = width;
            yield return text[..cut];
            text = text[cut..].TrimStart();
        }
        yield return text;
    }
}
