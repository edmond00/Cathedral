using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Narrative.World.Items;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Npc.Archetypes;

// ── The settled country's animals ──────────────────────────────────────────────
// The stock of the stables, byres and folds; the pests and scavengers of the towns and harbours; what
// lives around temples and among the dead; and the small life of groves, vineyards and plantations.
// Composed from the shared body-part vocabulary like every other creature: nothing here names a species
// in its drops.

// ── stock: the stable, the byre and the yard ──────────────────────────────────────────────────

public class HorseArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "horse";
    public override string TypeDisplayName => "Horse";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "tall", "heavy-quartered" },
        colors: new[] { "bay", "chestnut", "grey", "dun" },
        noun:   "horse",
        traits: new[] { "pulling at a hay net", "resting a hind foot, ears drooping", "whickering at the door" });
    protected override string CorpseBodyDescription => "the great sprawled body of a horse";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Hide()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Hair()),
        new ItemElement(new Tail()),
        new ItemElement(new Bone()),
        new ItemElement(new Liver()),
    };
}

public class FoalArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "foal";
    public override string TypeDisplayName => "Foal";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "leggy", "small" },
        colors: new[] { "bay", "grey", "chestnut" },
        noun:   "foal",
        traits: new[] { "bucking for no reason at all", "pressed against its mother's flank", "nosing at everything" });
    protected override string CorpseBodyDescription => "the leggy body of a foal";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Hide()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Bone()),
        new ItemElement(new Hair()),
    };
}

public class DonkeyArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "donkey";
    public override string TypeDisplayName => "Donkey";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "small", "sturdy" },
        colors: new[] { "grey", "mouse-brown" },
        noun:   "donkey",
        traits: new[] { "braying at nothing", "standing with enormous patience", "flicking its long ears at the flies" });
    protected override string CorpseBodyDescription => "the grey body of a donkey";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Hide()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Hair()),
        new ItemElement(new Tail()),
        new ItemElement(new Bone()),
    };
}

public class MuleArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "mule";
    public override string TypeDisplayName => "Mule";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "rangy", "stubborn-looking" },
        colors: new[] { "brown", "bay" },
        noun:   "mule",
        traits: new[] { "refusing to move", "eyeing everyone with suspicion", "chewing at a rope" });
    protected override string CorpseBodyDescription => "the rangy body of a mule";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Hide()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Hair()),
        new ItemElement(new Bone()),
    };
}

public class OxArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "ox";
    public override string TypeDisplayName => "Ox";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "massive", "broad-yoked" },
        colors: new[] { "red", "pale", "brindled" },
        noun:   "ox",
        traits: new[] { "standing yoked and patient", "chewing the cud slowly", "lowering its great head" });
    protected override string CorpseBodyDescription => "the massive body of an ox";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Hide()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Horn()),
        new ItemElement(new Horn()),
        new ItemElement(new Liver()),
        new ItemElement(new Suet()),
    };
}

public class BullArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "bull";
    public override string TypeDisplayName => "Bull";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "thick-necked", "heavy" },
        colors: new[] { "black", "red" },
        noun:   "bull",
        traits: new[] { "pawing at the dirt", "watching with a small hard eye", "snorting at the fence" });
    protected override string CorpseBodyDescription => "the huge black body of a bull";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Hide()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Horn()),
        new ItemElement(new Horn()),
        new ItemElement(new Heart()),
    };
}

public class CalfArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "calf";
    public override string TypeDisplayName => "Calf";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "knock-kneed", "small" },
        colors: new[] { "brown-and-white", "red" },
        noun:   "calf",
        traits: new[] { "bawling for its mother", "sucking at a fingertip", "butting at the gate" });
    protected override string CorpseBodyDescription => "the small body of a calf";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Hide()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Liver()),
        new ItemElement(new Bone()),
    };
}

public class GoatArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "goat";
    public override string TypeDisplayName => "Goat";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "wiry", "bearded" },
        colors: new[] { "white", "black", "piebald" },
        noun:   "goat",
        traits: new[] { "standing on top of something it should not be on", "chewing a piece of rope", "staring with slotted yellow eyes" });
    protected override string CorpseBodyDescription => "the wiry body of a goat";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Hide()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Horn()),
        new ItemElement(new Horn()),
        new ItemElement(new Hair()),
    };
}

