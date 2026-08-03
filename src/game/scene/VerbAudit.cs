using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cathedral.Game.Narrative;
using Cathedral.Game.Scene.Building;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Scene;

/// <summary>
/// Headless audit of what there is to <i>do</i> in a location, as opposed to what there is in it.
///
/// <para>Every observable object in a scene expands into a list of actions by asking each registered
/// verb whether it applies (<c>SceneViewAdapter.RefreshVerbs</c>). An object no verb accepts is
/// prose and nothing else — it can be looked at once and never interacted with. Before the sensory
/// verbs existed that described most of the furniture in the game, and the number is invisible in
/// play because nothing is missing, exactly: the object is there, it reads well, and its action list
/// is simply empty.</para>
///
/// <para>So this counts. Per factory, across a sample of location ids, it reports the distribution of
/// verbs-per-observable against the design targets — <b>80% of observables reachable by at least two
/// verbs, 50% by at least three</b>, and about half observable by at least one sense — then warns
/// about the failures that are silent at runtime:</para>
///
/// <list type="bullet">
/// <item>an observable no verb accepts at any period;</item>
/// <item>a verb in the registry that no sampled scene ever offers — dead content;</item>
/// <item>a verb with no <c>GrantedModusMentisId</c>, so succeeding at it teaches nothing;</item>
/// <item>a modus mentis id, declared by a verb or by a target override, that does not resolve —
///   which grants nothing, silently, exactly as a mistyped trait id does;</item>
/// <item>a <c>ReferenceToolIds</c> entry no item matches, which makes the verb permanently
///   impossible rather than merely hard;</item>
/// <item>a location with fewer than two landmark areas.</item>
/// </list>
///
/// <para>Verb applicability is sampled at every time period, because presence is period-gated: an NPC
/// verb that only exists at night is still a verb the object has. Run after adding a verb, a
/// connector, or a batch of scene content.</para>
/// </summary>
public static class VerbAudit
{
    private const int SampleSize = 40;

    /// <summary>How many merged sleeping observations the sweep exercised. Reported so a zero here —
    /// which would mean the whole sleeping path went untested — cannot pass unnoticed.</summary>
    private static int SleepersProbed;

    /// <summary>The design targets from the verb-expansion brief, as fractions of all observables.</summary>
    private const double TargetTwoPlusVerbs  = 0.80;
    private const double TargetThreePlusVerbs = 0.50;
    private const double TargetSensory        = 0.50;

    /// <summary>The four sensory verbs, whose coverage is tracked separately.</summary>
    private static readonly HashSet<string> SensoryVerbIds =
        new() { "examine", "contemplate", "listen", "smell" };

