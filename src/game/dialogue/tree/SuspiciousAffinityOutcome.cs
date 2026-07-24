using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// Dialogue outcome that sets the NPC's affinity with the party member to
/// <see cref="AffinityLevel.Suspicious"/> — the post-reconcile wary state.
/// Gives 0 bonus dice (same as Stranger) but signals the relationship is no longer hostile.
/// </summary>
public class SuspiciousAffinityOutcome : IDialogueOutcome
{
    public string Description => "NPC is now Suspicious of you (wary but no longer hostile)";

    public OutcomeReport? Apply(NpcEntity npc, string partyMemberId)
    {
        var before = npc.AffinityTable.GetLevel(partyMemberId);
        npc.AffinityTable.SetLevel(partyMemberId, AffinityLevel.Suspicious);
        return DialogueOutcomeReports.AffinityChange(npc, before, AffinityLevel.Suspicious);
    }
}
