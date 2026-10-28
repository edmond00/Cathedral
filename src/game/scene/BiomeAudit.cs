using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cathedral.Game.Scene.Building;

namespace Cathedral.Game.Scene;

/// <summary>
/// Headless audit of the wilderness factories — what a player finds on arriving somewhere wild, and
/// whether two arrivals at the same kind of place are different arrivals. It builds
/// <see cref="SampleSize"/> locations of every biome and measures three things:
///
/// <list type="bullet">
/// <item><b>Diversity.</b> Three forests should be three different forests: different areas,
/// different sections, a different shape on the map. Measured as the share of every triple of
/// samples whose members all differ from one another in content (area kinds and section names,
/// Jaccard distance at least <see cref="DistinctContent"/>) and all three in layout signature.</item>
/// <item><b>Counted diversity</b> (<c>div2%</c>, <c>wjac</c>). The first measure compares the <i>set</i> of
/// area kinds, which saturates in a large place: every town of fifty rooms has a kitchen, a bedroom and
/// a street, so two towns with different trades, rooms and furniture still read as the same. This one
/// compares <i>counts</i> — how many of each area kind and of each point-of-interest kind, names ignored
/// — by weighted Jaccard (shared counts over all counts), and passes a triple when every pair differs by
/// at least <see cref="DistinctContent"/>. It sees three forges against one, and a pottery's wheel
/// against a tannery's vats.</item>
/// <item><b>Layout coherence.</b> The map should read as the land it is. Every section must be one
/// connected piece, the place must not be a bare corridor, and a factory that states its profile
/// (<see cref="SceneFactory.Profile"/> and <see cref="Section.Tier"/>) is held to it: a pyramid narrows
/// as it rises and its tiers meet only their neighbours, a descent goes down through one mouth, a
/// radial place hangs off its hub.</item>
/// <item><b>Exploration.</b> The ways of moving that are not a plain walk — climb, scale, cross, swim,
/// slip in, follow a path, voyage toward a seen landscape — and how many of them one visit offers.</item>
/// </list>
///
/// It fails (✗) a biome under <see cref="DiversityFloor"/> or outside <see cref="MinAreas"/>–<see cref="MaxAreas"/>
/// areas, and lists every layout fault as a warning.
///
/// Run after touching a wilderness factory, its areas, or <c>OutdoorLayout</c>.
/// </summary>
public static class BiomeAudit
{
    private const int SampleSize = 40;

    /// <summary>Two samples are different in content when at least this share of their kinds differs.</summary>
    private const double DistinctContent = 0.34;

    /// <summary>
    /// The floors a biome is failed (✗) below. Diversity was 5% across the wilderness before the
    /// overhaul and 70% after, the lowest biome at 50%; a change that drops one under 40% has undone
    /// most of it. The area range is the agreed size of a wild place: larger than a corridor, smaller
    /// than a village.
    /// </summary>
    private const double DiversityFloor = 40;
    private const int MinAreas = 5, MaxAreas = 10;

    /// <summary>The ways of moving between areas besides a walk along an edge, by the connector that offers each.</summary>
    private static readonly (string Verb, Type Connector)[] MovementKinds =
    {
        ("climb",       typeof(CliffPointOfInterest)),
        ("scale",       typeof(ScalePointOfInterest)),
        ("cross",       typeof(CrossingPointOfInterest)),
        ("swim",        typeof(WaterCrossingPointOfInterest)),
        ("slip_into",   typeof(SlipIntoPointOfInterest)),
        ("follow_path", typeof(PathPointOfInterest)),
        ("voyage",      typeof(LandscapePointOfInterest)),
    };

    public sealed record Biome(string Label, Func<SceneFactory> Create, int Samples = SampleSize);

    /// <summary>
    /// The settled locations, for comparison (<c>--biome-audit settled</c>): every factory the sprawl and
    /// the history put on the map. A city or an urban historic place is dozens of buildings, so it is
    /// sampled twelve times rather than forty. They state no profile, so coherence is connectivity alone.
    /// </summary>
    public static IEnumerable<Biome> SettledBiomes()
        => Settled.SettledSceneFactories.All.Select(e => new Biome(e.AuditLabel, e.Create, e.Dense ? 12 : 20));

