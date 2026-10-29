using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Rules.Choice;
using Cathedral.Game.Npc;
using Cathedral.Game.Scene.Building;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Scene;

/// <summary>
/// Headless audit of the crime system — the three things about it that are invisible in play and
/// expensive to reach from a <c>--cli</c> script.
///
/// <list type="bullet">
/// <item><b>Contextual legality.</b> <see cref="Verb.IsIllegal"/> reads the verb, the target, the area
///   and who the actor's enemies are. Every one of those is a silent failure: a burglary that stopped
///   counting as one reads exactly like a burglary, right up to the witness who never appears.</item>
/// <item><b>The choice rules.</b> What a modus mentis is <i>not</i> offered leaves no trace anywhere —
///   the option is simply absent, and a rule that stopped firing would look like a persona making a
///   different choice.</item>
/// <item><b>Enmity across visits.</b> Needs two arrivals at one location to observe at all, and the
///   failure mode is the quiet one: the grudge is not there, and nothing says so.</item>
/// </list>
///
/// <para>Assertion-shaped rather than statistical, unlike the other audits: each case names what it
/// expected and what it got, and the report ends non-empty only when something disagreed. Run it after
/// touching a verb's legality, a choice rule, <see cref="PrivacyModel"/>, or
/// <see cref="LocationInstanceState"/>.</para>
/// </summary>
public static class CrimeAudit
{
    private const int SampleSize = 12;

    public static string BuildReport()
    {
        var sb       = new StringBuilder();
        var failures = new List<string>();

        sb.AppendLine("=== CRIME AUDIT ===");
        sb.AppendLine();

        AuditPrivacyModel(sb, failures);
        AuditVerbLegality(sb, failures);
        AuditGoalRules(sb, failures);
        AuditWillingnessRules(sb, failures);
        AuditDiscreetWillingness(sb, failures);
        AuditExamineRules(sb, failures);
        AuditFailurePenalties(sb, failures);
        AuditFoundWaiting(sb, failures);
        AuditWitnessReach(sb, failures);
        AuditDiscreteness(sb, failures);
        AuditApproach(sb, failures);
        AuditEnmityPersistence(sb, failures);

        sb.AppendLine();
        if (failures.Count == 0)
        {
            sb.AppendLine("No failures — legality reads its context, the choice rules narrow what they");
            sb.AppendLine("should, and enmity outlives the visit it was earned in.");
            return sb.ToString();
        }

        sb.AppendLine($"--- {failures.Count} FAILURE(S) ---");
        foreach (var f in failures) sb.AppendLine($"  ✗ {f}");
        return sb.ToString();
    }

    // ── 1. Privacy: which objects reach into somebody's private space ─────────

    /// <summary>
    /// Across real built scenes: every connector with a private endpoint must read private, every
    /// connector with two public endpoints must read public. The interesting case is the first —
    /// a house door is listed in the public street's PoIs too, and reading the actor's own area
    /// instead of the target's endpoints is what would call a burglary from the street lawful.
    /// </summary>
    private static void AuditPrivacyModel(StringBuilder sb, List<string> failures)
    {
        sb.AppendLine("--- PRIVACY: objects that reach into private space ---");

        int privateReaching = 0, publicOnly = 0, wrong = 0;

        foreach (var (label, build) in Factories())
        {
            for (int id = 0; id < SampleSize; id++)
            {
                var scene = build(id);
                foreach (var connector in scene.AllAreas
                             .SelectMany(a => a.PointsOfInterest)
                             .OfType<ConnectorPointOfInterest>()
                             .Distinct())
                {
                    bool expected = connector.AreaA.IsPrivate || connector.AreaB.IsPrivate;
                    bool actual   = PrivacyModel.ReachesPrivateArea(scene, connector);

                    if (expected) privateReaching++; else publicOnly++;
                    if (expected == actual) continue;

                    wrong++;
                    if (wrong <= 3)
                        failures.Add($"{label}#{id}: connector '{connector.DisplayName}' "
                                     + $"({connector.AreaA.DisplayName} ↔ {connector.AreaB.DisplayName}) "
                                     + $"reads {(actual ? "private" : "public")}, expected "
                                     + $"{(expected ? "private" : "public")}");
                }
            }
        }

        sb.AppendLine($"  {privateReaching} connector(s) reaching a private area, {publicOnly} wholly public");
        if (privateReaching == 0)
            failures.Add("no connector anywhere reaches a private area — the private-target rule is untested");
        sb.AppendLine();
    }

    // ── 2. Verb legality, in each context that is supposed to change it ───────