public class RamArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "ram";
    public override string TypeDisplayName => "Ram";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "heavy-horned", "thick-fleeced" },
        colors: new[] { "grey", "dirty-white" },
        noun:   "ram",
        traits: new[] { "lowering its curled horns", "standing apart from the ewes", "butting the fence post" });
    protected override string CorpseBodyDescription => "the heavy curled-horned body of a ram";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Hide()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Horn()),
        new ItemElement(new Horn()),
        new ItemElement(new Suet()),
    };
}

public class LambArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "lamb";
    public override string TypeDisplayName => "Lamb";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "small", "woolly" },
        colors: new[] { "white", "black-faced" },
        noun:   "lamb",
        traits: new[] { "leaping straight up for joy", "tucked against a ewe", "bleating thinly" });
    protected override string CorpseBodyDescription => "the small woolly body of a lamb";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Hide()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Bone()),
    };
}

public class PigletArchetype : SmallMammalShallowArchetype
{
    public override string ArchetypeId     => "piglet";
    public override string TypeDisplayName => "Piglet";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "tiny", "round" },
        colors: new[] { "pink", "spotted" },
        noun:   "piglet",
        traits: new[] { "squealing at nothing", "tumbling over its siblings", "rooting in the straw" });
    protected override string CorpseBodyDescription => "the small pink body of a piglet";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Skin()),
        new ItemElement(new Bone()),
    };
}

public class GooseArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "goose";
    public override string TypeDisplayName => "Goose";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "big", "long-necked" },
        colors: new[] { "white", "grey" },
        noun:   "goose",
        traits: new[] { "hissing with its neck stretched out", "honking at a stranger", "marching in a line with the others" });
    protected override string CorpseBodyDescription => "the heavy white body of a goose";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Suet()),
        new ItemElement(new Bone()),
    };
}

public class DuckArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "duck";
    public override string TypeDisplayName => "Duck";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "plump", "waddling" },
        colors: new[] { "brown", "white", "green-headed" },
        noun:   "duck",
        traits: new[] { "dabbling in a puddle", "quacking in a huddle", "preening on the bank" });
    protected override string CorpseBodyDescription => "the plump body of a duck";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Bone()),
    };
}

public class TurkeyArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "turkey";
    public override string TypeDisplayName => "Turkey";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "big", "puffed-up" },
        colors: new[] { "bronze", "black" },
        noun:   "turkey",
        traits: new[] { "gobbling at nothing", "fanning its tail", "strutting with its wattles red" });
    protected override string CorpseBodyDescription => "the big bronze body of a turkey";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Plume()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Bone()),
    };
}

public class GuineaFowlArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "guinea_fowl";
    public override string TypeDisplayName => "Guinea Fowl";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "round", "small-headed" },
        colors: new[] { "dotted grey", "pearl-spotted" },
        noun:   "guinea fowl",
        traits: new[] { "screeching in alarm", "running in a flock", "scratching in the dust" });
    protected override string CorpseBodyDescription => "the spotted body of a guinea fowl";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Meat()),
        new ItemElement(new Bone()),
    };
}

public class PeacockArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "peacock";
    public override string TypeDisplayName => "Peacock";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "long-tailed", "stately" },
        colors: new[] { "blue", "green-and-gold", "white" },
        noun:   "peacock",
        traits: new[] { "screaming from a wall", "spreading its tail in a shivering fan", "dragging its train across the grass" });
    protected override string CorpseBodyDescription => "the gorgeous body of a peacock";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Plume()),
        new ItemElement(new Plume()),
        new ItemElement(new Plume()),
        new ItemElement(new Feather()),
        new ItemElement(new Meat()),
        new ItemElement(new Bone()),
    };
}

public class WaterBuffaloArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "water_buffalo";
    public override string TypeDisplayName => "Water Buffalo";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "massive", "swept-horned" },
        colors: new[] { "slate-grey", "black" },
        noun:   "water buffalo",
        traits: new[] { "wallowing to the shoulders in mud", "standing in the paddy", "gazing with wet indifference" });
    protected override string CorpseBodyDescription => "the massive grey body of a water buffalo";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Hide()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Horn()),
        new ItemElement(new Horn()),
        new ItemElement(new Liver()),
    };
}

public class ZebuArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "zebu";
    public override string TypeDisplayName => "Zebu";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "humped", "long-eared" },
        colors: new[] { "pale grey", "tan" },
        noun:   "zebu",
        traits: new[] { "swinging its dewlap", "lying in the thin shade", "grazing on dry grass" });
    protected override string CorpseBodyDescription => "the humped body of a zebu";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Hide()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Horn()),
        new ItemElement(new Suet()),
    };
}

