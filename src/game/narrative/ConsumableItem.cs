using System;
using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Narrative;

/// <summary>
/// How a consumable item is used. Determines which humor queue receives the
/// item's composition, and the label shown on the consume button in the UI.
/// </summary>
public enum ConsumableType
{
    /// <summary>Eaten — humors pushed to the Paunch queue.</summary>
    Food,
    /// <summary>Drunk — humors pushed to the Hepar queue.</summary>
    Drink,
    /// <summary>Inhaled — humors pushed to the Pulmones queue.</summary>
    Inhalant,
}

/// <summary>
/// How rich a consumable's humor composition is — i.e. how many humor instances
/// it pushes into the body when consumed. This reflects how nourishing/substantial
/// the item is, NOT its physical size:
///   • Sparse — barely-food: foraged leaves/roots, tiny berries, scraps, inedible matter.
///   • Modest — light foods: small fruit, mushrooms, light snacks, most drinks.
///   • Hearty — proper food: vegetables, tree fruit, fresh fish, milk.
///   • Rich   — concrete, sustaining food: bread, meat, cheese, eggs, cured goods.
/// </summary>
public enum HumorRichness
{
    Sparse,
    Modest,
    Hearty,
    Rich,
}

/// <summary>
/// Base class for items that can be consumed (eaten, drunk, or inhaled).
///
/// A consumable declares a weighted <see cref="HumorRecipe"/> (which humors it can
/// yield, and how strongly each is represented) plus a <see cref="Richness"/> tier
/// (how many humor instances it yields). When the item is consumed the recipe is
/// sampled <em>with replacement</em> — so the same humor can appear several times,
/// and high-weight humors dominate (e.g. an apple that is mostly Pulp with a little
/// Sugar).
///
/// The composition is generated once (lazily) and cached for the lifetime of the
/// item instance, so two apples picked up separately can differ.
/// </summary>
public abstract class ConsumableItem : Item
{
    // ── Consumption type ─────────────────────────────────────────

    /// <summary>How this item is consumed (Food / Drink / Inhalant).</summary>
    public abstract ConsumableType ConsumableType { get; }

    /// <summary>Organ queue that receives the humors when consumed.</summary>
    public string TargetOrganId => ConsumableType switch
    {
        ConsumableType.Food     => "paunch",
        ConsumableType.Drink    => "hepar",
        ConsumableType.Inhalant => "pulmones",
        _                       => "paunch",
    };

    /// <summary>Label for the consume button in the inventory UI.</summary>
    public string ConsumeButtonLabel => ConsumableType switch
    {
        ConsumableType.Food     => "EAT",
        ConsumableType.Drink    => "DRINK",
        ConsumableType.Inhalant => "INHALE",
        _                       => "CONSUME",
    };

    // ── Hardness ─────────────────────────────────────────────────

    /// <summary>
    /// When true, this item requires functional teeth to consume.
    /// A character whose Teeths organ score is 0 (or the organ is disabled)
    /// cannot eat hard items.
    /// </summary>
    public virtual bool IsHard => false;

    // ── Composition recipe ────────────────────────────────────────

    /// <summary>
    /// A weighted set of humors that may appear in an item's composition.
    /// Weights are relative (they need not sum to any particular total): a recipe of
    /// Pulp(70)/Sugar(30) yields, on average, roughly 70% Pulp and 30% Sugar.
    /// Sampling is with replacement, so a single humor can appear multiple times.
    /// </summary>
    protected sealed class HumorRecipe
    {
        private readonly List<(Func<BodyHumor> factory, double weight)> _entries = new();

        /// <summary>Add humor type <typeparamref name="T"/> with a relative <paramref name="weight"/>.</summary>
        public HumorRecipe Add<T>(double weight = 1.0) where T : BodyHumor, new()
        {
            if (weight <= 0) throw new ArgumentOutOfRangeException(nameof(weight), "Humor weight must be positive.");
            _entries.Add((static () => new T(), weight));
            return this;
        }

