using System;
using System.Collections.Generic;

namespace Cathedral.Glyph
{
    /// <summary>
    /// The moons, as the game talks about them rather than as it draws them: an ordinal, a name and
    /// the world seed that ordinal stands for.
    ///
    /// <para>Every value here is a pure function of the ordinal, and the ordinal comes from the sky,
    /// which is drawn from <see cref="Config.SkyCloud.SkySeed"/> — a constant. So the third moon is
    /// the same moon, with the same name, over the same world, in every run on every machine. That
    /// is the whole point of the world-selection screen: a moon is a name a player can keep, and two
    /// players who pick the same one walk the same ground.</para>
    ///
    /// <para>Deliberately independent of <see cref="GameRng"/>. Seeding the sky from the master seed
    /// would mean the moon a player just clicked no longer existed the moment they clicked it, since
    /// clicking it is what changes the master seed.</para>
    /// </summary>
    public static class SkyMoons
    {
        // Syllables, not a word list: 383 moons need 383 distinct names, and a list that long would
        // be a file nobody maintains. Two or three syllables from these tables give ~7,000
        // combinations, drawn without replacement below.
        private static readonly string[] Heads =
        {
            "Ar", "Bel", "Cor", "Dun", "Eth", "Fen", "Gar", "Hal", "Ish", "Kel",
            "Lor", "Mar", "Nor", "Orr", "Per", "Quen", "Rav", "Sel", "Tor", "Ul",
            "Var", "Wen", "Yr", "Zel", "Ash", "Bran", "Cael", "Drem", "Esk", "Fal"
        };

        private static readonly string[] Middles =
        {
            "a", "e", "i", "o", "u", "en", "ar", "ol", "ir", "um", "ae", "ys"
        };

        private static readonly string[] Tails =
        {
            "moth", "din", "rath", "vel", "gorn", "sha", "riel", "kar", "thun", "mir",
            "los", "wyn", "dur", "neth", "sar"
        };

        /// <summary>
        /// Every moon name, indexed by ordinal, built once and never rebuilt. Names are drawn in a
        /// fixed order from the syllable tables and de-duplicated, so a moon's name never depends on
        /// how many moons happened to be asked for first.
        /// </summary>
        private static readonly List<string> _names = BuildNames(4096);

        /// <summary>The display name of the moon at <paramref name="ordinal"/>.</summary>
        public static string Name(int ordinal)
            => ordinal >= 0 && ordinal < _names.Count ? _names[ordinal] : $"Moon {ordinal}";

        /// <summary>
        /// The master seed the world under <paramref name="ordinal"/> is generated from.
        ///
        /// <para>Hashed from the ordinal rather than being the ordinal, so that neighbouring moons
        /// are not neighbouring worlds: seeds 0, 1 and 2 differ, but they share the low bits every
        /// derived stream is built out of, and three adjacent moons producing three suspiciously
        /// similar worlds is exactly the kind of thing a player would notice and nobody would be
        /// able to explain.</para>
        /// </summary>
        public static int WorldSeed(int ordinal) => StableHash($"moon:{ordinal}");

        /// <summary>
        /// The ordinal whose world seed is <paramref name="seed"/>, or -1 when the seed belongs to no
        /// moon — which is the normal case for a run pinned with <c>--seed</c>. Used to blank the
        /// moon of the world a continued save is played in, the same way a freshly chosen one is
        /// blanked.
        /// </summary>
        public static int OrdinalForSeed(int seed, int moonCount)
        {
            for (int i = 0; i < moonCount; i++)
                if (WorldSeed(i) == seed) return i;
            return -1;
        }

        private static List<string> BuildNames(int wanted)
        {
            // The middle syllable may be absent, which is what makes both Armoth and Aravmoth
            // possible; the empty string is one of its values rather than a special case.
            int heads = Heads.Length, middles = Middles.Length + 1, tails = Tails.Length;
            int space = heads * middles * tails;

            var names = new List<string>(Math.Min(wanted, space));
            var seen  = new HashSet<string>(StringComparer.Ordinal);

            // Walked with a stride coprime to the size of the space rather than by nested loops.
            // Nesting would hand out the first two hundred moons with the same head syllable, and a
            // sky where every visible moon is called Ar-something reads as a bug. A coprime stride
            // still visits every combination exactly once, just in a scattered order.
            const int Stride = 1201; // prime, and 5850 = 2·3·5²·13 shares no factor with it
            for (int i = 0; names.Count < wanted && i < space; i++)
            {
                int k = (int)(((long)i * Stride) % space);

                string head   = Heads[k % heads];
                int m         = (k / heads) % middles;
                string middle = m == 0 ? "" : Middles[m - 1];
                string tail   = Tails[(k / (heads * middles)) % tails];

                string name = head + middle + tail;
                if (seen.Add(name)) names.Add(name);
            }
            return names;
        }

        /// <summary>
        /// FNV-1a, for the same reason <see cref="GameRng"/> uses it: <c>string.GetHashCode</c> is
        /// randomised per process, and a moon whose world changed between launches would be worse
        /// than no naming at all.
        /// </summary>
        private static int StableHash(string s)
        {
            unchecked
            {
                uint h = 2166136261u;
                foreach (char c in s)
                {
                    h ^= c;
                    h *= 16777619u;
                }
                return (int)h;
            }
        }
    }
}
