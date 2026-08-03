using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// Records that an NPC agreed to travel with the player.
///
/// <para>Sets a flag rather than doing the recruiting, exactly as <c>OpenTradeMenuOutcome</c> and
/// <c>OpenJobMenuOutcome</c> do: a dialogue outcome has no scene and no party to reach, so the
/// controller acts on the flag once the conversation closes. That is also why the flag lives on the
/// NPC — it is the NPC's decision, and it survives the session ending.</para>
/// </summary>
public class JoinPartyOutcome : IDialogueOutcome
{
    public string Description => "the NPC agrees to travel with the player";

    public OutcomeReport? Apply(NpcEntity npc, string partyMemberId)
    {
        npc.JoinRequested = true;
        return new DialogueOutcomeReport($"{npc.DisplayName} will travel with you",
                                         OutcomeReportSeverity.Positive);
    }
}
