// SpawnRule.cs — where a run may begin.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Glyph.Microworld
{
    /// <summary>
    /// The cells a run may wake on. Shared by <c>MicroworldInterface.InitializeProtagonist</c> and
    /// <c>--world-variant-audit</c>, so the audit measures the spawn the game makes rather than a
    /// description of it — the two had drifted apart once already: the audit and the manual both
    /// described a landmass rule the game never applied.
    ///
    /// <para><b>Three tiers, the first that yields enough wins.</b>
    /// <list type="number">
    /// <item>Open temperate ground — plain, field, coast — on a landmass carrying at least
    /// <c>Config.WorldRegions.FieldsForAHome</c> fields. Since no route on foot crosses water, the
    /// landmass under the spawn is the whole of that run's world, and one without fields holds no
    /// farm and no village.</item>
    /// <item>Where that is scarce — a hot or cold world, whose fields have turned to desert or cold
    /// steppe — the climate's open ground joins it, on any landmass of real size. Such a world is
    /// unsettled for now by design, so the field rule cannot be what decides.</item>
    /// <item>Failing both, any open ground at all, rather than a game that will not begin.</item>
    /// </list></para>
    /// </summary>
    public static class SpawnRule
    {
        /// <summary>Temperate open ground: where a run begins in any world that has enough of it.</summary>
        public static readonly HashSet<string> TemperateGround = new() { "plain", "field", "coast" };

        /// <summary>The climate's open ground, taken only when the temperate kind is scarce.</summary>
        public static readonly HashSet<string> ClimateGround = new()
        {
            BiomeDatabase.Desert, BiomeDatabase.ColdSteppe, BiomeDatabase.Canyon,
            BiomeDatabase.HotSteppe, BiomeDatabase.Snowfield,
        };

        /// <summary>How many first-tier cells a world needs before the second tier is not consulted.</summary>
        public const int MinTemperateSpawns = 150;

        /// <summary>The land cells a landmass needs to be worth waking on in a world without fields.</summary>
        public const int LandForAHome = 300;

        /// <summary>Every cell of open ground of either kind — the set the tiers choose from.</summary>
        public static bool IsOpenGround(string? biome)
            => biome != null && (TemperateGround.Contains(biome) || ClimateGround.Contains(biome));

        /// <summary>The cells a run may begin on, sorted ascending.</summary>
        public static List<int> Candidates(int vertexCount, Func<int, string?> biomeAt,
                                           Func<int, IEnumerable<int>> neighbours)
        {
            var (massOf, massCount) = Landmasses(vertexCount, neighbours,
                v => biomeAt(v) is string b && !BiomeDatabase.WaterBiomes.Contains(b));

            var fields = new int[massCount];
            var cells = new int[massCount];
            for (int v = 0; v < vertexCount; v++)
            {
                if (massOf[v] < 0) continue;
                cells[massOf[v]]++;
                if (biomeAt(v) == "field") fields[massOf[v]]++;
            }

            var temperate = new List<int>();
            var open = new List<int>();
            var any = new List<int>();
            for (int v = 0; v < vertexCount; v++)
            {
                string? b = biomeAt(v);
                if (!IsOpenGround(b)) continue;
                any.Add(v);
                int m = massOf[v];
                if (m < 0) continue;
                bool settled = fields[m] >= Config.WorldRegions.FieldsForAHome;
                if (settled && TemperateGround.Contains(b!)) temperate.Add(v);
                if (settled || cells[m] >= LandForAHome) open.Add(v);
            }

            if (temperate.Count >= MinTemperateSpawns) return temperate;
            if (open.Count > 0) return open;
            return any;
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
