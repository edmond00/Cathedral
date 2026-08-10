using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Kills a living NPC without combat. Only possible when the player is in the same area
/// as the NPC and the NPC is currently alive.
/// On execution: sets <c>IsAlive = false</c>, spawns the NPC's remains — a
/// <see cref="Cathedral.Game.Npc.Corpse.CorpsePointOfInterest"/>, plus a belongings PoI for a human —
/// in the current area, and registers them in the scene.
/// </summary>
public class SlayVerb : Verb
{
    public override string VerbId         => "slay";
    public override string DisplayName    => "Slay";
    public override int    BaseDifficulty => 3;

    /// <summary>What a success teaches: killing without giving the other a fight.</summary>
    public override string? GrantedModusMentisId(Element? target) => "low_blow";

    /// <summary>
    /// Killing is a crime — unless the one being killed already counts you an enemy. Finishing
    /// somebody who has declared for your death is not what a witness would call murder.
    /// </summary>
    protected override bool IsIllegalFor(Scene scene, PoV pov, Element? target, PartyMember? actor)
        => !TargetIsAlreadyHostile(target, actor);

    /// <summary>Slaying is an attack — it can be attempted even under direct threat.</summary>
    public override bool CanBeUsedUnderThreat => true;

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
    {
        if (target is not SceneNpc npc) return false;
        if (!npc.IsAlive) return false;

        // Tiny creatures get catch/crush instead. Slaying a snail is not a thing anyone does, and
        // offering it beside "crush" reads as a bug rather than as a choice.
        if (npc.Entity.Archetype is Cathedral.Game.Npc.ShallowNpcArchetype { IsTiny: true }) return false;

        // NPC must be present at the current area and time
        return scene.GetNpcsAt(pov.Where, pov.When).Exists(n => n.Id == npc.Id);
    }

    // Named NPCs are introduced once in the prompt's attention line, so the verbatim refers back
    // by pronoun; shallow wildlife keeps its type name ("slay the crab").
    public override string Verbatim(Scene scene, PoV pov, Element target)
        => target is SceneNpc { Entity: Cathedral.Game.Npc.NpcEntity }
            ? $"slay {NpcPronoun(target)}"
            : $"slay the {target.DisplayName.ToLowerInvariant()}";

    // Read out of context in the routines menu, so the pronoun is replaced by the name.
    public override string RoutineLabel(Scene scene, PoV pov, Element target, VerbAction? view = null)
        => $"slay {NpcName(target)}";

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        if (target is not SceneNpc npc) return System.Array.Empty<Outcome>();
        return new[] { new NpcSlaynOutcome(npc) };
    }
}
