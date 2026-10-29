using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Narrative;

/// <summary>
/// <c>--will-audit</c>: where each will has something to do. <see cref="WillAudit"/> (in
/// <c>--mm-audit</c>) asks whether a will's target is offered <i>anywhere</i>; this asks the question a
/// player meets — standing in this place, is there a way to what the will wants? — and answers it with
/// the planner itself (<see cref="ActionPathPlanner.Plan"/>), so a "yes" is exactly a will that is not
/// refusing.
///
/// <list type="bullet">
/// <item><b>Coverage.</b> For every will and every location kind the audits sweep, from the area a visit
/// opens on, at the two hours <see cref="WillAudit"/> uses, over <see cref="Samples"/> location ids:
/// whether a plan exists, and how long it is.</item>
/// <item><b>Related coverage.</b> The same, from the areas each will declares it belongs in
/// (<see cref="ModusMentis.WillPlaces"/>, area kinds, typed). The headline number: a will standing in
/// a place it plainly should serve and finding nothing to do there is the playtest complaint.</item>
/// <item><b>Breadth.</b> The share of location kinds where the will has something to do at all. Above
/// <see cref="MaxBreadth"/> a will is no longer a desire but a general mind that happens to say no
/// sometimes, and a ✗ line fails the audit.</item>
/// </list>
///
/// Wills whose every target needs a state play brings about (a conversation with somebody won over, a
/// beast made hostile, a corpse) cannot be measured on a freshly built scene; they are listed and left
/// out of the shares. <c>--will-audit &lt;file.json&gt;</c> also writes the numbers out for the report.
/// </summary>
public static class WillCoverageAudit
{
    /// <summary>Location ids per factory; a dense one (a town, a city) is built once.</summary>
    private const int Samples = 3, DenseSamples = 1;

    private static readonly TimePeriod[] Hours = { TimePeriod.Morning, TimePeriod.Night };

    /// <summary>
    /// <see cref="MinRelated"/>: the share of its own places a will is expected to find something to do
    /// in — below it the will is marked (!), the idle will of the playtests. <see cref="MinRelatedOverall"/>:
    /// the same over every measured will, below which the audit fails (✗). It read 64.5% over 114 wills of
    /// one target, and 64% over 159 once 45 crafts joined them with several targets (the 114 alone: 69%). <see cref="MaxBreadth"/>: the share of location kinds above which a will with several
    /// targets fails, since added targets are exactly how a desire grows into a general mind. A will of one
    /// target whose act exists everywhere (to sit, to smell) was the design before; it is noted, not failed.
    /// </summary>
    public const double MinRelated = 0.60, MinRelatedOverall = 0.60, MaxBreadth = 0.75;

    /// <summary>One will's numbers.</summary>
    public sealed class Row
    {
        public string Id { get; set; } = "";
        public string Targets { get; set; } = "";
        public int TargetCount { get; set; }
        public bool Gated { get; set; }
        public int Starts { get; set; }
        public int Planned { get; set; }
        public double MeanSteps { get; set; }
        public int RelatedStarts { get; set; }
        public int RelatedPlanned { get; set; }
        public double RelatedMeanSteps { get; set; }
        public int Kinds { get; set; }
        public int KindsPlanned { get; set; }
        public string[] Places { get; set; } = Array.Empty<string>();
        public Dictionary<string, double> PerKind { get; set; } = new();
        public double Coverage => Starts == 0 ? 0 : (double)Planned / Starts;
        public double Related => RelatedStarts == 0 ? double.NaN : (double)RelatedPlanned / RelatedStarts;
        public double Breadth => Kinds == 0 ? 0 : (double)KindsPlanned / Kinds;
    }

