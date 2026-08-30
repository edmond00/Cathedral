using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Narrative;

/// <summary>
/// The counterpart to <c>--verb-audit</c>, for the other half of the loop: a verb says what you can
/// <i>do</i>, an <see cref="Outcome"/> says what it <i>did</i>.
///
/// <para><b>Why this exists.</b> Verbs have always been enumerable — <c>VerbRegistry</c> is a
/// hand-kept list, and three audits read it. Outcomes were not: they were plain classes constructed
/// inline inside each verb's <c>SuccessReports</c>, registered nowhere, so "what kinds of consequence
/// does this game have?" could only be answered by grepping. Two things were invisible as a result,
/// and both are silent at runtime:</para>
///
/// <list type="bullet">
/// <item>an outcome class nothing ever produces — dead content, the consequence-side twin of the
/// dead-verb warning;</item>
/// <item>a verb whose <c>SuccessReports</c> comes back empty in every situation. It still rolls,
/// still prints SUCCESS, still satisfies <c>expect-verb</c>, and does nothing.</item>
/// </list>
///
/// <para>The catalogue is built by reflection over <see cref="Outcome"/>, so a new one is covered the
/// moment it is written and there is no list to keep in step. What <i>produces</i> each is found by
/// sweeping real scenes the way <c>--verb-probe</c> does, rather than by reading source: the answer
/// is then about what the game actually offers, not about what a regex could see.</para>
/// </summary>
public static class OutcomeAudit
{
    private const int SampleIds = 6;

    public static string BuildReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== OUTCOME AUDIT ===");
        sb.AppendLine();

        // ── The catalogue: every consequence the game can express ────────────────
        var all = typeof(Outcome).Assembly.GetTypes()
            .Where(t => typeof(Outcome).IsAssignableFrom(t) && !t.IsAbstract)
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .ToList();

        sb.AppendLine($"── CATALOGUE ({all.Count}) ──");
        foreach (var t in all)
            sb.AppendLine($"     {IdOf(t),-28} {t.Name}");
        sb.AppendLine();

        // ── Who produces what ────────────────────────────────────────────────────
        var producedBy = new Dictionary<Type, SortedSet<string>>();
        var emptyVerbs = new SortedSet<string>();
        var everRolled = new SortedSet<string>();

        var actor = new Protagonist();
        foreach (var (label, build) in Factories())
        {
            for (int id = 0; id < SampleIds; id++)
            {
                Scene.Scene scene;
                try { scene = build(id); } catch { continue; }

                foreach (var area in scene.AllAreas)
                foreach (TimePeriod period in Enum.GetValues<TimePeriod>())
                {
                    var pov = new PoV(area, period);
                    foreach (var target in Targets(scene, area, period))
                    foreach (var verb in scene.Verbs)
                    {
                        try
                        {
                            if (!verb.IsPossible(scene, pov, target, actor)) continue;
                            everRolled.Add(verb.VerbId);

                            // Through the VIEW-aware overload, and once per expanded view. A verb that
                            // splits one target into several actions decides its outcome from the
                            // view's Variant — introduce_me needs to know WHICH third party — so the
                            // target-only overload comes back empty and the verb reads as doing nothing.
                            bool any = false;
                            foreach (var view in verb.ExpandViews(scene, pov, target, actor))
                            {
                                foreach (var r in verb.SuccessReports(scene, pov, actor, target, view))
                                {
                                    any = true;
                                    if (!producedBy.TryGetValue(r.GetType(), out var set))
                                        producedBy[r.GetType()] = set = new SortedSet<string>();
                                    set.Add(verb.VerbId);
                                }
                            }
                            if (!any) emptyVerbs.Add(verb.VerbId);

                            // ── The failure branch ──────────────────────────────────
                            // Sweeping SuccessReports alone told less than half the story: a verb's
                            // consequences include what happens when it MISSES, and those are
                            // declared elsewhere on the verb.
                            foreach (var r in verb.FailureReports(scene, pov, actor, target))
                                Record(producedBy, r.GetType(), verb.VerbId);

                            // A wound is sampled from FailurePenalties rather than returned as an
                            // outcome, so nothing in the reports lists shows that a verb can hurt you.
                            if (verb.FailurePenalties(target).Any(w => w != null))
                                Record(producedBy, typeof(WoundInflictionOutcome), verb.VerbId);

                            // ── The lesson ──────────────────────────────────────────
                            // Every successful verb teaches a modus mentis, and the grant is applied
                            // by NarrativeController rather than by the verb — which is why the two
                            // most common outcomes in the game were missing from this table entirely.
                            if (verb.Lessons(new Scene.Verbs.LessonContext(scene, pov, actor, target)).Any())
                            {
                                Record(producedBy, typeof(ModusMentisGrantOutcome),    verb.VerbId);
                                Record(producedBy, typeof(ModusMentisPracticeOutcome), verb.VerbId);
                            }
                        }
                        catch { /* a throwing verb is --verb-audit's business */ }
                    }
                }
            }
        }

