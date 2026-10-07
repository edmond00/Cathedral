using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using Cathedral.Fight;
using Cathedral.Fight.Generators;

namespace Cathedral.Glyph.Microworld
{
    public struct BiomeType
    {
        public string Name { get; set; }
        public char Glyph { get; set; }
        public Vector3 Color { get; set; } // RGB values 0-255
        public float Size { get; set; }
        public float Density { get; set; } // Ratio for location spawning

        /// <summary>
        /// Factory that produces the fight-arena generator used when a travel encounter
        /// happens in this biome. The caller passes a seed (e.g. Environment.TickCount)
        /// and gets a fully-configured generator back. Mandatory.
        /// </summary>
        public Func<int, IFightAreaGenerator> ArenaGeneratorFactory { get; set; }

        public BiomeType(string name, char glyph, Vector3 color, float size, float density,
                          Func<int, IFightAreaGenerator> arenaGeneratorFactory)
        {
            Name = name;
            Glyph = glyph;
            Color = color;
            Size = size;
            Density = density;
            ArenaGeneratorFactory = arenaGeneratorFactory
                ?? throw new ArgumentNullException(nameof(arenaGeneratorFactory));
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
            ["plain"]    = new BiomeType("plain",    '"', new Vector3(0,   255, 0  ), 1.3f, 0.06f, seed => new NoisyGenerator    { Seed = seed, Density = 0.85f }),
            ["forest"]   = new BiomeType("forest",   '⬤', new Vector3(0,   85,  0  ), 1.3f, 0.03f, seed => new NoisyGenerator    { Seed = seed, Density = 0.62f }),
            ["mountain"] = new BiomeType("mountain", '◭', new Vector3(130, 130, 130), 1.3f, 0.05f, seed => new WaveGenerator     { Seed = seed }),
            ["peak"]     = new BiomeType("peak",     '⋀', new Vector3(255, 255, 255), 1.3f, 0.2f,  seed => new WaveGenerator     { Seed = seed }),
            ["coast"]    = new BiomeType("coast",    ':', new Vector3(80,  200, 0  ), 1.3f, 0.2f,  seed => new RadiantGenerator  { Seed = seed }),
            ["sea"]      = new BiomeType("sea",      '~', new Vector3(30,  30,  225), 1f,   0.01f, seed => new WaveGenerator     { Seed = seed }),
            ["ocean"]    = new BiomeType("ocean",    '≈', new Vector3(10,  10,  200), 1f,   0.02f, seed => new WaveGenerator     { Seed = seed }),

            // ── Climate biomes ──────────────────────────────────────────────────────────
            // Never proposed by the noise directly: ClimateRule turns a temperate biome into one of
            // these where the climate layer reads hot or cold. No location may stand on any of them
            // yet — hot and cold country is unsettled until it is given settlements of its own.
            // Colours follow the map's convention: the sphere shader keeps nature to a grey scale and
            // reads only the luminance. Desert and hot steppe are the exception: they are drawn with
            // the field's colour and tint (as the coast is), so the hot country reads warm rather than
            // as snow. Sea ice and cold steppe share one pale grey.
            [HotSteppe]  = new BiomeType(HotSteppe,  '↼', new Vector3(80,  200, 0  ), 1.3f, 0.05f, seed => new NoisyGenerator    { Seed = seed, Density = 0.82f }),
            [Desert]     = new BiomeType(Desert,     '∩', new Vector3(80,  200, 0  ), 1.2f, 0.05f, seed => new RadiantGenerator  { Seed = seed, CentreDensity = 0.95f, EdgeDensity = 0.70f }),
            [Jungle]     = new BiomeType(Jungle,     '♠', new Vector3(0,   85,  0  ), 1.3f, 0.05f, seed => new NoisyGenerator    { Seed = seed, Density = 0.52f }),
            [Canyon]     = new BiomeType(Canyon,     '⇌', new Vector3(130, 130, 130), 1.2f, 0.05f, seed => new CorridorGenerator { Seed = seed }),
            [SeaIce]     = new BiomeType(SeaIce,     '⬟', new Vector3(255, 255, 0  ), 1.1f, 0.05f, seed => new GeometricGenerator{ Seed = seed }),
            [Glacier]    = new BiomeType(Glacier,    '⁂', new Vector3(255, 255, 255), 1.3f, 0.05f, seed => new WaveGenerator     { Seed = seed }),
            [Snowfield]  = new BiomeType(Snowfield,  '⁘', new Vector3(255, 255, 255), 1.3f, 0.05f, seed => new NoisyGenerator    { Seed = seed, Density = 0.9f }),
            [ColdSteppe] = new BiomeType(ColdSteppe, '↽', new Vector3(255, 255, 0  ), 1.3f, 0.05f, seed => new NoisyGenerator    { Seed = seed, Density = 0.86f }),
        };

