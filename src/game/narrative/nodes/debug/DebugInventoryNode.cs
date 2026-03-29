using System;
using System.Collections.Generic;
using Cathedral.Fight;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Narrative.Nodes.Debug;

/// <summary>
/// Temporary debug node that exists purely to host well-defined test items
/// for exercising the inventory / equipment system.
/// Not part of the narrative graph — items are instantiated directly in tests
/// and in Protagonist.InitializeTestEquipment().
/// </summary>
public class DebugInventoryNode : NarrationNode
{
    public override string NodeId              => "debug_inventory";
    public override string ContextDescription  => "debugging the inventory system";
    public override string TransitionDescription => "enter the debug inventory";
    public override bool   IsEntryNode         => false;
    public override List<KeywordInContext> NodeKeywordsInContext  => new();
    public override string GenerateNeutralDescription(int locationId = 0) => "debug inventory node";

    // ═══════════════════════════════════════════════════════════════
    // Containers
    // ═══════════════════════════════════════════════════════════════

    /// <summary>A worn leather backpack with many pockets.</summary>
    public sealed class TestBackpack : ContainerItem
    {
        public override string ItemId        => "debug_backpack";
        public override string DisplayName   => "Leather Backpack";
        public override string Description   => "A worn traveller's backpack stitched from thick cowhide. Multiple compartments keep small goods sorted.";
        public override float  Weight        => 1.2f;
        public override ItemSize Size        => ItemSize.Large;
        public override int    ContentSlots  => 20;
        public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.BeltGear;
        public override List<ItemType> Types => new() { ItemType.BeltGear };
        public override string[] Info => new[]
        {
            "Worn leather, still sturdy.",
            "Faint smell of pine resin.",
            "The straps have been let out to the last hole.",
        };
    }

    /// <summary>A small belt pouch — general container for testing recursion.</summary>
    public sealed class LeatherPouch : ContainerItem
    {
        public override string ItemId        => "debug_pouch";
        public override string DisplayName   => "Leather Pouch";
        public override string Description   => "A drawstring pouch of soft leather, sized to hang from a belt or tuck into a pack.";
        public override float  Weight        => 0.4f;
        public override ItemSize Size        => ItemSize.Small;
        public override int    ContentSlots  => 9;
        public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.BeltGear;
        public override List<ItemType> Types => new() { ItemType.BeltGear };
        public override string[] Info => new[]
        {
            "The drawstring is fraying.",
            "Smells faintly of dried herbs.",
        };
    }

    /// <summary>A small glass bottle that holds liquid.</summary>
    public sealed class GlassFlask : BottleItem
    {
        public override string ItemId        => "debug_glass_flask";
        public override string DisplayName   => "Glass Flask";
        public override string Description   => "A clear glass flask stoppered with a cork. Holds a single type of liquid.";
        public override float  Weight        => 0.3f;
        public override ItemSize Size        => ItemSize.Small;
        public override int    ContentSlots  => 9;   // fits up to 3 small liquids
        public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.LeftHold;
        public override string[] Info => new[]
        {
            "The glass is slightly green-tinted.",
            "A hairline crack runs up one side — cosmetic only.",
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // Clothing & Armour
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Hard leather cap.</summary>
    public override List<Item> GetItems() => new() { new LeatherCap(), new WoolenCloak(), new LinenShirt(), new WoolSocks(), new LeatherBoots(), new IronSword(), new FightKnife(), new IronDagger(), new SpringWater(), new RedWine() };

    public sealed class LeatherCap : Item
    {
        public override string ItemId        => "debug_leather_cap";
        public override string DisplayName   => "Leather Cap";
        public override string Description   => "A tight-fitting cap of boiled leather. Minimal protection, maximum discretion.";
        public override float  Weight        => 0.4f;
        public override ItemSize Size        => ItemSize.Small;
        public override List<ItemType> Types => new() { ItemType.Headgear };
        public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.Headgear;
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a tight-fitting boiled leather <hat> on the head"), KeywordInContext.Parse("the smell of old <leather> from the cap lining"), KeywordInContext.Parse("a plain protective <cap> of hardened hide") };
        public override string[] Info => new[]
        {
            "Smells of tallow and old sweat.",
            "Defence: 1",
        };
    }

    /// <summary>Heavy woollen cloak.</summary>
    public sealed class WoolenCloak : Item
    {
        public override string ItemId        => "debug_woolen_cloak";
        public override string DisplayName   => "Woolen Cloak";
        public override string Description   => "A broad cloak of undyed wool. Heavy in rain, warm in wind.";
        public override float  Weight        => 2.1f;
        public override ItemSize Size        => ItemSize.Large;
        public override List<ItemType> Types => new() { ItemType.Outerwear };
        public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.Outerwear;
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("the heavy drape of an undyed wool <cloak>"), KeywordInContext.Parse("the thick coarse <wool> that holds warmth even damp"), KeywordInContext.Parse("a broad piece of woven <cloth> pinned at the shoulder") };
        public override string[] Info => new[]
        {
            "Warmth: high",
            "Conspicuousness: low",
        };
    }

