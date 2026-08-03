using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

/// <summary>
/// What a caught tiny creature is worth keeping for. These are <b>specimens</b>, not carcasses: a
/// creature caught alive and intact, or the one useful part of one. Crushing yields none of them,
/// which is the whole distinction between the two verbs.
///
/// <para>Deliberately light and cheap. Nobody gets rich on beetles; the point of catching one is
/// that a herbalist, a fisherman or a curious person has a use for it, and that closing your hand on
/// something without harming it is a different skill from stepping on it.</para>
/// </summary>
public sealed class ButterflyWing : Item
{
    public override ItemCategory Category => ItemCategory.Crafting;
    public override string ItemId      => "butterfly_wing";
    public override string DisplayName => "Butterfly Wing";
    public override string Description => "A pair of wings scaled in powder-fine colour, light enough to lift on a breath";
    public override WeightClass Weight => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 2;
}

public sealed class BeetleShell : Item
{
    public override ItemCategory Category => ItemCategory.Crafting;
    public override string ItemId      => "beetle_shell";
    public override string DisplayName => "Beetle Shell";
    public override string Description => "A hard wing-case with an oil-slick sheen to it, hollow and surprisingly tough";
    public override WeightClass Weight => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 2;
}

public sealed class SnailShell : Item
{
    public override ItemCategory Category => ItemCategory.Crafting;
    public override string ItemId      => "snail_shell";
    public override string DisplayName => "Snail Shell";
    public override string Description => "A banded spiral shell, chalky and empty, worn thin at the lip";
    public override WeightClass Weight => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 1;
}

/// <summary>Bait. The reason a fisherman turns over stones.</summary>
public sealed class LiveGrub : Item
{
    public override ItemCategory Category => ItemCategory.Crafting;
    public override string ItemId      => "live_grub";
    public override string DisplayName => "Live Grub";
    public override string Description => "A pale soft grub that will not stop moving, kept in a twist of leaf";
    public override WeightClass Weight => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Forage };
    public override int PriceReference => 1;
}

public sealed class SpiderSilk : Item
{
    public override ItemCategory Category => ItemCategory.Crafting;
    public override string ItemId      => "spider_silk";
    public override string DisplayName => "Spider Silk";
    public override string Description => "A wound skein of web, stronger along its length than anything of that thinness has a right to be";
    public override WeightClass Weight => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Textile };
    public override int PriceReference => 4;
}

public sealed class MouseSkin : Item
{
    public override ItemCategory Category => ItemCategory.Crafting;
    public override string ItemId      => "mouse_skin";
    public override string DisplayName => "Mouse Skin";
    public override string Description => "A scrap of soft grey hide barely the size of a palm, good for trimming and little else";
    public override WeightClass Weight => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Pelt };
    public override int PriceReference => 2;
}

public sealed class LizardTail : Item
{
    public override ItemCategory Category => ItemCategory.Crafting;
    public override string ItemId      => "lizard_tail";
    public override string DisplayName => "Lizard Tail";
    public override string Description => "A shed tail, still twitching when it came away, dry and scaled";
    public override WeightClass Weight => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Herb };
    public override int PriceReference => 3;
}

public sealed class Beeswax : Item
{
    public override ItemCategory Category => ItemCategory.Crafting;
    public override string ItemId      => "beeswax";
    public override string DisplayName => "Beeswax";
    public override string Description => "A knuckle of comb wax, warm-smelling and soft enough to take a thumbprint";
    public override WeightClass Weight => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 5;
}
