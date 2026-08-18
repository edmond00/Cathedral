using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Initiates a direct physical attack on an NPC, starting a fight.
/// Attacking is always illegal. Can be attempted even when the enemy is right there
/// (it is inherently a combat verb).
/// </summary>
public class AttackVerb : Verb
{
    public override string VerbId         => "attack";
    public override string DisplayName    => "Attack";
    public override int    BaseDifficulty => 2;

    /// <summary>
    /// Nothing fixed. Every other verb teaches one lesson because there is one way to do it; a blow
    /// is whatever the body threw, so what it teaches is the modus mentis behind the fighting skill
    /// the first blow drew — a punch teaches Brawling, a chop teaches the axe. The grant is therefore
    /// made by <see cref="SuccessReports"/> alongside the blow that decided it, and this returns null
    /// so <c>NarrativeController.GatherVerbReports</c> does not append a second, unrelated lesson.
    ///
    /// <para><c>--verb-audit</c> names this verb in its <c>TeachesPerBlow</c> exemption, since a null
    /// here is otherwise exactly the dead-content fault it exists to catch.</para>
    /// </summary>
    public override string? GrantedModusMentisId(Element? target) => null;

    /// <summary>
    /// A blow is struck with a fighting medium, so a weapon is accepted outright and anything else is
    /// refused outright — see <see cref="Verb.UsesFightingMedium"/>.
    /// </summary>
    public override bool UsesFightingMedium => true;

    /// <summary>
    /// Attacking somebody is a crime — unless they already count you an enemy, in which case the
    /// quarrel was declared before the blow and striking first is only who moved quicker.
    /// </summary>
    protected override bool IsIllegalFor(Scene scene, PoV pov, Element? target, PartyMember? actor)
        => !TargetIsAlreadyHostile(target, actor);

    /// <summary>Attack is a combat verb — valid to attempt even under direct threat.</summary>
    public override bool CanBeUsedUnderThreat => true;

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
    {
        if (target is not SceneNpc sceneNpc) return false;

        // Tiny creatures get catch/crush instead. You do not attack a butterfly, and offering
        // the option alongside them reads as a bug rather than as a choice.
        if (sceneNpc.Entity.Archetype is ShallowNpcArchetype { IsTiny: true }) return false;
        if (!sceneNpc.IsAlive) return false;

        return scene.GetNpcsAt(pov.Where, pov.When).Exists(n => n.Id == sceneNpc.Id);
    }

    // Named NPCs are introduced once in the prompt's attention line, so the verbatim refers back
    // by pronoun; shallow wildlife keeps its type name, articled and lower-cased by DefiniteTarget
    // ("attack the crab") — the bare DisplayName read as a proper noun ("attack Pig") and the persona
    // copied it straight into the action text.
    public override string Verbatim(Scene scene, PoV pov, Element target)
        => target is SceneNpc { Entity: NpcEntity }
            ? $"attack {NpcPronoun(target)}"
            : $"attack {DefiniteTarget(target)}";

    // Read out of context in the routines menu, so the pronoun is replaced by the name.
    public override string RoutineLabel(Scene scene, PoV pov, Element target, VerbAction? view = null)
        => $"attack {NpcName(target)}";

    /// <summary>
    /// The first blow, what follows from it, and what it taught — in that order, which is the order
    /// they read in as chips.
    ///
    /// <para>Wildlife is killed by the blow (there is no anatomy to wound and no fight to have);
    /// anybody else is wounded and the fight begins. <see cref="FirstBlowOutcome"/> is what decides
    /// which skill was thrown, so the lesson is read back off it rather than declared — that is the
    /// whole reason <see cref="GrantedModusMentisId"/> is null.</para>
    ///
    /// <para>A null blow means the body has nothing to strike with, which <c>FirstBlowMediumRule</c>
    /// refuses before the dice. Reaching here anyway would be a body that changed under us, so the
    /// consequences are reported without it rather than the swing being silently dropped.</para>
    /// </summary>
    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor,
                                                          Element target, VerbAction view, Item? tool = null)
    {
        if (target is not SceneNpc sceneNpc) return System.Array.Empty<Outcome>();

        var reports = new List<Outcome>();
        var blow    = FirstBlowOutcome.For(actor, sceneNpc.Entity, tool);
        if (blow != null) reports.Add(blow);

        if (sceneNpc.Entity is ShallowNpcEntity)
            reports.Add(new NpcSlaynOutcome(sceneNpc));
        else if (sceneNpc.Entity is NpcEntity npc)
            reports.Add(new FightTriggerOutcome(npc));

        // Last, so the lesson reads as the consequence of the blow rather than as its headline —
        // the same placement the verb-level grant has in GatherVerbReports.
        if (blow != null)
        {
            var lesson = ModusMentisGrantOutcome.For(actor, blow.Skill.RequiredModusMentisId);
            if (lesson != null) reports.Add(lesson);
        }

        return reports;
    }

    /// <summary>
    /// A missed swing still starts the fight, and it costs the initiative. Nothing is learned — the
    /// blow that teaches is the blow that lands.
    ///
    /// <para>This is the one place the verb reads differently from every other: a failed action is
    /// normally a thing that did not happen, and a failed attack is a thing that happened badly. The
    /// person you swung at saw it either way, and leaving them unbothered was the old behaviour's
    /// real oddity — attack was the only crime whose victim could not tell it had been attempted.</para>
    /// </summary>
    public override IReadOnlyList<Outcome> FailureReports(Scene scene, PoV pov, PartyMember actor, Element target)
        => target is SceneNpc { Entity: NpcEntity npc }
            ? new Outcome[] { new FightTriggerOutcome(npc, $"a swing at {npc.DisplayName} that missed")
                              { EnemyInitiative = true } }
            : System.Array.Empty<Outcome>();
}