    /// <summary>
    /// The truth table the design asks for, checked against real scenes rather than against a mock:
    /// the verb objects, the areas and the doors are the ones the game builds.
    /// </summary>
    private static void AuditVerbLegality(StringBuilder sb, List<string> failures)
    {
        sb.AppendLine("--- LEGALITY: verb × context ---");

        var scene = new Village.VillageSceneFactory().Build(1);
        var actor = new Protagonist();

        var publicArea  = scene.AllAreas.FirstOrDefault(a => !a.IsPrivate);
        var privateArea = scene.AllAreas.FirstOrDefault(a => a.IsPrivate);
        if (publicArea == null || privateArea == null)
        {
            failures.Add("village#1 has no public and private area pair — legality cases cannot be built");
            return;
        }

        var publicPov  = new PoV(publicArea,  TimePeriod.Noon);
        var privatePov = new PoV(privateArea, TimePeriod.Noon);

        // A door reaching a private room, and one joining two public areas.
        var connectors = scene.AllAreas.SelectMany(a => a.PointsOfInterest)
                              .OfType<ConnectorPointOfInterest>().Distinct().ToList();
        var privateDoor = connectors.FirstOrDefault(c => c.AreaA.IsPrivate || c.AreaB.IsPrivate);
        var publicDoor  = connectors.FirstOrDefault(c => !c.AreaA.IsPrivate && !c.AreaB.IsPrivate);

        // A person to strike at, and the same person once they have declared for violence.
        var someone = scene.Npcs.FirstOrDefault(n => n.Entity is NpcEntity);

        void Case(string name, Verb verb, PoV pov, Element? target, bool expected)
        {
            if (target == null && verb is not (AttackVerb or SlayVerb))
            {
                sb.AppendLine($"  ~ {name,-52} (no such target in this scene — skipped)");
                return;
            }
            bool actual = verb.IsIllegal(scene, pov, target, actor);
            sb.AppendLine($"  {(actual == expected ? "✓" : "✗")} {name,-52} {(actual ? "illegal" : "legal")}");
            if (actual != expected)
                failures.Add($"{name}: expected {(expected ? "illegal" : "legal")}, got {(actual ? "illegal" : "legal")}");
        }

        // The setting test — it applies to every verb at once, so a harmless one proves it.
        Case("examine, in a public area",            new ExamineVerb(),    publicPov,  publicArea.PointsOfInterest.FirstOrDefault(), false);
        Case("examine, in a private area",           new ExamineVerb(),    privatePov, privateArea.PointsOfInterest.FirstOrDefault(), true);

        // Targets whose privacy is the question, judged from PUBLIC ground in both cases.
        Case("unlock a door reaching a private room", new UnlockDoorVerb(), publicPov, privateDoor, true);
        Case("unlock a wholly public door",           new UnlockDoorVerb(), publicPov, publicDoor,  false);
        Case("break a private-reaching object",       new BreakVerb(),      publicPov, privateDoor, true);
        Case("break a wholly public object",          new BreakVerb(),      publicPov, publicDoor,  false);

        // Violence: the same blow, before and after the other person declares for it.
        if (someone?.Entity is NpcEntity npc)
        {
            npc.AffinityTable.ClearEnemy(actor.AffinityKey);
            Case("attack a stranger",                 new AttackVerb(), publicPov, someone, true);
            Case("slay a stranger",                   new SlayVerb(),   publicPov, someone, true);

            npc.AffinityTable.SetEnemy(actor.AffinityKey);
            Case("attack someone already your enemy", new AttackVerb(), publicPov, someone, false);
            Case("slay someone already your enemy",   new SlayVerb(),   publicPov, someone, false);
            npc.AffinityTable.ClearEnemy(actor.AffinityKey);
        }
        else failures.Add("village#1 has no named NPC — the enemy-legality cases cannot be built");

        // Unconditional crimes, on public ground, to prove they are not merely inheriting the setting.
        Case("pickpocket, on public ground",          new PickpocketVerb(), publicPov, someone, true);
        Case("stalk, on public ground",               new StalkVerb(),      publicPov, someone, true);
        sb.AppendLine();

        AuditWildLegality(sb, failures);
    }

    /// <summary>
    /// Hunting: a beast on nobody's ground is fair game, the same beast on a farm is somebody's, and a
    /// person is never game. Checked on a forest (whose open ground must come out wild and whose
    /// woodshed must not) and on a farm's livestock.
    /// </summary>
    private static void AuditWildLegality(StringBuilder sb, List<string> failures)
    {
        var actor = new Protagonist();

        void Case(string name, Verb verb, Scene scene, PoV pov, Element target, bool expected)
        {
            bool actual = verb.IsIllegal(scene, pov, target, actor);
            sb.AppendLine($"  {(actual == expected ? "✓" : "✗")} {name,-52} {(actual ? "illegal" : "legal")}");
            if (actual != expected)
                failures.Add($"{name}: expected {(expected ? "illegal" : "legal")}, got {(actual ? "illegal" : "legal")}");
        }

        // Tenure: wilderness factories stamp their open ground wild, and leave stores public.
        int wild = 0, storesWild = 0, settledWild = 0;
        for (int id = 0; id < SampleSize; id++)
        {
            var forest = new Forest.ForestSceneFactory().Build(id);
            wild       += forest.AllAreas.Count(a => a.IsWild);
            storesWild += forest.Stores.Count(a => a.IsWild);
            foreach (var (_, build) in Factories())
                settledWild += build(id).AllAreas.Count(a => a.IsWild);
        }
        sb.AppendLine($"  {wild} wild forest area(s); {storesWild} wild store(s); {settledWild} wild settled area(s)");
        if (wild == 0)        failures.Add("no forest area is wild — the wilderness pass did not run");
        if (storesWild > 0)   failures.Add($"{storesWild} forest store(s) read wild — a woodshed is somebody's stock");
        if (settledWild > 0)  failures.Add($"{settledWild} village/farm area(s) read wild — settled ground is somebody's");

        // A beast in the wild, and a person beside it. The opening area always takes a spawned beast.
        var wildScene = new Forest.ForestSceneFactory().Build(1);
        var glade     = wildScene.AllAreas.First(a => a.IsWild);
        var boar      = new SceneNpc(new Npc.Archetypes.PigArchetype().Spawn(GameRng.For("crime_audit_beast")));
        var person    = wildScene.Npcs.FirstOrDefault(n => n.Entity is NpcEntity { Combatant.AnatomyType: AnatomyType.Human });
        var wildPov   = new PoV(glade, TimePeriod.Noon);

        Case("attack a beast, on wild ground",        new AttackVerb(), wildScene, wildPov, boar, false);
        Case("slay a beast, on wild ground",          new SlayVerb(),   wildScene, wildPov, boar, false);
        if (person != null)
        {
            Case("attack a person, on wild ground",   new AttackVerb(), wildScene, wildPov, person, true);
            Case("slay a person, on wild ground",     new SlayVerb(),   wildScene, wildPov, person, true);
            Case("pickpocket, on wild ground",        new PickpocketVerb(), wildScene, wildPov, person, true);
        }
        else sb.AppendLine("  ~ forest#1 has no person — the people-in-the-wild cases are skipped");

        // The same beast on a farm's public ground is somebody's.
        var farm     = new Farm.FarmSceneFactory().Build(1);
        var yard     = farm.AllAreas.First(a => a.Tenure == AreaTenure.Public);
        var farmPov  = new PoV(yard, TimePeriod.Noon);
        Case("slay a beast, on a farm's public ground", new SlayVerb(), farm, farmPov, boar, true);
        sb.AppendLine();
    }

