using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cathedral.Game.Narrative;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Scene;

/// <summary>
/// Answers the one question writing a CLI test for a verb always starts with: <b>where do I stand,
/// and what do I look at, to be offered this?</b>
///
/// <para><c>--verb-audit</c> already sweeps every factory × location id × area × period × object and
/// asks each verb whether it applies. It reports that as coverage statistics and throws the
/// particulars away. This keeps the particulars: for every verb, the first few concrete situations
/// that offered it, printed as the exact flags a script needs —</para>
///
/// <code>
///   gather      --start-at forest --start-area "Berry Thicket" --period noon --observe-only "bramble"
/// </code>
///
/// <para>Without this, authoring a test per verb is 55 rounds of guess-a-seed-and-see, at a run each.
/// With it, the flags are read off a table. Verbs that no sampled scene offers are listed at the end
/// with the reason they are unreachable from a cold start — those need a different approach (a debug
/// flag, or a script that sets the situation up first), and saying so is the point.</para>
/// </summary>
public static class VerbProbe
{
    private const int SampleSize   = 12;
    private const int ExamplesKept = 3;

    /// <summary>One situation in which a verb was offered, as the flags that reproduce it.</summary>
    /// <summary>Whether this sweep pinned schedules — emitted into the flags so a test reproduces it.</summary>
    private static bool Pinned;

    private readonly record struct Situation(string Factory, string Area, string Period, string Target)
    {
        /// <summary>
        /// The flags that reproduce this situation exactly, independently of world generation.
        ///
        /// <para><c>--location-type</c> picks the factory and <c>--location-id</c> picks the build;
        /// together they determine the whole scene, so the world need not contain the biome at all.
        /// <c>--start-at</c> is deliberately not used: it searches the generated world and shrugs when
        /// it finds nothing, which at seed 42 meant every forest test ran in a plain.
        /// <c>--npc-static</c> pins people to one room, since where somebody stands at a given hour is
        /// drawn from the location seed and is not something a test can name.</para>
        /// </summary>
        public string ToFlags(int locationId) =>
            $"--location-type {Factory.ToLowerInvariant()} --location-id {locationId} " +
            (Pinned ? "--npc-static " : "") +
            $"--start-area \"{Area}\" --period {Period.ToLowerInvariant()} --observe-only \"{Target}\"";
    }

    public static string BuildReport()
    {
        // Probe under whatever schedule regime the caller asked for, and emit the matching flag, so
        // its findings and the run agree by construction.
        //
        //   --verb-probe --npc-static   pins people to one room. What the generated tests use: where
        //                               somebody stands at a given hour is otherwise drawn from the
        //                               location seed, and a test cannot name that room.
        //   --verb-probe                real schedules. Needed for the verbs that are ABOUT the
        //                               schedule — murder and wake_up need somebody asleep in their
        //                               own bed, stalk needs somebody who goes somewhere — none of
        //                               which can happen when everyone stands still all day.
        Pinned = Config.Debug.NpcStatic;

        // verb -> situation -> the location ids it was seen in. A situation present in only one id
        // is a trap: area and object names are per-id (a village rolls Chain or Hub layouts with
        // different rooms), so a script pinned to one would open somewhere else on any other seed.
        var found = new Dictionary<string, Dictionary<Situation, HashSet<int>>>();
        var actor = new Protagonist();

        foreach (var (label, build) in Factories())
        {
            for (int id = 0; id < SampleSize; id++)
            {
                var scene = build(id);
                foreach (var area in scene.AllAreas)
                foreach (TimePeriod period in Enum.GetValues<TimePeriod>())
                {
                    var pov = new PoV(area, period);
                    foreach (var (target, observable) in Probes(scene, area, period))
                    foreach (var verb in scene.Verbs)
                    {
                        try
                        {
                            if (!verb.IsPossible(scene, pov, target, actor)) continue;
                        }
                        catch { continue; }   // VerbAudit is where a throwing verb is reported

                        if (!found.TryGetValue(verb.VerbId, out var seen))
                            found[verb.VerbId] = seen = new Dictionary<Situation, HashSet<int>>();

                        // Report the OBSERVABLE, not the probe element. An item is never observed
                        // in its own right — SceneViewAdapter folds an item's verbs into its holding
                        // PoI's action list — so --observe-only only ever matches the holder, and a
                        // script pinned to "Rock" pins to nothing and opens on the whole scene.
                        var s = new Situation(label, area.DisplayName, period.ToString(), observable.DisplayName);
                        if (!seen.TryGetValue(s, out var ids)) seen[s] = ids = new HashSet<int>();
                        ids.Add(id);
                    }
                }
            }
        }

        var sb = new StringBuilder();

        sb.AppendLine("=== VERB PROBE ===");
        sb.AppendLine($"Sampling {SampleSize} location ids per factory, every area, every period.");
        sb.AppendLine("Flags that reach each verb from a cold start — paste into a cli script's header.");
        sb.AppendLine();

        var all = VerbRegistry.Instance.GetAll().Select(v => v.VerbId).OrderBy(v => v, StringComparer.Ordinal).ToList();

        foreach (var verbId in all.Where(v => found.ContainsKey(v)))
        {
            sb.AppendLine($"── {verbId}");
            // Most ubiquitous first: a situation that held in every sampled id of its factory will
            // hold at whatever vertex the seed actually spawns on, which is the only thing a script
            // can rely on. The count is printed so an author can see how thin the ice is.
            foreach (var (s, ids) in found[verbId]
                         .OrderByDescending(kv => kv.Value.Count)
                         .ThenBy(kv => kv.Key.Area, StringComparer.Ordinal)
                         .Take(ExamplesKept))
                sb.AppendLine($"     [{ids.Count,2}/{SampleSize} ids] {s.ToFlags(ids.Min())}");
        }

        var unreached = all.Where(v => !found.ContainsKey(v)).ToList();
        sb.AppendLine();
        sb.AppendLine($"── UNREACHED BY THIS SWEEP ({unreached.Count}) ──");
        sb.AppendLine("   These need a situation to be built first — a companion, an enemy, an appeased");
        sb.AppendLine("   beast, a corpse, an acquaintance. A script reaches them by doing that work in");
        sb.AppendLine("   its own opening, or by a debug flag that sets it up.");
        foreach (var v in unreached)
            sb.AppendLine($"     {v,-26} {WhyUnreached(v)}");

        return sb.ToString();
    }

