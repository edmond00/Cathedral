using System.Collections.Generic;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Narrative.World.Items;

namespace Cathedral.Game.Scene.Shared;

/// <summary>
/// Builders for fully-populated village workshop areas (Forge, Carpenter, Cooper,
/// Weaver, Mill, Bakery, Alehouse). Each method returns a complete <see cref="Area"/>
/// with PoIs and items already attached.
///
/// <para>These are now the <b>public hall</b> of a workshop building: the caller passes one as
/// <see cref="Building.BuildingSpec.PublicHallBuilder"/> and <see cref="Building.BuildingFactory"/>
/// wraps it in the rest of the building — service rooms, the master's quarters, doors and an entry
/// threshold. The area keeps its own name ("Forge"), which is already unique within a village and
/// reads better than "Forge Hall".</para>
/// </summary>
public static class WorkshopSubfactory
{
    // ── Forge ────────────────────────────────────────────────────────────────

    public static Area BuildForge()
    {
        var forge = new Area(
            displayName: "Forge",
            referenceLemma: "forge",
            contextDescription: "in the village forge",
            transitionDescription: "step into the forge",
            descriptions: new() { "A low-roofed forge thick with the smell of coal-smoke and hot iron" },
            moods: new[] { "smoky", "hot", "ringing", "soot-blackened", "orange-lit", "loud" }
        );

        forge.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Anvil",
            referenceLemma: "anvil",
            descriptions: new() { "A great iron anvil bedded into a worn oak stump, the surface dented from a thousand strikes" },
            moods: new[] { "heavy", "scarred", "polished", "central" }
        ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "metalcraft", ["listen"] = "keen_ear" } });

        forge.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Bellows",
            referenceLemma: "bellows",
            descriptions: new() { "A pair of long bellows hanging beside the hearth, leather creased and patched" },
            moods: new[] { "leather-creased", "tall", "smoke-stained" }
        ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "metalcraft", ["listen"] = "keen_ear" } });

        forge.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Tool Rack",
            referenceLemma: "tool",
            descriptions: new() { "An iron-pegged rack of forge tools" },
            items: new()
            {
                new ItemElement(new Hammer()),
                new ItemElement(new Tongs()),
                new ItemElement(new Chisel()),
            },
            moods: new[] { "ordered", "soot-darkened", "iron-bright" }
        ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "metalcraft" } });

        forge.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Stock Shelf",
            referenceLemma: "shelf",
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
        ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "metalcraft" } });

        forge.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Finished Goods Rack",
            referenceLemma: "rack",
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
        ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "metalcraft", ["contemplate"] = "aesthetic" } });

        return forge;
    }

    // ── Carpenter ────────────────────────────────────────────────────────────

    public static Area BuildCarpenterWorkshop()
    {
        var shop = new Area(
            displayName: "Carpenter's Workshop",
            referenceLemma: "workshop",
            contextDescription: "in the carpenter's workshop",
            transitionDescription: "step into the carpenter's workshop",
            descriptions: new() { "A long timbered workshop heaped with shavings and the clean smell of fresh-cut wood" },
            moods: new[] { "wood-scented", "shavings", "tall-doored", "cluttered", "ordered" }
        );

        shop.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Workbench",
            referenceLemma: "workbench",
            descriptions: new() { "A long heavy workbench scored with cut-marks, vice fitted at one end" },
            items: new()
            {
                new ItemElement(new Saw()),
                new ItemElement(new Chisel()),
                new ItemElement(new Mallet()),
                new ItemElement(new Hammer()),
            },
            moods: new[] { "long", "scored", "heavy", "lit" }
        ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "whittlecraft" } });

        shop.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Wood Pile",
            referenceLemma: "wood",
            descriptions: new() { "A neat pile of seasoned planks and beams stacked along the wall" },
            items: new()
            {
                new ItemElement(new Plank()),
                new ItemElement(new Plank()),
                new ItemElement(new Log()),
            },
            moods: new[] { "neat", "tall", "fragrant", "dry" }
        ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "woodcraft", ["smell"] = "scenting" } });

        shop.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Shaving Pile",
            referenceLemma: "shaving",
            descriptions: new() { "A heap of curled shavings and sawdust against the back wall" },
            items: new()
            {
                new ItemElement(new Twig()),
            },
            moods: new[] { "soft", "fragrant", "dry", "loose" }
        ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "whittlecraft", ["smell"] = "scenting" } });

        return shop;
    }

    // ── Cooper ───────────────────────────────────────────────────────────────

    public static Area BuildCooperWorkshop()
    {
        var shop = new Area(
            displayName: "Cooper's Workshop",
            referenceLemma: "workshop",
            contextDescription: "in the cooper's workshop",
            transitionDescription: "step into the cooper's workshop",
            descriptions: new() { "A workshop laid out with half-built barrels and stave-piles" },
            moods: new[] { "ordered", "wood-scented", "iron-banded", "dim", "tidy" }
        );

        shop.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Stave Pile",
            referenceLemma: "stave",
            descriptions: new() { "A bundle of curved oak staves bound with cord, ready for the next barrel" },
            items: new()
            {
                new ItemElement(new Plank()),
                new ItemElement(new Plank()),
            },
            moods: new[] { "curved", "stacked", "wood-pale" }
        ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "cellarcraft" } });

        shop.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Barrel Stack",
            referenceLemma: "barrel",
            descriptions: new() { "A stack of finished barrels waiting for the brewer or the miller" },
            items: new()
            {
                new ItemElement(new Barrel()),
                new ItemElement(new Barrel()),
            },
            moods: new[] { "rounded", "iron-banded", "ready" }
        ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "cellarcraft", ["smell"] = "scenting" } });

        shop.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Hoop Bin",
            referenceLemma: "hoop",
            descriptions: new() { "A tall bin filled with iron hoops of varied size" },
            items: new()
            {
                new ItemElement(new IronHoop()),
                new ItemElement(new IronHoop()),
            },
            moods: new[] { "tall", "iron-grey", "ringed" }
        ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "metalcraft" } });

        return shop;
    }

    // ── Weaver ───────────────────────────────────────────────────────────────

    public static Area BuildWeaverWorkshop()
    {
        var shop = new Area(
            displayName: "Weaver's Workshop",
            referenceLemma: "workshop",
            contextDescription: "in the weaver's workshop",
            transitionDescription: "step into the weaver's workshop",
            descriptions: new() { "A bright room filled with the rhythmic clatter of a great loom" },
            moods: new[] { "bright", "rhythmic", "thread-strung", "ordered" }
        );

        shop.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Loom",
            referenceLemma: "loom",
            descriptions: new() { "A tall floor loom strung with warp threads, a half-finished bolt of cloth on the beam" },
            moods: new[] { "tall", "stretched", "threaded", "central" }
        ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "threadwork", ["listen"] = "keen_ear" } });

        shop.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Wool Basket",
            referenceLemma: "wool",
            descriptions: new() { "A wide woven basket heaped with carded wool and spools of thread" },
            items: new()
            {
                new ItemElement(new Wool()),
                new ItemElement(new Wool()),
                new ItemElement(new Cathedral.Game.Narrative.World.Items.Thread()),
            },
            moods: new[] { "soft", "white", "wide" }
        ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "threadwork", ["smell"] = "scenting" } });

        shop.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Cloth Bolt",
            referenceLemma: "cloth",
            descriptions: new() { "A bolt of finished cloth standing against the wall" },
            items: new()
            {
                new ItemElement(new Cloth()),
                new ItemElement(new Cloth()),
            },
            moods: new[] { "neat", "folded", "pale" }
        ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "threadwork", ["contemplate"] = "aesthetic" } });

        shop.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Flax Bundle",
            referenceLemma: "flax",
            descriptions: new() { "A bundle of flax stems leaning in the corner, paler than the wool" },
            items: new()
            {
                new ItemElement(new Flax()),
                new ItemElement(new Linen()),
            },
            moods: new[] { "pale", "stiff", "tall" }
        ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "threadwork" } });

        return shop;
    }

    // ── Mill ─────────────────────────────────────────────────────────────────

    public static Area BuildMill()
    {
        var mill = new Area(
            displayName: "Mill",
            referenceLemma: "mill",
            contextDescription: "in the village mill",
            transitionDescription: "step into the mill",
            descriptions: new() { "A high-roofed mill, the millstone groaning at the centre, dust thick in the air" },
            moods: new[] { "dusty", "rumbling", "high-roofed", "white-floored", "loud" }
        );

        mill.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Millstone",
            referenceLemma: "millstone",
            descriptions: new() { "A great round stone turning slowly, grain crunching between its faces" },
            moods: new[] { "great", "turning", "white-dusted", "central" }
        ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "millcraft", ["listen"] = "keen_ear" } });

        mill.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Grain Sacks",
            referenceLemma: "grain",
            descriptions: new() { "A row of bulging grain sacks waiting to be milled" },
            items: new()
            {
                new ItemElement(new Grain()),
                new ItemElement(new Grain()),
            },
            moods: new[] { "heavy", "stacked", "rough-cloth" }
        ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "harvestry", ["smell"] = "scenting" } });

        mill.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Flour Sacks",
            referenceLemma: "flour",
            descriptions: new() { "Fresh-tied sacks of flour, dust pale on the outside" },
            items: new()
            {
                new ItemElement(new Flour()),
                new ItemElement(new Flour()),
            },
            moods: new[] { "white-dusted", "tied", "ready" }
        ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "millcraft", ["smell"] = "scenting" } });

        return mill;
    }

    // ── Bakery ───────────────────────────────────────────────────────────────

    public static Area BuildBakery()
    {
        var bakery = new Area(
            displayName: "Bakery",
            referenceLemma: "bakery",
            contextDescription: "in the bakery",
            transitionDescription: "step into the bakery",
            descriptions: new() { "A close room thick with the smell of bread and the heat of the great oven" },
            moods: new[] { "warm", "bread-scented", "close", "flour-dusted" }
        );

        bakery.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Oven",
            referenceLemma: "oven",
            descriptions: new() { "A domed brick oven, the iron door open and breathing heat" },
            moods: new[] { "domed", "hot", "soot-blackened", "open" }
        ) { Senses = SensoryProfile.FullyAlive, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "doughcraft", ["listen"] = "keen_ear", ["smell"] = "scenting" } });

        bakery.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Flour Sack",
            referenceLemma: "flour",
            descriptions: new() { "A floury sack leaning against the kneading-bench" },
            items: new()
            {
                new ItemElement(new Flour()),
            },
            moods: new[] { "leaning", "white-dusted", "soft" }
        ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "millcraft", ["smell"] = "scenting" } });

        bakery.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Bread Shelf",
            referenceLemma: "bread",
            descriptions: new() { "A wooden shelf set with row upon row of fresh-baked loaves" },
            items: new()
            {
                new ItemElement(new Bread()),
                new ItemElement(new Bread()),
                new ItemElement(new Bread()),
            },
            moods: new[] { "warm", "fragrant", "ordered", "golden-crusted" }
        ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "doughcraft", ["smell"] = "scenting" } });

        return bakery;
    }

    // ── Alehouse ─────────────────────────────────────────────────────────────

    public static Area BuildAlehouse()
    {
        var alehouse = new Area(
            displayName: "Alehouse",
            referenceLemma: "alehouse",
            contextDescription: "in the village alehouse",
            transitionDescription: "step into the alehouse",
            descriptions: new() { "A low room with long benches and a rich brewing-malt smell" },
            moods: new[] { "low", "warm", "smoky", "malt-scented", "long-benched" }
        );

        alehouse.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Brew Barrel",
            referenceLemma: "barrel",
            descriptions: new() { "A great barrel set on a stand, dark spigot at its base" },
            items: new()
            {
                new ItemElement(new Ale()),
                new ItemElement(new Ale()),
            },
            moods: new[] { "great", "dark", "fragrant", "central" }
        ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "brewcraft", ["smell"] = "scenting" } });

        alehouse.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Mug Rack",
            referenceLemma: "mug",
            descriptions: new() { "A wooden rack of clay mugs, well-handled and chip-rimmed" },
            items: new()
            {
                new ItemElement(new Mug()),
                new ItemElement(new Mug()),
                new ItemElement(new Mug()),
            },
            moods: new[] { "rowed", "low", "well-used" }
        ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "cellarcraft" } });

        alehouse.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Grain Sack",
            referenceLemma: "grain",
            descriptions: new() { "A stout sack of barley standing by the brewing-floor" },
            items: new()
            {
                new ItemElement(new Grain()),
            },
            moods: new[] { "stout", "rough", "heavy" }
        ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "harvestry", ["smell"] = "scenting" } });

        return alehouse;
    }

    // Craftsmen Hall and Sleeping Quarters used to live here: one communal eating room and one
    // anonymous dormitory shared by every villager. Both are gone — each craftsman now has their own
    // building, generated by BuildingFactory, with a bed of their own in it.
}