        // Conversations produce outcomes too, and theirs are authored rather than swept.
        foreach (var tree in DialogueTreeRegistry.Instance.All)
        foreach (var o in tree.SuccessOutcomes.Concat(tree.FailureOutcomes))
        {
            if (!producedBy.TryGetValue(o.GetType(), out var set))
                producedBy[o.GetType()] = set = new SortedSet<string>();
            set.Add($"tree:{tree.TreeId}");
        }

        sb.AppendLine("── PRODUCED BY ──");
        foreach (var t in all.Where(producedBy.ContainsKey))
            sb.AppendLine($"     {IdOf(t),-28} {string.Join(", ", producedBy[t].Take(6))}"
                        + (producedBy[t].Count > 6 ? $" (+{producedBy[t].Count - 6})" : ""));
        sb.AppendLine();

        // The same table read the other way. This is the direction somebody writing a verb test
        // actually asks in — "what should my success.cli assert on?" — and the answer is this line.
        var byProducer = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        foreach (var (type, producers) in producedBy)
        foreach (var p in producers)
        {
            if (!byProducer.TryGetValue(p, out var set))
                byProducer[p] = set = new SortedSet<string>(StringComparer.Ordinal);
            set.Add(IdOf(type));
        }

        sb.AppendLine("── PER VERB / TREE ──");
        foreach (var (producer, outcomes) in byProducer)
            sb.AppendLine($"     {producer,-28} {string.Join(", ", outcomes)}");
        foreach (var v in everRolled.Where(v => !byProducer.ContainsKey(v)).OrderBy(v => v, StringComparer.Ordinal))
            sb.AppendLine($"     {v,-28} (no scene outcome"
                        + (TeachesOnly.Contains(v) ? " — its effect is the modus mentis it teaches)" : ")"));
        sb.AppendLine();

        // ── Warnings ─────────────────────────────────────────────────────────────
        var warnings = new List<string>();

        foreach (var t in all.Where(t => !producedBy.ContainsKey(t) && !Elsewhere.ContainsKey(IdOf(t))))
            warnings.Add($"outcome '{IdOf(t)}' ({t.Name}) is produced by no verb and no dialogue tree "
                       + "— dead content. If it is reached by a path this sweep cannot walk, say so "
                       + "in OutcomeAudit.Elsewhere rather than leaving the warning to be ignored");

        foreach (var v in emptyVerbs.Where(v => !ProducesSomethingSomewhere(v, producedBy)
                                             && !TeachesOnly.Contains(v)))
            warnings.Add($"verb '{v}' returned no outcome in any sampled situation — it rolls, prints "
                       + "SUCCESS and changes nothing");

