// SettlementTable.cs — what people make of each kind of livable ground.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Glyph.Microworld
{
    /// <summary>
    /// The locations the settlement sprawl may put on each livable biome: what is grown there, what
    /// people live in, what stock they keep. One column per livable biome, drawn at random by the
    /// sprawl (<c>SettlementSprawl</c>) from the column of the cell it lands on.
    ///
    /// <para><b>A name is a layout.</b> Two locations sharing their last word — "radish field" and
    /// "turnip field" — are built by the same factory and differ only in what grows there; "radish
    /// field" and "orange grove" are built by different ones (<see cref="FamilyOf"/>). Synonyms among
    /// the settlements are deliberate: a burg is the cold country's stone-and-timber village, a hamlet
    /// and a townlet are the hot steppe's mudbrick ones, and the jungle's fort is a stone outpost.</para>
    ///
    /// <para><b>Flavour, not an economy.</b> A burg is built as though potato fields lay around it,
    /// because in mountain and snowfield they often do; nothing checks that they really are.</para>
    /// </summary>
    public static class SettlementTable
    {
        /// <summary>The layout families of the agriculture locations: one factory each.</summary>
        public enum Family { Field, Orchard, Grove, Plantation, Cellar, Garden, Paddy, Vineyard }

        /// <summary>What is grown on each livable biome.</summary>
        public static readonly IReadOnlyDictionary<string, string[]> Agriculture = new Dictionary<string, string[]>
        {
            ["plain"] = new[]
            {
                "hop field", "wheat field", "barley field", "cabbage field", "tomato field", "pear orchard",
                "cherry orchard", "carrot field", "apple orchard", "onion field", "turnip field", "radish field",
            },
            ["mountain"] = new[]
            {
                "apple orchard", "onion field", "carrot field", "cabbage field", "potato field",
                "mushroom cellar", "endive cellar", "turnip field", "radish field",
            },
            [BiomeDatabase.HotSteppe] = new[]
            {
                "vineyard", "olive grove", "cotton field", "citrus grove", "almond grove", "corn field",
                "wheat field", "spice plantation",
            },
            [BiomeDatabase.ColdSteppe] = new[]
            {
                "potato field", "barley field", "oat field", "hay field", "pea field", "cabbage field",
                "wheat field", "bean field", "rye field", "radish field",
            },
            [BiomeDatabase.Jungle] = new[]
            {
                "banana plantation", "sugarcane plantation", "mango orchard", "rice paddy", "orange grove",
                "tea garden", "coconut grove",
            },
            [BiomeDatabase.Snowfield] = new[]
            {
                "mushroom cellar", "endive cellar", "potato field", "turnip field",
            },
        };

        /// <summary>Where people live on each livable biome.</summary>
        public static readonly IReadOnlyDictionary<string, string[]> Settlements = new Dictionary<string, string[]>
        {
            ["plain"]                  = new[] { "village" },
            ["mountain"]               = new[] { "burg", "village" },
            [BiomeDatabase.HotSteppe]  = new[] { "hamlet", "townlet" },
            [BiomeDatabase.ColdSteppe] = new[] { "village" },
            [BiomeDatabase.Jungle]     = new[] { "fort" },
            [BiomeDatabase.Snowfield]  = new[] { "burg" },
        };

        /// <summary>What stock is kept on each livable biome. Jungle and snowfield keep none.</summary>
        public static readonly IReadOnlyDictionary<string, string[]> Livestock = new Dictionary<string, string[]>
        {
            ["plain"]                  = new[] { "farm", "stable" },
            ["mountain"]               = new[] { "sheepfold" },
            [BiomeDatabase.HotSteppe]  = new[] { "ranch" },
            [BiomeDatabase.ColdSteppe] = new[] { "pasture" },
            [BiomeDatabase.Jungle]     = Array.Empty<string>(),
            [BiomeDatabase.Snowfield]  = Array.Empty<string>(),
        };

        /// <summary>The location around an urban place, on any city ground.</summary>
        public const string City = "city";

        /// <summary>A ruined historical place, whatever it was.</summary>
        public const string Ruin = "ruin";

        public static IEnumerable<string> AllAgriculture => Agriculture.Values.SelectMany(v => v).Distinct().OrderBy(s => s, StringComparer.Ordinal);
        public static IEnumerable<string> AllSettlements => Settlements.Values.SelectMany(v => v).Distinct().OrderBy(s => s, StringComparer.Ordinal);
        public static IEnumerable<string> AllLivestock   => Livestock.Values.SelectMany(v => v).Distinct().OrderBy(s => s, StringComparer.Ordinal);

        /// <summary>The layout family an agriculture location is built by: its last word.</summary>
        public static Family FamilyOf(string agriculture)
        {
            string last = agriculture[(agriculture.LastIndexOf(' ') + 1)..];
            return last switch
            {
                "field"      => Family.Field,
                "orchard"    => Family.Orchard,
                "grove"      => Family.Grove,
                "plantation" => Family.Plantation,
                "cellar"     => Family.Cellar,
                "garden"     => Family.Garden,
                "paddy"      => Family.Paddy,
                "vineyard"   => Family.Vineyard,
                _ => throw new ArgumentException($"'{agriculture}' names no agriculture family"),
            };
        }

        /// <summary>
        /// What an agriculture location grows, as the factory reads it: the name before the family
        /// word — "radish" for "radish field". A vineyard is all vine and has no first word.
        /// </summary>
        public static string CropOf(string agriculture)
        {
            int space = agriculture.LastIndexOf(' ');
            return space < 0 ? "vine" : agriculture[..space];
        }
    }
}