    public static IEnumerable<Biome> Biomes()
    {
        yield return new("PLAIN",    () => new Plain.PlainSceneFactory());
        yield return new("FOREST",   () => new Forest.ForestSceneFactory());
        yield return new("CAVE",     () => new Cave.CaveSceneFactory());
        yield return new("COAST",    () => new Coast.CoastSceneFactory());
        yield return new("MOUNTAIN", () => new Mountain.MountainSceneFactory());
        yield return new("PEAK",     () => new Peak.PeakSceneFactory());
        foreach (var e in Shared.ClimateSceneFactories.All)
            yield return new(e.AuditLabel, e.Create);
    }

    public static string BuildReport(string? only = null)
    {
        var sb       = new StringBuilder();
        var warnings = new List<string>();
        var rows     = new List<Row>();

        bool settled = string.Equals(only, "settled", StringComparison.OrdinalIgnoreCase);
        foreach (var biome in settled ? SettledBiomes() : Biomes())
        {
            if (only != null && !settled && !biome.Label.Replace(" ", "").Equals(only.Replace(" ", "").Replace("_", ""), StringComparison.OrdinalIgnoreCase))
                continue;
            var samples = new List<Sample>();
            LayoutProfile profile = LayoutProfile.Unstated;
            for (int id = 1; id <= biome.Samples; id++)
            {
                var factory = biome.Create();
                var scene   = factory.Build(id);
                profile     = factory.Profile;
                samples.Add(Measure(biome.Label, id, scene, factory.Profile, warnings));
            }
            rows.Add(Summarise(biome.Label, profile, samples, sb));
        }

        var report = new StringBuilder();
        report.AppendLine("=== BIOME AUDIT ===");
        report.AppendLine($"{(settled ? "20 (12 for a city)" : SampleSize.ToString())} locations per kind. Diversity = share of sample triples all different in content");
        report.AppendLine($"(Jaccard >= {DistinctContent:0.00}) and in layout. Coherence = share of samples passing every layout check.");
        report.AppendLine("div2% / wjac = the same test on counted content (areas and points of interest by kind, how many of each).");
        report.AppendLine("Exploration = movement kinds besides walking per visit (of 7), and how many a biome ever offers.");
        report.AppendLine();
        report.AppendLine($"{"biome",-12} {"areas",-9} {"mean",-5} {"out",-4} {"sect",-6} {"kinds",-6} {"div%",5} {"jac",5} {"div2%",5} {"wjac",5} {"lay",4} {"coh%",5} {"chain%",6} {"move",5} {"3+%",5} {"seen",4}  profile");
        foreach (var r in rows)
            report.AppendLine($"{r.Label,-12} {r.Areas,-9} {r.MeanAreas,-5:0} {r.MeanOutdoor,-4:0} {r.Sections,-6} {r.AreaKinds,-6} {r.Diversity,5:0} {r.MeanJaccard,5:0.00} {r.Counted,5:0} {r.MeanWeighted,5:0.00} {r.Layouts,4} {r.Coherence,5:0} {r.ChainShare,6:0} {r.MeanMoves,5:0.0} {r.ThreePlus,5:0} {r.KindsSeen,4}  {r.Profile}");
        if (rows.Count > 1)
        {
            report.AppendLine($"{"MEAN",-12} {"",-9} {rows.Average(r => r.MeanAreas),-5:0} {rows.Average(r => r.MeanOutdoor),-4:0} {"",-6} {rows.Average(r => r.AreaKindCount),-6:0.0} {rows.Average(r => r.Diversity),5:0} {rows.Average(r => r.MeanJaccard),5:0.00} {rows.Average(r => r.Counted),5:0} {rows.Average(r => r.MeanWeighted),5:0.00} {"",4} {rows.Average(r => r.Coherence),5:0} {rows.Average(r => r.ChainShare),6:0} {rows.Average(r => r.MeanMoves),5:0.0} {rows.Average(r => r.ThreePlus),5:0} {rows.Average(r => r.KindsSeenCount),4:0.0}");
        }
        foreach (var r in settled ? new List<Row>() : rows)
        {
            if (r.Diversity < DiversityFloor)
                report.AppendLine($"  ✗ {r.Label}: diversity {r.Diversity:0}% is under the {DiversityFloor:0}% floor");
            if (r.MinAreaCount < MinAreas || r.MaxAreaCount > MaxAreas)
                report.AppendLine($"  ✗ {r.Label}: {r.Areas} areas, outside {MinAreas}-{MaxAreas}");
        }
        report.AppendLine();
        report.Append(sb);

        report.AppendLine();
        if (warnings.Count == 0)
        {
            report.AppendLine("No layout faults.");
            return report.ToString();
        }
        report.AppendLine($"--- {warnings.Count} WARNING(S): LAYOUT FAULTS, BY KIND ---");
        foreach (var group in warnings.GroupBy(Kind).OrderByDescending(g => g.Count()))
        {
            report.AppendLine($"  [{group.Count(),4}] {group.Key}");
            foreach (var example in group.Distinct().Take(3))
                report.AppendLine($"         e.g. {example}");
        }
        return report.ToString();
    }