    // ── 3. Goal filtering by the thinking modus mentis's morality ─────────────

    private static void AuditGoalRules(StringBuilder sb, List<string> failures)
    {
        sb.AppendLine("--- CHOICE RULES: goals offered, by thinking morality ---");

        var (scene, ctxFor, lawful, crime) = BuildGoalFixture(failures);
        if (scene == null) { sb.AppendLine("  (fixture unavailable — skipped)"); sb.AppendLine(); return; }

        var mixed = new List<NarrativeAnchor> { lawful!, crime! };

        void Case(string name, MoralLevel morality, IReadOnlyList<NarrativeAnchor> offered, int expectedCount, bool expectCrime)
        {
            var got = ChoiceRulesChecker.FilterGoals(offered, ctxFor(morality));
            bool hasCrime = got.Any(o => o == crime);
            bool ok = got.Count == expectedCount && hasCrime == expectCrime;
            sb.AppendLine($"  {(ok ? "✓" : "✗")} {name,-52} {got.Count} goal(s){(hasCrime ? ", incl. the crime" : "")}");
            if (!ok)
                failures.Add($"{name}: expected {expectedCount} goal(s) "
                             + $"{(expectCrime ? "including" : "excluding")} the crime, got {got.Count} "
                             + $"{(hasCrime ? "including" : "excluding")} it");
        }

        Case("High morality, one lawful + one crime", MoralLevel.High,   mixed, 1, false);
        Case("Low morality, one lawful + one crime",  MoralLevel.Low,    mixed, 1, true);
        Case("Medium morality, one lawful + one crime", MoralLevel.Medium, mixed, 2, true);

        // The edge that must not throw: everything on offer is a crime.
        Case("High morality, crime only",             MoralLevel.High,   new List<NarrativeAnchor> { crime! }, 0, false);
        Case("Low morality, lawful only",             MoralLevel.Low,    new List<NarrativeAnchor> { lawful! }, 1, false);
        sb.AppendLine();
    }

    // ── 4. Willingness filtering by the action modus mentis's morality ────────

    private static void AuditWillingnessRules(StringBuilder sb, List<string> failures)
    {
        sb.AppendLine("--- CHOICE RULES: willingness answers, by action morality ---");

        var (scene, ctxFor, lawful, crime) = BuildGoalFixture(failures);
        if (scene == null) { sb.AppendLine("  (fixture unavailable — skipped)"); sb.AppendLine(); return; }

        var full = new WillingnessOptions(
            new[] { "eager to do it", "willing to do it", "reluctant to do it" }, "unwilling to do it");

        void Case(string name, MoralLevel morality, NarrativeAnchor goal, bool expectDecline)
        {
            var ctx = ctxFor(morality) with { Goal = goal };
            var got = ChoiceRulesChecker.FilterWillingness(full, ctx);
            bool ok = (got.DeclineOption != null) == expectDecline && got.Stances.Count == 3;
            sb.AppendLine($"  {(ok ? "✓" : "✗")} {name,-52} "
                          + $"{got.Stances.Count} stance(s), {(got.DeclineOption != null ? "may refuse" : "no refusal")}");
            if (!ok)
                failures.Add($"{name}: expected {(expectDecline ? "a refusal" : "no refusal")} and 3 stances, "
                             + $"got {(got.DeclineOption != null ? "a refusal" : "no refusal")} and {got.Stances.Count}");
        }

        Case("Low morality, asked to commit a crime",   MoralLevel.Low,    crime!,  false);
        Case("Low morality, asked to do something legal", MoralLevel.Low,  lawful!, true);
        Case("Medium morality, asked to commit a crime", MoralLevel.Medium, crime!, true);
        Case("High morality, asked to commit a crime",  MoralLevel.High,   crime!,  true);
        sb.AppendLine();
    }

    // ── 4a. A discreet means does not refuse what only discretion allows ─────

    /// <summary>
    /// <see cref="DiscreetNeverRefusesCoverRule"/>: a discreet action modus mentis loses the refusal
    /// exactly where a non-discreet one would be stopped by a rule of action that discretion passes —
    /// a crime in sight of a witness, anything but facing the threat with an enemy in the room — and
    /// keeps it everywhere else. Medium morality throughout, so the morality rules count for nothing;
    /// one High case proves the rule drops the option and never lifts a refusal another rule imposed.
    /// </summary>
    private static void AuditDiscreetWillingness(StringBuilder sb, List<string> failures)
    {
        sb.AppendLine("--- CHOICE RULES: willingness answers, by discretion ---");

        var (scene, _, lawful, crime) = BuildGoalFixture(failures);
        if (scene == null) { sb.AppendLine("  (fixture unavailable — skipped)"); sb.AppendLine(); return; }

        // A public standing with somebody in it, and one with nobody; the crime is the private door.
        var standings = scene.AllAreas.Where(a => !a.IsPrivate)
            .SelectMany(a => Enum.GetValues<TimePeriod>().Select(t => new PoV(a, t))).ToList();
        var watched = standings.FirstOrDefault(p => WitnessSelector.ComputeContext(scene, p).Type == WitnessType.Visual);
        var alone   = standings.FirstOrDefault(p => WitnessSelector.ComputeContext(scene, p).Type == WitnessType.None);
        if (watched == null || alone == null)
        {
            failures.Add("village#1 offers no public standing with a witness in sight and one without — discretion cases skipped");
            sb.AppendLine("  (no watched and unwatched standing — skipped)"); sb.AppendLine(); return;
        }

        var actor = new Protagonist();
        var full  = new WillingnessOptions(
            new[] { "eager to do it", "willing to do it", "reluctant to do it" }, "unwilling to do it");

        void Case(string name, PoV pov, MoralLevel morality, bool discreet, NarrativeAnchor goal, bool expectDecline, bool expectImposed = false)
        {
            var ctx = new ChoiceRuleContext(scene, pov, actor, new MoralityProbe(morality, discreet: discreet), goal, Probe: true);
            var got = ChoiceRulesChecker.FilterWillingness(full, ctx);
            bool imposed = got.ImposedRefusal != null;
            bool ok = (got.DeclineOption != null) == expectDecline && imposed == expectImposed && got.Stances.Count == 3;
            sb.AppendLine($"  {(ok ? "✓" : "✗")} {name,-52} "
                          + $"{(imposed ? "refused by rule" : got.DeclineOption != null ? "may refuse" : "no refusal")}");
            if (!ok)
                failures.Add($"{name}: expected {(expectImposed ? "an imposed refusal" : expectDecline ? "a refusal" : "no refusal")}, "
                             + $"got {(imposed ? "an imposed refusal" : got.DeclineOption != null ? "a refusal" : "no refusal")}");
        }

        Case("discreet, a crime in sight of a witness",     watched, MoralLevel.Medium, true,  crime!,  false);
        Case("not discreet, a crime in sight of a witness", watched, MoralLevel.Medium, false, crime!,  true);
        Case("discreet, a crime with nobody in sight",      alone,   MoralLevel.Medium, true,  crime!,  true);
        Case("discreet, lawful in sight of a witness",      watched, MoralLevel.Medium, true,  lawful!, true);
        Case("discreet and principled, a crime in sight",   watched, MoralLevel.High,   true,  crime!,  false, expectImposed: true);

        // An enemy in the room: anything but facing them needs discretion, the lawful act included.
        var foe = scene.GetNpcsAt(watched.Where, watched.When)
                       .Select(n => n.Entity).OfType<Cathedral.Game.Npc.NpcEntity>().First();
        foe.AffinityTable.SetEnemy(Protagonist.AffinityKeyConstant);
        Case("discreet, lawful with an enemy in the room",  watched, MoralLevel.Medium, true,  lawful!, false);
        Case("not discreet, lawful with an enemy in the room", watched, MoralLevel.Medium, false, lawful!, true);
        foe.AffinityTable.ClearEnemy(Protagonist.AffinityKeyConstant);
        sb.AppendLine();
    }

