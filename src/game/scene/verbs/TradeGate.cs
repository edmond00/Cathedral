using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Shared availability gate for the trade verbs. Trading is a social act that follows an
/// introduction: the NPC must already be a genuine acquaintance or better (not a stranger,
/// not merely an annoying/suspicious contact) and not an enemy.
/// </summary>
internal static class TradeGate
{
    public static bool CanTrade(NpcEntity npc, Protagonist? actor)
    {
        var partyMemberId = actor?.AffinityKey ?? "Protagonist";
        if (npc.AffinityTable.IsEnemy(partyMemberId)) return false;

        var level = npc.AffinityTable.GetLevel(partyMemberId);
        return level is AffinityLevel.DistantAcquaintance
                     or AffinityLevel.CloseAcquaintance
                     or AffinityLevel.DistantFriend
                     or AffinityLevel.CloseFriend;
    }
}
