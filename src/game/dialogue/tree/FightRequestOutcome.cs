using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// Dialogue outcome that flags the NPC as demanding a fight.
/// <see cref="NpcEntity.FightRequestedByDialogue"/> is set to true so the game controller
/// can transition into fight mode immediately after the dialogue session ends.
/// </summary>
public class FightRequestOutcome : IDialogueOutcome
{
    public string Description => "NPC demands a fight";

    public OutcomeReport? Apply(NpcEntity npc, string partyMemberId)
    {
        npc.FightRequestedByDialogue = true;
        return DialogueOutcomeReports.Relation(
            $"{npc.DisplayName} demands a fight", OutcomeReportSeverity.Negative);
    }
}