    // ── 4b. The senses belong to the observing modi mentis ──────────────────

    /// <summary>
    /// The two halves of the observation rules (<see cref="SensoryGoalNeedsObservationRule"/>,
    /// <see cref="ExamineActionNeedsObservationRule"/>): a human's thinking modus mentis without
    /// the Observation function is not offered any sense, a beast's is offered them all, and an action
    /// modus mentis without it is made to refuse examining — with the refusal imposed (no question
    /// asked) rather than merely allowed — and nothing else. The other goal in the fixture is a crime,
    /// so Medium morality is used throughout to keep the morality rules out of the count.
    /// </summary>
    private static void AuditExamineRules(StringBuilder sb, List<string> failures)
    {
        sb.AppendLine("--- CHOICE RULES: the senses, by whether the modus mentis observes ---");

        var (scene, _, examine, other) = BuildGoalFixture(failures);
        if (scene == null) { sb.AppendLine("  (fixture unavailable — skipped)"); sb.AppendLine(); return; }

        var pov   = new PoV(scene.AllAreas.First(), TimePeriod.Noon);
        var human = new Protagonist();
        var beast = new EnemyCombatant("beast", new WolfSpecies());
        ChoiceRuleContext Ctx(bool observes, PartyMember? actor = null)
            => new(scene, pov, actor ?? human, new MoralityProbe(MoralLevel.Medium, observes));

        // The rules read the verb's type and nothing else, so the sense needs no target it could reach.
        var listen = new VerbAction(new ListenVerb(), "listen", ((VerbAction)examine!).Target);

        var offered = new List<NarrativeAnchor> { examine!, listen, other! };
        void GoalCase(string name, bool observes, PartyMember actor, int expected)
        {
            var got = ChoiceRulesChecker.FilterGoals(offered, Ctx(observes, actor));
            int senses = got.Count(ObservationGoals.IsSensory);
            bool ok = got.Count == expected && got.Contains(other!);
            sb.AppendLine($"  {(ok ? "✓" : "✗")} {name,-52} {got.Count} goal(s), {senses} of them senses");
            if (!ok) failures.Add($"{name}: expected {expected} goal(s) incl. the non-sensory one, "
                                  + $"got {got.Count} ({senses} senses)");
        }
        GoalCase("human thinking, observes: 2 senses + another",       true,  human, 3);
        GoalCase("human thinking, does not observe: 2 senses + another", false, human, 1);
        GoalCase("beast thinking, does not observe: 2 senses + another", false, beast, 3);

        var full = new WillingnessOptions(
            new[] { "eager to do it", "willing to do it", "reluctant to do it" }, "unwilling to do it");
        void WillCase(string name, bool observes, NarrativeAnchor goal, bool expectImposed)
        {
            var got = ChoiceRulesChecker.FilterWillingness(full, Ctx(observes) with { Goal = goal });
            bool imposed = got.ImposedRefusal != null;
            bool ok = imposed == expectImposed;
            sb.AppendLine($"  {(ok ? "✓" : "✗")} {name,-52} {(imposed ? "refused by rule" : "asked")}");
            if (!ok) failures.Add($"{name}: expected {(expectImposed ? "an imposed refusal" : "the question asked")}, "
                                  + $"got {(imposed ? "an imposed refusal" : "the question asked")}");
        }
        WillCase("action, observes, asked to examine",          true,  examine!, false);
        WillCase("action, does not observe, asked to examine",  false, examine!, true);
        WillCase("action, does not observe, asked to listen",   false, listen,   false);
        WillCase("action, does not observe, asked to do else",  false, other!,   false);
        sb.AppendLine();
    }

    // ── 4c. What a failure costs, and what outranks it ───────────────────────

