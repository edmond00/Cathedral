using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Base class for items that can be acquired through successful actions.
/// Items have specific names but should not include qualifiers (e.g., "Trout" not "Small Fish").
/// Implements IObservation as self-referential: an Item IS its own single observation.
/// </summary>
public abstract class Item
{
    /// <summary>Human-readable name, shown in the pack and in outcome chips.</summary>
    public abstract string DisplayName { get; }

    /// <summary>Unique identifier for this item.</summary>
    public abstract string ItemId { get; }

    /// <summary>Short description of the item for player reference.</summary>
    public abstract string Description { get; }

    // ── Physical properties ───────────────────────────────────────

    /// <summary>
    /// How much carrying this burdens its owner. A label rather than a mass — see
    /// <see cref="WeightClass"/> for why. Summed across everything held and checked against the
    /// <c>maximum_weight</c> derived stat before anything may be picked up.
    /// </summary>
    public virtual WeightClass Weight => WeightClass.Light;

    /// <summary>This item's carrying cost, in the same unit as <c>maximum_weight</c>.</summary>
    public int WeightPoints => (int)Weight;

    /// <summary>Physical size — determines inventory slot count (Small=3, Medium=5, Large=7).</summary>
    public virtual ItemSize Size => ItemSize.Small;

    /// <summary>Number of inventory slots this item occupies.</summary>
    public int SlotCount => (int)Size;

    // ── Taxonomy ──────────────────────────────────────────────────

    /// <summary>
    /// What this item fundamentally is. Decides which systems consider it at all: only
    /// <see cref="ItemCategory.Weapon"/> grants a fighting medium, only Weapon and
    /// <see cref="ItemCategory.Tool"/> may be combined with a narration action, only
    /// <see cref="ItemCategory.Wearing"/> can protect or flatter.
    /// </summary>
    public virtual ItemCategory Category => ItemCategory.Other;

    /// <summary>
    /// The subcategory, flattened to a string for display and audit grouping. Each category names
    /// its own strongly-typed subcategory (<see cref="WearSlot"/>, <see cref="ConsumableType"/>,
    /// <see cref="ContainerKind"/>, or a weapon category id) — this is the common denominator, and
    /// nothing should parse it back.
    /// </summary>
    public virtual string SubcategoryKey => "";

    /// <summary>
    /// Whether this item pours. Inferred, never declared: a liquid is exactly a drinkable
    /// consumable. Liquids may only be carried inside a <see cref="ContainerKind.Vessel"/> —
    /// never loose in an anchor, never in a pack.
    /// </summary>
    public bool IsLiquid => this is ConsumableItem { ConsumableType: ConsumableType.Drink };

    // ── Trade ─────────────────────────────────────────────────────

    /// <summary>
    /// Trade category tags (see <see cref="ItemTag"/>). Empty means the item is not part of
    /// any NPC's buy/sell catalogue. Deliberately independent of <see cref="Category"/>: an item
    /// has one category but any number of tags, and the tags exist only to stock shelves.
    /// </summary>
    public virtual List<ItemTag> Tags => new();

    /// <summary>
    /// The name a merchant uses, which is not always the name the inventory uses. A catalogue line
    /// reads "bottle of ale" while the two items it delivers are a "Bottle" and an "Ale" — the
    /// bundle composes its own label from these, so the item names stay honest about what you
    /// actually receive. Defaults to <see cref="DisplayName"/>.
    /// </summary>
    public virtual string TradeName => DisplayName;

    /// <summary>The single coin denomination this item is priced in. Denominations are never mixed.</summary>
    public virtual CoinType PriceCoin => CoinType.Copper;

    /// <summary>
    /// Reference price of one unit, expressed in <see cref="PriceCoin"/>. Must be in the
    /// range 1..90 — NPC catalogues apply a small variation on top, clamped to [1, 100].
    /// </summary>
    public virtual int PriceReference => 1;

    /// <summary>
    /// Extra lines shown in the inventory info panel, <em>below</em> the description.
    /// Empty by default: the panel already prints <see cref="Description"/> itself, so returning
    /// it here again is what used to print every description twice. Override only to add detail
    /// the description does not already carry.
    /// </summary>
    public virtual string[] Info => System.Array.Empty<string>();

