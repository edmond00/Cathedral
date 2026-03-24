using System;
using Cathedral.Glyph;

namespace Cathedral.Fight.Generators
{
    /// <summary>
    /// Organic noise map where the wall-density threshold oscillates vertically like a
    /// sine wave.  This creates alternating horizontal bands of dense walls and open
    /// space, giving the feel of terrain layers (ridges, valleys, corridors between cliffs).
    /// </summary>
    public class WaveGenerator : FightAreaGeneratorBase
    {
        /// <summary>Mid-point noise threshold (cells above it = wall at the flat parts of the wave).</summary>
        public float BaseDensity  { get; set; } = 0.72f;

        /// <summary>How far the threshold swings above/below BaseDensity (0 = flat = same as NoisyGenerator).</summary>
        public float Amplitude    { get; set; } = 0.26f;

        /// <summary>Number of full sine cycles from top to bottom of the map.</summary>
        public float WaveFreq     { get; set; } = 2.5f;

        /// <summary>Phase offset (radians); randomised per seed for variety.</summary>
        public float PhaseOffset  { get; set; } = 0f;

        public float NoiseScale { get; set; } = 0.12f;
        public int   Seed       { get; set; } = 0;

        public float DangerousFraction   { get; set; } = 0.016f;
        public float TreacherousFraction { get; set; } = 0.12f;
        public float SoftFraction        { get; set; } = 0.20f;

        public WaveGenerator(CharMapping? mapping = null) : base(mapping) { }

        protected override void GenerateInternal(FightArea area)
        {
            var seedRng = new Random(Seed);
            float offX = (float)seedRng.NextDouble() * 100f;
            float offY = (float)seedRng.NextDouble() * 100f;

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
            {
                // Sine wave makes the wall threshold rise/fall as we move down the map
                float wavePhase = (2f * MathF.PI * WaveFreq * y / FightArea.Height) + PhaseOffset;
                float localDensity = Math.Clamp(BaseDensity + Amplitude * MathF.Sin(wavePhase), 0.05f, 0.95f);

                for (int x = 0; x < FightArea.Width; x++)
                {
                    float t = (noise[x, y] - min) / range; // 0..1

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
}
