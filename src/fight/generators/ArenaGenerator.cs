using System;

namespace Cathedral.Fight.Generators
{
    /// <summary>
    /// A large open central arena with small geometric structures (pillars, walls, L-shapes,
    /// clusters) scattered inside it. Feels like a temple hall, ruined courtyard, or
    /// tactical battle grid. The arena always contains both spawn zones.
    /// </summary>
    public class ArenaGenerator : FightAreaGeneratorBase
    {
        /// <summary>Number of geometric structures to scatter inside the arena.</summary>
        public int StructureCount { get; set; } = 12;

        /// <summary>Maximum half-size of each structure (actual size randomised per structure).</summary>
        public int MaxStructureSize { get; set; } = 4;

        /// <summary>Padding between the arena walls and the map edge (cells).</summary>
        public int ArenaPadding { get; set; } = 6;

        public int Seed { get; set; } = 0;

        public float DangerousChance    { get; set; } = 0.012f;
        public float TreacherousChance  { get; set; } = 0.06f;

        public ArenaGenerator(CharMapping? mapping = null) : base(mapping) { }

        protected override void GenerateInternal(FightArea area)
        {
            var rng = new Random(Seed);

            // 1. Fill everything with hard obstacles
            area.Fill(TerrainType.HardObstacle, Mapping, Rng);

            // 2. Carve the large central arena room
            int aX = ArenaPadding;
            int aY = ArenaPadding;
            int aW = FightArea.Width  - ArenaPadding * 2;
            int aH = FightArea.Height - ArenaPadding * 2;

            for (int y = aY; y < aY + aH; y++)
            for (int x = aX; x < aX + aW; x++)
                area.SetTerrain(x, y, FloorTerrain(rng), Mapping, Rng);

            // 3. Place geometric structures inside the arena
            for (int i = 0; i < StructureCount; i++)
                PlaceStructure(area, rng, aX, aY, aW, aH);
        }

        private void PlaceStructure(FightArea area, Random rng, int aX, int aY, int aW, int aH)
        {
            // Pick a random spot well inside the arena (avoid zone rows/cols)
            int attempts = 30;
            while (attempts-- > 0)
            {
                int cx = aX + 2 + rng.Next(aW - 4);
                int cy = aY + 2 + rng.Next(aH - 4);

                // Don't place structures on or adjacent to reserved spawn zones / exit
                if (FightArea.IsInReservedZone(cx, cy)) continue;
                if (Math.Abs(cx - FightArea.ExitCol) <= 3 &&
                    Math.Abs(cy - FightArea.ExitRow) <= 3) continue;

                int shape = rng.Next(5);
                switch (shape)
                {
                    case 0: PlacePillar (area, rng, cx, cy); break;
                    case 1: PlaceWallH  (area, rng, cx, cy); break;
                    case 2: PlaceWallV  (area, rng, cx, cy); break;
                    case 3: PlaceLShape (area, rng, cx, cy); break;
                    case 4: PlaceCluster(area, rng, cx, cy); break;
                }
                break;
            }
        }

        // Single thick pillar (1–2 cells wide)
        private void PlacePillar(FightArea area, Random rng, int cx, int cy)
        {
            int size = 1 + rng.Next(2);
            for (int dy = -size; dy <= size; dy++)
            for (int dx = -size; dx <= size; dx++)
                Carve(area, cx + dx, cy + dy, TerrainType.HardObstacle);
        }

        // Horizontal wall segment
        private void PlaceWallH(FightArea area, Random rng, int cx, int cy)
        {
            int len   = 2 + rng.Next(MaxStructureSize);
            int thick = 1 + rng.Next(2);
            for (int dy = 0; dy < thick; dy++)
            for (int dx = 0; dx < len; dx++)
                Carve(area, cx + dx, cy + dy, TerrainType.HardObstacle);
        }

        // Vertical wall segment
        private void PlaceWallV(FightArea area, Random rng, int cx, int cy)
        {
            int len   = 2 + rng.Next(MaxStructureSize);
            int thick = 1 + rng.Next(2);
            for (int dy = 0; dy < len; dy++)
            for (int dx = 0; dx < thick; dx++)
                Carve(area, cx + dx, cy + dy, TerrainType.HardObstacle);
        }

        // L-shaped wall junction
        private void PlaceLShape(FightArea area, Random rng, int cx, int cy)
        {
            int arm  = 2 + rng.Next(MaxStructureSize);
            int sx   = rng.Next(2) == 0 ? -1 : 1;
            int sy   = rng.Next(2) == 0 ? -1 : 1;
            for (int dx = 0; dx <= arm; dx++)
                Carve(area, cx + dx * sx, cy, TerrainType.HardObstacle);
            for (int dy = 1; dy <= arm; dy++)
                Carve(area, cx, cy + dy * sy, TerrainType.HardObstacle);
        }

        // Scattered cluster of soft/treacherous terrain
        private void PlaceCluster(FightArea area, Random rng, int cx, int cy)
        {
            int radius = 1 + rng.Next(MaxStructureSize - 1);
            for (int dy = -radius; dy <= radius; dy++)
            for (int dx = -radius; dx <= radius; dx++)
            {
                if (dx * dx + dy * dy > radius * radius) continue;
                if (rng.NextDouble() < 0.5) continue;
                TerrainType t = rng.NextDouble() < 0.4
                    ? TerrainType.TreacherousTerrain
                    : TerrainType.SoftObstacle;
                Carve(area, cx + dx, cy + dy, t);
            }
        }

        // Only carve if inside arena and not a reserved zone
        private void Carve(FightArea area, int x, int y, TerrainType type)
        {
            if (!area.IsInBounds(x, y)) return;
            if (FightArea.IsInReservedZone(x, y)) return;
            if (Math.Abs(x - FightArea.ExitCol) <= 2 &&
                Math.Abs(y - FightArea.ExitRow) <= 2) return;
            area.SetTerrain(x, y, type, Mapping, Rng);
        }

        private TerrainType FloorTerrain(Random rng)
        {
            double r = rng.NextDouble();
            if (r < DangerousChance)                       return TerrainType.DangerousTerrain;
            if (r < DangerousChance + TreacherousChance)   return TerrainType.TreacherousTerrain;
            return TerrainType.FreeSpace;
        }
    }
}
