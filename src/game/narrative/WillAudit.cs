using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Narrative;

/// <summary>
/// The <c>--mm-audit</c> section on wills (<see cref="ModusMentisFunction.Will"/>): how many there are,
/// what each wants, and two content checks a startup rule cannot make because they need scenes built.
///
/// <list type="bullet">
/// <item><b>The ratio.</b> About a third of the catalogue is a will; outside 30–36% a ✗ line fails the
/// audit. Wills narrow what a body attends to, and a catalogue of them would leave a body with nothing
/// that looks about freely.</item>
/// <item><b>The ratio of what is learned.</b> The catalogue share says nothing about what a body comes
/// to hold, which is mostly the lessons of the acts it performs. Each act the sampled locations offer
/// counts its lesson once (<see cref="Verb.ResolveLesson"/>, for a human), so a verb offered
/// everywhere weighs as it does in play; outside <see cref="MinLearned"/>–<see cref="MaxLearned"/> a ✗
/// line fails the audit. It was 85% once: the obvious will to unlock is the mind unlocking teaches, and
/// nearly every verb's own lesson had been made a will of that verb, so a played character filled up
/// with wills while the catalogue read a third.</item>
/// <item><b>Every target is offered somewhere.</b> A will whose target no location ever offers would
/// refuse in every place in the game, and blame the place. Three kinds of target are listed without
/// being failed, because a freshly built scene never starts in the state they need: a conversation
/// (most need an acquaintance, and a reconciliation an enemy), a beast to calm or tame (a hostile one,
/// an appeased one), and a body (a corpse exists only once somebody has died).</item>
/// <item><b>The planner sees every way of moving.</b> A verb whose success moves the body or the hour
/// but is missing from <see cref="ActionPathPlanner.TransitVerbTypes"/> is a way no will could plan
/// through, and nothing else would notice.</item>
/// </list>
/// </summary>
public static class WillAudit
{
    public const double MinShare = 0.25, MaxShare = 0.33;
    public const double MinLearned = 0.20, MaxLearned = 0.36;

    /// <summary>
    /// Up to twelve rounds of samples, at a day hour and at night: what a will needs is that its action
    /// exists, not how often. Water to fish is built only on the coast, and fish in only one coast in
    /// five; the night is where the sleepers are, whom murder is offered on.
    /// </summary>
    private static readonly TimePeriod[] Hours = { TimePeriod.Morning, TimePeriod.Night };
    private const int Samples = 12;

    /// <summary>
    /// Verbs no freshly built scene offers, because who they are offered on must first be won over or
    /// made hostile — or, for cutting, must first have died: it is offered only on a corpse's parts.
    /// </summary>
    private static readonly Type[] RelationGated = { typeof(DialogueVerb), typeof(TameVerb), typeof(AppeaseVerb), typeof(CutVerb) };

    /// <summary>Whether no built scene could show this target: every verb relation-gated, or aimed at a body.</summary>
    /// <summary>Whether no built scene could show any of this will's targets — read by <see cref="WillCoverageAudit"/> too.</summary>
    public static bool PlayGated(ModusMentis mm) => mm.Will is { } will && will.All(PlayGated);

    /// <summary>How many targets the will declares.</summary>
    public static int TargetCount(ModusMentis mm) => mm.Will?.Count ?? 0;

    /// <summary>The will's targets, for an audit line.</summary>
    public static string Describe(ModusMentis mm) => mm.Will?.Describe() ?? "";

    private static bool PlayGated(WillTarget will)
        => will.VerbTypes.All(v => RelationGated.Any(g => g.IsAssignableFrom(v)))
           || will.OnTypes.Any(t => typeof(Cathedral.Game.Npc.Corpse.CorpsePointOfInterest).IsAssignableFrom(t));

