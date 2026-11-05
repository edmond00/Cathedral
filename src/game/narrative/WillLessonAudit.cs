using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Narrative;

/// <summary>
/// <c>--will-lesson-audit</c>: an object that <b>teaches</b> a will must be something that will is
/// willing to act on. Examining the petroglyphs teaches Archeology; Archeology is a will (to dig, to
/// study a relic, to fetch a spade), and petroglyphs are none of those — so a player who reads the
/// carvings, picks the one mind the carvings themselves taught, and asks it to think about them, is
/// told it has no interest in them. Nothing throws and every other audit is green: the lesson resolves,
/// the will is offered somewhere, and the refusal is the planner working exactly as designed.
///
/// <para><b>What is related is read off the content, not judged.</b> The lesson an act on an object
/// teaches is the strongest statement the code makes that the two belong together, so it is the test:
/// the object's own declared lesson (<see cref="IVerbModusMentisSource"/>, a <c>VerbModiMentis</c>
/// entry on a point of interest or an archetype), and the lesson the act actually resolves for a human
/// body (<see cref="Verb.ResolveLesson"/>, which adds the verb's contextual cases and its default).</para>
///
/// <para>One observation object is one point of interest <b>with its items</b>, a person, or an area —
/// what a keyword reaches. For every will taught by any act it offers, the audit asks what the thinking
/// phase asks (<see cref="ActionPathPlanner.PathThrough"/>): does anything this object offers begin a
/// shortest path to the will? Three answers:</para>
/// <list type="bullet">
/// <item><b>wants it</b> — an act on it is one of the will's targets. Silent.</item>
/// <item><b>on the way</b> — it begins a path (a spade lying in it, a door to the place). Silent too:
/// the will does not refuse it.</item>
/// <item><b>refuses</b> — nothing it offers leads to the will. A ✗ when the object <i>declared</i>
/// the lesson (that is a content author saying "this is that mind's thing"), a <c>note</c> when only
/// the verb's own default or a contextual case taught it, since those are about the act and hold
/// across every object it is done to.</item>
/// </list>
///
/// <para>The fix is a target on the will (<see cref="WillTargets"/>, at most <see cref="WillTargets.Max"/>),
/// typed by the object's class: <c>WillTarget.To&lt;ExamineVerb&gt;("…").On&lt;PetroglyphPointOfInterest&gt;()</c>.
/// Narrow, not broad — <c>--will-audit</c> fails a will whose targets let it do something in more than
/// three kinds of place in four. Or, when the lesson is what is wrong, the lesson. A pairing that is right
/// as it stands goes in <see cref="Accepted"/>, with the reason.</para>
/// </summary>
public static class WillLessonAudit
{
    /// <summary>Location ids per factory: the factories' own sample sizes, capped here.</summary>
    private const int MaxSamples = 4;

    private static readonly TimePeriod[] Hours = { TimePeriod.Morning, TimePeriod.Night };

    /// <summary>
    /// (will id, object kind) pairs that refuse on purpose — the object teaches the will and the will is
    /// right not to want it. The kind is the point of interest's class name, or an archetype id for a
    /// person or a beast. Each needs a reason a reader can check.
    /// </summary>
    public static readonly IReadOnlyDictionary<(string Will, string Kind), string> Accepted =
        new Dictionary<(string, string), string>
        {
        };

    /// <summary>One will taught by one kind of object it refuses, summed over every place it was met.</summary>
    private sealed class Finding
    {
        public string Will = "", Kind = "", Example = "";
        public bool Declared;
        public readonly SortedSet<string> Verbs = new(), Places = new();
        public int Count;
    }

