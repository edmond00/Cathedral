using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;

namespace Cathedral.Glyph.Microworld
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
            ["plain"] = new BiomeType("plain", '"', new Vector3(0, 255, 0), 1.3f, 0.06f),
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
            ["catacombs"] = new LocationType("catacombs", '◙', new Vector3(100, 100, 100), 1.3f, new HashSet<string> { "mountain" }),
            ["market"] = new LocationType("market", '☵', new Vector3(220, 120, 80), 1.3f, new HashSet<string> { "city", "field" }),
            ["stadium"] = new LocationType("stadium", '⏣', new Vector3(180, 110, 110), 1.3f, new HashSet<string> { "city" }),
            ["forum"] = new LocationType("forum", '⌬', new Vector3(220, 120, 80), 1.3f, new HashSet<string> { "city" }),
            ["academy"] = new LocationType("academy", 'Ω', new Vector3(180, 110, 110), 1.3f, new HashSet<string> { "city" }),
            ["sanatorium"] = new LocationType("sanatorium", 'Θ', new Vector3(180, 110, 110), 1.3f, new HashSet<string> { "city", "field" }),
            ["cathedral"] = new LocationType("cathedral", 'Ψ', new Vector3(180, 110, 110), 1.3f, new HashSet<string> { "city" }),
            ["institute"] = new LocationType("institute", 'Φ', new Vector3(180, 110, 110), 1.3f, new HashSet<string> { "city", "field" }),
            ["library"] = new LocationType("library", 'ω', new Vector3(180, 110, 110), 1.3f, new HashSet<string> { "city" }),
            ["bank"] = new LocationType("bank", '$', new Vector3(220, 120, 80), 1.3f, new HashSet<string> { "city" }),
            ["port"] = new LocationType("port", '⁜', new Vector3(220, 120, 80), 1.1f, new HashSet<string> { "coast" }),
            ["forge"] = new LocationType("forge", '▟', new Vector3(130, 130, 130), 0.8f, new HashSet<string> { "city", "field" }),
            ["workshop"] = new LocationType("workshop", '▙', new Vector3(150, 100, 100), 0.8f, new HashSet<string> { "city", "field" }),
            ["reef"] = new LocationType("reef", '▴', new Vector3(130, 130, 130), 1.3f, new HashSet<string> { "ocean" }),
            ["mist"] = new LocationType("mist", '▒', new Vector3(40, 40, 150), 1.3f, new HashSet<string> { "ocean" }),
            ["haze"] = new LocationType("haze", '░', new Vector3(40, 40, 130), 1.3f, new HashSet<string> { "ocean" }),
            ["snowfield"] = new LocationType("snowfield", '⣿', new Vector3(255, 255, 255), 1.3f, new HashSet<string> { "peak" }),
            ["ice_lake"] = new LocationType("ice_lake", '≋', new Vector3(255, 255, 255), 1.3f, new HashSet<string> { "peak" }),
            ["lake"] = new LocationType("lake", '≋', new Vector3(0, 200, 200), 1.3f, new HashSet<string> { "plain", "forest" }),
            ["ruins"] = new LocationType("ruins", '⑆', new Vector3(20, 55, 20), 1.3f, new HashSet<string> { "forest" }),
            ["sunken_city"] = new LocationType("sunken_city", '☷', new Vector3(50, 50, 100), 1.3f, new HashSet<string> { "ocean" }),
            ["ancient_city"] = new LocationType("ancient_city", '☷', new Vector3(255, 255, 255), 1.3f, new HashSet<string> { "peak" }),
            ["swamp"] = new LocationType("swamp", '≋', new Vector3(40, 85, 0), 1.3f, new HashSet<string> { "forest", "coast" }),
            ["circus"] = new LocationType("circus", 'ʘ', new Vector3(220, 120, 80), 1.3f, new HashSet<string> { "field" }),
            ["ice_cave"] = new LocationType("ice_cave", '⟑', new Vector3(255, 255, 255), 1.3f, new HashSet<string> { "peak" }),
            ["oasis"] = new LocationType("oasis", '♠', new Vector3(0, 85, 40), 1.3f, new HashSet<string> { "forest" }),
            ["assassin_guild"] = new LocationType("assassin_guild", '⬖', new Vector3(180, 60, 40), 1.3f, new HashSet<string> { "city" }),
            ["villa"] = new LocationType("villa", '◈', new Vector3(220, 120, 80), 1.3f, new HashSet<string> { "city" }),
        };

        /// <summary>
        /// Generates a glyph set string containing all unique glyphs from biomes and locations.
        /// This ensures the atlas includes all necessary characters automatically.
        /// </summary>
        /// <returns>String containing all unique glyphs used by biomes and locations</returns>
        public static string GenerateGlyphSet()
        {
            var glyphs = new HashSet<char>();
            
            // Add all biome glyphs
            foreach (var biome in Biomes.Values)
            {
                glyphs.Add(biome.Glyph);
            }
            
            // Add all location glyphs
            foreach (var location in Locations.Values)
            {
                glyphs.Add(location.Glyph);
            }
            
            // Convert to sorted array for consistent ordering
            var sortedGlyphs = glyphs.OrderBy(g => g).ToArray();
            
            return new string(sortedGlyphs);
        }
    }
}