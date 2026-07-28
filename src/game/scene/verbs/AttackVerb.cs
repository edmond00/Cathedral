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

    /// <summary>Attacking a person is never a legal action.</summary>
    public override bool IsLegal => false;

    /// <summary>Attack is a combat verb — valid to attempt even under direct threat.</summary>
    public override bool CanBeUsedUnderThreat => true;

    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
    {
        if (target is not SceneNpc sceneNpc) return false;
        if (!sceneNpc.IsAlive) return false;
        if (pov.InSpot != null) return false;  // can't attack from inside a spot

        return scene.GetNpcsAt(pov.Where, pov.When).Exists(n => n.Id == sceneNpc.Id);
    }

    // Named NPCs are introduced once in the prompt's attention line, so the verbatim refers back
    // by pronoun; shallow wildlife keeps its type name ("attack the crab").
    public override string Verbatim(Scene scene, PoV pov, Element target)
        => target is SceneNpc { Entity: NpcEntity }
            ? $"attack {NpcPronoun(target)}"
            : $"attack {target.DisplayName}";

    // Read out of context in the routines menu, so the pronoun is replaced by the name.
    public override string RoutineLabel(Scene scene, PoV pov, Element target, VerbView? view = null)
        => $"attack {NpcName(target)}";

    public override IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        if (target is not SceneNpc sceneNpc) return System.Array.Empty<OutcomeReport>();
        if (sceneNpc.Entity is ShallowNpcEntity)
            return new[] { new NpcSlaynOutcome(sceneNpc) };
        if (sceneNpc.Entity is NpcEntity npc)
            return new[] { new FightTriggerOutcome(npc) };
        return System.Array.Empty<OutcomeReport>();
    }
}
