namespace Cathedral.Game.Narrative;

/// <summary>
/// The three coin denominations used by the trade economy.
/// Each tradeable item is priced in exactly ONE coin type — denominations are never
/// mixed within a single price, and there is no automatic conversion between them
/// (a purchase must be paid with enough coins of the item's own <see cref="Item.PriceCoin"/>).
///
/// Rough value scale: 1 copper ≈ an egg. Every good is priced in copper by <see cref="ItemPricing"/>,
/// from 1 (an egg, a sprig of mint) to 85 (an astrolabe); a sword is forty to seventy. Silver and gold
/// are larger denominations that buy no goods today.
/// </summary>
public enum CoinType
{
    Copper,
    Silver,
    Gold,
}