    /// <summary>
    /// The failure penalties (<see cref="FailurePenalty"/>) read the same ladder this audit is about:
    /// a failure somebody saw or heard cannot end with the way lost, because the confrontation
    /// outranks it. Checked here rather than by a script because staging a wayfinding failure in
    /// front of a witness is a long way round to one boolean. The other cases are the rest of
    /// <see cref="FailurePenalty.Realise"/> — a penalty with nothing to take — and the claim the whole
    /// change rests on: no verb a player can fail is free.
    /// </summary>
    private static void AuditFailurePenalties(StringBuilder sb, List<string> failures)
    {
        sb.AppendLine("--- FAILURE PENALTIES: what can take hold, and what outranks it ---");

        var human = new Protagonist();
        var beast = new EnemyCombatant("beast", new WolfSpecies());
        var area  = new Village.VillageSceneFactory().Build(1).AllAreas.First();
        var pov   = new PoV(area, TimePeriod.Dawn);
        FailureSituation At(PartyMember who, int noetic = 2, bool exit = true, bool confronted = false, bool caught = false)
            => new(who, pov, noetic, exit, confronted || caught, caught);

        void Case(string name, FailurePenalty penalty, FailureSituation s, Type? expected)
        {
            var got = penalty.Realise(s);
            bool ok = expected == null ? got == null : got != null && got.GetType() == expected;
            string what = got switch
            {
                null                     => "cannot take hold",
                WoundInflictionOutcome w => $"{got.GetType().Name} ({w.Wound.Template.GetType().Name})",
                _                        => got.GetType().Name,
            };
            sb.AppendLine($"  {(ok ? "✓" : "✗")} {name,-52} {what}");
            if (!ok) failures.Add($"{name}: expected {expected?.Name ?? "nothing"}, got {what}");
        }

        Case("lose the way, nobody saw",                 FailurePenalty.GetLost,   At(human),                   typeof(LostWayOutcome));
        Case("lose the way, seen or heard",              FailurePenalty.GetLost,   At(human, confronted: true), null);
        Case("lose the way, in a phase with no exit",    FailurePenalty.GetLost,   At(human, exit: false),      null);
        Case("waste the hour",                           FailurePenalty.WasteTime, At(human),                   typeof(TimeWastedOutcome));
        Case("waste the hour, heard only",               FailurePenalty.WasteTime, At(human, confronted: true), typeof(TimeWastedOutcome));
        Case("waste the hour, seen (a fight or caught)", FailurePenalty.WasteTime, At(human, caught: true),     null);
        Case("lose focus, a point left",                 FailurePenalty.LoseFocus, At(human, noetic: 1),        typeof(FocusLostOutcome));
        Case("lose focus, none left",                    FailurePenalty.LoseFocus, At(human, noetic: 0),        null);
        Case("a graze on a human",                       new ScrapeWound(),        At(human),                   typeof(WoundInflictionOutcome));
        Case("a graze on a beast (its own graze lands)", new ContusionWound(),     At(beast),                   typeof(WoundInflictionOutcome));
        Case("a knee fracture on a beast",               new KneeFractureLeftWound(), At(beast),                null);

        // The beast's graze must be the beast's own type, or the save refuses it (PartyState.Rebuild).
        if (FailurePenalty.Of(new ContusionWound()).Realise(At(beast)) is WoundInflictionOutcome bw
            && !WoundRegistry.CanBeSufferedBy(bw.Wound.Template, beast))
            failures.Add($"a graze translated for a beast is {bw.Wound.Template.GetType().Name}, which a beast cannot carry");

        // The draw itself: a table holding the way lost, drawn many times with a witness watching,
        // never loses it — the entry is set aside and the rest of the table drawn instead.
        var move = new MoveToAreaVerb();
        var rng  = GameRng.For("crime-audit-failure-penalties");
        int lost = 0, nothing = 0;
        for (int i = 0; i < 200; i++)
        {
            var drawn = move.DrawFailurePenalty(area, At(human, confronted: true), rng);
            if (drawn is LostWayOutcome) lost++;
            if (drawn == null) nothing++;
        }
        bool drawOk = lost == 0 && nothing == 0;
        sb.AppendLine($"  {(drawOk ? "✓" : "✗")} {"move failed in front of a witness, 200 draws",-52} lost {lost}, nothing {nothing}");
        if (!drawOk) failures.Add($"move drawn with a witness: the way was lost {lost} time(s), nothing drawn {nothing} time(s)");

        // Seen, the same draw never wastes the hour either: the fight or the confrontation begins on
        // the press that closes the act, and an hour passing would be played out over it.
        int wasted = 0; lost = 0; nothing = 0;
        for (int i = 0; i < 200; i++)
        {
            var drawn = move.DrawFailurePenalty(area, At(human, caught: true), rng);
            if (drawn is TimeWastedOutcome) wasted++;
            if (drawn is LostWayOutcome) lost++;
            if (drawn == null) nothing++;
        }
        drawOk = wasted == 0 && lost == 0 && nothing == 0;
        sb.AppendLine($"  {(drawOk ? "✓" : "✗")} {"move failed in plain sight, 200 draws",-52} wasted {wasted}, lost {lost}, nothing {nothing}");
        if (!drawOk) failures.Add($"move drawn in plain sight: the hour wasted {wasted}, the way lost {lost}, nothing drawn {nothing} time(s)");

        // And no table is left with nothing that can take hold when its failure is seen.
        foreach (var verb in VerbRegistry.Instance.GetAll().OrderBy(v => v.VerbId, StringComparer.Ordinal))
        {
            var table = verb.FailurePenalties(null);
            if (table.Count > 0 && !table.Any(p => p.Realise(At(human, caught: true)) != null))
                failures.Add($"{verb.VerbId}: a failure seen costs nothing — every entry is the way lost or the hour wasted");
        }

        // No verb a player can fail is free: every table is non-empty, save the phase-only verbs.
        var exempt = new HashSet<string> { "get_up", "remember" };
        foreach (var verb in VerbRegistry.Instance.GetAll().OrderBy(v => v.VerbId, StringComparer.Ordinal))
        {
            bool empty = verb.FailurePenalties(null).Count == 0;
            if (empty && !exempt.Contains(verb.VerbId))
                failures.Add($"{verb.VerbId}: a failure costs nothing — its FailurePenalties table is empty");
        }
        sb.AppendLine($"  ✓ every registered verb but {string.Join(" and ", exempt)} names a cost");
        sb.AppendLine();
    }

