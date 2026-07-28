using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Generation;

/// <summary>
/// Hands an item to a generated NPC.
///
/// <para>
/// <see cref="PartyMember.TryAcquireItem"/> exists for the <b>player</b>, where running out of
/// anchor space is a real constraint the capacity rule enforces and a dropped item is the intended
/// answer. An NPC being generated is a different situation: a smith who owns tongs, an apron, gloves
/// and a whetstone owns all four, and thirteen anchors are simply not how that is modelled. Silently
/// dropping the fourth would leave a smith with no whetstone and a line of console noise.
/// </para>
///
/// <para>
/// So: anchor it if it fits (which keeps clothing on the body and a tool in the hand, where the
/// corpse looter and the trade UI expect them), and otherwise put it in
/// <see cref="PartyMember.Inventory"/> — the overflow list that exists for exactly this. Either way
/// <see cref="PartyMember.GetAllItems"/> finds it, so loot and description are unaffected.
/// </para>
/// </summary>
public static class NpcBelongings
{
    /// <summary>Gives <paramref name="item"/> to <paramref name="body"/>, anchored if it fits.</summary>
    public static void Give(PartyMember body, Item item)
    {
        if (body.CanAcquireItem(item)) body.TryAcquireItem(item);
        else                           body.Inventory.Add(item);
    }
}
