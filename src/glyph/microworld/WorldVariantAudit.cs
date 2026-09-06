// WorldVariantAudit.cs — generates every world variant, headless, and says whether it is a world
// anyone could be dropped into.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Reflection;
using System.Text;
using OpenTK.Mathematics;

namespace Cathedral.Glyph.Microworld
{
    /// <summary>
    /// <c>--world-variant-audit</c>: builds the terrain of every variant across the first worlds a
    /// player could actually pick it on, and checks the handful of things that make a world playable
    /// rather than merely generatable.
    ///
    /// <para><b>Why this exists.</b> A variant is eleven numbers, and every way of getting them wrong
    /// produces a world that generates without complaint. A waterline half a point too high leaves an
    /// archipelago of six-cell islands, and the run ends when the player looks at the travel map and
    /// finds nowhere to go. A treeline half a point too low buries the fields, and since farms and
    /// villages are placed only on field cells, that world has no people in it at all. Neither throws.
    /// Both are one arithmetic comparison away from being caught here.</para>
    ///
    /// <para><b>What it measures, and why each one.</b>
    /// <list type="bullet">
    /// <item>Land share — a world that is nearly all sea has nowhere to walk; nearly all land has no
    /// shape.</item>
    /// <item>Field cells — <c>PostProcessWorld</c> hangs every farm and village off a field, so the
    /// field count is the settlement count. Below a floor, the world is uninhabited.</item>
    /// <item>Spawnable cells — <c>InitializeProtagonist</c> only ever starts the player on plain,
    /// field or coast.</item>
    /// <item>Viable spawns — the one that needs the graph. Travel is on foot and sea is forbidden
    /// (<c>BiomeTravelDatabase.LandForbiddenBiomes</c>), so the landmass under the spawn is the whole
    /// of the world that run can reach, and one with no fields on it holds no farms and no villages.
    /// <c>InitializeProtagonist</c> now refuses such ground, so what is checked is that it has enough
    /// left to choose from. The <c>dead</c> column beside it is the share of otherwise-spawnable
    /// ground that rule throws away — informational, and properly high in a world of islands.</item>
    /// <item>The richest country — how many fields the best landmass carries. A world can pass every
    /// other bound and still be one where the largest country holds a dozen farmsteads.</item>
    /// <item>Inland coast — how much of the shore the noise band proposed turns out to touch no water
    /// and is made plain by <see cref="CoastRule"/>. Reported, not faulted: it is waste rather than
    /// breakage, and it is governed by the <i>feature size</i> rather than by the band. A smooth
    /// field has shallow gradients, so a fixed band covers more cells the broader the continents are
    /// — which is why Continental throws away near half of its band at the same width where Insular,
    /// whose features are small, throws away a twentieth.</item>
    /// <item>Shallow water — sea as a share of all water. The one part of a coastal disposition the
    /// coast rule cannot take away, since it is about the water and not about the shore.</item>
    /// <item>Regions — the division the world history will hang off. A world of one region has no
    /// history to tell; a world of four hundred has no history anyone can hold.</item>
    /// </list></para>
    ///
    /// <para>Headless by construction: it builds the mesh through <see cref="IcosphereGeometry"/> and
    /// classifies vertices through <see cref="WorldShape"/>, both of which the running game uses too,
    /// so what is measured here is the world a player would walk — no window, no GL, no LLM.</para>
    /// </summary>
    public static class WorldVariantAudit
    {
        /// <summary>
        /// How many worlds of each variant to classify. Terrain is cheap - three Perlin reads per
        /// vertex - so this is set high enough that a variant which is playable on average but breaks
        /// on one seed in ten gets caught, which four samples would not have done.
        /// </summary>
        private const int SampleWorlds = 16;

        /// <summary>
        /// How many of those worlds also get their regions built. Dividing a world costs a hundred
        /// times what classifying it does, and the region count is the one measurement here that
        /// barely moves between seeds of the same variant, so it is sampled rather than swept.
        /// </summary>
        private const int RegionSamples = 3;

        /// <summary>How many of the sampled worlds are printed in full. The rest are summarised.</summary>
        private const int PrintedRows = 4;

        /// <summary>How far up the sky to look for sample worlds of a given variant.</summary>
        private const int OrdinalSearchLimit = 2000;

