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
public class StrengthenRelationshipVerb : Verb
{
    public override string VerbId         => "strengthen_relationship";
    public override string DisplayName    => "Talk";
    public override int    BaseDifficulty => 1;

    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
    {
        if (target is not SceneNpc sceneNpc) return false;
        if (sceneNpc.Entity is not NpcEntity npc) return false;
        if (!npc.CanSpeak) return false;
        if (!npc.IsAlive) return false;
        if (!scene.GetNpcsAt(pov.Where, pov.When).Exists(n => n.Id == sceneNpc.Id)) return false;

        // Only for known party members (non-strangers)
        var partyMemberId = actor?.DisplayName ?? "Protagonist";
        if (npc.AffinityTable.IsStranger(partyMemberId)) return false;

        // Protagonist must have at least one speaking modus mentis
        if (actor != null && ModusMentisRegistry.Instance.GetSpeakingModiMentis().Count == 0) return false;

        return true;
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
    {
        var actor        = null as Protagonist; // actor not available in Verbatim
        var partyId      = "Protagonist";
        if (target is SceneNpc sceneNpc && sceneNpc.Entity is NpcEntity npc)
        {
            var level   = npc.AffinityTable.GetLevel(partyId);
            var display = level.ToDisplayName(npc.DisplayName);
            return $"talk to {display}";
        }
        return $"talk to {target.DisplayName}";
    }

    public override IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, Protagonist actor, Element target)
    {
        if (target is not SceneNpc sceneNpc || sceneNpc.Entity is not NpcEntity npc)
            return System.Array.Empty<OutcomeReport>();
        return new[] { new DialogueTriggerOutcome(npc, DialogueTreeRegistry.Instance.Get("strengthen_relationship").TreeId) };
    }
}
