// ClimateRule.cs — what heat and cold make of the ground the other three layers proposed.
using System;
using System.Collections.Generic;
using static Cathedral.Glyph.Microworld.BiomeDatabase;

namespace Cathedral.Glyph.Microworld
{
    /// <summary>
    /// Turns temperate ground into hot or cold ground where the climate layer
    /// (<see cref="WorldShape.Temperature"/>) crosses a threshold.
    ///
    /// <para><b>Three passes, in this order, and the order is the rule.</b>
    /// <list type="number">
    /// <item><b>Per cell.</b> Above <see cref="HotThreshold"/>: mountain and peak become hot steppe,
    /// plain and field become desert, forest becomes jungle. Below <see cref="ColdThreshold"/>: coast
    /// becomes sea ice, peak glacier, mountain snowfield, plain and field cold steppe. Everything else
    /// is left as it was — a hot shore is still a shore, a cold forest still a forest.</item>
    /// <item><b>Canyon.</b> Any plain or field touching desert becomes canyon: the broken ground
    /// where temperate grass gives out on sand, as the coast lies between land and sea. Only the
    /// temperate side is touched, so the desert keeps its whole extent and gains a rim.</item>
    /// <item><b>Sea ice.</b> Water beside sea ice freezes too if it is colder still, and so on outward —
    /// each ring needs <see cref="SeaIceStep"/> more cold than the last, so the ice runs far out only
    /// where the climate is far below the threshold, and a temperate world's cold corner gets an icy
    /// shore and not a frozen sea.</item>
    /// </list></para>
    ///
    /// <para>Runs after <see cref="CoastRule"/>, so the shore is already exactly the land on the
    /// water: a stranded shore has become plain (and in cold country goes on to cold steppe like any
    /// other plain), and a mountain or wood on the water has become shore (and in cold country goes on
    /// to sea ice). Nothing but shore or sea ice therefore ever borders the sea.</para>
    ///
    /// <para>Shared by the running game, <see cref="HeadlessWorld"/> and <c>--world-variant-audit</c>
    /// through <see cref="WorldClassifier"/>, which is the only caller.</para>
    /// </summary>
    public static class ClimateRule
    {
        /// <summary>
        /// Above this the ground is hot country. Set so that a temperate world (offset zero) keeps a
        /// scatter of small hot pockets over a few percent of its land, and a hot variant's offset puts
        /// most of the world over it. Raise it to make the pockets rarer, and move every climate
        /// variant's offset by the same amount, or the hot and cold worlds shrink with them.
        /// <c>--world-variant-audit</c> prints the shares and the zone counts.
        /// </summary>
        public const float HotThreshold = 0.48f;

        /// <summary>Below this the ground is cold country. The mirror of <see cref="HotThreshold"/>.</summary>
        public const float ColdThreshold = -0.48f;

        /// <summary>How much colder each further ring of frozen sea must be than the one inside it.</summary>
        public const float SeaIceStep = 0.08f;

        public static bool IsHot(float temperature)  => temperature > HotThreshold;
        public static bool IsCold(float temperature) => temperature < ColdThreshold;

        /// <summary>What <paramref name="biome"/> becomes at <paramref name="temperature"/>, cell by cell.</summary>
        public static string Replace(string biome, float temperature)
        {
            if (IsHot(temperature))
            {
                return biome switch
                {
                    "mountain" or "peak" => HotSteppe,
                    "plain" or "field"   => Desert,
                    "forest"             => Jungle,
                    _                    => biome,
                };
            }

            if (IsCold(temperature))
            {
                return biome switch
                {
                    "coast"            => SeaIce,
                    "peak"             => Glacier,
                    "mountain"         => Snowfield,
                    "plain" or "field" => ColdSteppe,
                    _                  => biome,
                };
            }

            return biome;
        }

        /// <summary>The counts of what the three passes did, for the audit.</summary>
        public readonly record struct Report(int Replaced, int Canyon, int FrozenSea);

