using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// Dialogue outcome that clears the enemy flag on the NPC for the given party member.
/// Used after a successful reconciliation.
/// </summary>
public class ClearEnemyOutcome : IDialogueOutcome
{
    public string Description => "NPC is no longer considered an enemy";

    public void Apply(NpcEntity npc, string partyMemberId)
        => npc.AffinityTable.ClearEnemy(partyMemberId);
}