    private static string Kind(string warning)
    {
        var body = System.Text.RegularExpressions.Regex.Replace(warning, @"^[A-Z ]+ \d+: ", "");
        body     = System.Text.RegularExpressions.Regex.Replace(body, @"'[^']*'", "'…'");
        return System.Text.RegularExpressions.Regex.Replace(body, @"\b\d+\b", "N");
    }

    // ── One location ──────────────────────────────────────────────────────────

    private sealed record Sample(
        int Id,
        HashSet<string> Content,
        string Layout,
        int Areas,
        int Outdoor,
        int Sections,
        bool IsChain,
        bool Coherent,
        HashSet<string> Moves,
        List<string> AreaKinds,
        Dictionary<string, int> Counted);

    private static Sample Measure(string label, int id, Scene scene, LayoutProfile profile, List<string> warnings)
    {
        var areas    = scene.AllAreas;
        var index    = areas.Select((a, i) => (a, i)).ToDictionary(p => p.a.Id, p => p.i);
        var pois     = areas.SelectMany(a => a.PointsOfInterest).Distinct().ToList();
        var connectors = pois.OfType<ConnectorPointOfInterest>().ToList();

        // The walk graph: edges, and every two-way connector a body can pass (not a path, which skips
        // areas already joined, and not a landscape, which is one way and seen from above).
        var adj = areas.Select(_ => new HashSet<int>()).ToArray();
        void Join(Guid a, Guid b)
        {
            if (!index.TryGetValue(a, out int i) || !index.TryGetValue(b, out int j) || i == j) return;
            adj[i].Add(j); adj[j].Add(i);
        }
        foreach (var (from, tos) in scene.AreaGraph)
            foreach (var to in tos) Join(from, to);
        foreach (var c in connectors)
            if (c is not PathPointOfInterest and not LandscapePointOfInterest)
                Join(c.AreaA.Id, c.AreaB.Id);

        int edges   = adj.Sum(s => s.Count) / 2;
        int degree3 = adj.Count(s => s.Count >= 3);
        int cycles  = edges - areas.Count + Components(adj, Enumerable.Range(0, areas.Count));
        bool chain  = cycles == 0 && adj.All(s => s.Count <= 2);
        int climbs  = connectors.Count(c => c is CliffPointOfInterest or ScalePointOfInterest);

        bool coherent = true;
        void Fault(string message) { coherent = false; warnings.Add($"{label} {id}: {message}"); }

        // Every area reachable on foot (or by a climb) from the first.
        if (areas.Count > 0 && Reach(adj, 0).Count < areas.Count)
            Fault($"{areas.Count - Reach(adj, 0).Count} area(s) unreachable from '{areas[0].DisplayName}'");

        // A section is one connected piece of ground. An area reached only by a climb, a scale or a
        // squeeze — a roof scaled from the street, an alcove up a toe-hold trail — is a top, not ground,
        // and belongs to the section it stands over without having to be walked to inside it.
        var walkAdj = areas.Select(_ => new HashSet<int>()).ToArray();
        foreach (var (from, tos) in scene.AreaGraph)
            foreach (var to in tos)
                if (index.TryGetValue(from, out int i) && index.TryGetValue(to, out int j) && i != j) { walkAdj[i].Add(j); walkAdj[j].Add(i); }
        foreach (var c in connectors)
            if (c is DoorPointOfInterest or StairPointOfInterest or CrossingPointOfInterest or WaterCrossingPointOfInterest
                && index.TryGetValue(c.AreaA.Id, out int i) && index.TryGetValue(c.AreaB.Id, out int j) && i != j)
            { walkAdj[i].Add(j); walkAdj[j].Add(i); }
        foreach (var s in scene.Sections)
        {
            var members = s.Areas.Where(a => index.ContainsKey(a.Id)).Select(a => index[a.Id])
                                 .Where(i => walkAdj[i].Count > 0 || adj[i].Count == 0).ToHashSet();
            if (members.Count > 1 && Components(walkAdj, members, restrict: members) > 1)
                Fault($"section '{s.DisplayName}' is split in pieces");
        }

        CheckProfile(scene, profile, areas, index, adj, Fault);

        var moves = MovementKinds.Where(k => connectors.Any(c => k.Connector.IsInstanceOfType(c)))
                                 .Select(k => k.Verb).ToHashSet();

        var kinds   = areas.Select(a => a.GetType().Name.Replace("Area", "")).ToList();
        var content = kinds.Select(k => "A:" + k)
                           .Concat(scene.Sections.Select(s => "S:" + s.DisplayName))
                           .ToHashSet();
        string layout = $"{string.Join("/", scene.Sections.Select(s => s.Areas.Count))}|e{edges}|c{cycles}|b{degree3}|k{climbs}";

        // Counted content: every area and every point of interest by kind, how many of each. Connectors
        // are counted too — a moat and a ford are content — but a path laid along edges is not.
        var counted = kinds.Select(k => "A:" + k)
            .Concat(pois.Where(p => p is not PathPointOfInterest and not LandscapePointOfInterest)
                        .Select(p => "P:" + p.GetType().Name.Replace("PointOfInterest", "")))
            .GroupBy(k => k).ToDictionary(g => g.Key, g => g.Count());

        return new Sample(id, content, layout, areas.Count, scene.OutdoorAreas.Count, scene.Sections.Count, chain, coherent, moves, kinds, counted);
    }