        /// <summary>
        /// Draw <paramref name="count"/> humors from this recipe, with replacement,
        /// biased by weight. The result is ordered by descending weight so the most
        /// characteristic humors come first (they are revealed first at low Nose score).
        /// </summary>
        public List<BodyHumor> Sample(int count, Random rng)
        {
            if (_entries.Count == 0 || count <= 0) return new List<BodyHumor>();

            double total = 0;
            foreach (var e in _entries) total += e.weight;

            var picked = new List<(BodyHumor humor, double weight)>(count);
            for (int i = 0; i < count; i++)
            {
                double roll = rng.NextDouble() * total;
                var chosen = _entries[^1]; // fallback guards against float rounding
                foreach (var e in _entries)
                {
                    roll -= e.weight;
                    if (roll <= 0) { chosen = e; break; }
                }
                picked.Add((chosen.factory(), chosen.weight));
            }

            // OrderByDescending is stable, so duplicate humors stay grouped together.
            return picked.OrderByDescending(p => p.weight).Select(p => p.humor).ToList();
        }
    }

    /// <summary>
    /// The weighted humor recipe for this item. Order does not matter (weights do);
    /// put the item's defining humors at the highest weight.
    /// </summary>
    protected abstract HumorRecipe Recipe { get; }

    /// <summary>
    /// How nourishing this item is — controls how many humor instances it yields.
    /// Defaults to <see cref="HumorRichness.Modest"/>; override for sparse foraged
    /// matter or rich, sustaining foods.
    /// </summary>
    protected virtual HumorRichness Richness => HumorRichness.Modest;

    /// <summary>Inclusive (min, max) humor count for this item's richness tier.</summary>
    private (int min, int max) HumorCountRange => Richness switch
    {
        HumorRichness.Sparse => (1, 2),
        HumorRichness.Modest => (2, 3),
        HumorRichness.Hearty => (3, 4),
        HumorRichness.Rich   => (4, 5),
        _                    => (2, 3),
    };

    private int PickHumorCount(Random rng)
    {
        var (min, max) = HumorCountRange;
        return rng.Next(min, max + 1);
    }

    // ── Composition ───────────────────────────────────────────────

    private List<BodyHumor>? _composition;

    /// <summary>
    /// The humor composition of this item instance.
    /// Generated once on first access by sampling <see cref="Recipe"/>.
    /// Order matters for display: the UI reveals humors from front to back based on
    /// Nose score, and the sample is ordered most-characteristic-first.
    /// </summary>
    public List<BodyHumor> Composition => _composition ??= GenerateComposition(GameRng.Stream("item-composition"));

    /// <summary>
    /// Generate the composition for one instance by sampling the recipe.
    /// Virtual so unusual items can customise, but standard items only declare a recipe.
    /// </summary>
    protected virtual List<BodyHumor> GenerateComposition(Random rng) =>
        Recipe.Sample(PickHumorCount(rng), rng);

    // ── Eligibility ───────────────────────────────────────────────

    /// <summary>
    /// Returns true when the character is able to consume this item right now.
    /// Checks teeth for hard items (calls into derived-stat system).
    /// </summary>
    public bool CanConsume(PartyMember member)
    {
        if (IsHard)
        {
            var teethStat = new TeethHardFoodStat();
            int val = teethStat.GetValue(member);
            if (val <= 0) return false;
        }
        return true;
    }

    /// <summary>
    /// Human-readable reason the character cannot consume this item, or null when they can.
    /// </summary>
    public string? GetCannotConsumeReason(PartyMember member)
    {
        if (IsHard)
        {
            var teethStat = new TeethHardFoodStat();
            if (teethStat.GetValue(member) <= 0)
                return "Requires functional teeth";
        }
        return null;
    }

    // ── Consumption ───────────────────────────────────────────────

    /// <summary>
    /// Consume this item: push all humors to the appropriate organ queue,
    /// then remove the item from the member's inventory.
    /// </summary>
    public void Consume(PartyMember member)
    {
        foreach (var humor in Composition)
            member.HumorQueues.ProduceHumor(TargetOrganId, humor);
        member.RemoveItem(this);
    }

    // ── Taxonomy ──────────────────────────────────────────────────

    /// <summary>
    /// Every consumable is <see cref="ItemCategory.Consumable"/>; how it is taken is already
    /// declared by <see cref="ConsumableType"/>, so the subcategory simply mirrors it rather than
    /// duplicating the distinction in a second enum. A <see cref="ConsumableType.Drink"/> is
    /// additionally a liquid — see <see cref="Item.IsLiquid"/>, which infers it from here.
    /// </summary>
    public sealed override ItemCategory Category       => ItemCategory.Consumable;
    public sealed override string       SubcategoryKey => ConsumableType.ToString();
}