        /// <summary>Applies all three passes to <paramref name="biome"/> in place.</summary>
        public static Report Apply(string[] biome, float[] temperature, Func<int, IEnumerable<int>> neighbours)
        {
            int n = biome.Length;

            int replaced = 0;
            for (int v = 0; v < n; v++)
            {
                string after = Replace(biome[v], temperature[v]);
                if (after != biome[v]) { biome[v] = after; replaced++; }
            }

            // Canyon. Decided against the map as the first pass left it and written afterwards, so
            // the result does not depend on the order the cells are visited in.
            var canyon = new List<int>();
            for (int v = 0; v < n; v++)
            {
                if (biome[v] is not ("plain" or "field")) continue;
                foreach (int w in neighbours(v))
                    if (biome[w] == Desert) { canyon.Add(v); break; }
            }
            foreach (int v in canyon) biome[v] = Canyon;

            // Sea ice, ring by ring outward from the frozen shore.
            int frozen = 0;
            var frontier = new List<int>();
            for (int v = 0; v < n; v++)
                if (biome[v] == SeaIce) frontier.Add(v);

            for (int ring = 1; frontier.Count > 0; ring++)
            {
                float needed = ColdThreshold - ring * SeaIceStep;
                var next = new List<int>();
                foreach (int v in frontier)
                    foreach (int w in neighbours(v))
                    {
                        if (!WaterBiomes.Contains(biome[w]) || temperature[w] >= needed) continue;
                        biome[w] = SeaIce;
                        next.Add(w);
                        frozen++;
                    }
                frontier = next;
            }

            return new Report(replaced, canyon.Count, frozen);
        }
    }

    /// <summary>
    /// The biome of every vertex of a world, derived once and the same way everywhere: the three
    /// terrain layers through <see cref="WorldShape.BiomeNameFor"/>, the coast held to the sea by
    /// <see cref="CoastRule"/>, then heat and cold by <see cref="ClimateRule"/>.
    ///
    /// <para>Three callers used to do the first two steps each on their own — the game, the headless
    /// world the history is built on, and the variant audit — and every one of them had a comment
    /// saying it must do exactly what the others did. One function is how that stays true.</para>
    /// </summary>
    public static class WorldClassifier
    {
        public sealed class Result
        {
            public required string[] Biome { get; init; }
            public required float[] Settlement { get; init; }
            public required float[] Temperature { get; init; }

            /// <summary>Coast the water noise proposed, before the coast rule.</summary>
            public required int BandCoast { get; init; }

            /// <summary>Of that coast, how much touched no water and became plain.</summary>
            public required int StrandedCoast { get; init; }

            /// <summary>Land on the water that the noise had made something else, and that became shore.</summary>
            public required int ShoredCoast { get; init; }

            public required ClimateRule.Report Climate { get; init; }
        }

        public static Result Classify(WorldShape shape, int vertexCount, Func<int, OpenTK.Mathematics.Vector3> position,
                                      Func<int, IEnumerable<int>> neighbours, OpenTK.Mathematics.Vector3 worldOffset)
        {
            var biome = new string[vertexCount];
            var settlement = new float[vertexCount];
            var temperature = new float[vertexCount];

            for (int v = 0; v < vertexCount; v++)
            {
                var pos = position(v);
                var (water, settle, relief) = shape.Sample(pos, worldOffset);
                biome[v] = shape.BiomeNameFor(water, settle, relief);
                settlement[v] = settle;
                temperature[v] = shape.Temperature(pos, worldOffset);
            }

            int bandCoast = 0;
            foreach (string b in biome) if (CoastBiomes.Contains(b)) bandCoast++;

            var stranded = CoastRule.Stranded(vertexCount, v => biome[v], neighbours);
            foreach (int v in stranded) biome[v] = CoastRule.Replacement;
            var unshored = CoastRule.Unshored(vertexCount, v => biome[v], neighbours);
            foreach (int v in unshored) biome[v] = "coast";

            var climate = ClimateRule.Apply(biome, temperature, neighbours);

            return new Result
            {
                Biome = biome,
                Settlement = settlement,
                Temperature = temperature,
                BandCoast = bandCoast,
                StrandedCoast = stranded.Count,
                ShoredCoast = unshored.Count,
                Climate = climate,
            };
        }
    }
}
