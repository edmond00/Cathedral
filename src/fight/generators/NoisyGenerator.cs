using System;
using Cathedral.Glyph;

namespace Cathedral.Fight.Generators
{
    /// <summary>
    /// Generates organic-looking areas (forests, swamps, rocky terrain) using Perlin noise.
    /// Noise value thresholds determine terrain type at each cell.
    /// </summary>
    public class NoisyGenerator : FightAreaGeneratorBase
    {
        public float Density { get; set; } = 0.74f;       // Noise threshold above which cells become hard obstacles (higher = fewer walls)
        public float NoiseScale { get; set; } = 0.12f;    // Spatial frequency of the noise field
        public int Seed { get; set; } = 0;

        // Sub-thresholds for soft terrain types (applied to noise values below Density)
        // The "passable" band is split into: dangerous, treacherous, soft, free
        public float DangerousFraction   { get; set; } = 0.016f; // ~1% of total cells
        public float TreacherousFraction { get; set; } = 0.12f;
        public float SoftFraction        { get; set; } = 0.20f;

        public NoisyGenerator(CharMapping? mapping = null) : base(mapping) { }

        protected override void GenerateInternal(FightArea area)
        {
            // Keep offset in [0,100] range so float precision is maintained for every cell.
            // Large seeds (TickCount ~500M) multiplied by big constants would overflow
            // float mantissa and make all cells sample the same noise point.
            var seedRng = new Random(Seed);
            float offX = (float)seedRng.NextDouble() * 100f;
            float offY = (float)seedRng.NextDouble() * 100f;

            // Pre-compute normalised noise for every cell (Fbm for richer organic look)
            float[,] noise = new float[FightArea.Width, FightArea.Height];
            float min = float.MaxValue, max = float.MinValue;
            for (int y = 0; y < FightArea.Height; y++)
            for (int x = 0; x < FightArea.Width; x++)
            {
                float n = Perlin.Fbm(
                    (x * NoiseScale) + offX,
                    (y * NoiseScale) + offY,
                    4);
                noise[x, y] = n;
                if (n < min) min = n;
                if (n > max) max = n;
            }

            float range = max - min;
            if (range < 0.001f) range = 0.001f;

            for (int y = 0; y < FightArea.Height; y++)
            for (int x = 0; x < FightArea.Width; x++)
            {
                float t = (noise[x, y] - min) / range; // 0..1

                TerrainType type;
                if (t >= Density)
                {
                    // Never wall off spawn zones or cells immediately adjacent to the exit
                    bool isProtected = FightArea.IsInReservedZone(x, y) ||
                                       (Math.Abs(x - FightArea.ExitCol) <= 2 &&
                                        Math.Abs(y - FightArea.ExitRow) <= 2);
                    type = isProtected ? TerrainType.FreeSpace : TerrainType.HardObstacle;
                }
                else
                {
                    // Split the passable band
                    float passable = t / Density; // 0..1 within passable range
                    if      (passable < DangerousFraction)  type = TerrainType.DangerousTerrain;
                    else if (passable < DangerousFraction + TreacherousFraction) type = TerrainType.TreacherousTerrain;
                    else if (passable < DangerousFraction + TreacherousFraction + SoftFraction) type = TerrainType.SoftObstacle;
                    else    type = TerrainType.FreeSpace;
                }

                area.SetTerrain(x, y, type, Mapping, Rng);
            }
        }
    }
}