        // ── Test coverage ────────────────────────────────────────────────────────
        // A verb test proves the chip was printed; an outcome test proves the world actually moved.
        // Without this check the second half of that pair falls quietly behind the code — which is
        // exactly what happened to the verb suite before --verb-audit named the verbs nobody tested.
        var testRoot = new System.IO.DirectoryInfo("cli/outcome");
        if (testRoot.Exists)
        {
            var covered = testRoot.GetDirectories().Select(d => d.Name)
                                  .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var t in all.Select(IdOf).Where(id => !covered.Contains(id) && !NoTest.ContainsKey(id))
                                 .OrderBy(id => id, StringComparer.Ordinal))
                warnings.Add($"outcome '{t}' has no cli/outcome/{t}/ test — nothing proves that its "
                           + "chip corresponds to a real change in the world");
        }

        sb.AppendLine($"── WARNINGS ({warnings.Count}) ──");
        if (warnings.Count == 0) sb.AppendLine("     none");
        foreach (var w in warnings) sb.AppendLine($"     ⚠ {w}");

        return sb.ToString();
    }

    /// <summary>
    /// Outcomes this sweep cannot reach, and why. The sweep asks verbs and trees; anything applied by
    /// a phase, by a failure branch, or by a verb whose <c>IsPossible</c> needs a situation to be
    /// built first is invisible to it. Declaring them here is what keeps the warning list worth
    /// reading — an entry is a claim that somebody checked, and an outcome that genuinely dies still
    /// shows up the moment it is not on this list.
    /// </summary>
    private static readonly Dictionary<string, string> Elsewhere = new()
    {
        ["skill_acquisition"]       = "childhood reminescence outcomes",
        ["childhood_history"]       = "childhood reminescence outcomes",
        ["reminescence_transition"] = "childhood phase transition",
        ["get_up_transition"]       = "the get-up phase",
        ["state_capture"]           = "internal bookkeeping, never shown",
        ["no_dialogue_consequence"] = "built by DialogueTreeController when a branch resolved with no outcomes",
        ["recruited"]               = "tame, which needs an already-appeased beast",
        ["corpse_item_acquisition"] = "cut, which needs a corpse",
        ["affinity_change"]         = "appease, which needs a hostile NPC",
        // Not produced by a verb or a tree at all: it is produced BY the other outcomes, after
        // they are gathered, by EmotionResolver — so no sweep over verbs or trees can find it.
        ["emotion"]                 = "EmotionResolver, from whatever outcomes an action or a conversation produced",
    };

    /// <summary>
    /// Outcomes deliberately not covered by a <c>cli/outcome/</c> script, and why. Everything here
    /// belongs to the childhood and get-up phases, which every test script skips with
    /// <c>--skip-childhood</c>: driving the UI through a whole reminescence to assert a dictionary
    /// write is a long way round for no more certainty than a headless check would give.
    /// </summary>
    private static readonly Dictionary<string, string> NoTest = new()
    {
        ["childhood_history"]       = "childhood phase, skipped by every script",
        ["skill_acquisition"]       = "childhood phase, skipped by every script",
        ["reminescence_transition"] = "childhood phase, skipped by every script",
        ["get_up_transition"]       = "the get-up phase, skipped by every script",
        ["state_capture"]           = "internal bookkeeping — no chip, and no state a script can read",
    };

    /// <summary>
    /// Verbs whose whole effect IS the modus mentis they teach. They have no scene consequence by
    /// design, so an empty <c>SuccessReports</c> is correct rather than a fault.
    /// </summary>
    private static readonly HashSet<string> TeachesOnly = new()
    {
        "examine", "listen", "smell", "contemplate", "ignore",
    };

    private static void Record(Dictionary<Type, SortedSet<string>> into, Type outcome, string producer)
    {
        if (!into.TryGetValue(outcome, out var set))
            into[outcome] = set = new SortedSet<string>();
        set.Add(producer);
    }

    private static bool ProducesSomethingSomewhere(string verbId, Dictionary<Type, SortedSet<string>> produced)
        => produced.Values.Any(s => s.Contains(verbId));

    private static string IdOf(Type t)
        => ((Outcome)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(t)).OutcomeId;

    /// <summary>Everything a verb can be asked about — the same probe set <c>--verb-probe</c> uses.</summary>
    private static IEnumerable<Element> Targets(Scene.Scene scene, Area area, TimePeriod period)
    {
        foreach (var poi in area.PointsOfInterest)
        {
            yield return poi;
            foreach (var item in poi.Items) yield return item;
        }
        foreach (var npc in scene.GetNpcsAt(area, period)) yield return npc;
        yield return area;
        foreach (var reachable in scene.GetReachableAreas(area)) yield return reachable;
    }

    private static IEnumerable<(string Label, Func<int, Scene.Scene> Build)> Factories()
    {
        yield return ("VILLAGE",  id => new Scene.Village.VillageSceneFactory().Build(id));
        yield return ("FARM",     id => new Scene.Farm.FarmSceneFactory().Build(id));
        yield return ("FIELD",    id => new Scene.Field.FieldSceneFactory().Build(id));
        yield return ("PLAIN",    id => new Scene.Plain.PlainSceneFactory().Build(id));
        yield return ("FOREST",   id => new Scene.Forest.ForestSceneFactory().Build(id));
        yield return ("CAVE",     id => new Scene.Cave.CaveSceneFactory().Build(id));
        yield return ("MOUNTAIN", id => new Scene.Mountain.MountainSceneFactory().Build(id));
        yield return ("PEAK",     id => new Scene.Peak.PeakSceneFactory().Build(id));
        yield return ("COAST",    id => new Scene.Coast.CoastSceneFactory().Build(id));
    }
}