    public static string BuildReport()
    {
        var sb       = new StringBuilder();
        var actor    = new Protagonist();
        var registry = ModusMentisRegistry.Instance;
        // A modus mentis builds its targets afresh on every read: read once and keep.
        var targets  = registry.GetAllModiMentis().Where(m => m.IsWill).ToDictionary(m => m.ModusMentisId, m => m.Will!);
        var findings = new Dictionary<(string, string), Finding>();
        int objects = 0, taughtPairs = 0, wanted = 0, onWay = 0;

        foreach (var (label, build, samples) in VerbAudit.Factories())
        for (int id = 1; id <= Math.Min(samples, MaxSamples); id++)
        {
            Scene.Scene scene;
            try { scene = build(id); } catch { continue; }
            ActionPathPlanner.Forget();

            foreach (var area in scene.AllAreas)
            foreach (var hour in Hours)
            {
                var pov = new PoV(area, hour);
                foreach (var (source, actions) in Objects(scene.View(pov, actor)))
                {
                    objects++;
                    // Every will an act on this object teaches, and whether the object declared it.
                    var taught = new Dictionary<string, (bool Declared, string Verb)>();
                    foreach (var action in actions)
                    {
                        if ((action.Target as IVerbModusMentisSource)?.ModusMentisFor(action.Verb.VerbId) is { } declared
                            && targets.ContainsKey(declared))
                            taught[declared] = (true, action.Verb.VerbId);

                        ModusMentis? resolved = null;
                        try { resolved = action.Verb.ResolveLesson(new LessonContext(scene, pov, actor, action.Target)); }
                        catch { /* a lesson that cannot be read outside play is not counted */ }
                        if (resolved != null && targets.ContainsKey(resolved.ModusMentisId) && !taught.ContainsKey(resolved.ModusMentisId))
                            taught[resolved.ModusMentisId] = (false, action.Verb.VerbId);
                    }

                    foreach (var (willId, (declaredHere, verbId)) in taught)
                    {
                        taughtPairs++;
                        var will = targets[willId];
                        if (actions.Any(will.Matches)) { wanted++; continue; }
                        if (ActionPathPlanner.Plan(scene, pov, actor, will) is { } path
                            && ActionPathPlanner.PathThrough(scene, pov, actor, will, path, actions) != null) { onWay++; continue; }

                        var kind = KindOf(source);
                        if (!findings.TryGetValue((willId, kind), out var f))
                            findings[(willId, kind)] = f = new Finding { Will = willId, Kind = kind, Example = NameOf(source) };
                        f.Declared |= declaredHere;
                        f.Verbs.Add(verbId);
                        f.Places.Add(label);
                        f.Count++;
                    }
                }
            }
        }
        ActionPathPlanner.Forget();

        sb.AppendLine("── will lessons: does a will want what teaches it? ──");
        sb.AppendLine($"  {objects} observation objects sampled; {taughtPairs} (object, will taught) pairs — "
                    + $"{wanted} wanted, {onWay} on the way, {taughtPairs - wanted - onWay} refused");

        var declaredFaults = findings.Values.Where(f => f.Declared && !Accepted.ContainsKey((f.Will, f.Kind))).ToList();
        var defaultNotes   = findings.Values.Where(f => !f.Declared && !Accepted.ContainsKey((f.Will, f.Kind))).ToList();
        var accepted       = findings.Values.Where(f => Accepted.ContainsKey((f.Will, f.Kind))).ToList();

        sb.AppendLine();
        sb.AppendLine($"  The object declares the lesson and the will refuses it ({declaredFaults.Count}):");
        foreach (var f in declaredFaults.OrderBy(f => f.Will).ThenBy(f => f.Kind))
            sb.AppendLine($"  ✗ {f.Will,-22} refuses {f.Kind} (\"{f.Example}\") which teaches it by {string.Join("/", f.Verbs)}"
                        + $"   [{f.Count}× in {string.Join(", ", f.Places.Take(4))}{(f.Places.Count > 4 ? ", …" : "")}]");
        if (declaredFaults.Count == 0) sb.AppendLine("    none");

        sb.AppendLine();
        sb.AppendLine($"  The verb's own lesson is a will that refuses the object ({defaultNotes.Count}) — about the act, not the object:");
        foreach (var f in defaultNotes.OrderBy(f => f.Will).ThenBy(f => f.Kind))
            sb.AppendLine($"    note: {f.Will,-22} refuses {f.Kind} (\"{f.Example}\") which teaches it by {string.Join("/", f.Verbs)}"
                        + $"   [{f.Count}× in {string.Join(", ", f.Places.Take(3))}{(f.Places.Count > 3 ? ", …" : "")}]");
        if (defaultNotes.Count == 0) sb.AppendLine("    none");

        if (accepted.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"  Accepted on purpose ({accepted.Count}):");
            foreach (var f in accepted.OrderBy(f => f.Will))
                sb.AppendLine($"    {f.Will,-22} / {f.Kind}: {Accepted[(f.Will, f.Kind)]}");
        }

        foreach (var stale in Accepted.Keys.Where(k => !findings.ContainsKey(k)))
            sb.AppendLine($"  ✗ Accepted lists {stale.Will} / {stale.Kind}, which no longer refuses — remove the entry");

        return sb.ToString();
    }

    /// <summary>
    /// The view's entries as observation objects: a point of interest gathers the items listed after it,
    /// as <see cref="SyntheticObservationObject"/> does; anything else stands alone.
    /// </summary>
    private static IEnumerable<(Element Source, List<VerbAction> Actions)> Objects(SceneView view)
    {
        (Element Source, List<VerbAction> Actions)? current = null;
        foreach (var entry in view.Entries)
        {
            if (entry.Source is ItemElement && current is { Source: PointOfInterest } held)
            {
                held.Actions.AddRange(entry.ApplicableVerbs);
                continue;
            }
            if (current is { } done && done.Actions.Count > 0) yield return done;
            current = (entry.Source, new List<VerbAction>(entry.ApplicableVerbs));
        }
        if (current is { } last && last.Actions.Count > 0) yield return last;
    }

    /// <summary>The kind an object is fixed by: its class, or a person's or a beast's archetype.</summary>
    private static string KindOf(Element e) => e switch
    {
        SceneNpc npc => $"{npc.Entity.GetType().Name}:{ArchetypeOf(npc)}",
        _            => e.GetType().Name,
    };

    private static string ArchetypeOf(SceneNpc npc) => npc.Entity switch
    {
        Cathedral.Game.Npc.NpcEntity n        => n.Archetype.ArchetypeId,
        Cathedral.Game.Npc.ShallowNpcEntity s => s.Archetype.ArchetypeId,
        _                                     => "?",
    };

    private static string NameOf(Element e) => e switch
    {
        PointOfInterest poi => poi.DisplayName,
        Area a              => a.DisplayName,
        SceneNpc npc        => ArchetypeOf(npc),
        _                   => e.GetType().Name,
    };
}
