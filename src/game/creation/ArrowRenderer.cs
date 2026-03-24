using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Terminal.Utils;

namespace Cathedral.Game.Creation;

/// <summary>
/// Draws L-shaped arrow connectors between labels and body art cells.
/// Uses box-drawing characters for clean lines.
/// </summary>
public static class ArrowRenderer
{
    /// <summary>
    /// Draws an L-shaped connector from (x1, y1) to (x2, y2).
    /// Goes horizontal first then vertical, using box-drawing chars.
    /// </summary>
    public static void DrawConnector(TerminalHUD terminal, int x1, int y1, int x2, int y2,
        Vector4 color, Vector4 bgColor)
    {
        if (x1 == x2 && y1 == y2) return;

        // Draw endpoint dots
        terminal.SetCell(x1, y1, '●', color, bgColor);
        terminal.SetCell(x2, y2, '●', color, bgColor);

        if (y1 == y2)
        {
            // Pure horizontal line
            DrawHorizontalLine(terminal, x1, x2, y1, color, bgColor);
            return;
        }

        if (x1 == x2)
        {
            // Pure vertical line
            DrawVerticalLine(terminal, x1, y1, y2, color, bgColor);
            return;
        }

        // L-shaped: horizontal from x1 to x2, then vertical from y1 to y2
        // Corner is at (x2, y1)
        DrawHorizontalLine(terminal, x1, x2, y1, color, bgColor);
        DrawVerticalLine(terminal, x2, y1, y2, color, bgColor);

        // Draw corner character
        char corner = GetCornerChar(x1, y1, x2, y2);
        terminal.SetCell(x2, y1, corner, color, bgColor);
    }

    /// <summary>
    /// Draws a horizontal dashed connector for label alignment.
    /// </summary>
    public static void DrawHorizontalDash(TerminalHUD terminal, int x1, int x2, int y,
        Vector4 color, Vector4 bgColor)
    {
        int start = Math.Min(x1, x2);
        int end = Math.Max(x1, x2);
        for (int x = start; x <= end; x++)
        {
            char c = (x == start || x == end) ? '·' : '·';
            terminal.SetCell(x, y, c, color, bgColor);
        }
    }

    private static void DrawHorizontalLine(TerminalHUD terminal, int x1, int x2, int y,
        Vector4 color, Vector4 bgColor)
    {
        int start = Math.Min(x1, x2) + 1;
        int end = Math.Max(x1, x2) - 1;
        for (int x = start; x <= end; x++)
            terminal.SetCell(x, y, BoxChars.Single.Horizontal, color, bgColor);
    }

    private static void DrawVerticalLine(TerminalHUD terminal, int x, int y1, int y2,
        Vector4 color, Vector4 bgColor)
    {
        int start = Math.Min(y1, y2) + 1;
        int end = Math.Max(y1, y2) - 1;
        for (int y = start; y <= end; y++)
            terminal.SetCell(x, y, BoxChars.Single.Vertical, color, bgColor);
    }

    private static char GetCornerChar(int x1, int y1, int x2, int y2)
    {
        bool goingRight = x2 > x1;
        bool goingDown = y2 > y1;

        return (goingRight, goingDown) switch
        {
            (true, true) => BoxChars.Single.TopRight,     // ┐ going right then down
            (true, false) => BoxChars.Single.BottomRight,  // ┘ going right then up
            (false, true) => BoxChars.Single.TopLeft,      // ┌ going left then down
            (false, false) => BoxChars.Single.BottomLeft   // └ going left then up
        };
    }
}