    /// <summary>Light linen shirt.</summary>
    public sealed class LinenShirt : Item
    {
        public override string ItemId        => "debug_linen_shirt";
        public override string DisplayName   => "Linen Shirt";
        public override string Description   => "A loose-fitting shirt of pale linen, mended at both elbows.";
        public override float  Weight        => 0.3f;
        public override ItemSize Size        => ItemSize.Medium;
        public override List<ItemType> Types => new() { ItemType.Bodywear };
        public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.Bodywear;
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a loose pale <shirt> mended at the elbows"), KeywordInContext.Parse("the cool breathable <linen> weave against the skin"), KeywordInContext.Parse("a plain undyed <cloth> garment worn beneath the outer layers") };
        public override string[] Info => new[]
        {
            "Breathable in summer heat.",
        };
    }

    /// <summary>Coarse wool stockings.</summary>
    public sealed class WoolSocks : Item
    {
        public override string ItemId        => "debug_wool_socks";
        public override string DisplayName   => "Wool Socks";
        public override string Description   => "Heavy knitted socks that itch but keep the feet dry.";
        public override float  Weight        => 0.15f;
        public override ItemSize Size        => ItemSize.Small;
        public override List<ItemType> Types => new() { ItemType.Legwear };
        public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.Legwear;
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a pair of heavy knitted wool <socks> that itch"), KeywordInContext.Parse("the thick coarse <wool> yarn knitted into stockings"), KeywordInContext.Parse("the warmth kept at the <feet> by the heavy knit") };
        public override string[] Info => new[]
        {
            "A small hole near the left heel.",
        };
    }

