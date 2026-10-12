// SpawnRule.cs — where a run may begin.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Glyph.Microworld
{
    /// <summary>
    /// The cells a run may wake on. Shared by <c>MicroworldInterface.InitializeProtagonist</c> and
    /// <c>--world-variant-audit</c>, so the audit measures the spawn the game makes rather than a
    /// description of it.
    ///
    /// <para><b>Next to the sprawl.</b> A run begins on open ground with no location on it, beside the
    /// peopled country that history's places grew round them (<c>SettlementSprawl</c>): a field's
    /// edge, a village's outskirts. That is where a traveller can be from, and where the first journey
    /// leads somewhere. Since no route on foot crosses water, waking beside the sprawl also means
    /// waking on a landmass that has some.</para>
    ///
    /// <para><b>Three tiers, the first that yields any wins.</b>
    /// <list type="number">
    /// <item>Open ground — livable, or shore — free of any location and bordering sprawl.</item>
    /// <item>Open ground free of any location anywhere, for a world history left empty.</item>
    /// <item>Any land at all, rather than a game that will not begin.</item>
    /// </list></para>
    /// </summary>
    public static class SpawnRule
    {
        /// <summary>Ground a traveller can be set down on: livable, or the shore.</summary>
        public static bool IsOpenGround(string? biome)
            => biome != null && (BiomeDatabase.LivableBiomes.Contains(biome) || BiomeDatabase.CoastBiomes.Contains(biome));

        /// <summary>The cells a run may begin on, sorted ascending.</summary>
        /// <param name="hasLocation">Whether a location (of any kind) stands on the cell.</param>
        /// <param name="isSprawl">Whether the cell is sprawled country: a city, farmland, a settlement or stock.</param>
        public static List<int> Candidates(int vertexCount, Func<int, string?> biomeAt,
                                           Func<int, IEnumerable<int>> neighbours,
                                           Func<int, bool> hasLocation, Func<int, bool> isSprawl)
        {
            var beside = new List<int>();
            var open = new List<int>();
            var land = new List<int>();
            for (int v = 0; v < vertexCount; v++)
            {
                string? b = biomeAt(v);
                if (b == null || BiomeDatabase.WaterBiomes.Contains(b)) continue;
                land.Add(v);
                if (!IsOpenGround(b) || hasLocation(v)) continue;
                open.Add(v);
                if (neighbours(v).Any(isSprawl)) beside.Add(v);
            }
            if (beside.Count > 0) return beside;
            if (open.Count > 0) return open;
            return land;
        }

        /// <summary>Connected components of the land graph — the isles and continents.</summary>
        public static (int[] Of, int Count) Landmasses(int n, Func<int, IEnumerable<int>> neighbours, Func<int, bool> isLand)
        {
            var of = new int[n];
            Array.Fill(of, -1);
            int count = 0;
            var stack = new Stack<int>();

            for (int start = 0; start < n; start++)
            {
                if (of[start] >= 0 || !isLand(start)) continue;
                int id = count++;
                stack.Push(start);
                of[start] = id;
                while (stack.Count > 0)
                {
                    int v = stack.Pop();
                    foreach (int w in neighbours(v))
                    {
                        if (of[w] >= 0 || !isLand(w)) continue;
                        of[w] = id;
                        stack.Push(w);
                    }
                }
            }
            return (of, count);
        }
    }
}
