using System.Linq;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Narrative;

/// <summary>
/// How much the way someone is dressed helps them talk to a particular kind of person.
///
/// One bonus die per worn garment that reads well to that standing, and garments accumulate — a
/// townsman's cap, tunic and breeches together are worth three dice to a craftsman and nothing at
/// all to a hermit. A single garment never grants more than one die to any one standing, however
/// many standings it flatters.
///
/// Accumulation stops at <see cref="MaxDice"/>. Past that point a person is already as well turned
/// out as the standing can read; piling on a seventh flattering garment would otherwise let a player
/// dress their way past every difficulty a tree can author.
///
/// The "actually worn" test is the same one armour uses: the item must sit on an anchor its own
/// <see cref="WearSlot"/> maps to. A fine coat carried in a pack impresses nobody.
/// </summary>
public static class WearingDialogueBonus
{
    /// <summary>The most dice any outfit can earn with a single social standing.</summary>
    public const int MaxDice = 6;

    /// <summary>
    /// Bonus dialogue dice <paramref name="member"/>'s dress earns with <paramref name="social"/>,
    /// capped at <see cref="MaxDice"/>.
    /// </summary>
    public static int For(PartyMember member, SocialCategory social)
        => Math.Min(CountFlattering(member, social), MaxDice);

    /// <summary>
    /// Flattering garments actually worn, uncapped — what <see cref="Explain"/> lists, and the
    /// number the cap is applied to.
    /// </summary>
    private static int CountFlattering(PartyMember member, SocialCategory social)
    {
        int dice = 0;
        foreach (var (anchor, items) in member.EquippedItems)
            foreach (var item in items)
                if (item is WearableItem worn
                    && worn.Slot.AnchorsTo(anchor)
                    && worn.DialogueAppeal.Contains(social))
                    dice++;
        return dice;
    }

    /// <summary>
    /// The garments responsible, for the debug log and the inventory panel — knowing that the
    /// bonus is three is less useful than knowing which three things are earning it. Says so when
    /// the outfit has run past <see cref="MaxDice"/>, or the list would look like it disagrees
    /// with the number beside it.
    /// </summary>
    public static string Explain(PartyMember member, SocialCategory social)
    {
        var names = member.EquippedItems
            .SelectMany(kv => kv.Value.Select(item => (Anchor: kv.Key, Item: item)))
            .Where(x => x.Item is WearableItem w
                     && w.Slot.AnchorsTo(x.Anchor)
                     && w.DialogueAppeal.Contains(social))
            .Select(x => x.Item.DisplayName)
            .ToList();

        if (names.Count == 0) return "nothing";
        string list = string.Join(", ", names);
        return names.Count > MaxDice ? $"{list} — capped at {MaxDice}" : list;
    }
}
