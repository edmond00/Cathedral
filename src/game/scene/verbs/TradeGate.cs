using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Shared availability gate for the trade verbs. Two conditions, and both must hold:
/// <list type="bullet">
/// <item><b>Acquaintance.</b> Trading is a social act that follows an introduction: the NPC must
/// already be a genuine acquaintance or better (not a stranger, not merely an annoying/suspicious
/// contact) and not an enemy.</item>
/// <item><b>Premises.</b> The goods are where the trade is kept — the forge, the barn, the
/// woodcutter's shed — not in the trader's pockets. A smith met in the street sells nothing until
/// you are standing at the forge with them. <see cref="NpcEntity.TradeAreaIds"/> lists the places,
/// set by the scene factory; an NPC with none trades nowhere.</item>
/// </list>
/// </summary>
internal static class TradeGate
{
    public static bool CanTrade(NpcEntity npc, PartyMember? actor, Area where)
        => npc.TradesIn(where) && IsAcquainted(npc, actor);

    /// <summary>
    /// The acquaintance half alone. Also what gates asking for work, which needs the introduction but
    /// not the premises: a job is offered by a person, wherever they are met.
    /// </summary>
    public static bool IsAcquainted(NpcEntity npc, PartyMember? actor)
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
