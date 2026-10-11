using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Shared availability gate for the trade verbs. Two conditions, and both must hold:
/// <list type="bullet">
/// <item><b>Not an enemy.</b> No introduction is needed: a shop deals with strangers, and since trade
/// happens only at the shop, asking to buy is the one conversation nobody needs to be presented for.
/// It is also a way of meeting someone — a conversation that ends leaves them knowing you.</item>
/// <item><b>Premises.</b> The goods are where the trade is kept — the forge, the barn, the
/// woodcutter's shed — not in the trader's pockets. A smith met in the street sells nothing until
/// you are standing at the forge with them. <see cref="NpcEntity.TradeAreaIds"/> lists the places,
/// set by the scene factory; an NPC with none trades nowhere.</item>
/// </list>
/// </summary>
internal static class TradeGate
{
    /// <summary>
    /// The first period, from <paramref name="from"/> on through the day and round again, at which the
    /// trader stands on their own premises — and the area. Null when their day never brings them
    /// there. What <c>ask_to_be_led</c> walks the player to: the shop at the hour it is kept.
    /// </summary>
    public static (Area Area, TimePeriod When)? NextOnPremises(Scene scene, SceneNpc trader, TimePeriod from)
    {
        if (trader.Entity is not NpcEntity npc || !npc.Trades) return null;
        var all = (TimePeriod[])System.Enum.GetValues(typeof(TimePeriod));
        for (int i = 0; i < all.Length; i++)
        {
            var when = all[((int)from + i) % all.Length];
            if (scene.GetAreaOf(trader, when) is { } area && npc.TradesIn(area)) return (area, when);
        }
        return null;
    }

    public static bool CanTrade(NpcEntity npc, PartyMember? actor, Area where)
        => npc.TradesIn(where) && !npc.AffinityTable.IsEnemy(actor?.AffinityKey ?? "Protagonist");

    /// <summary>
    /// Acquaintance or better, and not an enemy. What gates asking for work, which needs the
    /// introduction but not the premises: a job is offered by a person, wherever they are met.
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
