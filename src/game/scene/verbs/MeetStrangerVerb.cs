using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Starts a "Meet Stranger" dialogue with an NPC the protagonist has never spoken to.
/// Requires: target is a speakable NpcEntity, current affinity is Stranger,
/// and the protagonist has at least one Speaking modus mentis.
/// </summary>
public class MeetStrangerVerb : Verb
{
    public override string VerbId      => "meet_stranger";
    public override string DisplayName => "Meet";

    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
    {
        if (target is not SceneNpc sceneNpc) return false;
        if (sceneNpc.Entity is not NpcEntity npc) return false;
        if (!npc.CanSpeak) return false;
        if (!npc.IsAlive) return false;
        if (!scene.GetNpcsAt(pov.Where, pov.When).Exists(n => n.Id == sceneNpc.Id)) return false;

        // Only for strangers
        var partyMemberId = actor?.DisplayName ?? "Protagonist";
        if (!npc.AffinityTable.IsStranger(partyMemberId)) return false;

        // Protagonist must have at least one speaking modus mentis
        if (actor != null && ModusMentisRegistry.Instance.GetSpeakingModiMentis().Count == 0) return false;

        return true;
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"introduce yourself to {target.DisplayName}";

    public override void Execute(Scene scene, PoV pov, Protagonist actor, Element target)
    {
        if (target is not SceneNpc sceneNpc || sceneNpc.Entity is not NpcEntity npc) return;
        scene.PendingDialogueRequest = new DialogueRequest(npc, DialogueTreeRegistry.Instance.Get("meet_stranger").TreeId);
    }
}
