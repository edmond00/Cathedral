using System;
using System.Collections.Generic;
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
    public override List<string> NodeKeywords  => new();
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
    public sealed class LeatherCap : Item
    {
        public override string ItemId        => "debug_leather_cap";
        public override string DisplayName   => "Leather Cap";
        public override string Description   => "A tight-fitting cap of boiled leather. Minimal protection, maximum discretion.";
        public override float  Weight        => 0.4f;
        public override ItemSize Size        => ItemSize.Small;
        public override List<ItemType> Types => new() { ItemType.Headgear };
        public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.Headgear;
        public override List<string> OutcomeKeywords => new() { "hat", "leather", "cap" };
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
        public override List<string> OutcomeKeywords => new() { "cloak", "wool", "cloth" };
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
        public override List<string> OutcomeKeywords => new() { "shirt", "linen", "cloth" };
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
        public override List<string> OutcomeKeywords => new() { "socks", "wool", "feet" };
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
        public override List<string> OutcomeKeywords => new() { "boots", "leather", "footwear" };
        public override string[] Info => new[]
        {
            "Defence: 1",
            "The right boot squeaks on wet stone.",
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // Weapons
    // ═══════════════════════════════════════════════════════════════

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
        public override List<string> OutcomeKeywords => new() { "blade", "dagger", "iron" };
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
        public override List<string> OutcomeKeywords => new() { "water", "liquid", "clear" };
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
        public override List<string> OutcomeKeywords => new() { "wine", "liquid", "red" };
        public override string[] Info => new[]
        {
            "Faintly astringent.",
            "Cannot be mixed with other liquids in the same bottle.",
        };
    }
}
