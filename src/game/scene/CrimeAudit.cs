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
    }

    // ── 3. Goal filtering by the thinking modus mentis's morality ─────────────

    private static void AuditGoalRules(StringBuilder sb, List<string> failures)
    {
        sb.AppendLine("--- CHOICE RULES: goals offered, by thinking morality ---");

        var (scene, ctxFor, lawful, crime) = BuildGoalFixture(failures);
        if (scene == null) { sb.AppendLine("  (fixture unavailable — skipped)"); sb.AppendLine(); return; }

        var mixed = new List<ConcreteOutcome> { lawful!, crime! };

        void Case(string name, MoralLevel morality, IReadOnlyList<ConcreteOutcome> offered, int expectedCount, bool expectCrime)
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
        Case("High morality, crime only",             MoralLevel.High,   new List<ConcreteOutcome> { crime! }, 0, false);
        Case("Low morality, lawful only",             MoralLevel.Low,    new List<ConcreteOutcome> { lawful! }, 1, false);
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

        void Case(string name, MoralLevel morality, ConcreteOutcome goal, bool expectDecline)
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
    private static (Scene? Scene, Func<MoralLevel, ChoiceRuleContext> CtxFor, ConcreteOutcome? Lawful, ConcreteOutcome? Crime)
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
        var lawful  = new VerbOutcome(new VerbView(examine, examine.Verbatim(scene, pov, anyThing), anyThing), anyThing);
        var crime   = new VerbOutcome(new VerbView(unlock, unlock.Verbatim(scene, pov, privateDoor), privateDoor), privateDoor);

        // Sanity: the fixture is only meaningful if the two really do differ in legality.
        if (!unlock.IsIllegal(scene, pov, privateDoor, actor) || examine.IsIllegal(scene, pov, anyThing, actor))
            failures.Add("choice-rule fixture is not a lawful/illegal pair — the rule cases below prove nothing");

        return (scene,
                morality => new ChoiceRuleContext(scene, pov, actor, new MoralityProbe(morality)),
                lawful, crime);
    }

    /// <summary>
    /// A modus mentis that exists only to carry a <see cref="MoralLevel"/>. Using a real one would
    /// tie each case to a piece of content that can be re-tuned, and the rules read nothing else.
    /// </summary>
    private sealed class MoralityProbe : ModusMentis
    {
        public override string ModusMentisId   => $"morality_probe_{MoralLevel.ToString().ToLowerInvariant()}";
        public override string DisplayName     => $"{MoralLevel} Probe";
        public override string MenuDescription => "audit fixture";
        public override string SkillMeans      => "the audit's own hands";
        public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking };
        public override string[] Organs        => new[] { "cerebrum", "eyes" };
        public override Cathedral.Game.Narrative.Memory.ModusMentisMemoryType MemoryType
            => Cathedral.Game.Narrative.Memory.ModusMentisMemoryType.Semantic;
        public override MoralLevel MoralLevel { get; }

        public MoralityProbe(MoralLevel morality) => MoralLevel = morality;
    }

    private static IEnumerable<(string Label, Func<int, Scene> Build)> Factories()
    {
        yield return ("VILLAGE", id => new Village.VillageSceneFactory().Build(id));
        yield return ("FARM",    id => new Farm.FarmSceneFactory().Build(id));
    }
}
