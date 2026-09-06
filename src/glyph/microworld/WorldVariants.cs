// WorldVariants.cs — the ten kinds of world a moon can turn out to be.
using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;

namespace Cathedral.Glyph.Microworld
{
    /// <summary>
    /// The numbers that turn three Perlin fields into a world: how big its features are, where the
    /// waterline sits, how high the treeline is, how much of the land is worth farming.
    ///
    /// <para>Written as a record so a variant can be stated as the baseline <c>with</c> the two or
    /// three numbers it actually changes. A variant that had to restate all eleven would be a variant
    /// nobody could read, and the one number that mattered would be lost among ten that did not.</para>
    ///
    /// <para><b>Three of these are widths, not levels</b> — <see cref="OceanDepth"/> and
    /// <see cref="CoastBand"/> are measured from <see cref="SeaLevel"/> rather than from zero. That is
    /// the whole reason a variant can raise the waterline in one line: absolute thresholds would leave
    /// the deep ocean above the shallows and the shore underwater, and the result would still generate
    /// — it would just be a world with no coast in it, and nothing would say so.</para>
    /// </summary>
    /// <param name="ContinentScale">Divisor on the water layer. Bigger means broader continents and
    /// fewer of them; smaller breaks the land into islands.</param>
    /// <param name="SettlementScale">Divisor on the settlement layer — the one that decides forest,
    /// field and town. Small on purpose: this is the layer that varies within a day's walk.</param>
    /// <param name="ReliefScale">Divisor on the mountain layer. Bigger means longer ranges.</param>
    /// <param name="SeaLevel">The waterline. Raise it for more sea, lower it for more land; this is
    /// the single knob behind every drowned or continental variant.</param>
    /// <param name="OceanDepth">How far below the waterline the shallow sea gives way to deep ocean.</param>
    /// <param name="CoastBand">How far above the waterline the shore reaches inland.</param>
    /// <param name="MountainLevel">Where the mountains begin. Lower means more of them.</param>
    /// <param name="PeakLevel">Where the mountains turn to bare peaks. Must sit above
    /// <see cref="MountainLevel"/> or there are no mountains, only peaks.</param>
    /// <param name="ForestLevel">Above this, the settlement layer reads as forest.</param>
    /// <param name="FieldLevel">Below this, it reads as tilled field — which is where every farm and
    /// village in the game is placed, so this is the knob that decides how peopled a world is.</param>
    /// <param name="TownLevel">Below this, a field would be a town. Inert while the city biome is
    /// disabled in <c>BiomeDatabase</c> — kept because the threshold is real and returns with it.</param>
    public sealed record WorldShape(
        float ContinentScale,
        float SettlementScale,
        float ReliefScale,
        float SeaLevel,
        float OceanDepth,
        float CoastBand,
        float MountainLevel,
        float PeakLevel,
        float ForestLevel,
        float FieldLevel,
        float TownLevel)
    {
        // The three layers are read out of one Perlin field at three offsets far enough apart that
        // they are independent. Fixed, and not part of a variant: moving them would not make a
        // different KIND of world, only a different world, which is what the seed is for.
        private static readonly Vector3 WaterOffset      = new Vector3(1337.0f, 2468.0f, 9876.0f);
        private static readonly Vector3 SettlementOffset = new Vector3(5432.0f, 8765.0f, 1234.0f);
        private static readonly Vector3 ReliefOffset     = new Vector3(9999.0f, 3333.0f, 7777.0f);

        /// <summary>
        /// The three noise readings at a point on the sphere, under this shape's feature sizes.
        /// <paramref name="worldOffset"/> is the per-seed displacement of the sampled region.
        /// </summary>
        public (float Water, float Settlement, float Relief) Sample(Vector3 position, Vector3 worldOffset)
        {
            Vector3 sp = position + worldOffset;
            Vector3 p1 = (WaterOffset      + sp) / ContinentScale;
            Vector3 p2 = (SettlementOffset + sp) / SettlementScale;
            Vector3 p3 = (ReliefOffset     + sp) / ReliefScale;

            return (Perlin.Noise(p1.X, p1.Y, p1.Z),
                    Perlin.Noise(p2.X, p2.Y, p2.Z),
                    Perlin.Noise(p3.X, p3.Y, p3.Z));
        }

        /// <summary>
        /// Which biome the three readings mean.
        ///
        /// <para><b>The order of the tests is the world's geology and must not be reordered.</b>
        /// Water first, because nothing grows in it. Mountains next, before anything about
        /// settlement is read at all — which is why the settlement layer says nothing whatever above
        /// the treeline, and why <c>WorldRegionInput.IsSettleable</c> has to exclude the peaks.</para>
        /// </summary>
        public string BiomeNameFor(float water, float settlement, float relief)
        {
            if (water <= SeaLevel - OceanDepth) return "ocean";
            if (water <= SeaLevel)              return "sea";

            if (relief > PeakLevel)     return "peak";
            if (relief > MountainLevel) return "mountain";

            // TODO: restore the city biome. The threshold is live; the biome it names is not, so a
            // town reads as the field it stands in.
            if (settlement < TownLevel) return "field";

            if (water <= SeaLevel + CoastBand) return "coast";

            if (settlement > ForestLevel) return "forest";
            if (settlement < FieldLevel)  return "field";

            return "plain";
        }

        /// <summary>The biome record itself, for callers that want the glyph and colour with it.</summary>
        public BiomeType BiomeFor(float water, float settlement, float relief)
            => BiomeDatabase.Biomes[BiomeNameFor(water, settlement, relief)];

        /// <summary>
        /// The orderings that have to hold for a shape to describe a world rather than a bug. Empty
        /// when the shape is sound; one line per fault otherwise. Checked by
        /// <c>--world-variant-audit</c>, which is the only thing between a mistyped constant and a
        /// world with no shoreline that generates perfectly happily.
        /// </summary>
        public IEnumerable<string> Faults()
        {
            if (ContinentScale   <= 0f) yield return "ContinentScale must be positive";
            if (SettlementScale  <= 0f) yield return "SettlementScale must be positive";
            if (ReliefScale      <= 0f) yield return "ReliefScale must be positive";
            if (OceanDepth       <= 0f) yield return "OceanDepth must be positive, or there is no deep water";
            if (CoastBand        <= 0f) yield return "CoastBand must be positive, or there is no shore";
            if (PeakLevel <= MountainLevel)
                yield return $"PeakLevel {PeakLevel} must sit above MountainLevel {MountainLevel}";
            if (TownLevel > FieldLevel)
                yield return $"TownLevel {TownLevel} must sit at or below FieldLevel {FieldLevel}";
            if (FieldLevel >= ForestLevel)
                yield return $"FieldLevel {FieldLevel} must sit below ForestLevel {ForestLevel}";
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  What a world IS, as a type rather than as a string — the same move already made for points
    //  of interest and areas, and made here for the same reason: the world history generation this
    //  was built for will want to ask what kind of world it is writing a history of, and
    //  `variant is InsularVariant` is a build error when that variant is renamed away, where
    //  `variant.Name == "Insular"` is a condition that silently stops matching.
    //
    //  The registry below is an EXPLICIT ordered array rather than a reflection sweep, because the
    //  order is load-bearing: a moon's variant is its seed's hash modulo the table, so reordering
    //  the table re-rolls every world in the sky. --world-variant-audit sweeps the assembly for
    //  subclasses and fails on any that the array forgot, which is the half reflection is good for.
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// One kind of world: what that kind is called, a line saying what a traveller is in for, and
    /// the numbers that make it so.
    /// </summary>
    public abstract class WorldVariant
    {
        /// <summary>The id this variant answers to on the command line and in the CLI. Stable.</summary>
        public abstract string Id { get; }

        /// <summary>
        /// What the moon box calls it: <b>a classification, not a place</b>. One word naming the
        /// kind of terrain, in the manner of a natural philosopher's taxonomy.
        ///
        /// <para>These were proper nouns once — The Riven Spine, The Empty Marches, The Scattered
        /// Isles — and every one read as a particular country somebody could travel to. Which is
        /// exactly wrong: a variant is not a place but a manner of composing one, and the three
        /// dozen or so moons that carry it are three dozen different countries.</para>
        /// </summary>
        public abstract string Name { get; }

        /// <summary>One line, in the box under the name. Says what the ground is like, not what it means.</summary>
        public abstract string Blurb { get; }

        /// <summary>The numbers.</summary>
        public abstract WorldShape Shape { get; }

        public override string ToString() => $"{Name} ({Id})";
    }

    /// <summary>The baseline: mixed ground, half of it under water. The world the game was tuned on.</summary>
    public sealed class TemperateVariant : WorldVariant
    {
        public const string Lemma = "temperate";
        public override string Id    => Lemma;
        public override string Name  => "Temperate";
        public override string Blurb => "mixed ground, evenly divided with the sea";
        public override WorldShape Shape => WorldVariants.Baseline;
    }

    /// <summary>Water risen over most of it; what land is left is thin and broken.</summary>
    public sealed class DrownedVariant : WorldVariant
    {
        public const string Lemma = "drowned";
        public override string Id    => Lemma;
        public override string Name  => "Drowned";
        public override string Blurb => "a high waterline, and narrow land";
        public override WorldShape Shape => WorldVariants.Baseline with
        {
            ContinentScale = 14f,
            SeaLevel       = 0.11f,
            CoastBand      = 0.09f,
        };
    }

    /// <summary>One mass of land, and the sea an edge to it rather than a division through it.</summary>
    public sealed class ContinentalVariant : WorldVariant
    {
        public const string Lemma = "continental";
        public override string Id    => Lemma;
        public override string Name  => "Continental";
        public override string Blurb => "one vast land, the sea only its edge";
        public override WorldShape Shape => WorldVariants.Baseline with
        {
            ContinentScale = 20f,
            SeaLevel       = -0.16f,
        };
    }

    /// <summary>Land ground into fragments, none of it far from water.</summary>
    public sealed class InsularVariant : WorldVariant
    {
        public const string Lemma = "insular";
        public override string Id    => Lemma;
        public override string Name  => "Insular";
        public override string Blurb => "small land, strewn across the water";
        public override WorldShape Shape => WorldVariants.Baseline with
        {
            ContinentScale = 5.5f,
            SeaLevel       = 0.02f,
        };
    }

    /// <summary>Mountains through everything; the passes matter more than the ground between them.</summary>
    public sealed class MontaneVariant : WorldVariant
    {
        public const string Lemma = "montane";
        public override string Id    => Lemma;
        public override string Name  => "Montane";
        public override string Blurb => "range upon range, and little between";
        public override WorldShape Shape => WorldVariants.Baseline with
        {
            ReliefScale   = 6f,
            MountainLevel = 0.10f,
            PeakLevel     = 0.38f,
        };
    }

    /// <summary>Old country, worn flat. Long walks, and nothing to climb.</summary>
    public sealed class ErodedVariant : WorldVariant
    {
        public const string Lemma = "eroded";
        public override string Id    => Lemma;
        public override string Name  => "Eroded";
        public override string Blurb => "low old ground, worn down to hills";
        public override WorldShape Shape => WorldVariants.Baseline with
        {
            ReliefScale   = 11f,
            MountainLevel = 0.48f,
            PeakLevel     = 0.66f,
        };
    }

    /// <summary>Forest over most of the land, and the clearings small.</summary>
    public sealed class SylvanVariant : WorldVariant
    {
        public const string Lemma = "sylvan";
        public override string Id    => Lemma;
        public override string Name  => "Sylvan";
        public override string Blurb => "forest over all of it, and few clearings";
        public override WorldShape Shape => WorldVariants.Baseline with
        {
            ForestLevel = -0.04f,
            FieldLevel  = -0.44f,
        };
    }

    /// <summary>Worked land, thick with farms and villages.</summary>
    public sealed class ArableVariant : WorldVariant
    {
        public const string Lemma = "arable";
        public override string Id    => Lemma;
        public override string Name  => "Arable";
        public override string Blurb => "field after field, and villages between";
        public override WorldShape Shape => WorldVariants.Baseline with
        {
            ForestLevel = 0.42f,
            FieldLevel  = -0.16f,
        };
    }

    /// <summary>Almost nobody. Days between one farm and the next.</summary>
    public sealed class DesolateVariant : WorldVariant
    {
        public const string Lemma = "desolate";
        public override string Id    => Lemma;
        public override string Name  => "Desolate";
        public override string Blurb => "open country, barely worked";
        public override WorldShape Shape => WorldVariants.Baseline with
        {
            ForestLevel = 0.18f,
            FieldLevel  = -0.47f,
        };
    }

    /// <summary>Shallow everywhere: broad shores, and the sea never far inland.</summary>
    public sealed class LittoralVariant : WorldVariant
    {
        public const string Lemma = "littoral";
        public override string Id    => Lemma;
        public override string Name  => "Littoral";
        public override string Blurb => "wide shores, and shallow water beyond";
        public override WorldShape Shape => WorldVariants.Baseline with
        {
            SeaLevel   = -0.03f,
            OceanDepth = 0.42f,
            CoastBand  = 0.20f,
            FieldLevel = -0.33f,
        };
    }

    /// <summary>
    /// The table of world variants, and the rule that turns a world seed into one of them.
    /// </summary>
    public static class WorldVariants
    {
        /// <summary>
        /// The numbers the game was tuned on, and the point every other variant is stated as a
        /// departure from. <see cref="TemperateVariant"/> is this shape unchanged.
        /// </summary>
        public static readonly WorldShape Baseline = new WorldShape(
            ContinentScale:  12f,
            SettlementScale:  3f,
            ReliefScale:      8f,
            SeaLevel:         0.0f,
            OceanDepth:       0.25f,
            CoastBand:        0.065f,
            MountainLevel:    0.30f,
            PeakLevel:        0.50f,
            ForestLevel:      0.25f,
            FieldLevel:      -0.38f,
            TownLevel:       -0.58f);

        /// <summary>
        /// Every variant, in the order that decides which moon gets which.
        ///
        /// <para><b>Adding to this table, or reordering it, re-rolls every world in the sky.</b>
        /// <see cref="ForSeed"/> indexes by the seed's hash modulo the length, so a moon that was
        /// The Green Shroud yesterday is something else today — and any save from before is refused,
        /// which is what the variant field in the save file is for. Append rather than insert when
        /// there is a choice, and expect to invalidate saves either way.</para>
        /// </summary>
        public static readonly WorldVariant[] All =
        {
            new TemperateVariant(),
            new DrownedVariant(),
            new ContinentalVariant(),
            new InsularVariant(),
            new MontaneVariant(),
            new ErodedVariant(),
            new SylvanVariant(),
            new ArableVariant(),
            new DesolateVariant(),
            new LittoralVariant(),
        };

        /// <summary>
        /// The variant a world on <paramref name="worldSeed"/> will actually be built to: the one the
        /// seed names, unless <c>--world-variant</c> overrides it.
        ///
        /// <para>Separate from <see cref="ForSeed"/> because two callers need the answer <b>before</b>
        /// the world exists and cannot ask the generator for it: the continue guard, which refuses a
        /// save whose world is not the world this process would build, and the generator itself. An
        /// unknown id in the flag falls back to the seed's own variant silently here — the miss is
        /// reported once, where the world is generated, rather than at every caller.</para>
        /// </summary>
        public static WorldVariant Resolve(int worldSeed)
            => ById(Config.Debug.WorldVariant) ?? ForSeed(worldSeed);

        /// <summary>The variant the world on <paramref name="worldSeed"/> is built to.</summary>
        ///
        /// <para>Deliberately independent of <see cref="GameRng"/>, and for the same reason
        /// <see cref="SkyMoons"/> is: the world-selection screen names a moon's variant before the
        /// master seed has been touched, and drawing it from a generator would mean the answer
        /// depended on how many other things had drawn first.</para>
        public static WorldVariant ForSeed(int worldSeed)
            => All[(int)((uint)StableHash($"variant:{worldSeed}") % (uint)All.Length)];

        /// <summary>
        /// The variant <paramref name="id"/> names, or null. Matches the id and, as a courtesy to
        /// anyone typing the flag, the display name with its article and spaces stripped.
        /// </summary>
        public static WorldVariant? ById(string? id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            string want = Normalise(id);
            return All.FirstOrDefault(v => Normalise(v.Id) == want || Normalise(v.Name) == want);
        }

        private static string Normalise(string s)
            => new string(s.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

        /// <summary>
        /// FNV-1a, for the same reason <see cref="SkyMoons"/> uses it: <c>string.GetHashCode</c> is
        /// randomised per process, so a world's variant would change between launches of the same
        /// build — the one thing a named world must never do.
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
