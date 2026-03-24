using System;

namespace Cathedral.Fight.Generators
{
    /// <summary>
    /// Places geometric shapes (rectangles and circles) as obstacles on a free-space background.
    /// Suitable for interiors: columns, furniture, ruins, barricades.
    /// Each shape has a hard-obstacle interior and optionally a soft-obstacle ring.
    /// </summary>
    public class GeometricGenerator : FightAreaGeneratorBase
    {
        public int ShapeCount   { get; set; } = 8;
        public int MinSize      { get; set; } = 3;
        public int MaxSize      { get; set; } = 8;
        public bool AllowCircles { get; set; } = true;
        public bool AllowRects   { get; set; } = true;
        public bool SoftRing     { get; set; } = true;  // ring of SoftObstacle around shapes
        public float DangerousChance    { get; set; } = 0.012f; // ~1% of open cells
        public float TreacherousChance  { get; set; } = 0.08f;

        public GeometricGenerator(CharMapping? mapping = null) : base(mapping) { }

        protected override void GenerateInternal(FightArea area)
        {
            // Start with free space
            area.Fill(TerrainType.FreeSpace, Mapping, Rng);

            for (int i = 0; i < ShapeCount; i++)
            {
                int size = Rng.Next(MinSize, MaxSize + 1);
                int cx   = Rng.Next(size, FightArea.Width  - size);
                int cy   = Rng.Next(size, FightArea.Height - size);

                bool useCircle = AllowCircles && (!AllowRects || Rng.Next(2) == 0);

                if (ShapeOverlapsZone(cx, cy, size)) continue;

                if (useCircle)
                    StampCircle(area, cx, cy, size);
                else
                    StampRect(area, cx, cy, size, size);
            }

            // Scatter dangerous/treacherous terrain in open areas for gameplay texture
            for (int y = 0; y < FightArea.Height; y++)
            for (int x = 0; x < FightArea.Width; x++)
            {
                if (area.GetCell(x, y).Type != TerrainType.FreeSpace) continue;
                if (FightArea.IsInReservedZone(x, y)) continue;
                float roll = (float)Rng.NextDouble();
                if (roll < DangerousChance)
                    area.SetTerrain(x, y, TerrainType.DangerousTerrain, Mapping, Rng);
                else if (roll < DangerousChance + TreacherousChance)
                    area.SetTerrain(x, y, TerrainType.TreacherousTerrain, Mapping, Rng);
            }
        }

        private static bool ShapeOverlapsZone(int cx, int cy, int size)
        {
            int pad = size + 2;
            if (cx + pad >= FightArea.ZoneColStart && cx - pad <= FightArea.ZoneColEnd &&
                cy + pad >= FightArea.EnemyRowStart && cy - pad <= FightArea.EnemyRowEnd)
                return true;
            if (cx + pad >= FightArea.ZoneColStart && cx - pad <= FightArea.ZoneColEnd &&
                cy + pad >= FightArea.PlayerRowStart && cy - pad <= FightArea.PlayerRowEnd)
                return true;
            return false;
        }

        private void StampCircle(FightArea area, int cx, int cy, int radius)
        {
            int r2 = radius * radius;
            int softR2 = (radius + 1) * (radius + 1);

            for (int y = cy - radius - 1; y <= cy + radius + 1; y++)
            for (int x = cx - radius - 1; x <= cx + radius + 1; x++)
            {
                if (!area.IsInBounds(x, y)) continue;
                int dx = x - cx, dy = y - cy;
                int dist2 = dx * dx + dy * dy;
                if (dist2 <= r2)
                    area.SetTerrain(x, y, TerrainType.HardObstacle, Mapping, Rng);
                else if (SoftRing && dist2 <= softR2 && area.GetCell(x, y).Type == TerrainType.FreeSpace)
                    area.SetTerrain(x, y, TerrainType.SoftObstacle, Mapping, Rng);
            }
        }

        private void StampRect(FightArea area, int cx, int cy, int hw, int hh)
        {
            for (int y = cy - hh; y <= cy + hh; y++)
            for (int x = cx - hw; x <= cx + hw; x++)
            {
                if (!area.IsInBounds(x, y)) continue;
                area.SetTerrain(x, y, TerrainType.HardObstacle, Mapping, Rng);
            }

            if (!SoftRing) return;

            // Outer ring
            for (int y = cy - hh - 1; y <= cy + hh + 1; y++)
            for (int x = cx - hw - 1; x <= cx + hw + 1; x++)
            {
                if (!area.IsInBounds(x, y)) continue;
                if (area.GetCell(x, y).Type == TerrainType.FreeSpace)
                    area.SetTerrain(x, y, TerrainType.SoftObstacle, Mapping, Rng);
            }
        }
    }
}