    public static string BuildReport(string? jsonPath = null)
    {
        var all   = ModusMentisRegistry.Instance.GetAllModiMentis().ToList();
        var wills = all.Where(m => m.IsWill).OrderBy(m => m.ModusMentisId).ToList();
        var actor = new Protagonist();
        var rows  = wills.ToDictionary(m => m, m => new Row
        {
            Id          = m.ModusMentisId,
            Targets     = WillAudit.Describe(m),
            TargetCount = WillAudit.TargetCount(m),
            Places      = m.WillPlaces.Select(t => t.Name).ToArray(),
        });
        var perKind = wills.ToDictionary(m => m, _ => new Dictionary<string, (int Starts, int Planned)>());
        var census  = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);

        var started = DateTime.UtcNow;
        int scenes = 0;
        foreach (var (label, build, samples) in VerbAudit.Factories())
        for (int id = 1; id <= Math.Min(samples, samples == VerbAudit.DenseSampleSize ? DenseSamples : Samples); id++)
        {
            Cathedral.Game.Scene.Scene scene;
            try { scene = build(id); } catch { continue; }
            scenes++;
            ActionPathPlanner.Forget();
            var kinds = census.TryGetValue(label, out var k) ? k : census[label] = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var a in scene.AllAreas) kinds.Add(a.GetType().Name);
            var entry = SceneSyntheticGraphFactory.ResolveEntryArea(scene) ?? scene.AllAreas.First();