public class YakArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "yak";
    public override string TypeDisplayName => "Yak";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "shaggy", "massive" },
        colors: new[] { "black", "brown-and-white" },
        noun:   "yak",
        traits: new[] { "grunting into the cold", "standing with its shaggy skirt to the wind", "pawing at the snow for grass" });
    protected override string CorpseBodyDescription => "the shaggy body of a yak";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Hide()),
        new ItemElement(new Hair()),
        new ItemElement(new Hair()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Horn()),
        new ItemElement(new Suet()),
    };
}

public class HoundArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "hound";
    public override string TypeDisplayName => "Hound";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "lean", "deep-chested" },
        colors: new[] { "tan", "black-and-white", "brindled" },
        noun:   "hound",
        traits: new[] { "sniffing along a trail", "sprawled by the fire", "baying at something outside" });
    protected override string CorpseBodyDescription => "the lean body of a hound";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Pelt()),
        new ItemElement(new Meat()),
        new ItemElement(new Bone()),
        new ItemElement(new Fang()),
    };
}

public class MastiffArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "mastiff";
    public override string TypeDisplayName => "Mastiff";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "huge", "heavy-jawed" },
        colors: new[] { "fawn", "black" },
        noun:   "mastiff",
        traits: new[] { "growling low at strangers", "chained by the gate", "drooling in its sleep" });
    protected override string CorpseBodyDescription => "the huge body of a mastiff";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Pelt()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Bone()),
        new ItemElement(new Fang()),
        new ItemElement(new Fang()),
    };
}

// ── the town and the harbour ──────────────────────────────────────────────────────────────────

public class PigeonArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "pigeon";
    public override string TypeDisplayName => "Pigeon";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "plump", "bobbing" },
        colors: new[] { "grey", "iridescent", "white" },
        noun:   "pigeon",
        traits: new[] { "bobbing for crumbs", "cooing on a ledge", "clattering up from the cobbles" });
    protected override string CorpseBodyDescription => "the grey body of a pigeon";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Meat()),
        new ItemElement(new Bone()),
    };
}

public class MagpieArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "magpie";
    public override string TypeDisplayName => "Magpie";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "long-tailed", "bold" },
        colors: new[] { "black-and-white", "pied" },
        noun:   "magpie",
        traits: new[] { "chattering on a roof ridge", "hopping off with something bright", "scolding a cat" });
    protected override string CorpseBodyDescription => "the pied body of a magpie";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Bone()),
        new ItemElement(new Meat()),
    };
}

public class StarlingArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "starling";
    public override string TypeDisplayName => "Starling";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "small", "glossy" },
        colors: new[] { "speckled black", "oily green" },
        noun:   "starling",
        traits: new[] { "mimicking a door creak", "chattering on a chimney", "probing the gutter" });
    protected override string CorpseBodyDescription => "the glossy body of a starling";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Bone()),
        new ItemElement(new Meat()),
    };
}

public class JackdawArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "jackdaw";
    public override string TypeDisplayName => "Jackdaw";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "small", "grey-naped" },
        colors: new[] { "black", "grey-hooded" },
        noun:   "jackdaw",
        traits: new[] { "tchacking from a tower", "stealing something shiny", "watching from the battlement" });
    protected override string CorpseBodyDescription => "the black body of a jackdaw";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Bone()),
        new ItemElement(new Meat()),
    };
}

public class SwallowArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "swallow";
    public override string TypeDisplayName => "Swallow";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "slight", "fork-tailed" },
        colors: new[] { "blue-black", "rust-throated" },
        noun:   "swallow",
        traits: new[] { "skimming low over the yard", "darting into the barn", "twittering on the beam" });
    protected override string CorpseBodyDescription => "the slight body of a swallow";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Bone()),
    };
}

public class CormorantArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "cormorant";
    public override string TypeDisplayName => "Cormorant";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "long-necked", "hook-billed" },
        colors: new[] { "black", "bronze-black" },
        noun:   "cormorant",
        traits: new[] { "holding its wings out to dry", "diving off the quay", "standing on a bollard" });
    protected override string CorpseBodyDescription => "the oily black body of a cormorant";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Meat()),
        new ItemElement(new Bone()),
    };
}