        // ── The bounds a world has to sit inside to count as playable ────────────────
        // Stated as shares of the whole sphere unless said otherwise. Deliberately generous: the
        // point is to catch a variant that is broken, not to make every variant the same.
        private const float MinLandShare      = 0.18f;
        private const float MaxLandShare      = 0.90f;
        // Of the LAND, not of the sphere. A world with a high waterline has less of everything, and
        // measuring its fields against the whole globe faults it for being a drowned world rather
        // than for being a barren one - which is the question actually being asked. What guards the
        // absolute number is FieldsForARun below, on the one landmass a run will be spent on.
        private const float MinFieldShareOfLand = 0.020f;
        private const float MinSpawnableShare = 0.030f;
        private const int   MinRegions        = 6;
        private const int   MaxRegions        = 300;
        private const int   MinViableSpawns   = 200;     // cells the spawn rule must have to choose from
        private const int   FieldsForARun     = 40;      // fields the world's richest country must carry
        private const int   BlurbRoom        = 56;      // characters a variant's one-line description may run to

        public static string BuildReport()
        {
            var sb = new StringBuilder();
            var faults = new List<string>();

            sb.AppendLine("WORLD VARIANT AUDIT");
            sb.AppendLine("===================");
            sb.AppendLine();

            CheckTable(sb, faults);

            sb.AppendLine("Building the sphere...");
            int subdivisions = Config.GlyphSphere.SphereSubdivisions;
            float radius     = Config.GlyphSphere.SphereRadius;
            var (positions, triangles) = IcosphereGeometry.Build(subdivisions, radius);
            var adjacency = IcosphereGeometry.Adjacency(positions.Count, triangles);
            sb.AppendLine($"  subdivision {subdivisions}, radius {radius}, {positions.Count} vertices, "
                        + $"{triangles.Count / 3} triangles.");
            sb.AppendLine();

            foreach (var variant in WorldVariants.All)
                Measure(sb, faults, variant, positions, adjacency);

            sb.AppendLine();
            sb.AppendLine("VERDICT");
            sb.AppendLine("-------");
            if (faults.Count == 0)
            {
                sb.AppendLine($"  {WorldVariants.All.Length} variant(s), "
                            + $"{WorldVariants.All.Length * SampleWorlds} world(s) built, "
                            + $"{WorldVariants.All.Length * RegionSamples} of them divided into regions. No faults.");
            }
            else
            {
                // Marked with the cross run_tests.sh greps for. An audit whose faults are prose is
                // an audit the suite reports as passing.
                sb.AppendLine($"  {faults.Count} fault(s):");
                foreach (string f in faults) sb.AppendLine($"    ✗ {f}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// The half that needs no terrain: is the table itself sound, does every variant class appear
        /// in it, and does every variant get reached by some moon.
        /// </summary>
        private static void CheckTable(StringBuilder sb, List<string> faults)
        {
            sb.AppendLine("TABLE");
            sb.AppendLine("-----");

            var registered = WorldVariants.All;
            sb.AppendLine($"  {registered.Length} variant(s) registered.");

            // A variant class that nobody put in the array is a variant no world is ever built to,
            // and nothing else in the game would ever mention it. This is the one thing reflection
            // is good for here — the ORDER has to stay hand-written, because it decides which moon
            // gets which world.
            var declared = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(WorldVariant).IsAssignableFrom(t))
                .ToList();
            var registeredTypes = registered.Select(v => v.GetType()).ToHashSet();
            foreach (var t in declared.Where(t => !registeredTypes.Contains(t)))
                faults.Add($"{t.Name} is a WorldVariant that WorldVariants.All does not list — no world is ever built to it");

            foreach (var group in registered.GroupBy(v => v.Id).Where(g => g.Count() > 1))
                faults.Add($"variant id '{group.Key}' is used by {group.Count()} variants");

            // The name goes in the moon box, which is fixed-width and does no wrapping: a name too
            // long for it does not overflow visibly, it runs off the border and into the sky behind,
            // which on a dark screen looks like nothing at all. The blurb is no longer drawn there -
            // it belongs to the CLI's variant table and the line the generator logs - so it is held
            // to one line of that rather than to the box.
            int room = Config.WorldSelectionUI.BoxWidth - 4;

            foreach (var v in registered)
            {
                foreach (string fault in v.Shape.Faults())
                    faults.Add($"{v.Id}: {fault}");

                if (WorldVariants.ById(v.Id) != v)
                    faults.Add($"{v.Id}: does not resolve back through WorldVariants.ById");

                if (v.Blurb.Length > BlurbRoom)
                    faults.Add($"{v.Id}: its line is {v.Blurb.Length} characters, past the {BlurbRoom} one line holds");
                if (v.Name.Length > room / 2)
                    faults.Add($"{v.Id}: the name \"{v.Name}\" is {v.Name.Length} characters, too long for its row in the moon box");
            }

            // How the sky divides among them. A variant nothing lands on is dead content.
            var hits = registered.ToDictionary(v => v.Id, _ => 0);
            const int Sky = 383; // the moon count the selection screen shows
            for (int ordinal = 0; ordinal < Sky; ordinal++)
                hits[WorldVariants.ForSeed(SkyMoons.WorldSeed(ordinal)).Id]++;

            sb.AppendLine($"  across the first {Sky} moons:");
            foreach (var v in registered)
                sb.AppendLine($"    {v.Id,-18} {hits[v.Id],4} moon(s)  {Pct(hits[v.Id], Sky)}");

            foreach (var v in registered.Where(v => hits[v.Id] == 0))
                faults.Add($"{v.Id}: no moon in the sky is this variant");

            sb.AppendLine();
        }

        private static void Measure(StringBuilder sb, List<string> faults, WorldVariant variant,
                                    List<Vector3> positions, List<int>[] adjacency)
        {
            sb.AppendLine($"{variant.Name.ToUpperInvariant()}  [{variant.Id}]");
            sb.AppendLine(new string('-', Math.Max(variant.Name.Length, 20)));
            sb.AppendLine($"  {variant.Blurb}");
            sb.AppendLine();
            sb.AppendLine("   seed        land   field  forest   mtn+pk   coast   plain  | inland  shallow  masses    dead  viable  best  regions");

            var ordinals = OrdinalsOf(variant).ToList();
            var built = new List<WorldStats>();

            for (int i = 0; i < ordinals.Count; i++)
            {
                int ordinal = ordinals[i];
                int seed = SkyMoons.WorldSeed(ordinal);
                var w = BuildWorld(variant, seed, positions, adjacency, withRegions: i < RegionSamples);
                built.Add(w);

                if (i < PrintedRows)
                    sb.AppendLine($"  {seed,11}  {Pct(w.Land, w.Total)}  {Pct(w.Field, w.Total)}  "
                                + $"{Pct(w.Forest, w.Total)}  {Pct(w.Mountain, w.Total)}  "
                                + $"{Pct(w.Coast, w.Total)}  {Pct(w.Plain, w.Total)}  | "
                                + $"{Pct(w.StrandedCoast, Math.Max(w.BandCoast, 1)),6}  "
                                + $"{Pct(w.Sea, Math.Max(w.Sea + w.Ocean, 1)),7}  "
                                + $"{w.Landmasses,6}  {Pct(w.Marooned, Math.Max(w.Spawnable, 1)),6}  "
                                + $"{w.ViableSpawns,6}  {w.BestLandmassFields,4}  "
                                + $"{(w.Regions > 0 ? w.Regions.ToString() : "-"),7}");

                string where = $"{variant.Id} (moon {ordinal}, seed {seed})";
                float land = Share(w.Land, w.Total);
                if (land < MinLandShare)
                    faults.Add($"{where}: only {P(land)} of the sphere is land (floor {P(MinLandShare, 0)}) - nowhere to walk");
                if (land > MaxLandShare)
                    faults.Add($"{where}: {P(land)} of the sphere is land (ceiling {P(MaxLandShare, 0)}) - the sea has stopped shaping it");
                if (Share(w.Field, w.Land) < MinFieldShareOfLand)
                    faults.Add($"{where}: fields are {P(Share(w.Field, w.Land), 2)} of the LAND (floor {P(MinFieldShareOfLand)}) - "
                             + "farms and villages are placed only on fields, so this world is unpeopled");
                if (Share(w.Spawnable, w.Total) < MinSpawnableShare)
                    faults.Add($"{where}: only {P(Share(w.Spawnable, w.Total), 2)} of the sphere can be spawned on "
                             + $"(floor {P(MinSpawnableShare)}) - InitializeProtagonist wants plain, field or coast");
                if (w.ViableSpawns < MinViableSpawns)
                    faults.Add($"{where}: only {w.ViableSpawns} cell(s) sit on a landmass worth waking on "
                             + $"(floor {MinViableSpawns}) - InitializeProtagonist has almost nothing to draw from, "
                             + "and falls back to spawning anywhere at all");
                if (w.BestLandmassFields < FieldsForARun)
                    faults.Add($"{where}: the richest landmass carries {w.BestLandmassFields} field(s) "
                             + $"(floor {FieldsForARun}) - the best country in this world is a hamlet");
                if (w.Regions > 0 && w.Regions < MinRegions)
                    faults.Add($"{where}: {w.Regions} region(s) (floor {MinRegions}) - too few to hang a history on");
                if (w.Regions > MaxRegions)
                    faults.Add($"{where}: {w.Regions} region(s) (ceiling {MaxRegions})");
            }

            // The worst case over the whole sample, which is what actually decides whether a variant
            // is safe to hand a player - the printed rows are only the first few of them.
            if (built.Count > PrintedRows)
                sb.AppendLine($"  {"(+" + (built.Count - PrintedRows) + ")",11}  "
                            + "more world(s) built and checked, not printed");

            sb.AppendLine($"  {"worst",11}  {Pct(built.Min(w => w.Land), built[0].Total)}  "
                        + $"{Pct(built.Min(w => w.Field), built[0].Total)}  "
                        + $"{"",6}   {"",6}   {"",6}   {"",6}  | "
                        + $"{(P(built.Max(w => Share(w.StrandedCoast, Math.Max(w.BandCoast, 1))))),6}  "
                        + $"{(P(built.Min(w => Share(w.Sea, Math.Max(w.Sea + w.Ocean, 1))))),7}  "
                        + $"{built.Max(w => w.Landmasses),6}  "
                        + $"{(P(built.Max(w => Share(w.Marooned, Math.Max(w.Spawnable, 1))))),6}  "
                        + $"{built.Min(w => w.ViableSpawns),6}  {built.Min(w => w.BestLandmassFields),4}");

            sb.AppendLine();
        }

        /// <summary>
        /// The first few moons that are this variant — so the worlds measured are worlds a player
        /// could really be given, not seeds invented for the audit.
        /// </summary>
        private static IEnumerable<int> OrdinalsOf(WorldVariant variant)
        {
            int found = 0;
            for (int ordinal = 0; ordinal < OrdinalSearchLimit && found < SampleWorlds; ordinal++)
            {
                if (WorldVariants.ForSeed(SkyMoons.WorldSeed(ordinal)) != variant) continue;
                found++;
                yield return ordinal;
            }
        }

        private readonly record struct WorldStats(
            int Total, int Land, int Field, int Forest, int Mountain, int Coast, int Plain,
            int Spawnable, int Marooned, int ViableSpawns, int BestLandmassFields,
            int BandCoast, int StrandedCoast, int Sea, int Ocean,
            int Landmasses, int Regions);

        /// <summary>
        /// Classifies every vertex under <paramref name="variant"/> on <paramref name="seed"/> and
        /// counts what came out. The noise offset is derived exactly as <c>GenerateWorld</c> derives
        /// it, so this is that world and not a world like it.
        /// </summary>
        private static WorldStats BuildWorld(WorldVariant variant, int seed,
                                             List<Vector3> positions, List<int>[] adjacency,
                                             bool withRegions)
        {
            GameRng.Reseed(seed);
            var worldRng = GameRng.For("world-terrain");
            var offset = new Vector3(
                (float)(worldRng.NextDouble() * 20000.0 - 10000.0),
                (float)(worldRng.NextDouble() * 20000.0 - 10000.0),
                (float)(worldRng.NextDouble() * 20000.0 - 10000.0));

            int n = positions.Count;
            var shape = variant.Shape;
            var biome = new string[n];
            var settlement = new float[n];

            for (int v = 0; v < n; v++)
            {
                var (water, settle, relief) = shape.Sample(positions[v], offset);
                biome[v] = shape.BiomeNameFor(water, settle, relief);
                settlement[v] = settle;
            }

            // Exactly what GenerateWorld does next. Skipping it would measure the world the noise
            // proposed rather than the one the game builds - and the difference between them is the
            // whole of what a wide coast band claims and does not deliver.
            int bandCoast = biome.Count(b => BiomeDatabase.CoastBiomes.Contains(b));
            var stranded = CoastRule.Stranded(n, v => biome[v], v => adjacency[v]);
            foreach (int v in stranded) biome[v] = CoastRule.Replacement;

            int land = 0, field = 0, forest = 0, mountain = 0, coast = 0, plain = 0, spawnable = 0;
            int sea = 0, ocean = 0;
            for (int v = 0; v < n; v++)
            {
                bool isLand = !BiomeDatabase.WaterBiomes.Contains(biome[v]);
                if (isLand) land++;
                switch (biome[v])
                {
                    case "field":    field++;    break;
                    case "forest":   forest++;   break;
                    case "mountain":
                    case "peak":     mountain++; break;
                    case "coast":    coast++;    break;
                    case "plain":    plain++;    break;
                    case "sea":      sea++;      break;
                    case "ocean":    ocean++;    break;
                }
                // Exactly InitializeProtagonist's list.
                if (biome[v] is "plain" or "field" or "coast") spawnable++;
            }

            var (landmassOf, landmassCount) = Landmasses(n, adjacency, v => !BiomeDatabase.WaterBiomes.Contains(biome[v]));

            // Fields per landmass, then the spawns that sit on a landmass carrying too few of them.
            var fieldsPerMass = new int[Math.Max(landmassCount, 1)];
            for (int v = 0; v < n; v++)
                if (biome[v] == "field" && landmassOf[v] >= 0) fieldsPerMass[landmassOf[v]]++;

            // Exactly InitializeProtagonist's rule, and its complement: what the spawn may draw from,
            // and what it throws away.
            int marooned = 0, viable = 0;
            for (int v = 0; v < n; v++)
            {
                if (biome[v] is not ("plain" or "field" or "coast")) continue;
                if (landmassOf[v] >= 0 && fieldsPerMass[landmassOf[v]] >= Config.WorldRegions.FieldsForAHome)
                    viable++;
                else
                    marooned++;
            }

            int bestFields = fieldsPerMass.Length > 0 ? fieldsPerMass.Max() : 0;

            // Zero means "not measured on this world", which the report prints as a dash and the
            // bounds skip. Dividing a world is the expensive half and does not need every seed.
            int regionCount = 0;
            if (withRegions)
            {
                var regions = WorldRegionMap.Build(new WorldRegionInput
                {
                    VertexCount     = n,
                    Neighbours      = v => adjacency[v],
                    IsLand          = v => !BiomeDatabase.WaterBiomes.Contains(biome[v]),
                    IsSettleable    = v => !BiomeDatabase.WaterBiomes.Contains(biome[v])
                                           && !BiomeDatabase.MountainBiomes.Contains(biome[v]),
                    SettlementNoise = v => settlement[v],
                    StepCostDays    = v => BiomeTravelDatabase.GetFor(biome[v]).DurationDays,
                    Position        = v => positions[v],
                });
                regionCount = regions.Regions.Count;
            }

            return new WorldStats(n, land, field, forest, mountain, coast, plain,
                                  spawnable, marooned, viable, bestFields,
                                  bandCoast, stranded.Count, sea, ocean, landmassCount, regionCount);
        }

        /// <summary>Connected components of the land graph — the isles and continents.</summary>
        private static (int[] Of, int Count) Landmasses(int n, List<int>[] adjacency, Func<int, bool> isLand)
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
                    foreach (int w in adjacency[v])
                    {
                        if (of[w] >= 0 || !isLand(w)) continue;
                        of[w] = id;
                        stack.Push(w);
                    }
                }
            }
            return (of, count);
        }

        private static float Share(int part, int whole) => whole <= 0 ? 0f : (float)part / whole;

        // Invariant on purpose: this machine formats 0.494 as "0,494", and a report whose numbers
        // change shape with the developer's locale is a report nobody can diff.
        private static string Pct(int part, int whole)
            => (100f * Share(part, whole)).ToString("F1", CultureInfo.InvariantCulture).PadLeft(5) + "%";

        /// <summary>A share as a percentage, for a fault line. Invariant, for the same reason.</summary>
        private static string P(float share, int decimals = 1)
            => (100f * share).ToString("F" + decimals, CultureInfo.InvariantCulture) + "%";
    }
}
