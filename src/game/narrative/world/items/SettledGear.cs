using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Narrative.World.Items;

// ─────────────────────────────────────────────────────────────────────────────
//  What the new trades wear and carry to work: the guard's tabard and the priest's vestment, the
//  vintner's apron and the drover's hat. Their loadouts draw on this file.
// ─────────────────────────────────────────────────────────────────────────────

// ── soldiery ────────────────────────────────────────────────────────────────

public sealed class Tabard : WearableItem
{
    public override string ItemId      => "tabard";
    public override string DisplayName => "Tabard";
    public override string Description => "A sleeveless surcoat in a lord's colours, frayed at the hem";
    public override ItemSize Size => ItemSize.Medium;
    public override WearSlot Slot => WearSlot.Outerwear;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing };
    public override int DefenseDice => 1;
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Military };
    public override int PriceReference => 15;
}

public sealed class Helm : WearableItem
{
    public override string ItemId      => "open_helm";
    public override string DisplayName => "Open Helm";
    public override string Description => "An iron cap with a nasal bar, dented and repaired";
    public override ItemSize Size => ItemSize.Small;
    public override WearSlot Slot => WearSlot.Headgear;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing };
    public override int DefenseDice => 2;
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Military };
    public override int PriceReference => 30;
}

public sealed class ArmingBoots : WearableItem
{
    public override string ItemId      => "arming_boots";
    public override string DisplayName => "Riding Boots";
    public override string Description => "Tall boots of thick leather, the heels shod with iron";
    public override ItemSize Size => ItemSize.Medium;
    public override WearSlot Slot => WearSlot.Footwear;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing };
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Military, SocialCategory.Aristocrat };
    public override int PriceReference => 20;
}

public sealed class Whistle : Item
{
    public override string ItemId      => "whistle";
    public override string DisplayName => "Signal Horn";
    public override string Description => "A small curved horn on a cord, for calling the watch";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 6;
}

public sealed class KeyRing : Item
{
    public override string ItemId      => "key_ring";
    public override string DisplayName => "Ring of Keys";
    public override string Description => "A heavy iron ring of a dozen keys, each one different";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Ironwork };
    public override int PriceReference => 10;
}

// ── the clergy and the schools ──────────────────────────────────────────────

public sealed class Vestment : WearableItem
{
    public override string ItemId      => "vestment";
    public override string DisplayName => "Vestment";
    public override string Description => "A long embroidered robe, its gold thread worn at the cuffs";
    public override ItemSize Size => ItemSize.Medium;
    public override WearSlot Slot => WearSlot.Outerwear;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing };
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Religious };
    public override int PriceReference => 45;
}

public sealed class Habit : WearableItem
{
    public override string ItemId      => "habit";
    public override string DisplayName => "Monk's Habit";
    public override string Description => "A coarse undyed habit with a deep hood and a rope belt";
    public override ItemSize Size => ItemSize.Medium;
    public override WearSlot Slot => WearSlot.Bodywear;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing };
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Religious };
    public override int PriceReference => 8;
}

public sealed class Tonsure : WearableItem
{
    public override string ItemId      => "skullcap";
    public override string DisplayName => "Skullcap";
    public override string Description => "A close black cap worn by the ordained";
    public override ItemSize Size => ItemSize.Small;
    public override WearSlot Slot => WearSlot.Headgear;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing };
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Religious };
    public override int PriceReference => 4;
}

public sealed class ScholarGown : WearableItem
{
    public override string ItemId      => "scholar_gown";
    public override string DisplayName => "Scholar's Gown";
    public override string Description => "A long black gown with open sleeves, ink on one of them";
    public override ItemSize Size => ItemSize.Medium;
    public override WearSlot Slot => WearSlot.Outerwear;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing };
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Bourgeois, SocialCategory.Religious };
    public override int PriceReference => 25;
}

public sealed class Sandals : WearableItem
{
    public override string ItemId      => "sandals";
    public override string DisplayName => "Sandals";
    public override string Description => "Plain leather sandals, the soles worn through to the last layer";
    public override ItemSize Size => ItemSize.Small;
    public override WearSlot Slot => WearSlot.Footwear;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing };
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Religious };
    public override int PriceReference => 4;
}

// ── trade and the court ─────────────────────────────────────────────────────

public sealed class Doublet : WearableItem
{
    public override string ItemId      => "doublet";
    public override string DisplayName => "Doublet";
    public override string Description => "A padded, buttoned doublet of good wool, cut to show the wearer has money";
    public override ItemSize Size => ItemSize.Medium;
    public override WearSlot Slot => WearSlot.Bodywear;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing };
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Bourgeois, SocialCategory.Urban };
    public override int PriceReference => 30;
}

