using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Scene;

/// <summary>
/// Sweeps every factory x location id x area x period x object — the same space <c>--verb-probe</c>
/// walks — and records what each verb would teach against each thing it accepts.
///
/// <para>It <b>calls</b> <see cref="Verb.Lessons"/> rather than reading a declaration of what that
/// method might return. A verb's lessons are free C#, and a list declared beside free logic drifts
/// from it; a call cannot. Every period is walked, and each pairing is asked under four situations —
/// alone, hostile heard, hostile seen — which between them reach every branch a
/// sweep is able to stage.</para>
///
/// <para>What no sweep can reach is a branch needing state only play produces. Those are written up
/// in prose by the <c>mm-grants</c> skill, which reads the methods themselves.</para>
///
/// <para>This class used to render a CSV of its own findings. That was retired: it could only report
/// what it could sweep, so everything decided in <c>LessonFor</c> came out labelled as taught by
/// nothing — wrong about two rows in three, and reassuring in exactly the wrong direction.</para>
/// </summary>
public static class VerbLessonSweep
{
    private const int SampleSize = 12;

    /// <summary>
    /// Every (modus mentis, verb, source) the sweep can see: <c>lesson</c> for anything
    /// <see cref="Verb.Lessons"/> yielded in a real scene, and <c>verb-default</c> for the verbs no
    /// sampled scene offered, whose declaration stands regardless.
    /// </summary>
    public static IEnumerable<(string Mm, string Verb, string Source)> Grants()
    {
        var seen    = new HashSet<(string, string, string)>();
        var offered = new HashSet<string>(StringComparer.Ordinal);
        var actor   = Actor;

        foreach (var (_, build) in Factories())
        {
            for (int id = 0; id < SampleSize; id++)
            {
                Scene scene;
                try { scene = build(id); } catch { continue; }

                foreach (var area in scene.AllAreas)
                foreach (TimePeriod period in Enum.GetValues<TimePeriod>())
                {
                    var pov = new PoV(area, period);
                    foreach (var (target, _) in Probes(scene, area, period))
                    foreach (var verb in scene.Verbs.Concat<Verb>(new[] { IgnoreVerb.Instance }))
                    {
                        try { if (!verb.IsPossible(scene, pov, target, actor)) continue; }
                        catch { continue; }

                        offered.Add(verb.VerbId);
                        foreach (var mm in LessonsIn(verb, scene, pov, target))
                            if (seen.Add((mm, verb.VerbId, "lesson")))
                                yield return (mm, verb.VerbId, "lesson");
                    }
                }
            }
        }

        // A verb NO sampled scene offered still declares a lesson, and that declaration stands.
        //
        // Only such a verb, though. Emitting every verb's declaration here is what let a default the
        // sweep had just proved unreachable come back in through the side door and read as reached.
        foreach (var verb in VerbRegistry.Instance.GetAll().Concat<Verb>(new[] { IgnoreVerb.Instance }))
        {
            if (offered.Contains(verb.VerbId)) continue;
            foreach (var id in verb.GrantedModusMentisIds(null))
                if (seen.Add((id, verb.VerbId, "verb-default")))
                    yield return (id, verb.VerbId, "verb-default");
        }
    }

    /// <summary>
    /// Everything <see cref="Verb.Lessons"/> yields here, across the handful of situations a sweep
    /// can stage. The lessons are free logic inside each verb, so they are <b>exercised</b> rather
    /// than declared — which is strictly better than the list of ids this used to read, because a
    /// declaration can drift from the method beside it and a call cannot.
    ///
    /// <para>Night is covered already: the caller iterates every period. What has to be staged is
    /// whether anybody hostile is near, so each pairing is asked three times — alone, with something
    /// hostile heard, and with something hostile seen.</para>
    /// </summary>
    private static IEnumerable<string> LessonsIn(Verb verb, Scene scene, PoV pov, Element target)
    {
        var plain = new LessonContext(scene, pov, Actor, target);

        foreach (var ctx in new[]
                 {
                     plain,
                     plain with { Hostile = ThreatLevel.Audio },
                     plain with { Hostile = ThreatLevel.Visual },
                 })
        {
            // ResolveLesson, not Lessons: the method yields ORDERED CANDIDATES and exactly one of
            // them is ever granted. Reading the whole list counted a candidate standing behind a
            // branch that always beats it as reachable — which is how the psalter appeared to teach
            // both philosophy AND its own declared decipher, when it can only ever teach the first.
            //
            // Asked twice: for a human body, and with no actor at all, which takes the head of the
            // list unconditionally and so covers the beast half of every beast/human pair.
            foreach (var resolved in new[] { verb.ResolveLesson(ctx), verb.ResolveLesson(ctx with { Actor = null }) })
                if (resolved != null) yield return resolved.ModusMentisId;
        }
    }

    private static readonly Protagonist Actor = new();

    /// <summary>Everything a verb can be asked about, as <c>--verb-probe</c> enumerates it.</summary>
    private static IEnumerable<(Element Target, Element Observable)> Probes(Scene scene, Area area, TimePeriod period)
    {
        foreach (var poi in area.PointsOfInterest)
        {
            yield return (poi, poi);
            foreach (var item in poi.Items) yield return (item, poi);
        }
        foreach (var npc in scene.GetNpcsAt(area, period))
        {
            if (npc.IsSleeping(scene, new PoV(area, period)))
            {
                var bed = Building.BuildingRooms.BedsIn(area).FirstOrDefault();
                if (bed != null)
                {
                    var merged = new SleepingNpcPointOfInterest(npc, bed);
                    yield return (merged, merged);
                }
            }
            yield return (npc, npc);
        }
        yield return (area, area);
        foreach (var reachable in scene.GetReachableAreas(area))
            yield return (reachable, reachable);
    }

    private static IEnumerable<(string Label, Func<int, Scene> Build)> Factories()
    {
        yield return ("VILLAGE",  id => new Village.VillageSceneFactory().Build(id));
        yield return ("FARM",     id => new Farm.FarmSceneFactory().Build(id));
        yield return ("FIELD",    id => new Field.FieldSceneFactory().Build(id));
        yield return ("PLAIN",    id => new Plain.PlainSceneFactory().Build(id));
        yield return ("FOREST",   id => new Forest.ForestSceneFactory().Build(id));
        yield return ("CAVE",     id => new Cave.CaveSceneFactory().Build(id));
        yield return ("COAST",    id => new Coast.CoastSceneFactory().Build(id));
        yield return ("MOUNTAIN", id => new Mountain.MountainSceneFactory().Build(id));
        yield return ("PEAK",     id => new Peak.PeakSceneFactory().Build(id));
    }
}