public class PelicanArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "pelican";
    public override string TypeDisplayName => "Pelican";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "huge-billed", "heavy" },
        colors: new[] { "white", "grey" },
        noun:   "pelican",
        traits: new[] { "scooping at the water", "waddling along the jetty", "gliding in low over the harbour" });
    protected override string CorpseBodyDescription => "the heavy body of a pelican";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Plume()),
        new ItemElement(new Meat()),
        new ItemElement(new Bone()),
    };
}

public class ShipRatArchetype : SmallMammalShallowArchetype
{
    public override string ArchetypeId     => "ship_rat";
    public override string TypeDisplayName => "Ship Rat";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "lean", "long-tailed" },
        colors: new[] { "black", "brown" },
        noun:   "ship rat",
        traits: new[] { "running along a mooring rope", "gnawing at a sack", "watching from a crack in the quay" });
    protected override string CorpseBodyDescription => "the lean body of a ship rat";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Skin()),
        new ItemElement(new Tail()),
        new ItemElement(new Bone()),
        new ItemElement(new Meat()),
    };
}

public class FerretArchetype : SmallMammalShallowArchetype
{
    public override string ArchetypeId     => "ferret";
    public override string TypeDisplayName => "Ferret";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "long", "sinuous" },
        colors: new[] { "cream", "polecat-brown" },
        noun:   "ferret",
        traits: new[] { "vanishing into a hole", "dancing sideways", "sniffing at a rat-run" });
    protected override string CorpseBodyDescription => "the long body of a ferret";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Pelt()),
        new ItemElement(new Bone()),
        new ItemElement(new Meat()),
        new ItemElement(new Tooth()),
    };
}

// ── the temple, the cloister and the dead ─────────────────────────────────────────────────────

public class WhiteDoveArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "white_dove";
    public override string TypeDisplayName => "White Dove";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "small", "soft" },
        colors: new[] { "white", "cream" },
        noun:   "white dove",
        traits: new[] { "cooing in the dovecote", "settling on the temple roof", "circling the courtyard" });
    protected override string CorpseBodyDescription => "the soft white body of a dove";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Meat()),
        new ItemElement(new Bone()),
    };
}

public class CarpArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "carp";
    public override string TypeDisplayName => "Carp";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "fat", "whiskered" },
        colors: new[] { "gold", "bronze", "red-and-white" },
        noun:   "carp",
        traits: new[] { "mouthing at the surface", "drifting under the lily pads", "turning lazily in the pond" });
    protected override string CorpseBodyDescription => "the heavy golden body of a carp";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Bone()),
        new ItemElement(new Skin()),
    };
}

public class IbisArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "ibis";
    public override string TypeDisplayName => "Ibis";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "long-legged", "curve-billed" },
        colors: new[] { "white", "black-headed" },
        noun:   "ibis",
        traits: new[] { "wading in the sacred pool", "probing the mud with its long bill", "standing like a carving on the wall" });
    protected override string CorpseBodyDescription => "the white body of an ibis";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Plume()),
        new ItemElement(new Bone()),
        new ItemElement(new Meat()),
    };
}

public class JackalArchetype : SmallMammalShallowArchetype
{
    public override string ArchetypeId     => "jackal";
    public override string TypeDisplayName => "Jackal";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "lean", "big-eared" },
        colors: new[] { "golden", "grey-backed" },
        noun:   "jackal",
        traits: new[] { "slinking among the graves", "yipping at dusk", "digging at a mound" });
    protected override string CorpseBodyDescription => "the lean body of a jackal";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Pelt()),
        new ItemElement(new Bone()),
        new ItemElement(new Fang()),
        new ItemElement(new Meat()),
    };
}

public class BarnOwlArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "barn_owl";
    public override string TypeDisplayName => "Barn Owl";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "pale", "heart-faced" },
        colors: new[] { "white", "gold-and-grey" },
        noun:   "barn owl",
        traits: new[] { "screeching from the roof beams", "drifting silently over the yard", "staring with black eyes" });
    protected override string CorpseBodyDescription => "the pale body of a barn owl";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Claw()),
        new ItemElement(new Bone()),
    };
}

public class CobraArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "cobra";
    public override string TypeDisplayName => "Cobra";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "long", "hooded" },
        colors: new[] { "olive", "black", "banded" },
        noun:   "cobra",
        traits: new[] { "rearing with its hood spread", "sliding between stones", "coiled in a niche" });
    protected override string CorpseBodyDescription => "the long limp body of a cobra";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Skin()),
        new ItemElement(new Fang()),
        new ItemElement(new Fang()),
        new ItemElement(new Meat()),
    };
}

