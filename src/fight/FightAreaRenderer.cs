using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Terminal.Utils;

namespace Cathedral.Fight
{
    /// <summary>
    /// Renders a FightArea to a TerminalHUD, centered in the 100×100 grid.
    /// The 60×60 map occupies columns 20-79, rows 20-79.
    /// Title is shown above the map and a terrain legend below.
    /// </summary>
    public static class FightAreaRenderer
    {
        private const int OffsetX = 20;
        private const int OffsetY = 20;

        public static void Render(TerminalHUD terminal, FightArea area, string modeLabel, int seed)
        {
            terminal.Clear();

            // Border box around the map (1-cell padding)
            terminal.DrawBox(OffsetX - 1, OffsetY - 1, FightArea.Width + 2, FightArea.Height + 2,
                BoxStyle.Single, Config.Colors.DarkGray, Config.Colors.Black);

            // Title row above map
            string title = $" [{modeLabel}]  seed:{seed} ";
            terminal.CenteredText(OffsetY - 2, title, Config.Colors.GoldYellow, Config.Colors.Black);

            // Instructions row
            terminal.CenteredText(OffsetY - 3,
                "Any key = regenerate   ESC = quit",
                Config.Colors.DarkGray35, Config.Colors.Black);

            // Render all cells
            for (int y = 0; y < FightArea.Height; y++)
            for (int x = 0; x < FightArea.Width; x++)
            {
                var cell = area.GetCell(x, y);
                terminal.SetCell(OffsetX + x, OffsetY + y, cell.Glyph, cell.TextColor, cell.BgColor);
            }

            // Overlay zone glyphs: enemies (orange ☹) and party (white ☻)
            OverlayZoneGlyphs(terminal, FightArea.ZoneColStart, FightArea.EnemyRowStart,
                FightArea.ZoneColEnd - FightArea.ZoneColStart + 1,
                FightArea.EnemyRowEnd - FightArea.EnemyRowStart + 1,
                '☹', Config.Colors.Orange);

            OverlayZoneGlyphs(terminal, FightArea.ZoneColStart, FightArea.PlayerRowStart,
                FightArea.ZoneColEnd - FightArea.ZoneColStart + 1,
                FightArea.PlayerRowEnd - FightArea.PlayerRowStart + 1,
                '☻', Config.Colors.White);

            // Legend below map
            RenderLegend(terminal, OffsetY + FightArea.Height + 2);
        }

        private static void OverlayZoneGlyphs(TerminalHUD terminal, int zx, int zy, int zw, int zh,
            char glyph, OpenTK.Mathematics.Vector4 glyphColor)
        {
            int midRow = zy + zh / 2;
            for (int i = 2; i < zw; i += 2)
                terminal.SetCell(OffsetX + zx + i, OffsetY + midRow, glyph, glyphColor,
                    terminal.View[OffsetX + zx + i, OffsetY + midRow].BackgroundColor);
        }

        /// <summary>
        /// Toggle the exit glyph color for a blinking effect. Call each frame from the render loop.
        /// </summary>
        public static void UpdateBlink(TerminalHUD terminal, bool blinkOn)
        {
            var textColor = blinkOn
                ? new OpenTK.Mathematics.Vector4(1.0f, 0.85f, 0.2f, 1.0f)   // GoldYellow - visible
                : new OpenTK.Mathematics.Vector4(0.16f, 0.11f, 0.0f, 1.0f); // Same as bg  - invisible
            terminal.SetCell(
                OffsetX + FightArea.ExitCol,
                OffsetY + FightArea.ExitRow,
                '⎆', textColor, new OpenTK.Mathematics.Vector4(0.16f, 0.11f, 0.0f, 1.0f));
        }

        public static void RenderLegend(TerminalHUD terminal, int startRow)
        {
            if (startRow >= terminal.Height) return;

            terminal.Text(OffsetX, startRow, "Terrain legend:", Config.Colors.LightGray, Config.Colors.Black);

            var entries = new[]
            {
                (TerrainType.Exit,              '⎆', Config.Colors.GoldYellow,     "Exit (blink)"),
                (TerrainType.FreeSpace,         '.', Config.Colors.DarkGray35,     "Free space"),
                (TerrainType.SoftObstacle,      '·', Config.Colors.DarkYellowGrey, "Soft obstacle  (slow)"),
                (TerrainType.TreacherousTerrain,'~', Config.Colors.MediumYellow,   "Treacherous    (fall risk)"),
                (TerrainType.DangerousTerrain,  '∴', Config.Colors.Orange,         "Dangerous      (wound risk)"),
                (TerrainType.HardObstacle,      '#', Config.Colors.LightGray75,    "Hard obstacle  (blocks)"),
            };

            int col = OffsetX;
            int colWidth = 28;
            int row = startRow + 1;

            for (int i = 0; i < entries.Length; i++)
            {
                var (_, glyph, color, label) = entries[i];

                if (col + colWidth > terminal.Width)
                {
                    col = OffsetX;
                    row++;
                }

                terminal.SetCell(col, row, glyph, color, Config.Colors.Black);
                terminal.Text(col + 2, row, label, Config.Colors.LightGray75, Config.Colors.Black);
                col += colWidth;
            }
        }
    }
}