    public static string Section(List<ModusMentis> all)
    {
        var sb    = new StringBuilder();
        var wills = all.Where(m => m.IsWill).OrderBy(m => m.ModusMentisId).ToList();
        double share = all.Count == 0 ? 0 : (double)wills.Count / all.Count;
        bool ratioOk = share >= MinShare && share <= MaxShare;

        sb.AppendLine("── wills ──");
        sb.AppendLine($"  {(ratioOk ? "✓" : "✗")} {wills.Count} of {all.Count} modi mentis bear the Will function "
                    + $"({100 * share:0.#}%, held to {100 * MinShare:0}–{100 * MaxShare:0}%)");
        int acting = wills.Count(m => m.Functions.Contains(ModusMentisFunction.Action));
        int feeling = wills.Count(m => m.Functions.Contains(ModusMentisFunction.Emotion));
        int talking = wills.Count(m => m.Functions.Contains(ModusMentisFunction.Speaking));
        sb.AppendLine($"    {acting} act themselves, {feeling} feel, {talking} speak");
        sb.AppendLine($"    {wills.Sum(m => m.Will!.Count)} targets, {(double)wills.Sum(m => m.Will!.Count) / Math.Max(1, wills.Count):0.00} a will; "
                    + $"{wills.Count(m => m.Will!.Count > 1)} wills want more than one thing");
        // A will that wants a conversation and has nothing to say in it: legal, and almost always a slip.
        foreach (var mute in wills.Where(m => m.Will!.Converses && !m.Functions.Contains(ModusMentisFunction.Speaking)))
            sb.AppendLine($"    note: {mute.ModusMentisId} wants a conversation but bears no Speaking function");

        // What the locations offer, sampled in rounds — the first build of every factory, then the
        // second of every factory, and so on — until every will has found its target or the samples
        // run out. A rare target (water to fish) costs rounds only while it is still missing.
        var hits     = wills.SelectMany(m => m.Will!).ToDictionary(t => t, _ => 0);
        var taught   = new Dictionary<string, int>();
        var movers   = new HashSet<Type>();
        var checkedT = new HashSet<Type>();
        var actor    = new Protagonist();
        var factories = VerbAudit.Factories().ToList();
        int rounds = 0;
        for (int id = 1; id <= Samples && hits.Any(kv => kv.Value == 0 && !PlayGated(kv.Key)); id++, rounds++)
        foreach (var (label, build, samples) in factories.Where(f => id <= f.Samples))
        {
            Cathedral.Game.Scene.Scene scene;
            try { scene = build(id); } catch { continue; }
            foreach (var area in scene.AllAreas)
            foreach (var hour in Hours)
            {
                var pov = new PoV(area, hour);
                foreach (var action in scene.View(pov, actor).Entries.SelectMany(e => e.ApplicableVerbs))
                {
                    foreach (var target in hits.Keys.ToList())
                        if (target.Matches(action)) hits[target]++;
                    try
                    {
                        if (action.Verb.ResolveLesson(new LessonContext(scene, pov, actor, action.Target)) is { } lesson)
                            taught[lesson.ModusMentisId] = taught.GetValueOrDefault(lesson.ModusMentisId) + 1;
                    }
                    catch { /* a lesson that cannot be read outside play is not counted */ }
                    if (!checkedT.Add(action.Verb.GetType())) continue;
                    try
                    {
                        if (action.Verb.SuccessReports(scene, pov, actor, action.Target!, action)
                                .Any(o => o is AreaMoveOutcome or DoorUnlockOutcome or TimeShiftOutcome))
                            movers.Add(action.Verb.GetType());
                    }
                    catch { /* a report that cannot be read outside play moves nothing the planner can see */ }
                }
            }
        }
        sb.AppendLine($"    sampled {rounds} round(s) of every location factory");

        var willIds = wills.Select(m => m.ModusMentisId).ToHashSet();
        int lessons = taught.Values.Sum(), willLessons = taught.Where(kv => willIds.Contains(kv.Key)).Sum(kv => kv.Value);
        double learned = lessons == 0 ? 0 : (double)willLessons / lessons;
        bool learnedOk = learned >= MinLearned && learned <= MaxLearned;
        sb.AppendLine($"  {(learnedOk ? "✓" : "✗")} {100 * learned:0.#}% of the lessons the offered acts teach are wills "
                    + $"({willLessons} of {lessons}, held to {100 * MinLearned:0}–{100 * MaxLearned:0}%)");
        foreach (var (id, n) in taught.OrderByDescending(kv => kv.Value).Take(25))
            sb.AppendLine($"    {100.0 * n / lessons,5:0.0}%  {id}{(willIds.Contains(id) ? "  (will)" : "")}");

        foreach (var will in wills)
        {
            var fns = string.Join("+", will.Functions);
            for (int i = 0; i < will.Will!.Count; i++)
            {
                var target = will.Will[i];
                int n      = hits[target];
                var mark   = n > 0 ? "✓" : PlayGated(target) ? "·" : "✗";
                sb.AppendLine((i == 0 ? $"  {mark} {will.ModusMentisId,-24} {fns,-22} wants " : $"  {mark} {"",-24} {"",-22}    or ")
                            + target.Describe()
                            + (n > 0 ? $"   ({n} offered)"
                               : PlayGated(target) ? "   (needs a state play brings about — not sampled)"
                               : "   — offered nowhere: this target would never be met"));
            }
        }

        foreach (var t in movers.Where(t => !ActionPathPlanner.TransitVerbTypes.Contains(t) && t != typeof(VoyageTowardVerb)).OrderBy(t => t.Name))
            sb.AppendLine($"  ✗ {t.Name} moves the body or the hour but is not in ActionPathPlanner.TransitVerbTypes — no will could plan through it");

        return sb.ToString();
    }
}