        // The climate biomes' names, as constants: ClimateRule writes them and a dozen tables key on
        // them, and a typo in one of those would be a biome with no travel cost and no scene.
        public const string HotSteppe  = "hot steppe";
        public const string Desert     = "desert";
        public const string Jungle     = "jungle";
        public const string Canyon     = "canyon";
        public const string SeaIce     = "sea ice";
        public const string Glacier    = "glacier";
        public const string Snowfield  = "snowfield";
        public const string ColdSteppe = "cold steppe";

        public static readonly Dictionary<string, LocationType> Locations = new Dictionary<string, LocationType>
        {
            // Farms, villages and every other settled place are not drawn per vertex any more: they are
            // put on the map by history and its sprawl (see RegisterSettledLocations below).
            ["cave"] = new LocationType("cave", '⟑', new Vector3(100, 100, 100), 1.3f, new HashSet<string> { "mountain" }),
            // ["church"] = new LocationType("church", '☨', new Vector3(100, 100, 100), 1.3f, new HashSet<string> { "plain", "field", "city" }),
            // ["dungeon"] = new LocationType("dungeon", '⍝', new Vector3(60, 60, 60), 1.3f, new HashSet<string> { "mountain" }),
            // ["castle"] = new LocationType("castle", '⚄', new Vector3(150, 150, 150), 1.3f, new HashSet<string> { "mountain", "plain" }),
            // ["observatory"] = new LocationType("observatory", '⍡', new Vector3(150, 100, 100), 1.3f, new HashSet<string> { "mountain", "coast" }),
            // ["stable"] = new LocationType("stable", '⑈', new Vector3(150, 100, 100), 1.3f, new HashSet<string> { "plain", "field" }),
            // // ["stable"] = new LocationType("stable", '♞', new Vector3(150, 100, 100), 1.3f, new HashSet<string> { "plain", "field" }),
            // ["grove"] = new LocationType("grove", '♣', new Vector3(0, 85, 0), 1.3f, new HashSet<string> { "forest" }),
            // ["amphitheater"] = new LocationType("amphitheater", '♫', new Vector3(220, 120, 80), 1.3f, new HashSet<string> { "city" }),
            // ["monastery"] = new LocationType("monastery", '◈', new Vector3(100, 100, 100), 1.3f, new HashSet<string> { "peak" }),
            // ["mine"] = new LocationType("mine", '⟁', new Vector3(100, 100, 100), 1.3f, new HashSet<string> { "mountain" }),
            // ["shrine"] = new LocationType("shrine", '♆', new Vector3(100, 100, 100), 1.3f, new HashSet<string> { "forest" }),
            // ["tavern"] = new LocationType("tavern", '⁌', new Vector3(220, 120, 80), 1.3f, new HashSet<string> { "plain", "city", "field", "coast" }),
            // ["catacombs"] = new LocationType("catacombs", '◙', new Vector3(100, 100, 100), 1.3f, new HashSet<string> { "mountain" }),
            // ["market"] = new LocationType("market", '☵', new Vector3(220, 120, 80), 1.3f, new HashSet<string> { "city", "field" }),
            // ["stadium"] = new LocationType("stadium", '⏣', new Vector3(180, 110, 110), 1.3f, new HashSet<string> { "city" }),
            // ["forum"] = new LocationType("forum", '⌬', new Vector3(220, 120, 80), 1.3f, new HashSet<string> { "city" }),
            // ["academy"] = new LocationType("academy", 'Ω', new Vector3(180, 110, 110), 1.3f, new HashSet<string> { "city" }),
            // ["sanatorium"] = new LocationType("sanatorium", 'Θ', new Vector3(180, 110, 110), 1.3f, new HashSet<string> { "city", "field" }),
            // ["cathedral"] = new LocationType("cathedral", 'Ψ', new Vector3(180, 110, 110), 1.3f, new HashSet<string> { "city" }),
            // ["institute"] = new LocationType("institute", 'Φ', new Vector3(180, 110, 110), 1.3f, new HashSet<string> { "city", "field" }),
            // ["library"] = new LocationType("library", 'ω', new Vector3(180, 110, 110), 1.3f, new HashSet<string> { "city" }),
            // ["bank"] = new LocationType("bank", '$', new Vector3(220, 120, 80), 1.3f, new HashSet<string> { "city" }),
            // ["port"] = new LocationType("port", '⁜', new Vector3(220, 120, 80), 1.1f, new HashSet<string> { "coast" }),
            // ["forge"] = new LocationType("forge", '▟', new Vector3(130, 130, 130), 0.8f, new HashSet<string> { "city", "field" }),
            // ["workshop"] = new LocationType("workshop", '▙', new Vector3(150, 100, 100), 0.8f, new HashSet<string> { "city", "field" }),
            // ["reef"] = new LocationType("reef", '▴', new Vector3(130, 130, 130), 1.3f, new HashSet<string> { "ocean" }),
            // ["mist"] = new LocationType("mist", '▒', new Vector3(40, 40, 150), 1.3f, new HashSet<string> { "ocean" }),
            // ["haze"] = new LocationType("haze", '░', new Vector3(40, 40, 130), 1.3f, new HashSet<string> { "ocean" }),
            // ["snowfield"] = new LocationType("snowfield", '⣿', new Vector3(255, 255, 255), 1.3f, new HashSet<string> { "peak" }),
            // ["ice_lake"] = new LocationType("ice_lake", '≋', new Vector3(255, 255, 255), 1.3f, new HashSet<string> { "peak" }),
            // ["lake"] = new LocationType("lake", '≋', new Vector3(0, 200, 200), 1.3f, new HashSet<string> { "plain", "forest" }),
            // ["ruins"] = new LocationType("ruins", '⑆', new Vector3(20, 55, 20), 1.3f, new HashSet<string> { "forest" }),
            // ["sunken_city"] = new LocationType("sunken_city", '☷', new Vector3(50, 50, 100), 1.3f, new HashSet<string> { "ocean" }),
            // ["ancient_city"] = new LocationType("ancient_city", '☷', new Vector3(255, 255, 255), 1.3f, new HashSet<string> { "peak" }),
            // ["swamp"] = new LocationType("swamp", '≋', new Vector3(40, 85, 0), 1.3f, new HashSet<string> { "forest", "coast" }),
            // ["circus"] = new LocationType("circus", 'ʘ', new Vector3(220, 120, 80), 1.3f, new HashSet<string> { "field" }),
            // ["ice_cave"] = new LocationType("ice_cave", '⟑', new Vector3(255, 255, 255), 1.3f, new HashSet<string> { "peak" }),
            // ["oasis"] = new LocationType("oasis", '♠', new Vector3(0, 85, 40), 1.3f, new HashSet<string> { "forest" }),
            // ["assassin_guild"] = new LocationType("assassin_guild", '⬖', new Vector3(180, 60, 40), 1.3f, new HashSet<string> { "city" }),
            // ["villa"] = new LocationType("villa", '◈', new Vector3(220, 120, 80), 1.3f, new HashSet<string> { "city" }),
        };

