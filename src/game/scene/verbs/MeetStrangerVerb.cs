using System.Collections.Generic;
using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Starts a "Meet Stranger" dialogue with an NPC the protagonist has never spoken to.
/// Requires: target is a speakable NpcEntity, current affinity is Stranger,
/// and the protagonist has at least one Speaking modus mentis.
/// </summary>
public class MeetStrangerVerb : DialogueVerb
{
    public override string VerbId            => "meet_stranger";
    public override string DisplayName       => "Meet";
    public override int    BaseDifficulty    => 1;
    protected override string DialogueTreeId => "meet_stranger";

    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
    {
        if (target is not SceneNpc sceneNpc) return false;
        if (sceneNpc.Entity is not NpcEntity npc) return false;
        if (!npc.CanSpeak) return false;
        if (!npc.IsAlive) return false;
        if (!scene.GetNpcsAt(pov.Where, pov.When).Exists(n => n.Id == sceneNpc.Id)) return false;

        // Only for strangers
        var partyMemberId = actor?.AffinityKey ?? "Protagonist";
        if (!npc.AffinityTable.IsStranger(partyMemberId)) return false;

        // Protagonist must have at least one speaking modus mentis
        if (actor != null && ModusMentisRegistry.Instance.GetSpeakingModiMentis().Count == 0) return false;

        return true;
    }

    // Leads with the encounter itself — the introduction is only the intent behind it, since the
    // conversation proper is handled by the dialogue tree this verb triggers. The NPC is named once
    // in the prompt's attention line; the verbatim refers back with the neutral pronoun.
    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"meet {NpcPronoun(target)} to introduce myself";

    // Read out of context in the routines menu, so the pronoun is replaced by the name.
    public override string RoutineLabel(Scene scene, PoV pov, Element target, VerbView? view = null)
        => $"meet {NpcName(target)} to introduce myself";

    public override IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        if (target is not SceneNpc sceneNpc || sceneNpc.Entity is not NpcEntity npc)
            return System.Array.Empty<OutcomeReport>();
        return new[] { new DialogueTriggerOutcome(npc, DialogueTreeRegistry.Instance.Get("meet_stranger").TreeId) };
    }
}
