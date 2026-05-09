using System.Collections.Generic;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Narrative.World.Items;

namespace Cathedral.Game.Scene.Shared;

/// <summary>
/// Builders for fully-populated village workshop areas (Forge, Carpenter, Cooper,
/// Weaver, Mill, Bakery, Alehouse). Each method returns a complete <see cref="Area"/>
/// with PoIs and items already attached. The caller (VillageSceneFactory) is
/// responsible for adding the area to a section, registering it, and connecting it.
/// </summary>
public static class WorkshopSubfactory
{
    // ── Forge ────────────────────────────────────────────────────────────────

    public static Area BuildForge()
    {
        var forge = new Area(
            displayName: "Forge",
            contextDescription: "in the village forge",
            transitionDescription: "step into the forge",
            descriptions: new() { "A low-roofed forge thick with the smell of coal-smoke and hot iron" },
            moods: new[] { "smoky", "hot", "ringing", "soot-blackened", "orange-lit", "loud" }
        );

        forge.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Anvil",
            descriptions: new() { "A great iron anvil bedded into a worn oak stump, the surface dented from a thousand strikes" },
            moods: new[] { "heavy", "scarred", "polished", "central" }
        ));

        forge.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Bellows",
            descriptions: new() { "A pair of long bellows hanging beside the hearth, leather creased and patched" },
            moods: new[] { "leather-creased", "tall", "smoke-stained" }
        ));

        forge.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Tool Rack",
            descriptions: new() { "An iron-pegged rack of forge tools" },
            items: new()
            {
                new ItemElement(new Hammer()),
                new ItemElement(new Tongs()),
                new ItemElement(new Chisel()),
            },
            moods: new[] { "ordered", "soot-darkened", "iron-bright" }
        ));

        forge.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Stock Shelf",
            descriptions: new() { "Shelves stacked with raw materials waiting their turn at the fire" },
            items: new()
            {
                new ItemElement(new IronBar()),
                new ItemElement(new IronBar()),
                new ItemElement(new Nail()),
                new ItemElement(new Coal()),
                new ItemElement(new Coal()),
            },
            moods: new[] { "stocked", "heavy", "dim", "iron-smelling" }
        ));

        forge.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Finished Goods Rack",
            descriptions: new() { "A rack of finished tools awaiting collection or sale" },
            items: new()
            {
                new ItemElement(new Saw()),
                new ItemElement(new Axe()),
                new ItemElement(new Knife()),
                new ItemElement(new Sickle()),
                new ItemElement(new Pick()),
            },
            moods: new[] { "ordered", "iron-bright", "ready" }
        ));

        return forge;
    }

    // ── Carpenter ────────────────────────────────────────────────────────────

    public static Area BuildCarpenterWorkshop()
    {
        var shop = new Area(
            displayName: "Carpenter's Workshop",
            contextDescription: "in the carpenter's workshop",
            transitionDescription: "step into the carpenter's workshop",
            descriptions: new() { "A long timbered workshop heaped with shavings and the clean smell of fresh-cut wood" },
            moods: new[] { "wood-scented", "shavings", "tall-doored", "cluttered", "ordered" }
        );

        shop.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Workbench",
            descriptions: new() { "A long heavy workbench scored with cut-marks, vice fitted at one end" },
            items: new()
            {
                new ItemElement(new Saw()),
                new ItemElement(new Chisel()),
                new ItemElement(new Mallet()),
                new ItemElement(new Hammer()),
            },
            moods: new[] { "long", "scored", "heavy", "lit" }
        ));

        shop.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Wood Pile",
            descriptions: new() { "A neat pile of seasoned planks and beams stacked along the wall" },
            items: new()
            {
                new ItemElement(new Plank()),
                new ItemElement(new Plank()),
                new ItemElement(new Log()),
            },
            moods: new[] { "neat", "tall", "fragrant", "dry" }
        ));

        shop.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Shaving Pile",
            descriptions: new() { "A heap of curled shavings and sawdust against the back wall" },
            items: new()
            {
                new ItemElement(new Twig()),
            },
            moods: new[] { "soft", "fragrant", "dry", "loose" }
        ));

        return shop;
    }

    // ── Cooper ───────────────────────────────────────────────────────────────

    public static Area BuildCooperWorkshop()
    {
        var shop = new Area(
            displayName: "Cooper's Workshop",
            contextDescription: "in the cooper's workshop",
            transitionDescription: "step into the cooper's workshop",
            descriptions: new() { "A workshop laid out with half-built barrels and stave-piles" },
            moods: new[] { "ordered", "wood-scented", "iron-banded", "dim", "tidy" }
        );

        shop.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Stave Pile",
            descriptions: new() { "A bundle of curved oak staves bound with cord, ready for the next barrel" },
            items: new()
            {
                new ItemElement(new Plank()),
                new ItemElement(new Plank()),
            },
            moods: new[] { "curved", "stacked", "wood-pale" }
        ));

        shop.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Barrel Stack",
            descriptions: new() { "A stack of finished barrels waiting for the brewer or the miller" },
            items: new()
            {
                new ItemElement(new Barrel()),
                new ItemElement(new Barrel()),
            },
            moods: new[] { "rounded", "iron-banded", "ready" }
        ));

        shop.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Hoop Bin",
            descriptions: new() { "A tall bin filled with iron hoops of varied size" },
            items: new()
            {
                new ItemElement(new IronHoop()),
                new ItemElement(new IronHoop()),
            },
            moods: new[] { "tall", "iron-grey", "ringed" }
        ));

        return shop;
    }

    // ── Weaver ───────────────────────────────────────────────────────────────

    public static Area BuildWeaverWorkshop()
    {
        var shop = new Area(
            displayName: "Weaver's Workshop",
            contextDescription: "in the weaver's workshop",
            transitionDescription: "step into the weaver's workshop",
            descriptions: new() { "A bright room filled with the rhythmic clatter of a great loom" },
            moods: new[] { "bright", "rhythmic", "thread-strung", "ordered" }
        );

        shop.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Loom",
            descriptions: new() { "A tall floor loom strung with warp threads, a half-finished bolt of cloth on the beam" },
            moods: new[] { "tall", "stretched", "threaded", "central" }
        ));

        shop.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Wool Basket",
            descriptions: new() { "A wide woven basket heaped with carded wool and spools of thread" },
            items: new()
            {
                new ItemElement(new Wool()),
                new ItemElement(new Wool()),
                new ItemElement(new Cathedral.Game.Narrative.World.Items.Thread()),
            },
            moods: new[] { "soft", "white", "wide" }
        ));

        shop.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Cloth Bolt",
            descriptions: new() { "A bolt of finished cloth standing against the wall" },
            items: new()
            {
                new ItemElement(new Cloth()),
                new ItemElement(new Cloth()),
            },
            moods: new[] { "neat", "folded", "pale" }
        ));

        shop.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Flax Bundle",
            descriptions: new() { "A bundle of flax stems leaning in the corner, paler than the wool" },
            items: new()
            {
                new ItemElement(new Flax()),
                new ItemElement(new Linen()),
            },
            moods: new[] { "pale", "stiff", "tall" }
        ));

        return shop;
    }

    // ── Mill ─────────────────────────────────────────────────────────────────

    public static Area BuildMill()
    {
        var mill = new Area(
            displayName: "Mill",
            contextDescription: "in the village mill",
            transitionDescription: "step into the mill",
            descriptions: new() { "A high-roofed mill, the millstone groaning at the centre, dust thick in the air" },
            moods: new[] { "dusty", "rumbling", "high-roofed", "white-floored", "loud" }
        );

        mill.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Millstone",
            descriptions: new() { "A great round stone turning slowly, grain crunching between its faces" },
            moods: new[] { "great", "turning", "white-dusted", "central" }
        ));

        mill.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Grain Sacks",
            descriptions: new() { "A row of bulging grain sacks waiting to be milled" },
            items: new()
            {
                new ItemElement(new Grain()),
                new ItemElement(new Grain()),
            },
            moods: new[] { "heavy", "stacked", "rough-cloth" }
        ));

        mill.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Flour Sacks",
            descriptions: new() { "Fresh-tied sacks of flour, dust pale on the outside" },
            items: new()
            {
                new ItemElement(new Flour()),
                new ItemElement(new Flour()),
            },
            moods: new[] { "white-dusted", "tied", "ready" }
        ));

        return mill;
    }

    // ── Bakery ───────────────────────────────────────────────────────────────

    public static Area BuildBakery()
    {
        var bakery = new Area(
            displayName: "Bakery",
            contextDescription: "in the bakery",
            transitionDescription: "step into the bakery",
            descriptions: new() { "A close room thick with the smell of bread and the heat of the great oven" },
            moods: new[] { "warm", "bread-scented", "close", "flour-dusted" }
        );

        bakery.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Oven",
            descriptions: new() { "A domed brick oven, the iron door open and breathing heat" },
            moods: new[] { "domed", "hot", "soot-blackened", "open" }
        ));

        bakery.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Flour Sack",
            descriptions: new() { "A floury sack leaning against the kneading-bench" },
            items: new()
            {
                new ItemElement(new Flour()),
            },
            moods: new[] { "leaning", "white-dusted", "soft" }
        ));

        bakery.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Bread Shelf",
            descriptions: new() { "A wooden shelf set with row upon row of fresh-baked loaves" },
            items: new()
            {
                new ItemElement(new Bread()),
                new ItemElement(new Bread()),
                new ItemElement(new Bread()),
            },
            moods: new[] { "warm", "fragrant", "ordered", "golden-crusted" }
        ));

        return bakery;
    }

    // ── Alehouse ─────────────────────────────────────────────────────────────

    public static Area BuildAlehouse()
    {
        var alehouse = new Area(
            displayName: "Alehouse",
            contextDescription: "in the village alehouse",
            transitionDescription: "step into the alehouse",
            descriptions: new() { "A low room with long benches and a rich brewing-malt smell" },
            moods: new[] { "low", "warm", "smoky", "malt-scented", "long-benched" }
        );

        alehouse.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Brew Barrel",
            descriptions: new() { "A great barrel set on a stand, dark spigot at its base" },
            items: new()
            {
                new ItemElement(new Ale()),
                new ItemElement(new Ale()),
            },
            moods: new[] { "great", "dark", "fragrant", "central" }
        ));

        alehouse.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Mug Rack",
            descriptions: new() { "A wooden rack of clay mugs, well-handled and chip-rimmed" },
            items: new()
            {
                new ItemElement(new Mug()),
                new ItemElement(new Mug()),
                new ItemElement(new Mug()),
            },
            moods: new[] { "rowed", "low", "well-used" }
        ));

        alehouse.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Grain Sack",
            descriptions: new() { "A stout sack of barley standing by the brewing-floor" },
            items: new()
            {
                new ItemElement(new Grain()),
            },
            moods: new[] { "stout", "rough", "heavy" }
        ));

        return alehouse;
    }

    // ── Craftsmen Hall ───────────────────────────────────────────────────────

    public static Area BuildCraftsmenHall()
    {
        var hall = new Area(
            displayName: "Craftsmen Hall",
            contextDescription: "in the craftsmen's hall",
            transitionDescription: "step into the craftsmen's hall",
            descriptions: new() { "A long communal hall where the village craftsmen take their meals" },
            moods: new[] { "long", "warm", "smoky", "smoke-blackened", "communal" }
        );

        hall.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Hearth",
            descriptions: new() { "A wide stone hearth with a slow-burning fire and a black kettle hanging above" },
            moods: new[] { "warm", "central", "soot-stained" }
        ));

        hall.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Long Table",
            descriptions: new() { "A long oak table set with bread and mugs for the next meal" },
            items: new()
            {
                new ItemElement(new Bread()),
                new ItemElement(new Bread()),
                new ItemElement(new Mug()),
                new ItemElement(new Mug()),
            },
            moods: new[] { "long", "scarred", "well-used" }
        ));

        return hall;
    }

    // ── Sleeping Quarters ────────────────────────────────────────────────────

    public static Area BuildSleepingQuarters()
    {
        var quarters = new Area(
            displayName: "Sleeping Quarters",
            contextDescription: "in the sleeping quarters",
            transitionDescription: "climb into the sleeping quarters",
            descriptions: new() { "A long low room laid out with straw pallets and a few wooden chests" },
            moods: new[] { "low", "shared", "dim", "close", "quiet" }
        );

        quarters.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Straw Pallets",
            descriptions: new() { "A row of straw pallets along the wall, blankets piled at the foot of each" },
            items: new()
            {
                new ItemElement(new Straw()),
                new ItemElement(new Straw()),
            },
            moods: new[] { "lined", "soft", "rough-blanketed" }
        ));

        quarters.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Chest",
            descriptions: new() { "A small iron-banded chest, lid scarred with use" },
            items: new()
            {
                new ItemElement(new Cloth()),
                new ItemElement(new Knife()),
            },
            moods: new[] { "small", "battered", "iron-banded" }
        ));

        return quarters;
    }
}