    /// <summary>
    /// Preferred equipment anchor. When null, the item auto-fills any free compatible slot
    /// or is placed inside an equipped container.
    /// </summary>
    public virtual EquipmentAnchor? PreferredAnchor => null;

    /// <summary>
    /// Usage level (1–10): adds bonus dice to the action roll when this item is combined
    /// with an action. Higher levels represent more specialised or potent tools.
    /// </summary>
    public virtual int UsageLevel => 1;

    /// <summary>
    /// The acts this item is <b>made for</b> — and, when non-empty, <b>the only acts it may be used
    /// for at all</b>. Empty (the default) means a general implement, judged on its merits against
    /// whatever it is offered to.
    ///
    /// <para>The declaration cuts both ways, and the second edge is the sharper one:</para>
    /// <list type="bullet">
    /// <item><b>Accepted here, without argument.</b> A combination against a listed verb skips the
    ///   item-use critic entirely — the only thing that can still refuse it is a body of
    ///   <see cref="ToolProficiency.None"/>, which can use nothing for anything. This is also the
    ///   <b>only</b> way past <see cref="Scene.Verbs.ToolUsage.Excluded"/>: a glass ground to
    ///   magnify is why EXAMINE can be excluded as a category and still admit the one implement
    ///   that genuinely bears on it.</item>
    /// <item><b>Refused everywhere else, without argument.</b> A thing made for one work is not a
    ///   general tool that happens to be good at it. Lenses cannot break ore out of a seam, and
    ///   asking a critic whether they might is a request spent to be told what the declaration
    ///   already said.</item>
    /// </list>
    ///
    /// <para>So this is for <b>single-purpose</b> implements only. A rope, a knife, an axe are
    /// general: they serve a dozen acts nobody has enumerated, and declaring three of them here
    /// would forbid the other nine. When in doubt, leave it empty and let the critic judge.</para>
    ///
    /// <para>Deliberately <b>not shown in the inventory panel</b>. An implement that announced the
    /// acts it was good for would turn the narration phase into a list to be read off, where the
    /// whole of the phase is working out what to try. A player learns this by trying it.</para>
    ///
    /// <para>A verb's own <c>ReferenceToolIds</c> is a different declaration, made from the verb's
    /// side, and carries no exclusivity: a knife is what CUT is done with, and is still an ordinary
    /// candidate for everything else.</para>
    /// </summary>
    public virtual IReadOnlyList<string> MadeForVerbIds => System.Array.Empty<string>();


    /// <summary>
    /// The article/determiner that precedes this item's lowercased name when it is mentioned
    /// mid-sentence (e.g. "a", "an", "some"). The default is derived from <see cref="DisplayName"/>:
    /// "some" for plurals (ending in -s but not -ss), "an" before a vowel, otherwise "a".
    /// Override for mass/uncountable nouns ("some moss", "some bread") or irregular phonetics
    /// ("an hour"). Keeping the choice on the item makes it discoverable when adding new items,
    /// rather than living in a central list that is easy to miss.
    /// </summary>
    public virtual string Article
    {
        get
        {
            string lower = DisplayName.ToLowerInvariant();
            string[] words = lower.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string last = words.Length > 0 ? words[^1] : lower;

            if (last.Length > 1 && last.EndsWith('s') && !last.EndsWith("ss")) return "some";
            if (lower.Length > 0 && "aeiou".Contains(lower[0])) return "an";
            return "a";
        }
    }

    /// <summary>
    /// Returns the item name with its <see cref="Article"/>, all lowercase.
    /// e.g. "a wool cloak", "an egg", "some grain", "some moss", "some leather boots".
    /// </summary>
    public string WithArticle() => $"{Article} {DisplayName.ToLowerInvariant()}";

    /// <summary>Returns the description with its first letter lowercased.</summary>
    public string DescriptionLower() =>
        Description.Length == 0 ? Description : char.ToLowerInvariant(Description[0]) + Description[1..];

}