    /// <summary>The structural promise of a stated profile, checked against the built graph.</summary>
    private static void CheckProfile(Scene scene, LayoutProfile profile, List<Area> areas,
                                     Dictionary<Guid, int> index, HashSet<int>[] adj, Action<string> fault)
    {
        if (profile == LayoutProfile.Unstated) return;

        var tierOf = new int[areas.Count];
        foreach (var s in scene.Sections)
            foreach (var a in s.Areas)
                if (index.TryGetValue(a.Id, out int i)) tierOf[i] = s.Tier;
        var tiers = scene.Sections.Select(s => s.Tier).Distinct().OrderBy(t => t).ToList();
        int Size(int tier) => scene.Sections.Where(s => s.Tier == tier).Sum(s => s.Areas.Count);

        switch (profile)
        {
            case LayoutProfile.Pyramid:
            case LayoutProfile.Descent:
                // Tiers meet only their neighbours.
                for (int i = 0; i < adj.Length; i++)
                    foreach (int j in adj[i])
                        if (Math.Abs(tiers.IndexOf(tierOf[i]) - tiers.IndexOf(tierOf[j])) > 1)
                            fault($"'{areas[i].DisplayName}' (tier {tierOf[i]}) joins '{areas[j].DisplayName}' (tier {tierOf[j]}), skipping a tier");
                if (tiers.Count < 2) fault("a tiered profile with one tier");
                if (profile == LayoutProfile.Pyramid)
                {
                    for (int t = 1; t < tiers.Count; t++)
                        if (Size(tiers[t]) > Size(tiers[t - 1]))
                            fault($"tier {tiers[t]} has {Size(tiers[t])} areas, more than the {Size(tiers[t - 1])} below it");
                    if (tiers.Count >= 3 && Size(tiers[^1]) > 2)
                        fault($"the top tier holds {Size(tiers[^1])} areas, not a summit");
                }
                else
                {
                    // One mouth: a single area of the first tier joins the second.
                    var mouths = Enumerable.Range(0, adj.Length)
                        .Where(i => tierOf[i] == tiers[0] && adj[i].Any(j => tierOf[j] != tiers[0])).Count();
                    if (tiers.Count >= 2 && mouths != 1) fault($"the way down starts in {mouths} places, not one");
                }
                break;

            case LayoutProfile.Radial:
                // Every other section touches the hub section (tier 0).
                foreach (var s in scene.Sections.Where(s => s.Tier != tiers[0]))
                {
                    bool touches = s.Areas.Where(a => index.ContainsKey(a.Id))
                        .Any(a => adj[index[a.Id]].Any(j => tierOf[j] == tiers[0]));
                    if (!touches) fault($"section '{s.DisplayName}' does not touch the hub");
                }
                break;

            case LayoutProfile.Spine:
                // The spine (tier 0) is a line; every other section hangs off it.
                var spine = Enumerable.Range(0, adj.Length).Where(i => tierOf[i] == tiers[0]).ToHashSet();
                foreach (var s in scene.Sections.Where(s => s.Tier != tiers[0]))
                {
                    bool touches = s.Areas.Where(a => index.ContainsKey(a.Id))
                        .Any(a => adj[index[a.Id]].Any(j => spine.Contains(j)));
                    if (!touches) fault($"section '{s.DisplayName}' does not touch the spine");
                }
                break;

            case LayoutProfile.Mosaic:
                // Patches of country meet each other in more than a line: some area joins two others'
                // sections, or the sections close a loop.
                var sectionOf = new int[areas.Count];
                for (int si = 0; si < scene.Sections.Count; si++)
                    foreach (var a in scene.Sections[si].Areas)
                        if (index.TryGetValue(a.Id, out int i)) sectionOf[i] = si;
                var links = new HashSet<(int, int)>();
                for (int i = 0; i < adj.Length; i++)
                    foreach (int j in adj[i])
                        if (sectionOf[i] < sectionOf[j]) links.Add((sectionOf[i], sectionOf[j]));
                if (scene.Sections.Count >= 3 && links.Count < scene.Sections.Count)
                    fault($"{scene.Sections.Count} sections meet along only {links.Count} border(s): a line, not a mosaic");
                break;
        }
    }

