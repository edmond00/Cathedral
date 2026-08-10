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

    /// <summary>What a success teaches: opening a fight with your hands.</summary>
    public override string? GrantedModusMentisId(Element? target) => "brawling";

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

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        if (target is not SceneNpc sceneNpc) return System.Array.Empty<Outcome>();
        if (sceneNpc.Entity is ShallowNpcEntity)
            return new[] { new NpcSlaynOutcome(sceneNpc) };
        if (sceneNpc.Entity is NpcEntity npc)
            return new[] { new FightTriggerOutcome(npc) };
        return System.Array.Empty<Outcome>();
    }
}
