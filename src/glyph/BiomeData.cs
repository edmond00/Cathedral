using System;
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace Cathedral.Glyph
{
    public struct BiomeType
    {
        public string Name { get; set; }
        public char Glyph { get; set; }
        public Vector3 Color { get; set; } // RGB values 0-255
        public float Size { get; set; }
        public float Density { get; set; } // Ratio for location spawning

        public BiomeType(string name, char glyph, Vector3 color, float size, float density)
        {
            Name = name;
            Glyph = glyph;
            Color = color;
            Size = size;
            Density = density;
        }
    }

    public struct LocationType
    {
        public string Name { get; set; }
        public char Glyph { get; set; }
        public Vector3 Color { get; set; } // RGB values 0-255
        public float Size { get; set; }
        public HashSet<string> AllowedBiomes { get; set; }

        public LocationType(string name, char glyph, Vector3 color, float size, HashSet<string> allowedBiomes)
        {
            Name = name;
            Glyph = glyph;
            Color = color;
            Size = size;
            AllowedBiomes = allowedBiomes;
        }
    }

    public static class BiomeDatabase
    {
        public static readonly Dictionary<string, BiomeType> Biomes = new Dictionary<string, BiomeType>
        {
            ["plain"] = new BiomeType("plain", ' ', new Vector3(0, 255, 0), 1.3f, 0.06f),
            ["forest"] = new BiomeType("forest", '⬤', new Vector3(0, 85, 0), 1.3f, 0.03f),
            ["mountain"] = new BiomeType("mountain", '◭', new Vector3(130, 130, 130), 1.3f, 0.05f),
            ["peak"] = new BiomeType("peak", '⋀', new Vector3(255, 255, 255), 1.3f, 0.2f),
            ["coast"] = new BiomeType("coast", ':', new Vector3(255, 255, 0), 1.3f, 0.2f),
            ["city"] = new BiomeType("city", '☷', new Vector3(150, 100, 100), 1.3f, 0.4f),
            ["sea"] = new BiomeType("sea", '~', new Vector3(120, 180, 255), 1.3f, 0.01f),
            ["ocean"] = new BiomeType("ocean", '≈', new Vector3(0, 0, 200), 1.3f, 0.02f),
            ["field"] = new BiomeType("field", '⣿', new Vector3(80, 200, 0), 1.2f, 0.1f),
        };

        public static readonly Dictionary<string, LocationType> Locations = new Dictionary<string, LocationType>
        {
            ["church"] = new LocationType("church", '☨', new Vector3(100, 100, 100), 1.3f, new HashSet<string> { "plain", "field", "city" }),
            ["dungeon"] = new LocationType("dungeon", '⍝', new Vector3(60, 60, 60), 1.3f, new HashSet<string> { "mountain" }),
            ["castle"] = new LocationType("castle", '⚄', new Vector3(150, 150, 150), 1.3f, new HashSet<string> { "mountain", "plain" }),
            ["village"] = new LocationType("village", '⑆', new Vector3(150, 100, 100), 1.3f, new HashSet<string> { "plain" }),
            ["observatory"] = new LocationType("observatory", '⍡', new Vector3(150, 100, 100), 1.3f, new HashSet<string> { "mountain", "coast" }),
            ["stable"] = new LocationType("stable", '⑈', new Vector3(150, 100, 100), 1.3f, new HashSet<string> { "plain", "field" }),
            ["farm"] = new LocationType("farm", '⑇', new Vector3(150, 100, 100), 1.3f, new HashSet<string> { "field" }),
            ["grove"] = new LocationType("grove", '♣', new Vector3(0, 85, 0), 1.3f, new HashSet<string> { "forest" }),
            ["amphitheater"] = new LocationType("amphitheater", '♫', new Vector3(220, 120, 80), 1.3f, new HashSet<string> { "city" }),
            ["monastery"] = new LocationType("monastery", '◈', new Vector3(100, 100, 100), 1.3f, new HashSet<string> { "peak" }),
            ["mine"] = new LocationType("mine", '⟑', new Vector3(100, 100, 100), 1.3f, new HashSet<string> { "mountain" }),
            ["cave"] = new LocationType("cave", '⟁', new Vector3(100, 100, 100), 1.3f, new HashSet<string> { "mountain" }),
            ["shrine"] = new LocationType("shrine", '♆', new Vector3(100, 100, 100), 1.3f, new HashSet<string> { "forest" }),
            ["tavern"] = new LocationType("tavern", '⁌', new Vector3(220, 120, 80), 1.3f, new HashSet<string> { "plain", "city", "field", "coast" }),
            ["market"] = new LocationType("market", '☵', new Vector3(220, 120, 80), 1.3f, new HashSet<string> { "city", "field" }),
            ["library"] = new LocationType("library", 'ω', new Vector3(180, 110, 110), 1.3f, new HashSet<string> { "city" }),
            ["port"] = new LocationType("port", '⁜', new Vector3(220, 120, 80), 1.1f, new HashSet<string> { "coast" }),
            ["forge"] = new LocationType("forge", '▟', new Vector3(130, 130, 130), 0.8f, new HashSet<string> { "city", "field" }),
            ["lake"] = new LocationType("lake", '≋', new Vector3(0, 200, 200), 1.3f, new HashSet<string> { "plain", "forest" }),
            ["ruins"] = new LocationType("ruins", '⑆', new Vector3(20, 55, 20), 1.3f, new HashSet<string> { "forest" }),
        };
    }
}