    private static HashSet<int> Reach(HashSet<int>[] adj, int start, HashSet<int>? restrict = null)
    {
        var seen  = new HashSet<int> { start };
        var queue = new Queue<int>(seen);
        while (queue.Count > 0)
            foreach (int n in adj[queue.Dequeue()])
                if ((restrict == null || restrict.Contains(n)) && seen.Add(n)) queue.Enqueue(n);
        return seen;
    }

    private static int Components(HashSet<int>[] adj, IEnumerable<int> nodes, HashSet<int>? restrict = null)
    {
        var left = nodes.ToHashSet();
        int count = 0;
        while (left.Count > 0)
        {
            count++;
            left.ExceptWith(Reach(adj, left.First(), restrict));
        }
        return count;
    }

    // ── One biome ─────────────────────────────────────────────────────────────

    private sealed record Row(string Label, string Areas, int MinAreaCount, int MaxAreaCount, double MeanAreas, double MeanOutdoor, string Sections, string AreaKinds, int AreaKindCount,
                              double Diversity, double MeanJaccard, double Counted, double MeanWeighted, int Layouts, double Coherence, double ChainShare,
                              double MeanMoves, double ThreePlus, string KindsSeen, int KindsSeenCount, LayoutProfile Profile);

    private static Row Summarise(string label, LayoutProfile profile, List<Sample> samples, StringBuilder detail)
    {
        int n = samples.Count;
        double Jac(Sample a, Sample b)
            => 1.0 - (double)a.Content.Intersect(b.Content).Count() / a.Content.Union(b.Content).Count();

        double WJac(Sample a, Sample b)
        {
            double shared = 0, total = 0;
            foreach (var key in a.Counted.Keys.Union(b.Counted.Keys))
            {
                int x = a.Counted.GetValueOrDefault(key), y = b.Counted.GetValueOrDefault(key);
                shared += Math.Min(x, y);
                total  += Math.Max(x, y);
            }
            return total == 0 ? 0 : 1.0 - shared / total;
        }

        var wdist = new double[n, n];
        double wsum = 0;
        for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++) { wdist[i, j] = wdist[j, i] = WJac(samples[i], samples[j]); wsum += wdist[i, j]; }
        int counted = 0;
        for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
                for (int k = j + 1; k < n; k++)
                    if (wdist[i, j] >= DistinctContent && wdist[i, k] >= DistinctContent && wdist[j, k] >= DistinctContent)
                        counted++;