public sealed class Chain : WearableItem
{
    public override string ItemId      => "chain_of_office";
    public override string DisplayName => "Chain of Office";
    public override string Description => "A heavy gilt chain worn across the shoulders";
    public override ItemSize Size => ItemSize.Small;
    public override WearSlot Slot => WearSlot.Neckwear;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing };
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Aristocrat, SocialCategory.Bourgeois };
    public override int PriceReference => 90;
}

public sealed class Mantle : WearableItem
{
    public override string ItemId      => "mantle";
    public override string DisplayName => "Fur-trimmed Mantle";
    public override string Description => "A long mantle of fine wool trimmed with grey fur";
    public override ItemSize Size => ItemSize.Medium;
    public override WearSlot Slot => WearSlot.Outerwear;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing };
    public override int DefenseDice => 1;
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Aristocrat };
    public override int PriceReference => 80;
}

public sealed class Apron : WearableItem
{
    public override string ItemId      => "tavern_apron";
    public override string DisplayName => "Tavern Apron";
    public override string Description => "A long stained apron with a pocket for coin";
    public override ItemSize Size => ItemSize.Medium;
    public override WearSlot Slot => WearSlot.Outerwear;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing };
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Urban };
    public override int PriceReference => 5;
}

public sealed class Sleeves : WearableItem
{
    public override string ItemId      => "oversleeves";
    public override string DisplayName => "Clerk's Oversleeves";
    public override string Description => "Linen oversleeves that keep ink off a better coat";
    public override ItemSize Size => ItemSize.Small;
    public override WearSlot Slot => WearSlot.Handwear;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing };
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Urban, SocialCategory.Bourgeois };
    public override int PriceReference => 3;
}

public sealed class Slops : WearableItem
{
    public override string ItemId      => "slops";
    public override string DisplayName => "Sailor's Slops";
    public override string Description => "Wide canvas breeches tarred at the knee";
    public override ItemSize Size => ItemSize.Medium;
    public override WearSlot Slot => WearSlot.Legwear;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing };
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Urban };
    public override int PriceReference => 6;
}

// ── the land ────────────────────────────────────────────────────────────────

public sealed class VintnerApron : WearableItem
{
    public override string ItemId      => "vintner_apron";
    public override string DisplayName => "Vintner's Apron";
    public override string Description => "A leather apron stained purple to the waist";
    public override ItemSize Size => ItemSize.Medium;
    public override WearSlot Slot => WearSlot.Outerwear;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing };
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Bourgeois };
    public override int PriceReference => 8;
}

public sealed class PlanterHat : WearableItem
{
    public override string ItemId      => "planter_hat";
    public override string DisplayName => "Wide Hat";
    public override string Description => "A broad-brimmed hat of woven palm, the crown sweat-darkened";
    public override ItemSize Size => ItemSize.Small;
    public override WearSlot Slot => WearSlot.Headgear;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing };
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Bourgeois, SocialCategory.Peasant };
    public override int PriceReference => 5;
}

public sealed class DroverHat : WearableItem
{
    public override string ItemId      => "drover_hat";
    public override string DisplayName => "Drover's Hat";
    public override string Description => "A battered felt hat with a wide brim and a cord under the chin";
    public override ItemSize Size => ItemSize.Small;
    public override WearSlot Slot => WearSlot.Headgear;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing };
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Peasant };
    public override int PriceReference => 6;
}

public sealed class Jerkin : WearableItem
{
    public override string ItemId      => "jerkin";
    public override string DisplayName => "Leather Jerkin";
    public override string Description => "A sleeveless jerkin of stiff leather, scuffed pale at the front";
    public override ItemSize Size => ItemSize.Medium;
    public override WearSlot Slot => WearSlot.Bodywear;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing };
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Peasant, SocialCategory.Military };
    public override int PriceReference => 12;
}

public sealed class Hood : WearableItem
{
    public override string ItemId      => "hood";
    public override string DisplayName => "Gravedigger's Hood";
    public override string Description => "A heavy hood of oiled cloth, stiff with old mud";
    public override ItemSize Size => ItemSize.Small;
    public override WearSlot Slot => WearSlot.Headgear;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing };
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Peasant };
    public override int PriceReference => 3;
}

public sealed class GraveSpade : Item
{
    public override string ItemId      => "grave_spade";
    public override string DisplayName => "Grave Spade";
    public override string Description => "A long narrow spade with a worn iron edge and a T-handle";
    public override ItemCategory Category => ItemCategory.Tool;
    public override ItemSize Size => ItemSize.Large;
    public override WeightClass Weight => WeightClass.Light;
    public override int UsageLevel => 4;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Ironwork };
    public override int PriceReference => 12;
}