public class ScarabArchetype : TinyShallowArchetype
{
    public override string ArchetypeId     => "scarab";
    public override string TypeDisplayName => "Scarab";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "round", "small" },
        colors: new[] { "iridescent green", "black" },
        noun:   "scarab",
        traits: new[] { "rolling a ball of dung", "crawling over warm stone", "clicking its wing-cases" });
    public override List<Item> BuildCatchYield() => new() { new Carapace(), new Carapace() };
}

public class CentipedeArchetype : TinyShallowArchetype
{
    public override string ArchetypeId     => "centipede";
    public override string TypeDisplayName => "Centipede";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "long", "many-legged" },
        colors: new[] { "red-brown", "yellow" },
        noun:   "centipede",
        traits: new[] { "rippling across the floor", "vanishing under a slab", "curling up when touched" });
    public override List<Item> BuildCatchYield() => new() { new Carapace(), new Sting() };
}

public class TombSpiderArchetype : TinyShallowArchetype
{
    public override string ArchetypeId     => "tomb_spider";
    public override string TypeDisplayName => "Tomb Spider";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "pale", "long-legged" },
        colors: new[] { "grey", "white" },
        noun:   "tomb spider",
        traits: new[] { "hanging in an old web", "waiting in the dark of a niche", "running over bone" });
    public override List<Item> BuildCatchYield() => new() { new Silk(), new Carapace() };
}

public class DeathsHeadMothArchetype : TinyShallowArchetype
{
    public override string ArchetypeId     => "deaths_head_moth";
    public override string TypeDisplayName => "Death's-Head Moth";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "large", "heavy-bodied" },
        colors: new[] { "brown", "yellow-banded" },
        noun:   "death's-head moth",
        traits: new[] { "squeaking when disturbed", "resting on a tomb, skull-mark showing", "blundering at the lamp" });
    public override List<Item> BuildCatchYield() => new() { new Wing(), new Wing() };
}

// ── the fields, the groves and the plantations ────────────────────────────────────────────────

public class FieldMouseArchetype : SmallMammalShallowArchetype
{
    public override string ArchetypeId     => "field_mouse";
    public override string TypeDisplayName => "Field Mouse";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "tiny", "round-eared" },
        colors: new[] { "russet", "brown" },
        noun:   "field mouse",
        traits: new[] { "climbing a grain stalk", "darting between the furrows", "nibbling a seed" });
    protected override string CorpseBodyDescription => "the tiny body of a field mouse";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Skin()),
        new ItemElement(new Tail()),
        new ItemElement(new Bone()),
        new ItemElement(new Meat()),
    };
}

public class VoleArchetype : SmallMammalShallowArchetype
{
    public override string ArchetypeId     => "vole";
    public override string TypeDisplayName => "Vole";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "stubby", "small" },
        colors: new[] { "dark brown", "grey" },
        noun:   "vole",
        traits: new[] { "scuttling down a runway in the grass", "gnawing a root", "popping out of a hole" });
    protected override string CorpseBodyDescription => "the stubby body of a vole";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Skin()),
        new ItemElement(new Bone()),
        new ItemElement(new Meat()),
        new ItemElement(new Tooth()),
    };
}

public class QuailArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "quail";
    public override string TypeDisplayName => "Quail";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "small", "round" },
        colors: new[] { "streaked brown", "buff" },
        noun:   "quail",
        traits: new[] { "calling wet-my-lips from the crop", "scuttling between the rows", "bursting up underfoot" });
    protected override string CorpseBodyDescription => "the small round body of a quail";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Meat()),
        new ItemElement(new Bone()),
    };
}

public class PartridgeArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "partridge";
    public override string TypeDisplayName => "Partridge";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "plump", "round" },
        colors: new[] { "grey", "red-legged" },
        noun:   "partridge",
        traits: new[] { "running along the furrow", "whirring off low", "calling from the stubble" });
    protected override string CorpseBodyDescription => "the plump grey body of a partridge";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Bone()),
    };
}

public class PheasantArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "pheasant";
    public override string TypeDisplayName => "Pheasant";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "long-tailed", "bright" },
        colors: new[] { "copper", "red-wattled" },
        noun:   "pheasant",
        traits: new[] { "crowing from the hedge", "stalking along the margin", "clattering up in a panic" });
    protected override string CorpseBodyDescription => "the copper body of a pheasant";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Plume()),
        new ItemElement(new Plume()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Bone()),
    };
}