        // ── Settled locations ──
        // Placed by history (its places) and by the settlement sprawl around them, never drawn per
        // vertex: their AllowedBiomes are empty so DetermineLocation cannot pick them.

        /// <summary>The map glyph of each agriculture layout family.</summary>
        /// <summary>
        /// One glyph per settled location, never shared: the map must tell a radish field from a turnip
        /// field at a glance. Glyphs that look alike mark locations that are alike - farmland is
        /// braille (fields dense, orchards and groves in the right-hand column, plantations sparse) save
        /// the cellars, which are solid blocks; villages and stock the small circled and barred marks,
        /// towns and cities the trigrams, great buildings and the fort the boxed and ringed shapes. <c>--world-variant-audit</c> refuses a repeat.
        /// </summary>
        private static readonly Dictionary<string, char> SettledGlyphs = new()
        {
            // fields
            ["wheat field"] = '⣿', ["barley field"] = '⣾', ["rye field"] = '⣽', ["oat field"] = '⣻',
            ["corn field"] = '⣷', ["hay field"] = '⣯', ["hop field"] = '⣟', ["cotton field"] = '⣶',
            ["pea field"] = '⣵', ["bean field"] = '⣳', ["cabbage field"] = '⣮', ["carrot field"] = '⣭',
            ["onion field"] = '⣫', ["turnip field"] = '⣝', ["radish field"] = '⣛', ["potato field"] = '⣗',
            ["tomato field"] = '⣞',
            // orchards and groves
            ["apple orchard"] = '⢷', ["pear orchard"] = '⢾', ["cherry orchard"] = '⢿', ["mango orchard"] = '⢽',
            ["olive grove"] = '⢹', ["citrus grove"] = '⢺', ["almond grove"] = '⢻', ["orange grove"] = '⢼',
            ["coconut grove"] = '⢸',
            // plantations, garden, paddy, vineyard; the cellars as the solid blocks they are dug into
            ["banana plantation"] = '⠷', ["sugarcane plantation"] = '⠾', ["spice plantation"] = '⠿',
            ["mushroom cellar"] = '▰', ["endive cellar"] = '▮', ["tea garden"] = '⠻', ["rice paddy"] = '⠼',
            ["vineyard"] = '⠽',
            // stock
            ["farm"] = '⑇', ["stable"] = '≐', ["sheepfold"] = '≑', ["ranch"] = '≒', ["pasture"] = '≓',
            // where people live
            ["village"] = '⑆', ["burg"] = '⑈', ["hamlet"] = '⑉', ["fort"] = '▦', ["townlet"] = '☴',
            [SettlementTable.City] = '☷',
            // history's places: the urban ones as large settlements, the rest as great buildings
            ["citadel"] = '☰', ["port"] = '☵', ["palace"] = '☲', ["imperial school"] = '☳',
            ["castle"] = '▣', ["fortress"] = '▩', ["temple"] = '◎', ["imperial temple"] = '◉',
            ["commandery"] = '⌬', ["monastery"] = '◈', ["mine"] = '⏣', ["sanctuary"] = '◴',
            ["burial field"] = '▤', ["pyramid"] = '▲', ["wreck"] = '⍾',
            [SettlementTable.Ruin] = '◲',
        };

