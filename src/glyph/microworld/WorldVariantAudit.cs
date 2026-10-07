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
        /// How many of those worlds are also peopled: divided into regions, given a history, and the
        /// settled country sprawled round history's places. That costs a thousand times what
        /// classifying a world does, and what it measures moves little between seeds of a variant, so
        /// it is sampled rather than swept.
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
        private const float MinSpawnableShare = 0.030f;   // open ground, of the sphere
        private const int   MinRegions        = 6;
        private const int   MaxRegions        = 300;
        // On the peopled samples. Sprawl is the cities, farmland, settlements and stock history's
        // places grew round them: the whole of where people live, and so of what a run can visit.
        private const int   MinSprawl         = 150;     // sprawled locations in the world
        private const int   SprawlForARun     = 40;      // sprawled locations on the richest landmass (the old 40 fields)
        private const int   MinViableSpawns   = 100;     // cells beside the sprawl the spawn rule can draw
        private const int   BlurbRoom        = 56;      // characters a variant's one-line description may run to
        private const float MaxTemperateClimateShare = 0.06f; // hot or cold land in a temperate world, each
        private const float MinClimateShare = 0.45f;          // a hot or cold world's own climate, of its land

        public static string BuildReport()
        {
            var sb = new StringBuilder();
            var faults = new List<string>();

            sb.AppendLine("WORLD VARIANT AUDIT");
            sb.AppendLine("===================");
            sb.AppendLine();

            CheckTable(sb, faults);
            CheckGlyphs(sb, faults);

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
        /// <summary>
        /// Every biome and every location draws in a glyph of its own. Two kinds sharing one are two
        /// kinds the map cannot tell apart, and nothing else would ever notice.
        /// </summary>
        private static void CheckGlyphs(StringBuilder sb, List<string> faults)
        {
            var owners = BiomeDatabase.Biomes.Values.Select(b => (b.Name, b.Glyph))
                .Concat(BiomeDatabase.Locations.Values.Select(l => (l.Name, l.Glyph)));
            int kinds = 0;
            foreach (var group in owners.GroupBy(o => o.Glyph))
            {
                kinds += group.Count();
                if (group.Count() > 1)
                    faults.Add($"glyph '{group.Key}' is shared by {string.Join(", ", group.Select(g => g.Name))}");
            }
            sb.AppendLine($"GLYPHS: {kinds} biome and location kind(s) checked for a glyph of their own.");
            sb.AppendLine();
        }

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
            sb.AppendLine($"  climate offset {variant.Shape.TemperatureOffset.ToString("+0.00;-0.00;0", CultureInfo.InvariantCulture)}");
            sb.AppendLine("   seed        land  forest   mtn+pk   coast   plain    hot    cold  | inland  shallow  masses | sprawl  settl  viable   best  regions");

            var ordinals = OrdinalsOf(variant).ToList();
            var built = new List<WorldStats>();

            for (int i = 0; i < ordinals.Count; i++)
            {
                int ordinal = ordinals[i];
                int seed = SkyMoons.WorldSeed(ordinal);
                var w = BuildWorld(variant, seed, positions, adjacency, withRegions: i < RegionSamples);
                built.Add(w);

                string Num(int x) => x >= 0 ? x.ToString() : "-";
                if (i < PrintedRows)
                    sb.AppendLine($"  {seed,11}  {Pct(w.Land, w.Total)}  "
                                + $"{Pct(w.Forest, w.Total)}  {Pct(w.Mountain, w.Total)}  "
                                + $"{Pct(w.Coast, w.Total)}  {Pct(w.Plain, w.Total)}  "
                                + $"{Pct(w.Hot, w.Total)}  {Pct(w.Cold, w.Total)}  | "
                                + $"{Pct(w.StrandedCoast, Math.Max(w.BandCoast, 1)),6}  "
                                + $"{Pct(w.Sea, Math.Max(w.Sea + w.Ocean, 1)),7}  "
                                + $"{w.Landmasses,6} | {Num(w.Sprawl),6}  {Num(w.Settlements),5}  "
                                + $"{Num(w.ViableSpawns),6}  {Num(w.BestLandmassSprawl),5}  "
                                + $"{(w.Regions > 0 ? w.Regions.ToString() : "-"),7}");

                string where = $"{variant.Id} (moon {ordinal}, seed {seed})";
                float land = Share(w.Land, w.Total);
                if (land < MinLandShare)
                    faults.Add($"{where}: only {P(land)} of the sphere is land (floor {P(MinLandShare, 0)}) - nowhere to walk");
                if (land > MaxLandShare)
                    faults.Add($"{where}: {P(land)} of the sphere is land (ceiling {P(MaxLandShare, 0)}) - the sea has stopped shaping it");
                if (Share(w.Spawnable, w.Total) < MinSpawnableShare)
                    faults.Add($"{where}: only {P(Share(w.Spawnable, w.Total), 2)} of the sphere is open ground "
                             + $"(floor {P(MinSpawnableShare)}) - nowhere to live and nowhere to wake");
                if (w.Sprawl >= 0 && w.Sprawl < MinSprawl)
                    faults.Add($"{where}: history's places sprawled only {w.Sprawl} location(s) (floor {MinSprawl}) - "
                             + "this world is unpeopled");
                if (w.ViableSpawns >= 0 && w.ViableSpawns < MinViableSpawns)
                    faults.Add($"{where}: only {w.ViableSpawns} cell(s) of open ground lie beside the sprawl "
                             + $"(floor {MinViableSpawns}) - InitializeProtagonist has almost nothing to draw from");
                if (w.BestLandmassSprawl >= 0 && w.BestLandmassSprawl < SprawlForARun)
                    faults.Add($"{where}: the richest landmass carries {w.BestLandmassSprawl} sprawled location(s) "
                             + $"(floor {SprawlForARun}) - the best country in this world is a hamlet");
                if (w.ShoreBreaches > 0)
                    faults.Add($"{where}: {w.ShoreBreaches} land cell(s) on the water are neither shore nor sea ice");
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

            var peopled = built.Where(w => w.Sprawl >= 0).ToList();
            sb.AppendLine($"  {"worst",11}  {Pct(built.Min(w => w.Land), built[0].Total)}  "
                        + $"{"",6}   {"",6}   {"",6}   {"",6}   {"",6}  {"",6}  | "
                        + $"{(P(built.Max(w => Share(w.StrandedCoast, Math.Max(w.BandCoast, 1))))),6}  "
                        + $"{(P(built.Min(w => Share(w.Sea, Math.Max(w.Sea + w.Ocean, 1))))),7}  "
                        + $"{built.Max(w => w.Landmasses),6} | "
                        + (peopled.Count == 0 ? "" :
                           $"{peopled.Min(w => w.Sprawl),6}  {peopled.Min(w => w.Settlements),5}  "
                         + $"{peopled.Min(w => w.ViableSpawns),6}  {peopled.Min(w => w.BestLandmassSprawl),5}"));

            // The climate. A temperate world should have a little hot and a little cold country and
            // not much of either; a climate world should be mostly its climate. Judged over the whole
            // sample, since one temperate seed with no desert in it is no fault at all.
            float hotShare  = built.Sum(w => Share(w.Hot,  w.Land)) / built.Count;
            float coldShare = built.Sum(w => Share(w.Cold, w.Land)) / built.Count;
            float hotZones  = (float)built.Sum(w => w.HotZones)  / built.Count;
            float coldZones = (float)built.Sum(w => w.ColdZones) / built.Count;
            sb.AppendLine($"  climate: hot {P(hotShare)} and cold {P(coldShare)} of the land on average; "
                        + $"canyon {built.Sum(w => w.Canyon)}, frozen sea {built.Sum(w => w.FrozenSea)} cell(s) over the sample");
            sb.AppendLine($"  zones:   {hotZones.ToString("F1", CultureInfo.InvariantCulture)} hot and "
                        + $"{coldZones.ToString("F1", CultureInfo.InvariantCulture)} cold per world on average "
                        + $"(cold per world: {string.Join(" ", built.Select(w => w.ColdZones))})");
            float t = variant.Shape.TemperatureOffset;
            if (t == 0f)
            {
                if (hotShare <= 0f || coldShare <= 0f)
                    faults.Add($"{variant.Id}: a temperate variant with no {(hotShare <= 0f ? "hot" : "cold")} country anywhere in {built.Count} worlds");
                if (hotShare > MaxTemperateClimateShare || coldShare > MaxTemperateClimateShare)
                    faults.Add($"{variant.Id}: {P(Math.Max(hotShare, coldShare))} of a temperate variant's land is hot or cold "
                             + $"(ceiling {P(MaxTemperateClimateShare, 0)}) - the climate pockets have stopped being pockets");
            }
            else
            {
                float own = t > 0 ? hotShare : coldShare;
                if (own < MinClimateShare)
                    faults.Add($"{variant.Id}: only {P(own)} of a {(t > 0 ? "hot" : "cold")} variant's land is {(t > 0 ? "hot" : "cold")} "
                             + $"(floor {P(MinClimateShare, 0)}) - its offset does not make the world it names");
            }

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

        // Sprawl, Settlements, ViableSpawns and BestLandmassSprawl are -1 on a world not peopled.
        private readonly record struct WorldStats(
            int Total, int Land, int Forest, int Mountain, int Coast, int Plain,
            int Spawnable, int Sprawl, int Settlements, int ViableSpawns, int BestLandmassSprawl,
            int BandCoast, int StrandedCoast, int Sea, int Ocean,
            int Landmasses, int Regions,
            int Hot, int Cold, int Canyon, int FrozenSea,
            int HotZones, int ColdZones, int ShoreBreaches);

        /// <summary>
        /// Land cells on the water that are neither shore nor sea ice. Always zero: CoastRule makes
        /// every such cell shore, and only cold turns shore into anything else.
        /// </summary>
        private static int ShoreBreaches(string[] biome, List<int>[] adjacency)
        {
            int breaches = 0;
            for (int v = 0; v < biome.Length; v++)
            {
                string b = biome[v];
                if (BiomeDatabase.WaterBiomes.Contains(b) || BiomeDatabase.CoastBiomes.Contains(b) || b == BiomeDatabase.SeaIce)
                    continue;
                if (adjacency[v].Any(w => BiomeDatabase.WaterBiomes.Contains(biome[w]))) breaches++;
            }
            return breaches;
        }

        /// <summary>
        /// A climate zone too small to count as one: a stray handful of cells over the threshold is a
        /// speck on the map, not a cold country anyone would name.
        /// </summary>
        private const int MinZoneCells = 30;

        /// <summary>
        /// How many separate hot or cold countries a world has: connected stretches of the sphere,
        /// water included, whose temperature crosses the threshold, counting only those of
        /// <see cref="MinZoneCells"/> or more.
        /// </summary>
        private static int Zones(float[] temperature, List<int>[] adjacency, Func<float, bool> inZone)
        {
            int n = temperature.Length;
            var (of, count) = SpawnRule.Landmasses(n, v => adjacency[v], v => inZone(temperature[v]));
            var size = new int[count];
            for (int v = 0; v < n; v++) if (of[v] >= 0) size[of[v]]++;
            return size.Count(s => s >= MinZoneCells);
        }

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

            // Exactly what GenerateWorld does: the shared classifier, coast rule and climate included.
            // Measuring the noise's proposal instead would measure a world nobody plays - and the
            // difference between them is the whole of what a wide coast band claims and does not deliver.
            var classified = WorldClassifier.Classify(variant.Shape, n, v => positions[v], v => adjacency[v], offset);
            var biome = classified.Biome;
            var settlement = classified.Settlement;
            int bandCoast = classified.BandCoast;

            int land = 0, forest = 0, mountain = 0, coast = 0, plain = 0;
            int sea = 0, ocean = 0, hot = 0, cold = 0;
            for (int v = 0; v < n; v++)
            {
                bool isLand = !BiomeDatabase.WaterBiomes.Contains(biome[v]);
                if (isLand) land++;
                switch (biome[v])
                {
                    case "forest":   forest++;   break;
                    case "mountain":
                    case "peak":     mountain++; break;
                    case "coast":    coast++;    break;
                    case "plain":    plain++;    break;
                    case "sea":      sea++;      break;
                    case "ocean":    ocean++;    break;
                    case BiomeDatabase.HotSteppe:
                    case BiomeDatabase.Desert:
                    case BiomeDatabase.Jungle:
                    case BiomeDatabase.Canyon:     hot++;  break;
                    case BiomeDatabase.SeaIce:
                    case BiomeDatabase.Glacier:
                    case BiomeDatabase.Snowfield:
                    case BiomeDatabase.ColdSteppe: cold++; break;
                }
            }

            int spawnable = 0;
            for (int v = 0; v < n; v++) if (SpawnRule.IsOpenGround(biome[v])) spawnable++;

            var (_, landmassCount) = SpawnRule.Landmasses(n, v => adjacency[v], v => !BiomeDatabase.WaterBiomes.Contains(biome[v]));

            // The peopled half — regions, history, sprawl and the spawn they allow — on the sampled
            // worlds only, built exactly as the game builds it. -1 everywhere else.
            int regionCount = 0, sprawl = -1, settlements = -1, viable = -1, bestSprawl = -1;
            if (withRegions)
            {
                var world = HeadlessWorld.Build(seed, positions, adjacency, variant);
                regionCount = world.Regions.Regions.Count;
                var (history, map) = PeopleWorld(world);
                _ = history;
                sprawl = map.Sites.Keys.Count(map.IsSprawl);
                settlements = map.Count(Cathedral.Game.History.SiteRole.Settlement);
                viable = SpawnRule.Candidates(n, v => world.Biome[v], v => adjacency[v],
                                              v => map.At(v) != null, map.IsSprawl).Count(v => adjacency[v].Any(map.IsSprawl));

                var (massOf, massCount) = SpawnRule.Landmasses(n, v => adjacency[v], v => !BiomeDatabase.WaterBiomes.Contains(world.Biome[v]));
                var perMass = new int[Math.Max(massCount, 1)];
                foreach (int v in map.Sites.Keys) if (map.IsSprawl(v) && massOf[v] >= 0) perMass[massOf[v]]++;
                bestSprawl = perMass.Max();
            }

            return new WorldStats(n, land, forest, mountain, coast, plain,
                                  spawnable, sprawl, settlements, viable, bestSprawl,
                                  bandCoast, classified.StrandedCoast, sea, ocean, landmassCount, regionCount,
                                  hot, cold, classified.Climate.Canyon, classified.Climate.FrozenSea,
                                  Zones(classified.Temperature, adjacency, ClimateRule.IsHot),
                                  Zones(classified.Temperature, adjacency, ClimateRule.IsCold),
                                  ShoreBreaches(biome, adjacency));
        }

        /// <summary>
        /// A headless world's history and settled country, built exactly as <c>MicroworldInterface</c>
        /// builds them (the cave noise aside: a cave only ever blocks a few cells).
        /// </summary>
        public static (Cathedral.Game.History.WorldHistory History, Cathedral.Game.History.SettlementMap Map) PeopleWorld(HeadlessWorld world)
        {
            var geo = Cathedral.Game.History.HistoryGeography.Build(world.Regions, world.VertexCount, v => world.Adjacency[v],
                                                                    v => world.Biome[v], world.Variant.Shape.SettlementDensity);
            var history = Cathedral.Game.History.WorldHistoryGenerator.Generate(geo, world.Seed);
            var map = Cathedral.Game.History.SettlementSprawl.Build(world.VertexCount, v => world.Biome[v], v => world.Adjacency[v],
                history.Places, v => false,
                v => world.Regions.RegionAt(v) is int r && r >= 0 ? history.OwnerOf(r) : null,
                world.Variant.Shape.SettlementDensity, GameRng.ForWorld(world.Seed, "sprawl"));
            return (history, map);
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
