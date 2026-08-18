using System.Collections.Generic;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Narrative.Items;

// Garments for the slots the wardrobe never covered: nothing at all was worn under the clothes,
// and the hands, eyes and throat had one piece between them. These also carry most of the real
// armour in the game — ordinary dress sits at 0–1 defence dice by design, so anything that
// genuinely turns a blade lives here.
//
// A note on the numbers: armour is uncapped in code, so a section's ceiling is whatever the best
// garment in each contributing slot adds up to. The trunk draws from three slots at once
// (outerwear + bodywear + underwear), which is why the gambeson stops at 2 — pushing it to 3 would
// put the trunk at 4 and start making a dressed torso genuinely hard to hit. `--item-audit` prints
// the ceiling per section; keep an eye on it when adding anything here.

// ── Underwear ─────────────────────────────────────────────────────────────────

public sealed class LinenBraies : WearableItem
{
    public override string ItemId      => "linen_braies";
    public override string DisplayName => "Linen Braies";
    public override string Description => "Loose linen underbreeches, tied at the waist with a drawstring";
    public override WearSlot Slot      => WearSlot.Underwear;
    public override WeightClass Weight => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 6;

    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Peasant };
}

public sealed class WoolUndershirt : WearableItem
{
    public override string ItemId      => "wool_undershirt";
    public override string DisplayName => "Wool Undershirt";
    public override string Description => "A close-fitting undershirt of soft-combed wool, worn thin at the shoulders";
    public override WearSlot Slot      => WearSlot.Underwear;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 9;

    public override int DefenseDice => 1;
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Peasant };
}

/// <summary>
/// Layered and quilted linen — the one thing here that was made to be hit. Worn under mail by
/// those who own mail, and instead of it by those who do not.
/// </summary>
public sealed class PaddedGambeson : WearableItem
{
    public override string ItemId      => "padded_gambeson";
    public override string DisplayName => "Padded Gambeson";
    public override string Description => "A thick quilted coat of layered linen, stitched in close vertical channels and stained at the collar";
    // Medium fills the six-slot Underwear anchor exactly — a gambeson *is* your whole underlayer,
    // and nothing else fits beneath it. Large would exceed the anchor and could never be worn.
    public override ItemSize Size      => ItemSize.Medium;
    public override WearSlot Slot      => WearSlot.Underwear;
    public override WeightClass Weight => WeightClass.Medium;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing };
    public override int PriceReference => 40;

    public override int DefenseDice => 2;
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Military };
}

// ── Handwear ──────────────────────────────────────────────────────────────────

public sealed class MailMittens : WearableItem
{
    public override string ItemId      => "mail_mittens";
    public override string DisplayName => "Mail Mittens";
    public override string Description => "A pair of riveted mail mittens backed with leather, heavy and cold to put on";
    public override WearSlot Slot      => WearSlot.Handwear;
    public override WeightClass Weight => WeightClass.Medium;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing, ItemTag.Ironwork };
    public override int PriceReference => 50;

    public override int DefenseDice => 2;
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Military };
}

public sealed class WorkMitts : WearableItem
{
    public override string ItemId      => "work_mitts";
    public override string DisplayName => "Work Mitts";
    public override string Description => "Fingerless mitts of doubled sacking, bound at the wrist with twine";
    public override WearSlot Slot      => WearSlot.Handwear;
    public override WeightClass Weight => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing, ItemTag.Textile };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 3;

    // Sacking and twine — not bought, made. Nobody above the poorest mistakes them for anything else.
    public override IReadOnlyList<SocialCategory> DialogueAppeal =>
        new[] { SocialCategory.Peasant, SocialCategory.Pauper };
}

// ── Eyewear ───────────────────────────────────────────────────────────────────

/// <summary>
/// Rare and expensive, and unmistakably the property of someone whose work is reading — which is
/// exactly why they open doors with clerks and clergy and none at all in a field.
/// </summary>
public sealed class ReadingLenses : WearableItem
{
    public override string ItemId      => "reading_lenses";
    public override string DisplayName => "Reading Lenses";
    public override string Description => "Two ground glass discs in a riveted bone frame, folded shut and carried in a scrap of felt";
    public override WearSlot Slot      => WearSlot.Eyewear;
    public override WeightClass Weight => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 60;

    public override IReadOnlyList<SocialCategory> DialogueAppeal =>
        new[] { SocialCategory.Bourgeois, SocialCategory.Religious, SocialCategory.Aristocrat };

    /// <summary>
    /// Ground glass raised to the eye is the one implement that bears on looking closely, and so the
    /// case the whole exception mechanism was built for: EXAMINE is excluded as a category — no
    /// implement sharpens an eye — and these are the exception that proves it rather than the
    /// counter-example that would sink it.
    ///
    /// <para>They are a garment by category, which is why <c>GetCombinableItems</c> admits anything
    /// declaring an exception regardless of category. Nothing about them is shown in the pack: a
    /// player finds this by holding them up to something.</para>
    /// </summary>
    public override IReadOnlyList<string> MadeForVerbIds => new[] { "examine" };

    /// <summary>Ground lenses, not a jeweller's loupe: enough to see by, not enough to see far.</summary>
    public override int UsageLevel => 3;
}

public sealed class LinenBlindfold : WearableItem
{
    public override string ItemId      => "linen_blindfold";
    public override string DisplayName => "Linen Blindfold";
    public override string Description => "A folded strip of bleached linen, worn by those under a vow not to look upon something";
    public override WearSlot Slot      => WearSlot.Eyewear;
    public override WeightClass Weight => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 2;

    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Religious };
}

// ── Headgear ──────────────────────────────────────────────────────────────────

/// <summary>
/// The only hard thing anyone wears on their head. Without it the encephalon section has no
/// armour available at all, which would leave head protection a rule with nothing to trigger it.
/// </summary>
public sealed class IronKettleHelm : WearableItem
{
    public override string ItemId      => "iron_kettle_helm";
    public override string DisplayName => "Iron Kettle Helm";
    public override string Description => "A wide-brimmed iron helm beaten from a single plate, lined with rag and rust-pitted along the rim";
    // Headgear holds three slots, so every hat is Small whatever it is made of.
    public override ItemSize Size      => ItemSize.Small;
    public override WearSlot Slot      => WearSlot.Headgear;
    public override WeightClass Weight => WeightClass.Medium;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing, ItemTag.Ironwork };
    public override int PriceReference => 50;

    public override int DefenseDice => 2;
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Military };
}

// ── Neckwear ──────────────────────────────────────────────────────────────────

public sealed class Gorget : WearableItem
{
    public override string ItemId      => "gorget";
    public override string DisplayName => "Gorget";
    public override string Description => "A collar of overlapping steel plates on a leather backing, buckled at the nape";
    public override WearSlot Slot      => WearSlot.Neckwear;
    public override WeightClass Weight => WeightClass.Medium;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing, ItemTag.Ironwork };
    public override int PriceReference => 60;

    public override int DefenseDice => 2;
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Military };
}
