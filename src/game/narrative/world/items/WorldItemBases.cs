using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Narrative.World.Items;

// ── Foraged / produce ───────────────────────────────────────────────────────

/// <summary>Edible orchard or wild fruit. Small, light, perishable.</summary>
public abstract class FruitItem : Item
{
    public override ItemSize Size => ItemSize.Small;
    public override float    Weight => 0.1f;
    public override List<ItemType> Types => new() { ItemType.Other };
}

/// <summary>Edible root vegetable or pod. Small, light, edible raw.</summary>
public abstract class VegetableItem : Item
{
    public override ItemSize Size => ItemSize.Small;
    public override float    Weight => 0.2f;
    public override List<ItemType> Types => new() { ItemType.Other };
}

/// <summary>Foraged herb sprig. Very light, used for flavour/medicine.</summary>
public abstract class HerbItem : Item
{
    public override ItemSize Size => ItemSize.Small;
    public override float    Weight => 0.05f;
    public override List<ItemType> Types => new() { ItemType.Other };
}

// ── Raw materials ───────────────────────────────────────────────────────────

/// <summary>Cut or fallen wood — log, plank, twig, sap, etc.</summary>
public abstract class WoodRawItem : Item
{
    public override ItemSize Size => ItemSize.Medium;
    public override float    Weight => 1.5f;
    public override List<ItemType> Types => new() { ItemType.Other };
}

/// <summary>Stone, clay, lichen — heavy and earthy.</summary>
public abstract class StoneRawItem : Item
{
    public override ItemSize Size => ItemSize.Small;
    public override float    Weight => 0.8f;
    public override List<ItemType> Types => new() { ItemType.Other };
}

/// <summary>Smelted or raw metal goods. Heavy for their volume.</summary>
public abstract class MetalItem : Item
{
    public override ItemSize Size => ItemSize.Small;
    public override float    Weight => 1.0f;
    public override List<ItemType> Types => new() { ItemType.Other };
}

/// <summary>Forged iron tool. Medium, heavy-ish, gives a usage bonus when wielded.</summary>
public abstract class ToolItem : Item
{
    public override ItemSize Size => ItemSize.Medium;
    public override float    Weight => 0.7f;
    public override int      UsageLevel => 4;
    public override List<ItemType> Types => new() { ItemType.Other };
}

/// <summary>Spun or woven textile, thread, raw fibre.</summary>
public abstract class TextileItem : Item
{
    public override ItemSize Size => ItemSize.Small;
    public override float    Weight => 0.2f;
    public override List<ItemType> Types => new() { ItemType.Other };
}

/// <summary>Output of a farm animal: milk, butter, hide, etc.</summary>
public abstract class AnimalProductItem : Item
{
    public override ItemSize Size => ItemSize.Small;
    public override float    Weight => 0.3f;
    public override List<ItemType> Types => new() { ItemType.Other };
}

/// <summary>Fish, shellfish, seaweed and other sea-edge yields.</summary>
public abstract class SeaFoodItem : Item
{
    public override ItemSize Size => ItemSize.Small;
    public override float    Weight => 0.3f;
    public override List<ItemType> Types => new() { ItemType.Other };
}