    // ── 5. Can a witness actually be caught in earshot? ──────────────────────

    /// <summary>
    /// Counts, across every private area at every period, which tier of witness is reachable — and
    /// then what each tier means for a non-discrete and a discrete modus mentis.
    ///
    /// <para>This measures the thing the whole crime system hangs on. <b>Visual</b> blocks a
    /// non-discrete action outright and costs a discrete one the confrontation if it fails;
    /// <b>Audio</b> is unheard by a discrete modus mentis and draws the witness into the room when a
    /// non-discrete one fails. If either tier is empty everywhere, a whole branch of the design is
    /// dead content and nothing at runtime would say so.</para>
    ///
    /// <para>The Audio tier is the one that has been empty before: earshot used to be read from the
    /// <c>AreaGraph</c>, and gate connectors deliberately carry no graph edge (an edge would hand
    /// <c>MoveToAreaVerb</c> a way around the gate, which <c>BuildingAudit</c> treats as a fault), so
    /// two rooms joined by a door were not neighbours and nothing indoors could ever be overheard.
    /// <see cref="SceneAdjacency"/> reads the section instead.</para>
    /// </summary>
    /// <summary>
    /// 4d. Found waiting: night brings the householder to a trespasser, the confrontation it makes
    /// opens found_waiting (and only for the waiting), and the leave it can win is leave to stand in
    /// the house — not to take from it, force its doors or learn a routine in it.
    /// </summary>
    private static void AuditFoundWaiting(StringBuilder sb, List<string> failures)
    {
        sb.AppendLine("--- FOUND WAITING: the householder at nightfall, and what their leave covers ---");

        void Check(string name, bool ok, string detail = "")
        {
            sb.AppendLine($"  {(ok ? "✓" : "✗")} {name}{(detail.Length > 0 ? $" — {detail}" : "")}");
            if (!ok) failures.Add($"found waiting: {name}{(detail.Length > 0 ? $" ({detail})" : "")}");
        }

        var protagonist = new Protagonist();
        var key   = protagonist.AffinityKey;
        var scene = new Village.VillageSceneFactory().Build(1);

        var here = scene.AllAreas.FirstOrDefault(a => a.IsPrivate && ConfrontationSelector.Householder(scene, a, key) != null);
        if (here == null || scene.SectionOf(here) is not { } house)
        {
            Check("a village has a private room with a householder", false);
            return;
        }
        var pov = new PoV(here, TimePeriod.Night);

        Check("waiting in a private room is trespass", PrivacyModel.IsTrespassing(scene, here));

        var came = ConfrontationSelector.SummonHouseholder(scene, pov, key);
        Check("night brings the householder", came != null, came?.Entity.DisplayName ?? "nobody came");
        if (came == null) return;
        var npc = (NpcEntity)came.Entity;

        Check("the householder stands in the room", scene.GetAreaOf(came, TimePeriod.Night)?.Id == here.Id);
        Check("the householder is awake, not put to bed by the room", !came.IsSleeping(scene, pov));
        Check("their confrontation is FoundWaiting",
              ConfrontationSelector.Witness(scene, pov, key)?.Kind == ConfrontationKind.FoundWaiting);
        Check("explaining yourself opens found_waiting",
              ConfrontationSelector.TreeFor(scene, npc, null, true, cameForward: true) is Dialogue.Tree.Trees.FoundWaitingTree);
        Check("a lawful act failing in front of them opens found_waiting",
              ConfrontationSelector.TreeFor(scene, npc, new SitAndWaitVerb(), true) is Dialogue.Tree.Trees.FoundWaitingTree);
        Check("a theft failing in front of them opens caught-red-handed",
              ConfrontationSelector.TreeFor(scene, npc, new StealVerb(), true) is not Dialogue.Tree.Trees.FoundWaitingTree);

        // Won: what OnDialogueCompleted does with ToleratesTrespass.
        ConfrontationSelector.Release(scene, npc, protagonist);
        scene.ToleratedSections.Add(house);

        Check("tolerated: standing in the house is no longer trespass", !PrivacyModel.IsTrespassing(scene, here));
        Check("tolerated: nobody confronts", ConfrontationSelector.Witness(scene, pov, key) == null);
        Check("tolerated: sitting is lawful", !new SitAndWaitVerb().IsIllegal(scene, pov, null, protagonist));
        Check("tolerated: stealing is still a crime", new StealVerb().IsIllegal(scene, pov, null, protagonist));
        Check("tolerated: no routine is learned here", !Cathedral.Game.Narrative.Routines.RoutineRecorder.IsRecordable(here));

        var innerDoor = house.Areas.SelectMany(a => a.PointsOfInterest).OfType<DoorPointOfInterest>()
                             .FirstOrDefault(d => d.AreaA.IsPrivate || d.AreaB.IsPrivate);
        if (innerDoor != null)
        {
            Check("tolerated: unlocking an inner door is still a crime", new UnlockDoorVerb().IsIllegal(scene, pov, innerDoor, protagonist));
            Check("tolerated: opening an unlocked inner door is lawful", !new OpenDoorVerb().IsIllegal(scene, pov, innerDoor, protagonist));
        }

        var elsewhere = scene.AllAreas.FirstOrDefault(a => a.IsPrivate && scene.SectionOf(a) != house);
        if (elsewhere != null)
            Check("another building is still trespass", PrivacyModel.IsTrespassing(scene, elsewhere), elsewhere.DisplayName);

        sb.AppendLine();
    }

