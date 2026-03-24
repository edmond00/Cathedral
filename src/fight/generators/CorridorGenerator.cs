using System;
using System.Collections.Generic;

namespace Cathedral.Fight.Generators
{
    /// <summary>
    /// Carves branching corridors outward from the center using iterative DFS.
    /// Suitable for caves, sewers, underground tunnels, narrow alleyways.
    /// </summary>
    public class CorridorGenerator : FightAreaGeneratorBase
    {
        public int BranchFactor   { get; set; } = 3;   // New branches attempted at each carve step
        public int MinLength      { get; set; } = 4;   // Minimum corridor segment length
        public int MaxLength      { get; set; } = 10;  // Maximum corridor segment length
        public int MaxBranchDepth { get; set; } = 5;   // Maximum recursion depth of branches
        public int CorridorWidth  { get; set; } = 2;   // Width of carved tunnels
        public float WideningChance   { get; set; } = 0.20f; // Chance a segment gets widened
        public float ScatterChance    { get; set; } = 0.06f; // Chance of scattered terrain in corridors
        public float DangerousChance  { get; set; } = 0.012f; // Separate low chance for dangerous terrain

        public CorridorGenerator(CharMapping? mapping = null) : base(mapping) { }

        protected override void GenerateInternal(FightArea area)
        {
            // Start with solid walls
            area.Fill(TerrainType.HardObstacle, Mapping, Rng);

            // Pre-carve rooms at spawn zones and exit — corridors will radiate naturally from these
            int zoneW = FightArea.ZoneColEnd - FightArea.ZoneColStart + 5;
            CarveRect(area, Math.Max(0, FightArea.ExitCol - 3),
                            Math.Max(0, FightArea.ExitRow - 2), 7, 5);
            CarveRect(area, Math.Max(0, FightArea.ZoneColStart - 2),
                            Math.Max(0, FightArea.EnemyRowStart - 2), zoneW, 7);
            CarveRect(area, Math.Max(0, FightArea.ZoneColStart - 2),
                            Math.Max(0, FightArea.PlayerRowStart - 2), zoneW, 7);

            // Start DFS from the map center and each zone center
            var stack = new Stack<(int x, int y, int depth)>();
            stack.Push((FightArea.Width  / 2, FightArea.Height / 2, 0));
            stack.Push((FightArea.ExitCol, FightArea.ExitRow + 3, 1));
            stack.Push((FightArea.ExitCol, (FightArea.EnemyRowStart + FightArea.EnemyRowEnd) / 2, 1));
            stack.Push((FightArea.ExitCol, (FightArea.PlayerRowStart + FightArea.PlayerRowEnd) / 2, 1));

            while (stack.Count > 0)
            {
                var (cx, cy, depth) = stack.Pop();
                if (depth >= MaxBranchDepth) continue;

                // Randomize cardinal directions
                var dirs = ShuffledDirections();
                int branchesCarved = 0;

                foreach (var (dx, dy) in dirs)
                {
                    if (branchesCarved >= BranchFactor) break;

                    int length = Rng.Next(MinLength, MaxLength + 1);
                    bool wide = Rng.NextDouble() < WideningChance;
                    int width = wide ? CorridorWidth + 1 : CorridorWidth;

                    // Check whether there is at least one uncarved cell in this direction
                    int endX = cx + dx * length;
                    int endY = cy + dy * length;
                    if (!area.IsInBounds(endX, endY)) continue;

                    // Carve the segment
                    for (int step = 0; step < length; step++)
                    {
                        int px = cx + dx * step;
                        int py = cy + dy * step;

                        for (int w = 0; w < width; w++)
                        {
                            // Perpendicular offset
                            int ox = (dy != 0) ? w : 0;
                            int oy = (dx != 0) ? w : 0;
                            int tx = px + ox;
                            int ty = py + oy;
                            if (!area.IsInBounds(tx, ty)) continue;

                            if (area.GetCell(tx, ty).Type == TerrainType.HardObstacle)
                            {
                                float roll = (float)Rng.NextDouble();
                                TerrainType t;
                                if      (roll < DangerousChance)                  t = TerrainType.DangerousTerrain;
                                else if (roll < DangerousChance + ScatterChance * 0.5f) t = TerrainType.TreacherousTerrain;
                                else if (roll < DangerousChance + ScatterChance)  t = TerrainType.SoftObstacle;
                                else                                              t = TerrainType.FreeSpace;
                                area.SetTerrain(tx, ty, t, Mapping, Rng);
                            }
                        }
                    }

                    stack.Push((endX, endY, depth + 1));
                    branchesCarved++;
                }
            }
        }

        private void CarveRect(FightArea area, int x, int y, int w, int h)
        {
            for (int ry = y; ry < y + h; ry++)
            for (int rx = x; rx < x + w; rx++)
            {
                if (!area.IsInBounds(rx, ry)) continue;
                if (area.GetCell(rx, ry).Type == TerrainType.HardObstacle)
                    area.SetTerrain(rx, ry, TerrainType.FreeSpace, Mapping, Rng);
            }
        }

        private (int dx, int dy)[] ShuffledDirections()
        {
            var dirs = new (int, int)[] { (1, 0), (-1, 0), (0, 1), (0, -1) };
            // Fisher-Yates shuffle
            for (int i = dirs.Length - 1; i > 0; i--)
            {
                int j = Rng.Next(i + 1);
                (dirs[i], dirs[j]) = (dirs[j], dirs[i]);
            }
            return dirs;
        }
    }
}
