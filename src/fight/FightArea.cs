using System;

namespace Cathedral.Fight
{
    public class FightArea
    {
        public const int Width = 60;
        public const int Height = 60;

        // Reserved zones (col start, col end inclusive, row start, row end inclusive)
        // Each zone is halfway between the map center and its respective edge
        public const int ZoneColStart   = 25;
        public const int ZoneColEnd     = 35;
        public const int EnemyRowStart  = 14;  // halfway between top (0) and center (29)
        public const int EnemyRowEnd    = 16;
        public const int PlayerRowStart = 43;  // halfway between bottom (59) and center (30)
        public const int PlayerRowEnd   = 45;

        // Exit is ~2/3 from center toward the top edge (well above enemy spawn zone)
        public const int ExitCol = 30;
        public const int ExitRow = 10;

        private readonly FightCell[,] _cells = new FightCell[Width, Height];

        public FightCell GetCell(int x, int y) => _cells[x, y];
        public void SetCell(int x, int y, FightCell cell) => _cells[x, y] = cell;

        public bool IsInBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

        /// <summary>Returns true if the cell falls inside the enemy or player reserved spawn zone.</summary>
        public static bool IsInReservedZone(int x, int y)
        {
            bool inEnemy  = x >= ZoneColStart && x <= ZoneColEnd && y >= EnemyRowStart  && y <= EnemyRowEnd;
            bool inPlayer = x >= ZoneColStart && x <= ZoneColEnd && y >= PlayerRowStart && y <= PlayerRowEnd;
            return inEnemy || inPlayer;
        }

        /// <summary>Fill every cell in the area with the given terrain type using the provided mapping.</summary>
        public void Fill(TerrainType type, CharMapping mapping, Random rng)
        {
            var appearance = mapping[type];
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                _cells[x, y] = new FightCell
                {
                    Glyph = appearance.PickChar(rng),
                    TextColor = appearance.TextColor,
                    BgColor = appearance.BgColor,
                    Type = type
                };
        }

        /// <summary>Fill a rectangular region.</summary>
        public void FillRect(int x, int y, int w, int h, TerrainType type, CharMapping mapping, Random rng)
        {
            var appearance = mapping[type];
            for (int ry = y; ry < y + h; ry++)
            for (int rx = x; rx < x + w; rx++)
            {
                if (!IsInBounds(rx, ry)) continue;
                _cells[rx, ry] = new FightCell
                {
                    Glyph = appearance.PickChar(rng),
                    TextColor = appearance.TextColor,
                    BgColor = appearance.BgColor,
                    Type = type
                };
            }
        }

        /// <summary>Set the terrain of a single cell, picking a glyph from the mapping.</summary>
        public void SetTerrain(int x, int y, TerrainType type, CharMapping mapping, Random rng)
        {
            if (!IsInBounds(x, y)) return;
            var appearance = mapping[type];
            _cells[x, y] = new FightCell
            {
                Glyph = appearance.PickChar(rng),
                TextColor = appearance.TextColor,
                BgColor = appearance.BgColor,
                Type = type
            };
        }

        /// <summary>
        /// Removes only hard obstacles from reserved zones (soft/treacherous/dangerous terrain is kept).
        /// Then places the exit cell at the enemy side (top center).
        /// </summary>
        public void ClearReservedZones(CharMapping mapping, Random rng)
        {
            ClearHardObstaclesInZone(ZoneColStart, EnemyRowStart,
                ZoneColEnd - ZoneColStart + 1, EnemyRowEnd - EnemyRowStart + 1, mapping, rng);
            ClearHardObstaclesInZone(ZoneColStart, PlayerRowStart,
                ZoneColEnd - ZoneColStart + 1, PlayerRowEnd - PlayerRowStart + 1, mapping, rng);

            // Place exit at top center (enemy side)
            var exitAppearance = mapping[TerrainType.Exit];
            _cells[ExitCol, ExitRow] = new FightCell
            {
                Glyph = '⎆',
                TextColor = exitAppearance.TextColor,
                BgColor = exitAppearance.BgColor,
                Type = TerrainType.Exit
            };
        }

        private void ClearHardObstaclesInZone(int x, int y, int w, int h, CharMapping mapping, Random rng)
        {
            for (int ry = y; ry < y + h; ry++)
            for (int rx = x; rx < x + w; rx++)
            {
                if (!IsInBounds(rx, ry)) continue;
                if (_cells[rx, ry].Type == TerrainType.HardObstacle)
                    SetTerrain(rx, ry, TerrainType.FreeSpace, mapping, rng);
            }
        }
    }
}