    private static void AuditWitnessReach(StringBuilder sb, List<string> failures)
    {
        sb.AppendLine("--- WITNESS REACH: which tier can occur in a private area ---");

        int none = 0, visual = 0, audio = 0;

        foreach (var (label, build) in Factories())
        {
            for (int id = 0; id < SampleSize; id++)
            {
                var scene = build(id);
                foreach (var area in scene.AllAreas.Where(a => a.IsPrivate))
                {
                    foreach (TimePeriod period in Enum.GetValues<TimePeriod>())
                    {
                        var ctx = WitnessSelector.ComputeContext(scene, new PoV(area, period));
                        switch (ctx.Type)
                        {
                            case WitnessType.Visual: visual++; break;
                            case WitnessType.Audio:  audio++;  break;
                            default:                 none++;   break;
                        }
                    }
                }
            }
        }

        int total = none + visual + audio;
        sb.AppendLine($"  {total} (private area × period) situations sampled");
        sb.AppendLine($"    unobserved   {none,6}   nobody is any the wiser");
        sb.AppendLine($"    Visual       {visual,6}   in the room: only a discreet skill may try at all");
        sb.AppendLine($"    Audio        {audio,6}   within the section: a botch brings them in");

        if (audio == 0)
            failures.Add($"no private area at any period can hold an Audio witness ({total} sampled) — "
                         + "nothing can ever be overheard, so the approach and the confrontation "
                         + "behind it are unreachable. Check SceneAdjacency.WithinEarshot.");
        if (visual == 0)
            failures.Add($"no private area at any period can hold a Visual witness ({total} sampled) — "
                         + "nothing can ever be caught in the act.");
        sb.AppendLine();
    }

    /// <summary>
    /// The two-question model, as a truth table: may this modus mentis act, and what does failing
    /// cost it? Discreteness answers the first outright and shifts the second by one rung — and only
    /// at Audio range, since being seen is being seen.
    /// </summary>
    private static void AuditDiscreteness(StringBuilder sb, List<string> failures)
    {
        sb.AppendLine("--- DISCRETENESS: may I act, and what does failing cost? ---");

        void Case(string name, WitnessType raw, bool discrete, bool mayAct, WitnessType costsAs)
        {
            // "May I act" is the raw proximity plus discreteness; the cost is effective proximity.
            bool actual   = raw != WitnessType.Visual || discrete;
            var  effective = ProximityModel.Effective(raw, discrete);
            bool ok = actual == mayAct && effective == costsAs;

            string cost = effective switch
            {
                WitnessType.Visual => "confronted",
                WitnessType.Audio  => "they come looking",
                _                  => "nothing",
            };
            sb.AppendLine($"  {(ok ? "✓" : "✗")} {name,-46} {(actual ? "may act" : "blocked"),-8} → {cost}");
            if (!ok)
                failures.Add($"{name}: expected {(mayAct ? "may act" : "blocked")} costing {costsAs}, "
                             + $"got {(actual ? "may act" : "blocked")} costing {effective}");
        }

        Case("non-discrete, witness in the room",  WitnessType.Visual, false, mayAct: false, costsAs: WitnessType.Visual);
        Case("discrete, witness in the room",      WitnessType.Visual, true,  mayAct: true,  costsAs: WitnessType.Visual);
        Case("non-discrete, witness in the section", WitnessType.Audio, false, mayAct: true, costsAs: WitnessType.Audio);
        Case("discrete, witness in the section",   WitnessType.Audio,  true,  mayAct: true,  costsAs: WitnessType.None);
        Case("non-discrete, nobody about",         WitnessType.None,   false, mayAct: true,  costsAs: WitnessType.None);
        sb.AppendLine();
    }

    /// <summary>
    /// The approach: an NPC drawn into the player's area must actually <i>be</i> there — at every
    /// period, to every reader — and must survive the placement pass that rebuilds NPC positions on
    /// each new segment. That pass is what silently undid earlier attempts at moving somebody.
    /// </summary>
    private static void AuditApproach(StringBuilder sb, List<string> failures)
    {
        sb.AppendLine("--- APPROACH: drawing somebody out of their day ---");

        var scene = new Village.VillageSceneFactory().Build(1);
        var npc   = scene.Npcs.FirstOrDefault(n => n.Entity is NpcEntity);
        var here  = scene.AllAreas.FirstOrDefault(a => a.IsPrivate);
        if (npc == null || here == null)
        {
            sb.AppendLine("  (fixture unavailable — skipped)");
            sb.AppendLine();
            return;
        }

        var wasAt = scene.GetAreaOf(npc, TimePeriod.Noon);
        scene.DrawNpcTo(npc, here);

        void Case(string name, bool actual)
        {
            sb.AppendLine($"  {(actual ? "✓" : "✗")} {name,-52} {actual}");
            if (!actual) failures.Add($"approach: {name} — failed");
        }

        Case("stands in the area they were drawn to",
             scene.GetNpcsAt(here, TimePeriod.Noon).Contains(npc));
        Case("is no longer where their schedule put them",
             wasAt == null || wasAt.Id == here.Id || !scene.GetNpcsAt(wasAt, TimePeriod.Noon).Contains(npc));
        Case("is there at every period, not just this one",
             Enum.GetValues<TimePeriod>().All(p => scene.GetNpcsAt(here, p).Contains(npc)));
        Case("reads as going nowhere, so cannot be followed",
             scene.NextRelocation(npc, TimePeriod.Noon) == null);
        Case("is queued to open the next observation phase",
             scene.PendingArrivalObservations.Contains(npc));
        Case("announcing them twice does not queue them twice",
             QueuedTwice(scene, npc, here) == 1);

        sb.AppendLine();
    }

    private static int QueuedTwice(Scene scene, SceneNpc npc, Area here)
    {
        scene.DrawNpcTo(npc, here);
        return scene.PendingArrivalObservations.Count(n => ReferenceEquals(n, npc));
    }

    // ── 6. Enmity outliving the visit ────────────────────────────────────────

