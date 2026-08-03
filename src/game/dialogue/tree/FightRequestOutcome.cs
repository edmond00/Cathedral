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
    /// <summary>
    /// Whether the fight is between the two of them alone. False by default — a fight demanded after
    /// a failed reconciliation or a caught theft brings the NPC's friends — and true for a
    /// provocation, where getting somebody on their own is the whole point.
    /// </summary>
    private readonly bool _personal;

    public FightRequestOutcome(bool personal = false) => _personal = personal;

    public string Description => _personal ? "NPC is goaded into a fight, alone" : "NPC demands a fight";

    public OutcomeReport? Apply(NpcEntity npc, string partyMemberId)
    {
        npc.FightRequestedByDialogue = true;
        npc.FightIsPersonal          = _personal;
        return DialogueOutcomeReports.Relation(
            $"{npc.DisplayName} demands a fight", OutcomeReportSeverity.Negative);
    }
}