    public static string BuildReport()
    {
        var sb       = new StringBuilder();
        var warnings = new List<string>();

        // Verbs seen offered anywhere, so the sweep can name the ones that were never offered at all.
        var everOffered = new HashSet<string>();

        sb.AppendLine("=== VERB AUDIT ===");
        sb.AppendLine($"Sampling {SampleSize} location ids per factory, at every time period.");
        sb.AppendLine();

        AuditVerbDeclarations(sb, warnings);

        sb.AppendLine("--- COVERAGE BY FACTORY ---");
        sb.AppendLine($"  {"factory",-10} {"observables",11} {"≥1",6} {"≥2",6} {"≥3",6} {"sensory",8}  {"mean",5}");

        foreach (var (label, build) in Factories())
            AuditFactory(sb, warnings, everOffered, label, build);

        sb.AppendLine();
        sb.AppendLine($"  targets: ≥2 verbs on {TargetTwoPlusVerbs:P0} of observables, " +
                      $"≥3 on {TargetThreePlusVerbs:P0}, a sense on {TargetSensory:P0}");

        AuditDeadVerbs(sb, warnings, everOffered);

        sb.AppendLine();
        if (warnings.Count == 0)
        {
            sb.AppendLine("No warnings — every verb is reachable and teaches something, and every");
            sb.AppendLine("observable has an action on it.");
            return sb.ToString();
        }

        // Grouped by kind: one systemic fault across 40 ids × 9 factories buries everything else.
        sb.AppendLine($"--- {warnings.Count} WARNING(S), BY KIND ---");
        foreach (var group in warnings.GroupBy(Kind).OrderByDescending(g => g.Count()))
        {
            sb.AppendLine($"  [{group.Count(),4}] {group.Key}");
            foreach (var example in group.Distinct().Take(3))
                sb.AppendLine($"         e.g. {example}");
        }

        return sb.ToString();
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

    /// <summary>Strips ids and quoted names so the same fault at 40 locations counts as one kind.</summary>
    private static string Kind(string warning)
    {
        var body = System.Text.RegularExpressions.Regex.Replace(warning, @"^[A-Z]+ \d+: ", "");
        body     = System.Text.RegularExpressions.Regex.Replace(body, @"'[^']*'", "'…'");
        return System.Text.RegularExpressions.Regex.Replace(body, @"\b\d+\b", "N");
    }

    // ── Declaration checks (registry only, no scenes needed) ──────────────────

    /// <summary>
    /// Checks what every verb <i>declares</i>, independently of whether any scene offers it: that it
    /// teaches something, that what it teaches exists, and that the tools it demands exist. All three
    /// fail silently at runtime — an unresolvable lesson grants nothing and an unresolvable tool makes
    /// the verb unperformable — so they are worth catching before a scene is even built.
    /// </summary>
    private static void AuditVerbDeclarations(StringBuilder sb, List<string> warnings)
    {
        var verbs    = VerbRegistry.Instance.GetAll().OrderBy(v => v.VerbId).ToList();
        var itemIds  = ItemRegistry.Instance.All.Select(i => i.ItemId).ToHashSet();
        int teaching = 0, gated = 0;

        foreach (var verb in verbs)
        {
            var mmId = verb.GrantedModusMentisId(null);
            if (string.IsNullOrWhiteSpace(mmId))
                warnings.Add($"verb '{verb.VerbId}' teaches no modus mentis — succeeding at it grants nothing");
            else
            {
                teaching++;
                if (ModusMentisRegistry.Instance.GetModusMentis(mmId) == null)
                    warnings.Add($"verb '{verb.VerbId}' teaches '{mmId}', which no modus mentis answers to");
            }

            if (!verb.RequiresTool) continue;
            gated++;
            foreach (var toolId in verb.ReferenceToolIds.Where(t => !itemIds.Contains(t)))
                warnings.Add($"verb '{verb.VerbId}' requires tool '{toolId}', which no item answers to — the verb can never be performed");
        }

        sb.AppendLine("--- VERB DECLARATIONS ---");
        sb.AppendLine($"  {verbs.Count} verb(s) registered; {teaching} teach a modus mentis; {gated} require a tool");
        sb.AppendLine();
    }

    // ── Per-factory coverage sweep ────────────────────────────────────────────

    private static void AuditFactory(
        StringBuilder sb, List<string> warnings, HashSet<string> everOffered,
        string label, Func<int, Scene> build)
    {
        // Verb counts across every observable of every sampled location, one entry per observable.
        var counts        = new List<int>();
        int sensoryCovered = 0;

        // A stand-in actor. Verbs that gate on the player — the dialogue verbs want somebody with a
        // speaking modus mentis — need one, and a default protagonist is the plainest possible player.
        var actor = new Protagonist();

        for (int id = 1; id <= SampleSize; id++)
        {
            Scene scene;
            try
            {
                scene = build(id);
            }
            catch (Exception ex)
            {
                warnings.Add($"{label} {id}: generation threw — {ex.GetType().Name}: {ex.Message}");
                continue;
            }

            SweepAreaVerbs(scene, everOffered, actor);

            if (scene.AllAreas.Count(a => a.IsLandmark) < 2)
                warnings.Add($"{label} {id}: fewer than two landmark areas — there is nothing for a horizon to name");

            foreach (var area in scene.AllAreas)
            {
                foreach (var observable in Observables(area, scene))
                {
                    var offered = OfferedVerbIds(scene, area, observable, actor);
                    foreach (var verbId in offered) everOffered.Add(verbId);

                    counts.Add(offered.Count);
                    if (offered.Overlaps(SensoryVerbIds)) sensoryCovered++;

                    if (offered.Count == 0)
                        warnings.Add($"{label} {id}: '{observable.DisplayName}' in '{area.DisplayName}' has no verb at any period — it is prose only");

                    CheckTargetOverrides(warnings, label, id, observable);
                }
            }
        }

        if (counts.Count == 0)
        {
            sb.AppendLine($"  {label,-10} (nothing generated)");
            return;
        }

        double one   = counts.Count(c => c >= 1) / (double)counts.Count;
        double two   = counts.Count(c => c >= 2) / (double)counts.Count;
        double three = counts.Count(c => c >= 3) / (double)counts.Count;
        double sense = sensoryCovered / (double)counts.Count;

        sb.AppendLine($"  {label,-10} {counts.Count,11} " +
                      $"{Pct(one),6} {Pct(two, TargetTwoPlusVerbs),6} {Pct(three, TargetThreePlusVerbs),6} " +
                      $"{Pct(sense, TargetSensory),8}  {counts.Average(),5:F1}");
    }

    /// <summary>A percentage, marked with '*' when it falls short of a target it is measured against.</summary>
    private static string Pct(double value, double? target = null)
    {
        string text = $"{value * 100:F0}%";
        return target.HasValue && value < target.Value ? text + "*" : text;
    }

    /// <summary>
    /// Everything in an area a player can put a keyword on: its points of interest, the PoIs inside
    /// its spots, the spots themselves, and the NPCs scheduled anywhere in it. Items are deliberately
    /// left out — they are never standalone observations, their verbs fold into the parent PoI.
    /// </summary>
    private static IEnumerable<Element> Observables(Area area, Scene scene)
    {
        foreach (var poi in area.PointsOfInterest) yield return poi;

        foreach (var spot in area.Spots)
        {
            yield return spot;
            foreach (var poi in spot.PointsOfInterest) yield return poi;
        }

        foreach (var npc in scene.Npcs)
        {
            if (!scene.NpcSchedules.TryGetValue(npc.Id, out var schedule)) continue;
            if (schedule.ActivePeriods.Any(p => p.Area.Id == area.Id))
                yield return npc;
        }
    }

    /// <summary>
    /// Every verb that accepts <paramref name="target"/> from <paramref name="area"/> at any period.
    ///
    /// <para>Sampled across all six periods because presence, lock state and schedule all move with
    /// the clock: a verb that only exists at night is still a verb this object has. A default
    /// protagonist stands in for the actor — several verbs gate on one (the dialogue verbs need
    /// somebody with a speaking modus mentis to be speaking), and passing null there silently zeroed
    /// every conversation out of the count.</para>
    /// </summary>
    private static HashSet<string> OfferedVerbIds(Scene scene, Area area, Element target, Protagonist actor)
    {
        var offered = new HashSet<string>();
        SleepingNpcPointOfInterest? addedSleeper = null;

        // An item is never an observation of its own: SceneViewAdapter folds each item's verb views
        // into its holding PoI's action list. So the actions a PoI offers are its own plus those of
        // everything in it, and counting only its own said a well-stocked tree had one verb when the
        // player is looking at five.
        var probes = new List<Element> { target };
        if (target is PointOfInterest holder) probes.AddRange(holder.Items);

        foreach (TimePeriod period in Enum.GetValues<TimePeriod>())
        {
            var pov = new PoV(area, period);

            // A spot's contents are only reachable from inside it.
            if (target is not Spot && area.Spots.FirstOrDefault(s => s.PointsOfInterest.Contains(target)) is { } holdingSpot)
                pov.InSpot = holdingSpot;

            // A sleeping person is not observed as an NPC: placement swaps them and their bed for a
            // single merged object while the sleep lasts, and murder / wake / pickpocket are offered
            // on that. Probing only the NPC form would report those three as reachable while the
            // shape the player actually meets went untested.
            var sleeperProbe = addedSleeper ??= SleeperProbe(scene, area, target, period);
            if (sleeperProbe != null) SleepersProbed++;
            var thisPeriod   = sleeperProbe == null ? probes : probes.Append<Element>(sleeperProbe).ToList();

            foreach (var probe in thisPeriod)
            foreach (var verb in scene.Verbs)
            {
                try
                {
                    if (verb.IsPossible(scene, pov, probe, actor)) offered.Add(verb.VerbId);
                }
                catch (Exception ex)
                {
                    // A verb that throws on an element it does not recognise would crash the real
                    // action menu the moment that element is observed, so this is a finding, not noise.
                    offered.Add($"!{verb.VerbId}");
                    Console.Error.WriteLine(
                        $"VerbAudit: '{verb.VerbId}'.IsPossible threw on '{probe.DisplayName}' — {ex.GetType().Name}: {ex.Message}");
                }
            }
        }

        if (addedSleeper != null) area.PointsOfInterest.Remove(addedSleeper);
        return offered;
    }

    /// <summary>
    /// Builds the merged sleeping observation for <paramref name="target"/> when that NPC would be
    /// asleep here at this hour, and puts it in the area so the verbs that gate on presence can see
    /// it. Mirrors what <c>SceneNpcPlacement</c> does at runtime; the caller takes it out again.
    /// </summary>
    private static SleepingNpcPointOfInterest? SleeperProbe(Scene scene, Area area, Element target, TimePeriod period)
    {
        if (target is not SceneNpc npc) return null;
        if (!npc.IsSleeping(scene, new PoV(area, period))) return null;

        var bed = Building.BuildingRooms.BedsIn(area).FirstOrDefault();
        if (bed == null) return null;

        var merged = new SleepingNpcPointOfInterest(npc, bed);
        area.PointsOfInterest.Add(merged);
        return merged;
    }

    /// <summary>
    /// Records the verbs offered on the areas themselves, which are observations too — every area
    /// bordering you is listed, carrying <c>move</c>. Not folded into the per-observable coverage
    /// figures (an area is a destination, not a thing in a room), but it has to be swept or the
    /// reach check reports the game's most-used verb as dead content.
    /// </summary>
    private static void SweepAreaVerbs(Scene scene, HashSet<string> everOffered, Protagonist actor)
    {
        foreach (var from in scene.AllAreas)
        foreach (TimePeriod period in Enum.GetValues<TimePeriod>())
        {
            var pov = new PoV(from, period);
            foreach (var to in scene.AllAreas)
            foreach (var verb in scene.Verbs)
            {
                try
                {
                    if (verb.IsPossible(scene, pov, to, actor)) everOffered.Add(verb.VerbId);
                }
                catch { /* reported by the per-observable sweep */ }
            }
        }
    }

    /// <summary>
    /// Checks that a target's per-verb modus mentis overrides name verbs that exist and lessons that
    /// resolve. An override keyed to a misspelt verb id is never consulted, and one naming a
    /// non-existent modus mentis silently teaches nothing — both leave the object looking correct.
    /// </summary>
    private static void CheckTargetOverrides(List<string> warnings, string label, int id, Element observable)
    {
        if (observable is not PointOfInterest { VerbModiMentis: { } overrides }) return;

        foreach (var (verbId, mmId) in overrides)
        {
            if (VerbRegistry.Instance.Get(verbId) == null)
                warnings.Add($"{label} {id}: '{observable.DisplayName}' overrides the lesson for verb '{verbId}', which is not a registered verb");

            if (ModusMentisRegistry.Instance.GetModusMentis(mmId) == null)
                warnings.Add($"{label} {id}: '{observable.DisplayName}' teaches '{mmId}' for '{verbId}', which no modus mentis answers to");
        }
    }

    // ── Dead verbs ────────────────────────────────────────────────────────────

    /// <summary>
    /// Verbs the registry knows about that no sampled scene ever offered. Either nothing places their
    /// target yet, or their gate is unsatisfiable — both mean the verb is currently unreachable.
    /// Phase-scoped verbs are exempt: their scenes are built ad hoc and are not in the sweep.
    /// </summary>
    private static void AuditDeadVerbs(StringBuilder sb, List<string> warnings, HashSet<string> everOffered)
    {
        // Verbs the sweep cannot legitimately reach, and why. Excluded from the dead list because
        // they are not dead — reporting them sends the reader looking for scene content that is not
        // missing.
        var unreachable = new Dictionary<string, string>
        {
            // Phase-scoped: their scenes are built ad hoc and are not in this sweep.
            ["remember"] = "childhood phase only",
            ["get_up"]   = "get-up phase only",
            ["ignore"]   = "injected directly, never registry-discovered",
            ["cut"]      = "needs a corpse spot, which only exists after a kill",
            ["leave"]    = "needs the actor to already be inside a spot",
            ["enter_spot"] = "no factory builds a Spot; the only one is the corpse spot",

            // Relationship-gated: the stand-in actor is a stranger to everyone, by construction.
            ["go_toward"] = "needs a landmark already picked out from a high place this visit",
            ["appease"]                 = "needs an enemy or an annoyed acquaintance",
            ["propose_to_join"]         = "needs close-acquaintance-or-better, and room in the party",
            ["tame"]                    = "needs a beast already appeased",
            ["reconcile"]               = "needs an enemy or an annoyed acquaintance",
            ["strengthen_relationship"] = "needs a non-stranger",
            ["propose_to_buy"]          = "needs acquaintance-or-better with a seller",
            ["propose_to_sell"]         = "needs acquaintance-or-better with a buyer",
            ["request_job"]             = "needs acquaintance-or-better with an employer",
        };
        var phaseScoped = unreachable.Keys.ToHashSet();

        var dead = VerbRegistry.Instance.GetAll()
            .Select(v => v.VerbId)
            .Where(id => !everOffered.Contains(id) && !phaseScoped.Contains(id))
            .OrderBy(id => id)
            .ToList();

        sb.AppendLine();
        sb.AppendLine("--- REACH ---");
        sb.AppendLine($"  {everOffered.Count} verb(s) offered somewhere in the sample");
        sb.AppendLine($"  {SleepersProbed} merged sleeping observation(s) exercised");

        // Listed in full here rather than left to the by-kind warning summary, which shows three
        // examples per group. This set is small, it is the to-do list for scene content, and seeing
        // three of nineteen entries is worse than useless.
        var gated = unreachable.Keys
            .Where(id => !everOffered.Contains(id) && VerbRegistry.Instance.Get(id) != null)
            .OrderBy(id => id)
            .ToList();
        if (gated.Count > 0)
        {
            sb.AppendLine($"  {gated.Count} verb(s) not reached by this sweep, legitimately:");
            foreach (var id in gated)
                sb.AppendLine($"      {id,-24} {unreachable[id]}");
        }

        if (dead.Count > 0)
        {
            sb.AppendLine($"  {dead.Count} verb(s) NEVER offered — nothing places their target:");
            foreach (var id in dead)
                sb.AppendLine($"      {id}");
        }

        foreach (var id in dead)
            warnings.Add($"verb '{id}' is never offered by any sampled location — nothing places its target");
    }
}
