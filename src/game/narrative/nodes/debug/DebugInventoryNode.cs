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
    public override string GenerateNeutralDescription(int locationId = 0) => "debug inventory node";

    // ═══════════════════════════════════════════════════════════════
    // Containers
    // ═══════════════════════════════════════════════════════════════

    /// <summary>A worn leather backpack with many pockets.</summary>
    public sealed class TestBackpack : WearableContainerItem, IDebugItem
    {
        public override string ItemId        => "debug_backpack";
        public override string DisplayName   => "Leather Backpack";
        public override string Description   => "A worn traveller's backpack stitched from thick cowhide. Multiple compartments keep small goods sorted.";
        public override WeightClass  Weight        => WeightClass.Medium;
        public override ItemSize Size        => ItemSize.Large;
        public override WearSlot Slot        => WearSlot.Outerwear;
        public override ContainerKind Kind   => ContainerKind.Storage;
        public override int    ContentSlots  => 20;
        public override string[] Info => new[]
        {
            "Worn leather, still sturdy.",
            "Faint smell of pine resin.",
            "The straps have been let out to the last hole.",
        };
    }

    /// <summary>A small belt pouch — general container for testing recursion.</summary>
    public sealed class LeatherPouch : WearableContainerItem, IDebugItem
    {
        public override string ItemId        => "debug_pouch";
        public override string DisplayName   => "Leather Pouch";
        public override string Description   => "A drawstring pouch of soft leather, sized to hang from a belt or tuck into a pack.";
        public override WeightClass  Weight        => WeightClass.Light;
        public override ItemSize Size        => ItemSize.Small;
        public override WearSlot Slot        => WearSlot.BeltGear;
        public override ContainerKind Kind   => ContainerKind.Storage;
        public override int    ContentSlots  => 9;
        public override string[] Info => new[]
        {
            "The drawstring is fraying.",
            "Smells faintly of dried herbs.",
        };
    }

    /// <summary>A small glass bottle that holds liquid.</summary>
    public sealed class GlassFlask : ContainerItem, IDebugItem
    {
        public override string ItemId        => "debug_glass_flask";
        public override string DisplayName   => "Glass Flask";
        public override string Description   => "A clear glass flask stoppered with a cork. Holds a single type of liquid.";
        public override WeightClass  Weight        => WeightClass.Light;
        public override ItemSize Size        => ItemSize.Small;
        public override ContainerKind Kind   => ContainerKind.Vessel;
        public override int    ContentSlots  => 9;   // fits up to 3 small liquids
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
    public override List<Item> GetItems() => new List<Item> { new LeatherCap(), new WoolenCloak(), new LinenShirt(), new WoolSocks(), new LeatherBoots(), new IronSword(), new FightKnife(), new IronDagger(), new SpringWater(), new RedWine() };

    public sealed class LeatherCap : WearableItem, IDebugItem
    {
        public override string ItemId        => "debug_leather_cap";
        public override string DisplayName   => "Leather Cap";
        public override string Description   => "A tight-fitting cap of boiled leather. Minimal protection, maximum discretion.";
        public override WeightClass  Weight        => WeightClass.Light;
        public override ItemSize Size        => ItemSize.Small;
        public override WearSlot Slot        => WearSlot.Headgear;
        public override int DefenseDice      => 1;
        public override string[] Info => new[]
        {
            "Smells of tallow and old sweat.",
        };
    }

    /// <summary>Heavy woollen cloak.</summary>
    public sealed class WoolenCloak : WearableItem, IDebugItem
    {
        public override string ItemId        => "debug_woolen_cloak";
        public override string DisplayName   => "Woolen Cloak";
        public override string Description   => "A broad cloak of undyed wool. Heavy in rain, warm in wind.";
        public override WeightClass  Weight        => WeightClass.Heavy;
        public override ItemSize Size        => ItemSize.Large;
        public override WearSlot Slot        => WearSlot.Outerwear;
        public override int DefenseDice      => 1;
        public override string[] Info => new[]
        {
            "Warmth: high",
            "Conspicuousness: low",
        };
    }

    /// <summary>Light linen shirt.</summary>
    public sealed class LinenShirt : WearableItem, IDebugItem
    {
        public override string ItemId        => "debug_linen_shirt";
        public override string DisplayName   => "Linen Shirt";
        public override string Description   => "A loose-fitting shirt of pale linen, mended at both elbows.";
        public override WeightClass  Weight        => WeightClass.Light;
        public override ItemSize Size        => ItemSize.Medium;
        public override WearSlot Slot        => WearSlot.Bodywear;
        public override IReadOnlyList<Npc.SocialCategory> DialogueAppeal =>
            new[] { Npc.SocialCategory.Peasant };
        public override string[] Info => new[]
        {
            "Breathable in summer heat.",
        };
    }

    /// <summary>Coarse wool stockings.</summary>
    public sealed class WoolSocks : WearableItem, IDebugItem
    {
        public override string ItemId        => "debug_wool_socks";
        public override string DisplayName   => "Wool Socks";
        public override string Description   => "Heavy knitted socks that itch but keep the feet dry.";
        public override WeightClass  Weight        => WeightClass.Light;
        public override ItemSize Size        => ItemSize.Small;
        public override WearSlot Slot        => WearSlot.Legwear;
        public override IReadOnlyList<Npc.SocialCategory> DialogueAppeal =>
            new[] { Npc.SocialCategory.Peasant };
        public override string[] Info => new[]
        {
            "A small hole near the left heel.",
        };
    }

    /// <summary>Sturdy leather boots.</summary>
    public sealed class LeatherBoots : WearableItem, IDebugItem
    {
        public override string ItemId        => "debug_leather_boots";
        public override string DisplayName   => "Leather Boots";
        public override string Description   => "Ankle-high boots resoled twice. Reliable on rough ground.";
        public override WeightClass  Weight        => WeightClass.Medium;
        public override ItemSize Size        => ItemSize.Small;
        public override WearSlot Slot        => WearSlot.Footwear;
        public override int DefenseDice      => 1;
        public override string[] Info => new[]
        {
            "The right boot squeaks on wet stone.",
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // Weapons
    // ═══════════════════════════════════════════════════════════════

    /// <summary>An iron sword — straight double-edged blade. Weapon medium for blade fighting skills.</summary>
    public sealed class IronSword : Item, IWeaponItem, IDebugItem
    {
        public override string ItemId       => "fight_iron_sword";
        public override string DisplayName  => "Iron Sword";
        public override string Description  => "A serviceable iron blade, straight and double-edged.";
        public override WeightClass  Weight       => WeightClass.Medium;
        public override ItemSize Size       => ItemSize.Medium;
        public override ItemCategory Category => ItemCategory.Weapon;
        public override string SubcategoryKey => WeaponCategory;
        public override int    UsageLevel   => 3;
        public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.RightHold;
        public int Level => 2;
        public string WeaponCategory => "long_blade";
        public override string[] Info => new[]
        {
            "Damage: 1d6+2",
            "Usable as weapon medium for blade fighting skills.",
        };
    }

    /// <summary>A fight knife — short, fast, lightweight. Weapon medium for blade fighting skills.</summary>
    public sealed class FightKnife : Item, IWeaponItem, IDebugItem
    {
        public override string ItemId       => "fight_knife";
        public override string DisplayName  => "Fight Knife";
        public override string Description  => "A short sturdy blade designed for close quarters.";
        public override WeightClass  Weight       => WeightClass.Light;
        public override ItemSize Size       => ItemSize.Small;
        public override ItemCategory Category => ItemCategory.Weapon;
        public override string SubcategoryKey => WeaponCategory;
        public override int    UsageLevel   => 3;
        public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.RightHold;
        public int Level => 1;
        public string WeaponCategory => "short_blade";
        public override string[] Info => new[]
        {
            "Damage: 1d4+2",
            "Usable as weapon medium for blade fighting skills.",
        };
    }

    /// <summary>Short iron dagger — a tool here, with no fighting medium behind it.</summary>
    public sealed class IronDagger : Item, IDebugItem
    {
        public override string ItemId        => "debug_iron_dagger";
        public override string DisplayName   => "Iron Dagger";
        public override string Description   => "A plain double-edged dagger with a bone handle. Functional and forgettable.";
        public override WeightClass  Weight        => WeightClass.Light;
        public override ItemSize Size        => ItemSize.Small;
        public override ItemCategory Category => ItemCategory.Tool;
        public override int    UsageLevel    => 3;
        public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.RightHold;
        public override string[] Info => new[]
        {
            "Damage: 1d4+1",
            "Edge is chipped but serviceable.",
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // Liquids
    // ═══════════════════════════════════════════════════════════════

    // Both are Drink, and therefore liquid: they can only be carried inside a Vessel, which is
    // what the GlassFlask above is for. Their Info lines used to claim these rules while nothing
    // enforced them — the rules are now real.

    /// <summary>Clear spring water.</summary>
    public sealed class SpringWater : ConsumableItem, IDebugItem
    {
        public override string ItemId        => "debug_spring_water";
        public override string DisplayName   => "Spring Water";
        public override string Description   => "Cold, clear water drawn from a mountain spring.";
        public override WeightClass  Weight        => WeightClass.Light;
        public override ItemSize Size        => ItemSize.Small;
        public override ConsumableType ConsumableType => ConsumableType.Drink;
        public override string[] Info => new[]
        {
            "Clean and refreshing.",
            "Needs a vessel — it cannot be carried in bare hands.",
        };
        protected override HumorRichness Richness => HumorRichness.Sparse;
        protected override HumorRecipe Recipe => new HumorRecipe()
            .Add<AquaHumor>(70).Add<SerumHumor>(30);
    }

    /// <summary>Red wine.</summary>
    public sealed class RedWine : ConsumableItem, IDebugItem
    {
        public override string ItemId        => "debug_red_wine";
        public override string DisplayName   => "Red Wine";
        public override string Description   => "A rough local wine, dark and sharp. Keeps the cold out.";
        public override WeightClass  Weight        => WeightClass.Light;
        public override ItemSize Size        => ItemSize.Small;
        public override ConsumableType ConsumableType => ConsumableType.Drink;
        public override string[] Info => new[]
        {
            "Faintly astringent.",
            "Cannot share a vessel with any other liquid.",
        };
        protected override HumorRecipe Recipe => new HumorRecipe()
            .Add<AlcoholHumor>(50).Add<SugarHumor>(25).Add<MiasmaHumor>(25);
    }
}