    /// <summary>
    /// The whole point of persisting enmity: a scene is rebuilt from its factory on every arrival, so
    /// anything not filed in <see cref="LocationInstanceState"/> did not happen. Checked twice — once
    /// across a rebuild, and once across the JSON round trip a save file will do.
    /// </summary>
    private static void AuditEnmityPersistence(StringBuilder sb, List<string> failures)
    {
        sb.AppendLine("--- PERSISTENCE: enmity across a rebuild ---");

        var state = LocationInstanceState.ForScene(vertex: 1, locationType: "village");
        const string npcId = "blacksmith_Aldric Holt";
        const string me    = "Protagonist";

        // First visit: they declare for violence.
        state.AffinityFor(npcId).SetEnemy(me);

        // Second visit: the scene is gone, the state is not.
        bool acrossRebuild = state.AffinityFor(npcId).IsEnemy(me);
        sb.AppendLine($"  {(acrossRebuild ? "✓" : "✗")} {"enemy on the next arrival",-52} {acrossRebuild}");
        if (!acrossRebuild)
            failures.Add("enmity did not survive a rebuild — AffinityFor is not sharing the enemy store");

        // And across a save.
        var reloaded = LocationInstanceState.FromJson(state.ToJson());
        bool acrossSave = reloaded?.AffinityFor(npcId).IsEnemy(me) ?? false;
        sb.AppendLine($"  {(acrossSave ? "✓" : "✗")} {"enemy after a save/load round trip",-52} {acrossSave}");
        if (!acrossSave)
            failures.Add("enmity did not survive ToJson/FromJson — NpcEnemies is not round-tripping");

        // Reconciling must still clear it, permanently.
        state.AffinityFor(npcId).ClearEnemy(me);
        bool cleared = !state.AffinityFor(npcId).IsEnemy(me);
        sb.AppendLine($"  {(cleared ? "✓" : "✗")} {"reconciliation clears it for good",-52} {cleared}");
        if (!cleared)
            failures.Add("ClearEnemy did not write through to the persisted store");

        // Affinity and enmity move independently — an enemy still has an affinity level.
        state.AffinityFor(npcId).SetEnemy(me);
        state.AffinityFor(npcId).SetLevel(me, AffinityLevel.Suspicious);
        var table = state.AffinityFor(npcId);
        bool independent = table.IsEnemy(me) && table.GetLevel(me) == AffinityLevel.Suspicious;
        sb.AppendLine($"  {(independent ? "✓" : "✗")} {"enmity and affinity persist independently",-52} {independent}");
        if (!independent)
            failures.Add("enmity and affinity are not both persisted — one store is overwriting the other");
    }



    // ── Fixtures ─────────────────────────────────────────────────────────────

    /// <summary>
    /// A real scene, a lawful goal and an illegal one, plus a factory for a context at any morality.
    /// The goals are built from real verbs against real targets so the legality test under them is
    /// the one the game runs, not a stand-in.
    /// </summary>
    private static (Scene? Scene, Func<MoralLevel, ChoiceRuleContext> CtxFor, NarrativeAnchor? Lawful, NarrativeAnchor? Crime)
        BuildGoalFixture(List<string> failures)
    {
        var scene = new Village.VillageSceneFactory().Build(1);
        var actor = new Protagonist();

        var publicArea = scene.AllAreas.FirstOrDefault(a => !a.IsPrivate);
        var connectors = scene.AllAreas.SelectMany(a => a.PointsOfInterest)
                              .OfType<ConnectorPointOfInterest>().Distinct().ToList();
        var privateDoor = connectors.FirstOrDefault(c => c.AreaA.IsPrivate || c.AreaB.IsPrivate);
        var anyThing    = publicArea?.PointsOfInterest.FirstOrDefault();

        if (publicArea == null || privateDoor == null || anyThing == null)
        {
            failures.Add("village#1 lacks a public area, a private door and an object — choice-rule fixture unavailable");
            return (null, _ => null!, null, null);
        }

        var pov = new PoV(publicArea, TimePeriod.Noon);

        var examine = new ExamineVerb();
        var unlock  = new UnlockDoorVerb();
        var lawful  = new VerbAction(examine, examine.Verbatim(scene, pov, anyThing), anyThing);
        var crime   = new VerbAction(unlock, unlock.Verbatim(scene, pov, privateDoor), privateDoor);

        // Sanity: the fixture is only meaningful if the two really do differ in legality.
        if (!unlock.IsIllegal(scene, pov, privateDoor, actor) || examine.IsIllegal(scene, pov, anyThing, actor))
            failures.Add("choice-rule fixture is not a lawful/illegal pair — the rule cases below prove nothing");

        return (scene,
                morality => new ChoiceRuleContext(scene, pov, actor, new MoralityProbe(morality)),
                lawful, crime);
    }

    /// <summary>
    /// A modus mentis that exists only to carry a <see cref="MoralLevel"/> — and whether it observes.
    /// Using a real one would tie each case to a piece of content that can be re-tuned, and the rules
    /// read nothing else.
    ///
    /// <para>It observes by default, and must for the morality cases: their lawful goal is examining,
    /// which a human's mind without the Observation function is never offered
    /// (<see cref="SensoryGoalNeedsObservationRule"/>) — so a non-observing probe would lose that goal
    /// to a rule the morality cases are not about, and count it as a morality failure.</para>
    /// </summary>
    private sealed class MoralityProbe : ModusMentis
    {
        private readonly bool _observes;
        public override string ModusMentisId   => $"morality_probe_{MoralLevel.ToString().ToLowerInvariant()}{(ActsDiscretely ? "_discreet" : "")}";
        public override string DisplayName     => $"{MoralLevel} Probe";
        public override string MenuDescription => "audit fixture";
        public override string SkillMeans      => "the audit's own hands";
        public override ModusMentisFunction[] Functions => _observes
            ? new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking }
            : new[] { ModusMentisFunction.Thinking };
        public override string[] Organs        => new[] { "cerebrum", "eyes" };
        public override Cathedral.Game.Narrative.Memory.ModusMentisMemoryType MemoryType
            => Cathedral.Game.Narrative.Memory.ModusMentisMemoryType.Semantic;
        public override MoralLevel MoralLevel { get; }
        public override bool ActsDiscretely { get; }

        public MoralityProbe(MoralLevel morality, bool observes = true, bool discreet = false)
        {
            MoralLevel     = morality;
            _observes      = observes;
            ActsDiscretely = discreet;
        }
    }

    private static IEnumerable<(string Label, Func<int, Scene> Build)> Factories()
    {
        yield return ("VILLAGE", id => new Village.VillageSceneFactory().Build(id));
        yield return ("FARM",    id => new Farm.FarmSceneFactory().Build(id));
    }
}