    /// <summary>Sturdy leather boots.</summary>
    public sealed class LeatherBoots : Item
    {
        public override string ItemId        => "debug_leather_boots";
        public override string DisplayName   => "Leather Boots";
        public override string Description   => "Ankle-high boots resoled twice. Reliable on rough ground.";
        public override float  Weight        => 1.0f;
        public override ItemSize Size        => ItemSize.Small;
        public override List<ItemType> Types => new() { ItemType.Footwear };
        public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.Footwear;
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a pair of ankle-high <boots> resoled twice over"), KeywordInContext.Parse("the worn <leather> upper of a reliable walking boot"), KeywordInContext.Parse("the right piece of <footwear> for rough and uneven ground") };
        public override string[] Info => new[]
        {
            "Defence: 1",
            "The right boot squeaks on wet stone.",
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // Weapons
    // ═══════════════════════════════════════════════════════════════

    /// <summary>An iron sword — straight double-edged blade. Weapon medium for blade fighting skills.</summary>
    public sealed class IronSword : Item, IWeaponItem
    {
        public override string ItemId       => "fight_iron_sword";
        public override string DisplayName  => "Iron Sword";
        public override string Description  => "A serviceable iron blade, straight and double-edged.";
        public override float  Weight       => 1.4f;
        public override ItemSize Size       => ItemSize.Medium;
        public override List<ItemType> Types => new() { ItemType.Other };
        public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.RightHold;
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a serviceable straight double-edged <sword> at the hip"), KeywordInContext.Parse("the <blade> catching light along its double edge"), KeywordInContext.Parse("the cold grey weight of <iron> in the hand") };
        public override string[] Info => new[]
        {
            "Damage: 1d6+2",
            "Weight: 1.4 kg",
            "Usable as weapon medium for blade fighting skills.",
        };
    }

    /// <summary>A fight knife — short, fast, lightweight. Weapon medium for blade fighting skills.</summary>
    public sealed class FightKnife : Item, IWeaponItem
    {
        public override string ItemId       => "fight_knife";
        public override string DisplayName  => "Fight Knife";
        public override string Description  => "A short sturdy blade designed for close quarters.";
        public override float  Weight       => 0.35f;
        public override ItemSize Size       => ItemSize.Small;
        public override List<ItemType> Types => new() { ItemType.Other };
        public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.RightHold;
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a short sturdy fighting <knife> for close quarters"), KeywordInContext.Parse("the short thick <blade> designed for stabbing work"), KeywordInContext.Parse("a keen <edge> honed for use at close range") };
        public override string[] Info => new[]
        {
            "Damage: 1d4+2",
            "Weight: 0.35 kg",
            "Usable as weapon medium for blade fighting skills.",
        };
    }

    /// <summary>Short iron dagger.</summary>
    public sealed class IronDagger : Item
    {
        public override string ItemId        => "debug_iron_dagger";
        public override string DisplayName   => "Iron Dagger";
        public override string Description   => "A plain double-edged dagger with a bone handle. Functional and forgettable.";
        public override float  Weight        => 0.6f;
        public override ItemSize Size        => ItemSize.Small;
        public override List<ItemType> Types => new() { ItemType.Other };
        public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.RightHold;
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a short double-edged <blade> with a bone handle"), KeywordInContext.Parse("a plain functional <dagger> chipped but still serviceable"), KeywordInContext.Parse("the cold weight of <iron> in a forgettable but reliable weapon") };
        public override string[] Info => new[]
        {
            "Damage: 1d4+1",
            "Edge is chipped but serviceable.",
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // Liquids
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Clear spring water — liquid type.</summary>
    public sealed class SpringWater : Item
    {
        public override string ItemId        => "debug_spring_water";
        public override string DisplayName   => "Spring Water";
        public override string Description   => "Cold, clear water drawn from a mountain spring.";
        public override float  Weight        => 0.5f;
        public override ItemSize Size        => ItemSize.Small;
        public override List<ItemType> Types => new() { ItemType.Liquid };
        public override EquipmentAnchor? PreferredAnchor => null;
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("the cold clear <water> drawn from a mountain spring"), KeywordInContext.Parse("a clean odourless <liquid> good for drinking"), KeywordInContext.Parse("a sense of <purity> in the taste of uncontaminated spring water") };
        public override string[] Info => new[]
        {
            "Clean and refreshing.",
            "Cannot be placed in bare hands.",
        };
    }

    /// <summary>Red wine — liquid type.</summary>
    public sealed class RedWine : Item
    {
        public override string ItemId        => "debug_red_wine";
        public override string DisplayName   => "Red Wine";
        public override string Description   => "A rough local wine, dark and sharp. Keeps the cold out.";
        public override float  Weight        => 0.5f;
        public override ItemSize Size        => ItemSize.Small;
        public override List<ItemType> Types => new() { ItemType.Liquid };
        public override EquipmentAnchor? PreferredAnchor => null;
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a rough dark local <wine> sharp enough to keep the cold out"), KeywordInContext.Parse("a faintly astringent red <liquid> that stains the teeth"), KeywordInContext.Parse("the sour tang of <fermentation> in the rough local vintage") };
        public override string[] Info => new[]
        {
            "Faintly astringent.",
            "Cannot be mixed with other liquids in the same bottle.",
        };
    }
}
