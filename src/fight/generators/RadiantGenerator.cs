using System;
using Cathedral.Glyph;

namespace Cathedral.Fight.Generators
{
    /// <summary>
    /// Noise map where the wall-density threshold rises with distance from the map centre.
    /// The result is an open arena in the middle that grows denser and more impassable
    /// toward the edges, like a clearing surrounded by thick forest or ruins.
    /// </summary>
    public class RadiantGenerator : FightAreaGeneratorBase
    {
        /// <summary>Threshold at the exact centre of the map (fewest walls).</summary>
        public float CentreDensity { get; set; } = 0.88f;

        /// <summary>Threshold at the furthest corner of the map (most walls).</summary>
        public float EdgeDensity   { get; set; } = 0.40f;

        /// <summary>Controls how fast the transition happens. 1 = linear; >1 = slow centre fast edges; <1 = inverse.</summary>
        public float FalloffPower  { get; set; } = 1.6f;

        public float NoiseScale { get; set; } = 0.12f;
        public int   Seed       { get; set; } = 0;

        public float DangerousFraction   { get; set; } = 0.016f;
        public float TreacherousFraction { get; set; } = 0.12f;
        public float SoftFraction        { get; set; } = 0.20f;

        public RadiantGenerator(CharMapping? mapping = null) : base(mapping) { }

        protected override void GenerateInternal(FightArea area)
        {
            var seedRng = new Random(Seed);
            float offX = (float)seedRng.NextDouble() * 100f;
            float offY = (float)seedRng.NextDouble() * 100f;

            float cx = (FightArea.Width  - 1) * 0.5f;
            float cy = (FightArea.Height - 1) * 0.5f;
            // Max possible distance (corner to centre)
            float maxDist = MathF.Sqrt(cx * cx + cy * cy);

            // Pre-compute and normalise noise
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

                // Normalised distance from centre [0..1], shaped by falloff power
                float dx   = x - cx, dy = y - cy;
                float dist = MathF.Sqrt(dx * dx + dy * dy) / maxDist; // 0..1
                float tDist = MathF.Pow(dist, FalloffPower);           // shaped

                // Linearly interpolate threshold: open at centre, dense at edge
                float localDensity = Math.Clamp(
                    CentreDensity + (EdgeDensity - CentreDensity) * tDist,
                    0.05f, 0.95f);

                TerrainType type;
                if (t >= localDensity)
                {
                    bool isProtected = FightArea.IsInReservedZone(x, y) ||
                                       (Math.Abs(x - FightArea.ExitCol) <= 2 &&
                                        Math.Abs(y - FightArea.ExitRow) <= 2);
                    type = isProtected ? TerrainType.FreeSpace : TerrainType.HardObstacle;
                }
                else
                {
                    float passable = t / localDensity;
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
