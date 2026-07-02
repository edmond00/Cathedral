using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// Dialogue outcome that flags the NPC as having agreed to hire the player. Promotes the
/// <see cref="NpcEntity.PendingJobOffer"/> (chosen when the REQUEST_JOB verb succeeded) to
/// <see cref="NpcEntity.JobRequest"/> so the game controller can open the work menu immediately
/// after the dialogue session ends (mirrors <see cref="OpenTradeMenuOutcome"/>).
/// </summary>
public class OpenJobMenuOutcome : IDialogueOutcome
{
    public string Description => "NPC agrees to take you on for the work";

    public void Apply(NpcEntity npc, string partyMemberId) => npc.JobRequest = npc.PendingJobOffer;
}