        /// <summary>The history-placed location keys: every kind of historical place, and the ruin.</summary>
        public static readonly string[] HistoricLocations =
        {
            "citadel", "port", "palace", "imperial school", "castle", "fortress", "temple", "imperial temple",
            "commandery", "monastery", "mine", "sanctuary", "burial field", "pyramid", "wreck",
        };

        /// <summary>Agriculture and livestock: drawn with the field's warm tint on the sphere.</summary>
        public static readonly HashSet<string> FarmlandLocations = new();

        static BiomeDatabase()
        {
            var tilled = new Vector3(80, 200, 0);
            var built = new Vector3(150, 100, 100);
            foreach (var name in SettlementTable.AllAgriculture)
            {
                Locations[name] = new LocationType(name, SettledGlyphs[name], tilled, 1.2f, new HashSet<string>());
                FarmlandLocations.Add(name);
            }
            foreach (var name in SettlementTable.AllLivestock)
            {
                Locations[name] = new LocationType(name, SettledGlyphs[name], tilled, 1.2f, new HashSet<string>());
                FarmlandLocations.Add(name);
            }
            foreach (var name in SettlementTable.AllSettlements.Append(SettlementTable.City).Concat(HistoricLocations))
            {
                Locations[name] = new LocationType(name, SettledGlyphs[name], built, 1.3f, new HashSet<string>());
                HumanLocations.Add(name);
            }
            Locations[SettlementTable.Ruin] = new LocationType(SettlementTable.Ruin, SettledGlyphs[SettlementTable.Ruin],
                                                               new Vector3(120, 120, 120), 1.2f, new HashSet<string>());
        }