public class BlackbirdArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "blackbird";
    public override string TypeDisplayName => "Blackbird";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "sleek", "small" },
        colors: new[] { "black", "brown" },
        noun:   "blackbird",
        traits: new[] { "singing from the top of a tree", "tossing leaves aside", "pecking at a windfall" });
    protected override string CorpseBodyDescription => "the sleek body of a blackbird";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Bone()),
        new ItemElement(new Meat()),
    };
}

public class ThrushArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "thrush";
    public override string TypeDisplayName => "Thrush";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "speckled", "upright" },
        colors: new[] { "brown", "cream-spotted" },
        noun:   "thrush",
        traits: new[] { "smashing a snail on a stone", "singing every phrase twice", "eating grapes off the vine" });
    protected override string CorpseBodyDescription => "the speckled body of a thrush";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Bone()),
        new ItemElement(new Meat()),
    };
}

public class MongooseArchetype : SmallMammalShallowArchetype
{
    public override string ArchetypeId     => "mongoose";
    public override string TypeDisplayName => "Mongoose";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "slender", "quick" },
        colors: new[] { "grizzled grey", "tawny" },
        noun:   "mongoose",
        traits: new[] { "standing up to look around", "darting through the cane", "worrying at something under a leaf" });
    protected override string CorpseBodyDescription => "the slender body of a mongoose";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Pelt()),
        new ItemElement(new Bone()),
        new ItemElement(new Fang()),
        new ItemElement(new Meat()),
    };
}

public class FruitBatArchetype : SmallMammalShallowArchetype
{
    public override string ArchetypeId     => "fruit_bat";
    public override string TypeDisplayName => "Fruit Bat";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "fox-faced", "large" },
        colors: new[] { "brown", "gold-collared" },
        noun:   "fruit bat",
        traits: new[] { "hanging wrapped in its wings", "squabbling over a fruit", "flapping heavily between the trees" });
    protected override string CorpseBodyDescription => "the fox-faced body of a fruit bat";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Wing()),
        new ItemElement(new Wing()),
        new ItemElement(new Skin()),
        new ItemElement(new Bone()),
    };
}

public class SunbirdArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "sunbird";
    public override string TypeDisplayName => "Sunbird";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "tiny", "curve-billed" },
        colors: new[] { "metallic green", "violet" },
        noun:   "sunbird",
        traits: new[] { "hovering at a flower", "flashing between the bushes", "clinging to a blossom" });
    protected override string CorpseBodyDescription => "the tiny bright body of a sunbird";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Bone()),
    };
}

public class HornetArchetype : TinyShallowArchetype
{
    public override string ArchetypeId     => "hornet";
    public override string TypeDisplayName => "Hornet";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "large", "heavy" },
        colors: new[] { "yellow-and-brown", "orange" },
        noun:   "hornet",
        traits: new[] { "droning in the rafters", "feeding on a split fruit", "patrolling the vines" });
    public override List<Item> BuildCatchYield() => new() { new Sting(), new Wing() };
}

public class WaspArchetype : TinyShallowArchetype
{
    public override string ArchetypeId     => "wasp";
    public override string TypeDisplayName => "Wasp";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "small", "narrow-waisted" },
        colors: new[] { "yellow-and-black" },
        noun:   "wasp",
        traits: new[] { "crawling into a windfall", "buzzing round a sticky cup", "hovering at the press" });
    public override List<Item> BuildCatchYield() => new() { new Sting(), new Wing() };
}

public class AphidArchetype : TinyShallowArchetype
{
    public override string ArchetypeId     => "aphid";
    public override string TypeDisplayName => "Aphid";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "tiny", "soft" },
        colors: new[] { "green", "black" },
        noun:   "cluster of aphids",
        traits: new[] { "clustered on a young shoot", "sucking at a leaf", "tended by ants" });
    public override List<Item> BuildCatchYield() => new() { new Grub(), new Grub() };
}

public class LadybirdArchetype : TinyShallowArchetype
{
    public override string ArchetypeId     => "ladybird";
    public override string TypeDisplayName => "Ladybird";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "tiny", "round" },
        colors: new[] { "red", "black-spotted" },
        noun:   "ladybird",
        traits: new[] { "climbing a stem", "opening its shell to fly", "eating aphids" });
    public override List<Item> BuildCatchYield() => new() { new Carapace(), new Wing() };
}
