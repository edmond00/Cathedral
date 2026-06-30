namespace Cathedral.Game.Narrative;

/// <summary>
/// The three coin denominations used by the trade economy.
/// Each tradeable item is priced in exactly ONE coin type — denominations are never
/// mixed within a single price, and there is no automatic conversion between them
/// (a purchase must be paid with enough coins of the item's own <see cref="Item.PriceCoin"/>).
///
/// Rough value scale (for authoring reference prices, not enforced in code):
///   • 1 gold   ≈ the price of a horse ≈ 100 silver
///   • 1 silver ≈ the price of a sword ≈ 100 copper
///   • 1 copper ≈ the price of an egg
/// </summary>
public enum CoinType
{
    Copper,
    Silver,
    Gold,
}
