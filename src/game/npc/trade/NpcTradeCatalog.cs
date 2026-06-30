using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Trade;

/// <summary>Which side of a trade is being conducted with an NPC.</summary>
public enum TradeMode
{
    None,
    /// <summary>The NPC sells to the player (uses the NPC's sell tag).</summary>
    Buy,
    /// <summary>The NPC buys from the player (uses the NPC's buy tag).</summary>
    Sell,
}

/// <summary>A single line in a catalogue: an item prototype and its agreed unit price.</summary>
public sealed class TradeOffer
{
    /// <summary>Prototype instance from <see cref="ItemRegistry"/> — clone it to create stock.</summary>
    public Item Prototype { get; }

    /// <summary>Unit price in <see cref="Item.PriceCoin"/>, after catalogue variation (1..100).</summary>
    public int UnitPrice { get; }

    public CoinType Coin => Prototype.PriceCoin;

    public TradeOffer(Item prototype, int unitPrice)
    {
        Prototype = prototype;
        UnitPrice = unitPrice;
    }
}

/// <summary>
/// The fixed set of goods an NPC will trade in one direction, sampled from the items carrying
/// the NPC's relevant tag. Both the item selection and the per-item prices are seeded from the
/// NPC id (and the trade direction), so the same NPC always presents the same catalogue.
///
/// Sampling: ~5 items on average, at most 10, and at least 3 when the tag has 3+ items
/// (otherwise every tagged item is offered).
/// </summary>
public sealed class NpcTradeCatalog
{
    public TradeMode Mode { get; }
    public ItemTag   Tag  { get; }
    public IReadOnlyList<TradeOffer> Offers { get; }

    private NpcTradeCatalog(TradeMode mode, ItemTag tag, IReadOnlyList<TradeOffer> offers)
    {
        Mode   = mode;
        Tag    = tag;
        Offers = offers;
    }

    /// <summary>
    /// Builds the catalogue for one NPC and direction. Deterministic for a given
    /// (<paramref name="npcId"/>, <paramref name="mode"/>, <paramref name="tag"/>).
    /// </summary>
    public static NpcTradeCatalog Build(string npcId, TradeMode mode, ItemTag tag)
    {
        var rng = new Random(StableSeed(npcId, mode));

        var available = ItemRegistry.Instance.WithTag(tag).ToList();

        // Choose how many items to offer.
        int count;
        if (available.Count <= 3)
        {
            count = available.Count;                 // take all when scarce
        }
        else
        {
            // Skewed toward ~5: product of two uniforms biases low, scaled into 0..7, +3 → 3..10.
            int extra = (int)Math.Round(rng.NextDouble() * rng.NextDouble() * 7);
            count = Math.Clamp(3 + extra, 3, Math.Min(10, available.Count));
        }

        // Sample distinct prototypes.
        var picked = available.OrderBy(_ => rng.Next()).Take(count).ToList();

        var offers = picked
            .Select(proto => new TradeOffer(proto, VaryPrice(proto.PriceReference, rng)))
            .ToList();

        return new NpcTradeCatalog(mode, tag, offers);
    }

    /// <summary>Applies a small price variation around the reference, clamped to [1, 100].</summary>
    private static int VaryPrice(int reference, Random rng)
    {
        int variation = Math.Max(1, (int)Math.Round(reference * 0.15)); // ~±15%, at least ±1
        int delta = rng.Next(-variation, variation + 1);
        return Math.Clamp(reference + delta, 1, 100);
    }

    /// <summary>Process-stable 32-bit FNV-1a hash of the npc id + mode (String.GetHashCode is randomized).</summary>
    private static int StableSeed(string npcId, TradeMode mode)
    {
        unchecked
        {
            const uint offset = 2166136261;
            const uint prime  = 16777619;
            uint hash = offset;
            foreach (char c in npcId)
            {
                hash ^= c;
                hash *= prime;
            }
            hash ^= (uint)mode;
            hash *= prime;
            return (int)hash;
        }
    }
}
