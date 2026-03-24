using System.Collections.Generic;
using OpenTK.Mathematics;

namespace Cathedral.Fight
{
    /// <summary>
    /// Maps each TerrainType to its visual appearance (glyph pool + colors).
    /// </summary>
    public class CharMapping
    {
        private readonly Dictionary<TerrainType, TerrainAppearance> _map;

        public CharMapping(Dictionary<TerrainType, TerrainAppearance> map) => _map = map;

        public TerrainAppearance this[TerrainType type] => _map[type];

        public static CharMapping Default { get; } = new CharMapping(new Dictionary<TerrainType, TerrainAppearance>
        {
            [TerrainType.Exit] = new TerrainAppearance(
                new[] { '⎆' },
                new Vector4(0.88f, 0.72f, 0.12f, 1.0f),      // Muted gold (blinks)
                new Vector4(0.16f, 0.11f, 0.0f, 1.0f)        // Dark warm yellow bg
            ),
            [TerrainType.HardObstacle] = new TerrainAppearance(
                new[] { '#', '█', '▓', '▒' },
                new Vector4(0.52f, 0.50f, 0.46f, 1.0f),      // Warm medium gray
                new Vector4(0.20f, 0.19f, 0.17f, 1.0f)       // Dark warm gray bg
            ),
            [TerrainType.SoftObstacle] = new TerrainAppearance(
                new[] { '^', '+', ',', '"', '\'' },
                new Vector4(0.44f, 0.42f, 0.24f, 1.0f),      // Muted dark olive
                new Vector4(0.10f, 0.09f, 0.04f, 1.0f)       // Very dark olive bg
            ),
            [TerrainType.TreacherousTerrain] = new TerrainAppearance(
                new[] { '~', '≈', '∼' },
                new Vector4(0.50f, 0.46f, 0.20f, 1.0f),      // Muted dull yellow-brown
                new Vector4(0.13f, 0.11f, 0.02f, 1.0f)       // Very dark yellow bg
            ),
            [TerrainType.DangerousTerrain] = new TerrainAppearance(
                new[] { '!', '⁘', '*', '%' },
                new Vector4(0.55f, 0.33f, 0.16f, 1.0f),      // Muted dark rust
                new Vector4(0.15f, 0.06f, 0.01f, 1.0f)       // Very dark rust bg
            ),
            [TerrainType.FreeSpace] = new TerrainAppearance(
                new[] { '.', '-', ' ' },                      // Mostly spaces for readability
                new Vector4(0.22f, 0.20f, 0.17f, 1.0f),      // Near-black warm gray
                new Vector4(0.0f, 0.0f, 0.0f, 1.0f)          // Black bg
            ),
        });
    }
}