        // ── Tile category sets (used by the world sphere shader for coloring) ──

        /// <summary>Biomes rendered in dark purple on the world sphere.</summary>
        public static readonly HashSet<string> WaterBiomes = new HashSet<string> { "sea", "ocean" };

        /// <summary>
        /// The high ground. <c>DetermineBiome</c> tests the mountain noise layer BEFORE the field
        /// and city thresholds, so on these tiles the settlement layer decides nothing — which is
        /// why the region division will grow across them but never centre a region on one.
        /// </summary>
        /// <para>The climate's high ground belongs here for the same reason: hot steppe, snowfield and
        /// glacier are what a mountain or a peak becomes, and the settlement layer was never read there.</para>
        public static readonly HashSet<string> MountainBiomes = new HashSet<string>
        {
            "mountain", "peak", HotSteppe, Snowfield, Glacier,
        };

        /// <summary>
        /// The ground people live on: where history's urban and rural places may stand and where their
        /// farmland and settlements sprawl (see <c>PlaceSites</c> and <c>SettlementSprawl</c>). Forest is
        /// left wild on purpose, and so is every barren biome — desert, canyon, coast, peak, glacier,
        /// sea ice — which hold isolated places only.
        /// </summary>
        public static readonly HashSet<string> LivableBiomes = new HashSet<string>
        {
            "plain", "mountain", HotSteppe, ColdSteppe, Jungle, Snowfield,
        };

        /// <summary>The livable ground a city may stand on: all of it but jungle and snowfield.</summary>
        public static readonly HashSet<string> CityBiomes = new HashSet<string>
        {
            "plain", "mountain", HotSteppe, ColdSteppe,
        };

        /// <summary>The biomes <see cref="ClimateRule"/> produces. None of them carries a location yet.</summary>
        public static readonly HashSet<string> ClimateBiomes = new HashSet<string>
        {
            HotSteppe, Desert, Jungle, Canyon, SeaIce, Glacier, Snowfield, ColdSteppe,
        };

        /// <summary>
        /// The shore. Proposed by the water noise and then checked against the map by
        /// <see cref="CoastRule"/>: a cell keeps this only if it really borders sea or ocean.
        /// </summary>
        public static readonly HashSet<string> CoastBiomes = new HashSet<string> { "coast" };

        /// <summary>Locations rendered in dark purple on the world sphere.</summary>
        public static readonly HashSet<string> WaterLocations = new HashSet<string>
        {
            "lake", "reef", "mist", "haze", "sunken_city"
        };

        /// <summary>Locations rendered in dark yellow on the world sphere.</summary>
        public static readonly HashSet<string> HumanLocations = new HashSet<string>
        {
            "village", "castle", "church", "stable", "farm", "tavern", "market", "forum",
            "bank", "port", "forge", "workshop", "amphitheater", "stadium", "academy",
            "sanatorium", "cathedral", "institute", "library", "villa", "circus",
            "assassin_guild", "observatory", "monastery"
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