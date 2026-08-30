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

/// <summary>
/// A single line in a catalogue: what is sold, and for how much.
///
/// Usually that is one item. A liquid, though, cannot change hands on its own — it needs something
/// to be in — so a drink offer also carries the <see cref="VesselPrototype"/> it comes in, and the
/// two are bought and sold together. This is why a catalogue line reads "bottle of ale" while the
/// inventory afterwards holds a "Bottle" and an "Ale": the trade name describes the transaction,
/// the item names describe what you own.
/// </summary>
public sealed class TradeOffer
{
    /// <summary>Prototype instance from <see cref="ItemRegistry"/> — clone it to create stock.</summary>
    public Item Prototype { get; }

    /// <summary>
    /// The vessel a liquid is traded in, or null for anything else. Always non-null when
    /// <see cref="Prototype"/> is a liquid, since a bare liquid cannot be carried.
    /// </summary>
    public Item? VesselPrototype { get; }

    /// <summary>Unit price in <see cref="Coin"/>, after catalogue variation (1..100).</summary>
    public int UnitPrice { get; }

    public CoinType Coin => Prototype.PriceCoin;

    /// <summary>True when this line delivers two items rather than one.</summary>
    public bool IsBundle => VesselPrototype is not null;

    /// <summary>
    /// What the merchant calls it: "bottle of ale" for a bundle, the item's own trade name
    /// otherwise. Composed here rather than stored on either item, because neither item alone is
    /// what is being sold.
    /// </summary>
    public string DisplayName => VesselPrototype is null
        ? Prototype.TradeName
        : $"{VesselPrototype.TradeName} of {Prototype.TradeName}";

    /// <summary>The same name with its article, for dialogue ("I sell a bottle of ale").</summary>
    public string WithArticle() => VesselPrototype is null
        ? Prototype.WithArticle()
        : $"{VesselPrototype.Article} {DisplayName.ToLowerInvariant()}";

    public TradeOffer(Item prototype, int unitPrice, Item? vesselPrototype = null)
    {
        Prototype       = prototype;
        UnitPrice       = unitPrice;
        VesselPrototype = vesselPrototype;
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

        var offers = picked.Select(proto =>
        {
            // A liquid is sold in something. Pick the vessel from the same seeded rng so the
            // catalogue stays reproducible, and fold its price into the line.
            if (!proto.IsLiquid) return new TradeOffer(proto, VaryPrice(proto.PriceReference, rng));

            var vessel = PickVesselFor(proto, rng);
            if (vessel == null) return null;                       // nothing to pour it into — skip
            int bundle = proto.PriceReference + vessel.PriceReference;
            return new TradeOffer(proto, VaryPrice(bundle, rng), vessel);
        })
        .Where(o => o != null)
        .Select(o => o!)
        .ToList();

        return new NpcTradeCatalog(mode, tag, offers);
    }

    /// <summary>
    /// A vessel that will hold <paramref name="liquid"/>, chosen deterministically. Ordered by id
    /// first so the choice depends only on the seed, never on reflection order.
    /// </summary>
    private static Item? PickVesselFor(Item liquid, Random rng)
    {
        var vessels = ItemRegistry.Instance.All
            .Where(i => i is IContainer { Kind: ContainerKind.Vessel } c && c.CanContain(liquid))
            .OrderBy(i => i.ItemId)
            .ToList();

        return vessels.Count == 0 ? null : vessels[rng.Next(vessels.Count)];
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
