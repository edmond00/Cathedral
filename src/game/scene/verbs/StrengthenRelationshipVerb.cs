using System.Collections.Generic;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Starts a "Strengthen Relationship" dialogue with an NPC the protagonist already knows.
/// Requires: target is a speakable NpcEntity, current affinity is NOT Stranger,
/// and the protagonist has at least one Speaking modus mentis.
/// </summary>
public class StrengthenRelationshipVerb : DialogueVerb
{
    public override string VerbId            => "strengthen_relationship";
    public override string DisplayName       => "Talk";
    public override int    BaseDifficulty    => 1;
    protected override string DialogueTreeId => "strengthen_relationship";

    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
    {
        if (target is not SceneNpc sceneNpc) return false;
        if (SleeperGate.IsAsleep(scene, pov, target)) return false;  // wake them first
        if (sceneNpc.Entity is not NpcEntity npc) return false;
        if (!npc.CanSpeak) return false;
        if (!npc.IsAlive) return false;
        if (!scene.GetNpcsAt(pov.Where, pov.When).Exists(n => n.Id == sceneNpc.Id)) return false;

        // Only for known party members (non-strangers)
        var partyMemberId = actor?.AffinityKey ?? "Protagonist";
        if (npc.AffinityTable.IsStranger(partyMemberId)) return false;

        // Protagonist must have at least one speaking modus mentis
        if (actor != null && ModusMentisRegistry.Instance.GetSpeakingModiMentis().Count == 0) return false;

        return true;
    }

    // The NPC (and their affinity) is named once in the prompt's attention line; the verbatim
    // refers back with the neutral pronoun — embedding the affinity display here used to double up
    // with the contextual label ("meet a distant acquaintance (my acquaintance …), to talk").
    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"meet {NpcPronoun(target)} to talk";

    // Read out of context in the routines menu, so the pronoun is replaced by the name.
    public override string RoutineLabel(Scene scene, PoV pov, Element target, VerbView? view = null)
        => $"meet {NpcName(target)} to talk";

    public override IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        if (target is not SceneNpc sceneNpc || sceneNpc.Entity is not NpcEntity npc)
            return System.Array.Empty<OutcomeReport>();
        return new[] { new DialogueTriggerOutcome(npc, DialogueTreeRegistry.Instance.Get("strengthen_relationship").TreeId) };
    }
}