    /// <summary>
    /// Everything a verb can be asked about, paired with the thing a script would have to
    /// <c>--observe-only</c> to reach it. For a point of interest and an NPC those are the same; for
    /// an item they are not — the item is what the verb targets, the holder is what is observable.
    /// </summary>
    private static IEnumerable<(Element Target, Element Observable)> Probes(Scene scene, Area area, TimePeriod period)
    {
        foreach (var poi in area.PointsOfInterest)
        {
            yield return (poi, poi);
            foreach (var item in poi.Items) yield return (item, poi);
        }
        foreach (var npc in scene.GetNpcsAt(area, period))
        {
            // A sleeping NPC is not observable AS an NPC: SceneNpcPlacement swaps them and their bed
            // for one merged object while the sleep lasts, and murder / wake_up / pickpocket are
            // offered on that. Reporting the person's name for a period they spend in bed sends a
            // test after something the scene does not contain — which is how every NPC verb probed
            // at dawn came back untargetable.
            if (npc.IsSleeping(scene, new PoV(area, period)))
            {
                var bed = Building.BuildingRooms.BedsIn(area).FirstOrDefault();
                if (bed != null)
                {
                    var merged = new SleepingNpcPointOfInterest(npc, bed);
                    yield return (merged, merged);
                }
            }

            // Always the NPC too, sleeping or not. The sleeper verbs gate through
            // SleeperGate.Sleeper, which accepts either form, so yielding only the merged object hid
            // murder and wake_up entirely whenever a bedroom had no bed to merge with — the probe
            // reported them unreachable when they are not.
            yield return (npc, npc);
        }
        yield return (area, area);

        // The areas you could walk to. `move` targets a DESTINATION, never the room you are standing
        // in, so probing only the current area reported the game's most-used verb as unreachable.
        // Every bordering area is listed as an observation in its own right, so each is targetable.
        foreach (var reachable in scene.GetReachableAreas(area))
            yield return (reachable, reachable);
    }

    private static string WhyUnreached(string verbId) => verbId switch
    {
        "appease"   or "reconcile"  => "needs an enemy or an annoyed acquaintance",
        "tame"                      => "needs a beast already appeased (appease first, then tame)",
        "cut"                       => "needs a corpse, which only exists after a kill",
        "get_up"                    => "get-up phase only",
        "remember"                  => "childhood phase only",
        "propose_to_buy"  or "propose_to_sell" => "needs acquaintance-or-better with a trader",
        "propose_to_join"           => "needs close-acquaintance-or-better, and room in the party",
        "request_job"               => "needs acquaintance-or-better with an employer",
        "strengthen_relationship"
            or "gather_knowledge"   => "needs a non-stranger",
        _                           => "no sampled scene offered it",
    };

    private static IEnumerable<(string Label, Func<int, Scene> Build)> Factories()
    {
        yield return ("VILLAGE",  id => new Village.VillageSceneFactory().Build(id));
        yield return ("FARM",     id => new Farm.FarmSceneFactory().Build(id));
        yield return ("FIELD",    id => new Field.FieldSceneFactory().Build(id));
        yield return ("PLAIN",    id => new Plain.PlainSceneFactory().Build(id));
        yield return ("FOREST",   id => new Forest.ForestSceneFactory().Build(id));
        yield return ("CAVE",     id => new Cave.CaveSceneFactory().Build(id));
        yield return ("MOUNTAIN", id => new Mountain.MountainSceneFactory().Build(id));
        yield return ("PEAK",     id => new Peak.PeakSceneFactory().Build(id));
        // Coast is registered by the game and was missing here, which is why `fish` and
        // `swim_across` came back unreachable: nothing else in the sample has open water.
        yield return ("COAST",    id => new Coast.CoastSceneFactory().Build(id));
        // The test location, so --verb-probe can report which verbs it covers. It is not real
        // content: --verb-audit deliberately does NOT sweep it, so a verb reachable only here still
        // shows up there as dead content.
        yield return ("TEST",     id => new Test.TestSceneFactory().Build(id));
    }
}