            foreach (var hour in Hours)
            foreach (var will in wills)
            {
                var row = rows[will];
                var path = PlanFor(scene, new PoV(entry, hour), actor, will);
                row.Starts++;
                var (s, p) = perKind[will].GetValueOrDefault(label);
                perKind[will][label] = (s + 1, p + (path != null ? 1 : 0));
                if (path != null) { row.Planned++; row.MeanSteps += path.Steps.Count; }

                // A related place: the first area of a kind the will declares, planned from there.
                var related = scene.AllAreas.FirstOrDefault(a => will.WillPlaces.Any(t => t.IsInstanceOfType(a)));
                if (related == null) continue;
                var rpath = related == entry ? path : PlanFor(scene, new PoV(related, hour), actor, will);
                row.RelatedStarts++;
                if (rpath != null) { row.RelatedPlanned++; row.RelatedMeanSteps += rpath.Steps.Count; }
            }
        }
        ActionPathPlanner.Forget();

        foreach (var (will, row) in rows)
        {
            if (row.Planned > 0)        row.MeanSteps        /= row.Planned;
            if (row.RelatedPlanned > 0) row.RelatedMeanSteps /= row.RelatedPlanned;
            // Gated only when the sample bears it out: a trade is a conversation, yet needs no introduction.
            row.Gated        = WillAudit.PlayGated(will) && row.Planned == 0;
            row.Kinds        = perKind[will].Count;
            row.KindsPlanned = perKind[will].Count(kv => kv.Value.Planned > 0);
            row.PerKind      = perKind[will].ToDictionary(kv => kv.Key, kv => (double)kv.Value.Planned / kv.Value.Starts);
        }

        var sb = new StringBuilder();
        sb.AppendLine("=== WILL AUDIT ===");
        sb.AppendLine($"{wills.Count} wills of {all.Count} modi mentis; {scenes} scenes built ({Samples} ids per factory, "
                    + $"{DenseSamples} for a town or city), planned from the entry area and from each will's places, "
                    + $"at {string.Join(" and ", Hours)} ({(DateTime.UtcNow - started).TotalSeconds:0}s).");
        sb.AppendLine();

        var measured = rows.Values.Where(r => !r.Gated).ToList();
        int starts = measured.Sum(r => r.Starts), planned = measured.Sum(r => r.Planned);
        int rStarts = measured.Sum(r => r.RelatedStarts), rPlanned = measured.Sum(r => r.RelatedPlanned);
        int pairs = measured.Sum(r => r.Kinds), pairsPlanned = measured.Sum(r => r.KindsPlanned);
        sb.AppendLine("--- HEADLINE (wills measurable on a fresh scene) ---");
        sb.AppendLine($"  wills measured                 {measured.Count} ({rows.Count - measured.Count} need a state play brings about)");
        sb.AppendLine($"  targets per will               {Mean(rows.Values.Select(r => (double)r.TargetCount)):0.00}");
        sb.AppendLine($"  coverage (will, start)         {Pct(planned, starts)}   ({planned} of {starts})");
        sb.AppendLine($"  coverage (will, location kind) {Pct(pairsPlanned, pairs)}   ({pairsPlanned} of {pairs})");
        bool relatedOk = rStarts > 0 && (double)rPlanned / rStarts >= MinRelatedOverall;
        sb.AppendLine($"{(relatedOk ? "✓" : "✗")} related coverage               {Pct(rPlanned, rStarts)}   ({rPlanned} of {rStarts}, held to ≥{100 * MinRelatedOverall:0}%)");
        var withPlaces = measured.Where(r => r.RelatedStarts > 0).ToList();
        sb.AppendLine($"  wills ≥{100 * MinRelated:0}% in their places      {withPlaces.Count(r => r.Related >= MinRelated)} of {withPlaces.Count}");
        sb.AppendLine($"  mean plan length               {Mean(measured.Where(r => r.Planned > 0).Select(r => r.MeanSteps)):0.00} steps");
        sb.AppendLine();

        sb.AppendLine("--- PER WILL ---");
        sb.AppendLine($"  {"",1} {"will",-22} {"tgts",4} {"cover",6} {"steps",5} {"related",7} {"breadth",7}  places");
        foreach (var row in rows.Values.OrderBy(r => r.Id, StringComparer.Ordinal))
        {
            if (row.Gated)
            {
                sb.AppendLine($"  · {row.Id,-22} {row.TargetCount,4} {"—",6} {"",5} {"—",7} {"—",7}  needs a state play brings about: {row.Targets}");
                continue;
            }
            bool noPlaces = row.Places.Length == 0;
            bool narrow   = row.RelatedStarts > 0 && row.Related < MinRelated;
            bool broad    = row.Breadth > MaxBreadth;
            bool grown    = broad && row.TargetCount > 1;
            string mark   = noPlaces || grown ? "✗" : narrow ? "!" : broad ? "~" : "✓";
            sb.AppendLine($"  {mark} {row.Id,-22} {row.TargetCount,4} {100 * row.Coverage,5:0}% {row.MeanSteps,5:0.0} "
                        + $"{(row.RelatedStarts == 0 ? "  none" : $"{100 * row.Related,6:0}%")} {100 * row.Breadth,6:0}%  "
                        + (noPlaces ? "declares no places it belongs in" : string.Join(",", row.Places.Select(p => p.Replace("Area", ""))))
                        + (grown ? $"  — its targets reach more than {100 * MaxBreadth:0}% of location kinds: no longer a will" : ""));
        }
        sb.AppendLine($"  (! under {100 * MinRelated:0}% in its own places: a will that idles where it ought not;");
        sb.AppendLine($"   ~ one target whose act is offered nearly everywhere — broad by nature, not by its targets)");
        sb.AppendLine();

        sb.AppendLine("--- PER LOCATION KIND (share of wills with something to do) ---");
        foreach (var label in census.Keys)
        {
            var rs = measured.Where(r => r.PerKind.ContainsKey(label)).ToList();
            sb.AppendLine($"  {label,-24} {Pct(rs.Count(r => r.PerKind[label] > 0), rs.Count)}");
        }
        sb.AppendLine();

        sb.AppendLine("--- AREA KINDS BUILT, PER LOCATION KIND (for declaring a will's places) ---");
        foreach (var (label, kinds) in census)
            sb.AppendLine($"  {label,-24} {string.Join(" ", kinds.Select(n => n.Replace("Area", "")))}");

        if (jsonPath != null)
        {
            File.WriteAllText(jsonPath, JsonSerializer.Serialize(new
            {
                wills = wills.Count, catalogue = all.Count, scenes,
                share = (double)wills.Count / all.Count,
                rows  = rows.Values.OrderBy(r => r.Id, StringComparer.Ordinal).Select(r => new
                {
                    r.Id, r.Targets, r.TargetCount, r.Gated, r.Starts, r.Planned, r.Coverage, r.MeanSteps,
                    r.RelatedStarts, r.RelatedPlanned,
                    Related = double.IsNaN(r.Related) ? (double?)null : r.Related, r.RelatedMeanSteps,
                    r.Kinds, r.KindsPlanned, r.Breadth, r.Places, r.PerKind,
                }),
            }, new JsonSerializerOptions { WriteIndented = true }));
            sb.AppendLine();
            sb.AppendLine($"wrote {jsonPath}");
        }
        return sb.ToString();
    }

    /// <summary>
    /// <c>--will-trace &lt;will-id&gt; [location label]</c>: why a will has, or has not, something to do —
    /// per sampled scene, every element whose actions any target's verb is offered on, whether it matches,
    /// and the plan from the entry and from the will's first place. The diagnosis behind a low line in
    /// <see cref="BuildReport"/>.
    /// </summary>
    public static string Trace(string willId, string? label)
    {
        var will = ModusMentisRegistry.Instance.GetAllModiMentis().FirstOrDefault(m => m.ModusMentisId == willId && m.IsWill);
        if (will == null) return $"no will '{willId}'";
        var actor = new Protagonist();
        var sb = new StringBuilder();
        sb.AppendLine($"{willId} wants {will.Will!.Describe()}");
        foreach (var (l, build, samples) in VerbAudit.Factories())
        {
            if (label != null && !l.Equals(label, StringComparison.OrdinalIgnoreCase)) continue;
            for (int id = 1; id <= Math.Min(samples, Samples); id++)
            {
                Cathedral.Game.Scene.Scene scene;
                try { scene = build(id); } catch { continue; }
                ActionPathPlanner.Forget();
                var entry = SceneSyntheticGraphFactory.ResolveEntryArea(scene) ?? scene.AllAreas.First();
                var related = scene.AllAreas.FirstOrDefault(a => will.WillPlaces.Any(t => t.IsInstanceOfType(a)));
                var p1 = PlanFor(scene, new PoV(entry, TimePeriod.Morning), actor, will);
                var p2 = related == null ? null : PlanFor(scene, new PoV(related, TimePeriod.Morning), actor, will);
                sb.AppendLine($"── {l} #{id}: from {entry.DisplayName}: {(p1 == null ? "none" : string.Join(" | ", p1.Steps.Select(x => x.Verbatim)))}");
                sb.AppendLine($"   from {related?.DisplayName ?? "(no place of its)"}: {(p2 == null ? "none" : string.Join(" | ", p2.Steps.Select(x => x.Verbatim)))}");
                var goalVerbs = scene.Verbs.Where(will.Will.Wants).ToList();
                foreach (var area in scene.AllAreas)
                foreach (var e in scene.View(new PoV(area, TimePeriod.Morning), actor, goalVerbs).Entries)
                foreach (var a in e.ApplicableVerbs)
                    sb.AppendLine($"   {area.DisplayName}: {a.Verb.VerbId} {a.Target?.DisplayName} — {(will.Will.Matches(a) ? "MATCHES" : "no")}");
            }
        }
        ActionPathPlanner.Forget();
        return sb.ToString();
    }

    private static ActionPath? PlanFor(Cathedral.Game.Scene.Scene scene, PoV pov, PartyMember actor, ModusMentis will)
    {
        try { return ActionPathPlanner.Plan(scene, pov, actor, will.Will!); }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"WillCoverageAudit: {will.ModusMentisId} could not be planned ({ex.Message})");
            return null;
        }
    }

    private static string Pct(int n, int d) => d == 0 ? "  —" : $"{100.0 * n / d,5:0.0}%";
    private static double Mean(IEnumerable<double> xs) { var l = xs.ToList(); return l.Count == 0 ? 0 : l.Average(); }
}
