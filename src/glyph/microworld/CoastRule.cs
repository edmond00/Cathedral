// CoastRule.cs — the rule that a shore has to be on one.
using System;
using System.Collections.Generic;

namespace Cathedral.Glyph.Microworld
{
    /// <summary>
    /// Reclaims coast that touches no water.
    ///
    /// <para><b>Why it is needed.</b> <see cref="WorldShape.BiomeNameFor"/> decides coast from the
    /// <i>water noise</i> — a cell reads as shore when its water reading sits within
    /// <see cref="WorldShape.CoastBand"/> above the waterline. That is a threshold on a field, not a
    /// distance from the sea, and the two only agree while the band is narrow. Widen it and the band
    /// spreads across every shallow gradient in the field, so a world can be painted with shoreline
    /// a dozen cells from the nearest water — beaches in the middle of a continent, which read as a
    /// bug because they are one.</para>
    ///
    /// <para>So the noise proposes and this disposes: whatever the band offered, a cell keeps the
    /// shore only if it actually borders sea or ocean. Everything else becomes ordinary
    /// <see cref="Replacement"/>. The consequence worth knowing is that <c>CoastBand</c> can no
    /// longer manufacture a wide shore — the coast is however long the coastline is, and a variant
    /// that wants more of it has to break its land up rather than raise a threshold.</para>
    ///
    /// <para><b>And the converse: every cell on the water is shore.</b> The noise decides mountain,
    /// forest and field before it ever reads the coast band, so a range or a wood could run straight
    /// into the sea with no shore between — and once climate remade those cells, hot steppe and jungle
    /// stood in the surf. <see cref="Unshored"/> finds every land cell that touches water and is not
    /// already shore, and it becomes shore (which cold country then turns to sea ice). Together the
    /// two halves make the shore exactly the land on the water: no more, no less.</para>
    ///
    /// <para>Shared by the running game and <c>--world-variant-audit</c>, which has to classify the
    /// same way or it measures a world nobody plays.</para>
    /// </summary>
    public static class CoastRule
    {
        /// <summary>What a stranded shore becomes. Plain: it is ordinary open ground and always was.</summary>
        public const string Replacement = "plain";

        /// <summary>
        /// Every land vertex that borders sea or ocean and is not shore — the cells the converse rule
        /// makes shore. Returned rather than rewritten, like <see cref="Stranded"/>.
        /// </summary>
        public static List<int> Unshored(int vertexCount, Func<int, string?> biomeAt,
                                         Func<int, IEnumerable<int>> neighbours)
        {
            var unshored = new List<int>();

            for (int v = 0; v < vertexCount; v++)
            {
                string? here = biomeAt(v);
                if (here == null || BiomeDatabase.WaterBiomes.Contains(here)
                                 || BiomeDatabase.CoastBiomes.Contains(here)) continue;

                foreach (int w in neighbours(v))
                {
                    string? there = biomeAt(w);
                    if (there != null && BiomeDatabase.WaterBiomes.Contains(there))
                    {
                        unshored.Add(v);
                        break;
                    }
                }
            }

            return unshored;
        }

        /// <summary>
        /// Every vertex that reads as coast and borders no water. Returned rather than rewritten,
        /// because the game and the audit hold their biomes in different shapes.
        /// </summary>
        public static List<int> Stranded(int vertexCount, Func<int, string?> biomeAt,
                                         Func<int, IEnumerable<int>> neighbours)
        {
            var stranded = new List<int>();

            for (int v = 0; v < vertexCount; v++)
            {
                string? here = biomeAt(v);
                if (here == null || !BiomeDatabase.CoastBiomes.Contains(here)) continue;

                bool touchesWater = false;
                foreach (int w in neighbours(v))
                {
                    string? there = biomeAt(w);
                    if (there != null && BiomeDatabase.WaterBiomes.Contains(there))
                    {
                        touchesWater = true;
                        break;
                    }
                }

                if (!touchesWater) stranded.Add(v);
            }

            return stranded;
        }
    }
}