        var dist = new double[n, n];
        double sum = 0; int pairs = 0;
        for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++) { dist[i, j] = dist[j, i] = Jac(samples[i], samples[j]); sum += dist[i, j]; pairs++; }

        int triples = 0, distinct = 0;
        for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
                for (int k = j + 1; k < n; k++)
                {
                    triples++;
                    if (dist[i, j] >= DistinctContent && dist[i, k] >= DistinctContent && dist[j, k] >= DistinctContent
                        && samples[i].Layout != samples[j].Layout && samples[i].Layout != samples[k].Layout
                        && samples[j].Layout != samples[k].Layout)
                        distinct++;
                }

        var kindsSeen = samples.SelectMany(s => s.Moves).Distinct().ToList();
        var areaKinds = samples.SelectMany(s => s.AreaKinds).Distinct().Count();

        detail.AppendLine($"--- {label} ---");
        detail.AppendLine($"  area kinds ({areaKinds}): " + string.Join(", ",
            samples.SelectMany(s => s.AreaKinds.Distinct()).GroupBy(k => k)
                   .OrderByDescending(g => g.Count()).Select(g => $"{g.Key} {100 * g.Count() / n}%")));
        detail.AppendLine($"  sections: " + string.Join(", ",
            samples.SelectMany(s => s.Content.Where(c => c.StartsWith("S:")).Select(c => c[2..])).GroupBy(k => k)
                   .OrderByDescending(g => g.Count()).Select(g => $"{g.Key} {100 * g.Count() / n}%")));
        detail.AppendLine($"  movement: " + string.Join(", ",
            MovementKinds.Select(k => $"{k.Verb} {100 * samples.Count(s => s.Moves.Contains(k.Verb)) / n}%")));
        detail.AppendLine($"  layouts: " + string.Join("  ",
            samples.GroupBy(s => s.Layout).OrderByDescending(g => g.Count()).Take(6).Select(g => $"{g.Key} x{g.Count()}")));

        return new Row(
            label,
            $"{samples.Min(s => s.Areas)}-{samples.Max(s => s.Areas)}",
            samples.Min(s => s.Areas), samples.Max(s => s.Areas),
            samples.Average(s => s.Areas), samples.Average(s => s.Outdoor),
            $"{samples.Min(s => s.Sections)}-{samples.Max(s => s.Sections)}",
            areaKinds.ToString(), areaKinds,
            100.0 * distinct / Math.Max(1, triples),
            sum / Math.Max(1, pairs),
            100.0 * counted / Math.Max(1, n * (n - 1) * (n - 2) / 6),
            wsum / Math.Max(1, n * (n - 1) / 2),
            samples.Select(s => s.Layout).Distinct().Count(),
            100.0 * samples.Count(s => s.Coherent) / n,
            100.0 * samples.Count(s => s.IsChain) / n,
            samples.Average(s => s.Moves.Count),
            100.0 * samples.Count(s => s.Moves.Count >= 3) / n,
            kindsSeen.Count.ToString(), kindsSeen.Count,
            profile);
    }
}
