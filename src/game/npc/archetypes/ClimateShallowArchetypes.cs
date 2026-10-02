using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Npc.Archetypes;

// ─────────────────────────────────────────────────────────────────────────────
//  The shallow life of the hot and cold country: everything there that does not fight. Built on the
//  same four bases as the temperate kind (bird, small mammal, generic, tiny), so each is examined,
//  heard and smelled — or caught and crushed — exactly as a lark or a beetle is. The parts a body
//  yields are the shared body-part vocabulary; a gazelle is a gazelle because of which parts and how
//  many, not because its meat is called gazelle meat.
// ─────────────────────────────────────────────────────────────────────────────

// ── Desert ──────────────────────────────────────────────────────────────────

public class FennecArchetype : SmallMammalShallowArchetype
{
    public override string ArchetypeId     => "fennec";
    public override string TypeDisplayName => "Fennec";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "small", "huge-eared" },
        colors: new[] { "sand-coloured", "cream", "pale gold" },
        noun:   "fennec",
        traits: new[] { "ears turning toward every sound", "trotting along a dune crest", "peering from a burrow mouth" });
    protected override string CorpseBodyDescription => "the tiny sand-coloured body of a fennec, the great ears flat";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Pelt()),
        new ItemElement(new Meat()),
        new ItemElement(new Tail()),
        new ItemElement(new Bone()),
    };
}

public class JerboaArchetype : SmallMammalShallowArchetype
{
    public override string ArchetypeId     => "jerboa";
    public override string TypeDisplayName => "Jerboa";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "tiny", "long-legged" },
        colors: new[] { "sandy", "buff", "pale" },
        noun:   "jerboa",
        traits: new[] { "hopping off on its hind legs like a bird", "tail held out behind it for balance", "vanishing into the sand" });
    protected override string CorpseBodyDescription => "the tiny long-legged body of a jerboa";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Skin()),
        new ItemElement(new Meat()),
        new ItemElement(new Tail()),
        new ItemElement(new Bone()),
    };
}

public class SandViperArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "sand_viper";
    public override string TypeDisplayName => "Sand Viper";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "horned", "thick-bodied" },
        colors: new[] { "sand-coloured", "pale", "speckled" },
        noun:   "sand viper",
        traits: new[] { "half-buried with only its eyes showing", "sidewinding away across the slope", "coiled in the shade of a stone" });
    protected override string CorpseBodyDescription => "the thick pale body of a sand viper, the little horns over its eyes";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Skin()),
        new ItemElement(new Skin()),
        new ItemElement(new Meat()),
        new ItemElement(new Fang()),
        new ItemElement(new Fang()),
    };
}

public class VultureArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "vulture";
    public override string TypeDisplayName => "Vulture";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "great", "bald-headed" },
        colors: new[] { "dun", "dark brown", "dirty white" },
        noun:   "vulture",
        traits: new[] { "circling high on still wings", "hunched on a rock, waiting", "tearing at something out of sight" });
    protected override string CorpseBodyDescription => "the great untidy body of a vulture, wings spread in the dust";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Plume()),
        new ItemElement(new Plume()),
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Meat()),
        new ItemElement(new Claw()),
        new ItemElement(new Claw()),
    };
}

public class SandgrouseArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "sandgrouse";
    public override string TypeDisplayName => "Sandgrouse";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "plump", "small-headed" },
        colors: new[] { "sandy", "barred", "buff" },
        noun:   "sandgrouse",
        traits: new[] { "whirring low over the ground", "calling as it flies to water", "crouched so still it is nearly the ground" });
    protected override string CorpseBodyDescription => "the plump barred body of a sandgrouse";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Meat()),
        new ItemElement(new Bone()),
    };
}

public class DromedaryArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "dromedary";
    public override string TypeDisplayName => "Dromedary";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "one-humped", "long-legged" },
        colors: new[] { "tawny", "dun", "pale" },
        noun:   "dromedary",
        traits: new[] { "chewing with an air of contempt", "folding down onto its knees", "plodding past, unbothered by the heat" });
    protected override string CorpseBodyDescription => "the huge folded body of a dromedary, the hump slumped to one side";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Hide()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Suet()),
        new ItemElement(new Bone()),
        new ItemElement(new Hair()),
    };
}

public class ScorpionArchetype : TinyShallowArchetype
{
    public override string ArchetypeId     => "scorpion";
    public override string TypeDisplayName => "Scorpion";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "small", "finger-long", "fat-tailed" },
        colors: new[] { "straw-yellow", "black", "amber" },
        noun:   "scorpion",
        traits: new[] { "tail curled up over its back", "backing into a crack under a stone", "claws raised and open" });
    public override List<Item> BuildCatchYield() => new() { new Carapace(), new Sting() };
}

public class DungBeetleArchetype : TinyShallowArchetype
{
    public override string ArchetypeId     => "dung_beetle";
    public override string TypeDisplayName => "Dung Beetle";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "round", "thumb-sized" },
        colors: new[] { "black", "bronze-black" },
        noun:   "dung beetle",
        traits: new[] { "rolling a ball of dung backwards, head down", "climbing its ball and toppling off", "digging furiously" });
    public override List<Item> BuildCatchYield() => new() { new Carapace(), new Carapace() };
}

public class LocustArchetype : TinyShallowArchetype
{
    public override string ArchetypeId     => "locust";
    public override string TypeDisplayName => "Locust";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "long", "heavy-bodied" },
        colors: new[] { "sand-coloured", "yellow-banded", "brown" },
        noun:   "locust",
        traits: new[] { "clattering off on stiff wings", "clinging to a dry stem", "chewing steadily" });
    public override List<Item> BuildCatchYield() => new() { new Wing(), new Wing(), new Grub() };
}

// ── Hot steppe ──────────────────────────────────────────────────────────────

public class GazelleArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "gazelle";
    public override string TypeDisplayName => "Gazelle";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "slender", "ring-horned" },
        colors: new[] { "fawn", "sandy", "white-bellied" },
        noun:   "gazelle",
        traits: new[] { "springing away stiff-legged", "grazing with one ear always turned to you", "standing frozen at the edge of the herd" });
    protected override string CorpseBodyDescription => "the slender body of a gazelle, legs folded beneath it";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Hide()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Horn()),
        new ItemElement(new Horn()),
        new ItemElement(new Liver()),
    };
}

public class ZebraArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "zebra";
    public override string TypeDisplayName => "Zebra";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "stocky", "short-maned" },
        colors: new[] { "striped", "black-and-white", "dusty" },
        noun:   "zebra",
        traits: new[] { "switching its tail at the flies", "barking an alarm to the others", "rolling in the dust" });
    protected override string CorpseBodyDescription => "the striped barrel of a dead zebra";
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

public class OstrichArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "ostrich";
    public override string TypeDisplayName => "Ostrich";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "enormous", "long-necked" },
        colors: new[] { "black-plumed", "grey", "dusty" },
        noun:   "ostrich",
        traits: new[] { "running off with great springing strides", "staring down its beak at you", "spreading its wings in a threat" });
    protected override string CorpseBodyDescription => "the enormous feathered heap of an ostrich, its neck folded";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Plume()),
        new ItemElement(new Plume()),
        new ItemElement(new Plume()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Claw()),
    };
}

public class MeerkatArchetype : SmallMammalShallowArchetype
{
    public override string ArchetypeId     => "meerkat";
    public override string TypeDisplayName => "Meerkat";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "small", "upright" },
        colors: new[] { "sandy", "grizzled", "dun" },
        noun:   "meerkat",
        traits: new[] { "standing sentry on a mound", "yapping an alarm", "diving headfirst into a burrow" });
    protected override string CorpseBodyDescription => "the small grizzled body of a meerkat";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Pelt()),
        new ItemElement(new Meat()),
        new ItemElement(new Tail()),
        new ItemElement(new Bone()),
    };
}

public class WeaverBirdArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "weaver_bird";
    public override string TypeDisplayName => "Weaver Bird";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "small", "busy" },
        colors: new[] { "yellow", "black-masked", "golden" },
        noun:   "weaver bird",
        traits: new[] { "hanging upside down from its nest", "stripping a grass blade", "chattering in the thorn tree" });
    protected override string CorpseBodyDescription => "the tiny yellow body of a weaver bird";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Meat()),
        new ItemElement(new Bone()),
    };
}

public class TortoiseArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "tortoise";
    public override string TypeDisplayName => "Tortoise";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "old", "heavy-shelled" },
        colors: new[] { "dun", "ochre-plated", "dusty" },
        noun:   "tortoise",
        traits: new[] { "pulling its head in at your approach", "chewing a dry leaf very slowly", "plodding toward the shade" });
    protected override string CorpseBodyDescription => "the empty-looking shell of a tortoise, the head drawn in for good";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Carapace()),
        new ItemElement(new Carapace()),
        new ItemElement(new Meat()),
        new ItemElement(new Bone()),
    };
}

public class TermiteArchetype : TinyShallowArchetype
{
    public override string ArchetypeId     => "termite";
    public override string TypeDisplayName => "Termite";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "tiny", "soft-bodied" },
        colors: new[] { "pale", "straw", "cream" },
        noun:   "termite",
        traits: new[] { "streaming out of a crack in the mound", "carrying a grain of earth", "milling in the gallery mouth" });
    public override List<Item> BuildCatchYield() => new() { new Grub(), new Wing() };
}

// ── Jungle ──────────────────────────────────────────────────────────────────

public class MonkeyArchetype : SmallMammalShallowArchetype
{
    public override string ArchetypeId     => "monkey";
    public override string TypeDisplayName => "Monkey";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "small", "long-tailed" },
        colors: new[] { "brown", "black-capped", "grizzled" },
        noun:   "monkey",
        traits: new[] { "swinging away through the branches", "shrieking at you from above", "picking at a fruit, watching you" });
    protected override string CorpseBodyDescription => "the small, dreadfully human body of a monkey";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Pelt()),
        new ItemElement(new Meat()),
        new ItemElement(new Tail()),
        new ItemElement(new Bone()),
        new ItemElement(new Liver()),
    };
}

public class ParrotArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "parrot";
    public override string TypeDisplayName => "Parrot";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "loud", "heavy-billed" },
        colors: new[] { "scarlet", "green", "blue-and-gold" },
        noun:   "parrot",
        traits: new[] { "screeching from the canopy", "climbing beak over claw along a branch", "cracking a nut" });
    protected override string CorpseBodyDescription => "the bright crumpled body of a parrot";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Plume()),
        new ItemElement(new Plume()),
        new ItemElement(new Feather()),
        new ItemElement(new Meat()),
        new ItemElement(new Bone()),
    };
}

public class ToucanArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "toucan";
    public override string TypeDisplayName => "Toucan";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "black", "huge-billed" },
        colors: new[] { "black-and-yellow", "orange-billed" },
        noun:   "toucan",
        traits: new[] { "tossing a berry up and catching it", "hopping heavily along a bough", "croaking from the high branches" });
    protected override string CorpseBodyDescription => "the black body of a toucan, the great bill strangely light";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Meat()),
        new ItemElement(new Bone()),
    };
}

public class HummingbirdArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "hummingbird";
    public override string TypeDisplayName => "Hummingbird";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "tiny", "needle-billed" },
        colors: new[] { "emerald", "ruby-throated", "glittering" },
        noun:   "hummingbird",
        traits: new[] { "hanging in the air at a flower", "gone with a hum", "perched on a twig, pulsing" });
    protected override string CorpseBodyDescription => "the tiny glittering body of a hummingbird";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Bone()),
    };
}

public class PythonArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "python";
    public override string TypeDisplayName => "Python";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "vast", "thick-bodied" },
        colors: new[] { "olive", "mottled brown", "dark-patterned" },
        noun:   "python",
        traits: new[] { "draped along a low branch", "sliding into the water", "coiled around something no longer moving" });
    protected override string CorpseBodyDescription => "the vast patterned length of a python, slack at last";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Skin()),
        new ItemElement(new Skin()),
        new ItemElement(new Skin()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Fang()),
    };
}

public class TapirArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "tapir";
    public override string TypeDisplayName => "Tapir";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "heavy", "snouted" },
        colors: new[] { "dark", "black-and-white", "brown" },
        noun:   "tapir",
        traits: new[] { "browsing with its little trunk", "crashing off through the undergrowth", "wallowing in the shallows" });
    protected override string CorpseBodyDescription => "the heavy dark body of a tapir";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Hide()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Suet()),
        new ItemElement(new Bone()),
    };
}

public class TreeFrogArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "tree_frog";
    public override string TypeDisplayName => "Tree Frog";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "small", "sticky-toed" },
        colors: new[] { "bright green", "red-eyed", "poison-blue" },
        noun:   "tree frog",
        traits: new[] { "clinging to the underside of a leaf", "throat bubbling as it calls", "leaping from leaf to leaf" });
    protected override string CorpseBodyDescription => "the small bright body of a tree frog";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Skin()),
        new ItemElement(new Meat()),
        new ItemElement(new Bone()),
    };
}

public class CapybaraArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "capybara";
    public override string TypeDisplayName => "Capybara";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "huge", "blunt-nosed" },
        colors: new[] { "brown", "reddish", "grizzled" },
        noun:   "capybara",
        traits: new[] { "sitting in the shallows up to its eyes", "grazing at the water's edge", "lying in the mud with others" });
    protected override string CorpseBodyDescription => "the barrel-shaped body of a capybara";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Hide()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Suet()),
        new ItemElement(new Bone()),
    };
}

public class LeechArchetype : TinyShallowArchetype
{
    public override string ArchetypeId     => "leech";
    public override string TypeDisplayName => "Leech";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "slick", "finger-long" },
        colors: new[] { "black", "olive-striped", "brown" },
        noun:   "leech",
        traits: new[] { "looping across a wet leaf toward you", "swimming in long undulations", "waving one end in the air" });
    public override List<Item> BuildCatchYield() => new() { new Grub(), new Grub() };
}

public class ArmyAntArchetype : TinyShallowArchetype
{
    public override string ArchetypeId     => "army_ant";
    public override string TypeDisplayName => "Army Ant";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "tiny", "large-jawed" },
        colors: new[] { "red-brown", "black", "rust" },
        noun:   "army ant",
        traits: new[] { "one of a river of thousands", "carrying a scrap of something", "mandibles open at your boot" });
    public override List<Item> BuildCatchYield() => new() { new Carapace(), new Grub() };
}

public class FireflyArchetype : TinyShallowArchetype
{
    public override string ArchetypeId     => "firefly";
    public override string TypeDisplayName => "Firefly";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "tiny", "soft-winged" },
        colors: new[] { "dull brown", "green-lit" },
        noun:   "firefly",
        traits: new[] { "blinking in the shade", "drifting in slow lit loops", "settled on a leaf, glowing" });
    public override List<Item> BuildCatchYield() => new() { new Wing(), new Wing() };
}

// ── Canyon ──────────────────────────────────────────────────────────────────

public class BighornArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "bighorn";
    public override string TypeDisplayName => "Bighorn";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "heavy-horned", "sure-footed" },
        colors: new[] { "brown", "grey-brown", "white-rumped" },
        noun:   "bighorn",
        traits: new[] { "standing on a ledge no wider than its hooves", "clashing horns somewhere above", "watching from the rim" });
    protected override string CorpseBodyDescription => "the heavy body of a bighorn, the coiled horns gouged in the scree";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Hide()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Horn()),
        new ItemElement(new Horn()),
        new ItemElement(new Liver()),
    };
}

public class CondorArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "condor";
    public override string TypeDisplayName => "Condor";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "huge", "white-ruffed" },
        colors: new[] { "black", "sooty" },
        noun:   "condor",
        traits: new[] { "soaring along the canyon wall without a wingbeat", "perched on a crag, ruff puffed", "dropping out of the sky" });
    protected override string CorpseBodyDescription => "the vast black body of a condor";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Plume()),
        new ItemElement(new Plume()),
        new ItemElement(new Plume()),
        new ItemElement(new Feather()),
        new ItemElement(new Meat()),
        new ItemElement(new Claw()),
        new ItemElement(new Claw()),
    };
}

public class CanyonWrenArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "canyon_wren";
    public override string TypeDisplayName => "Canyon Wren";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "small", "long-billed" },
        colors: new[] { "rufous", "white-throated" },
        noun:   "canyon wren",
        traits: new[] { "pouring a falling song down the rock", "creeping into a crevice", "bobbing on a ledge" });
    protected override string CorpseBodyDescription => "the tiny rufous body of a canyon wren";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Bone()),
    };
}

public class RockDoveArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "rock_dove";
    public override string TypeDisplayName => "Rock Dove";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "plump", "small-headed" },
        colors: new[] { "blue-grey", "iridescent" },
        noun:   "rock dove",
        traits: new[] { "clattering out of a ledge", "cooing in an alcove", "strutting along the rim" });
    protected override string CorpseBodyDescription => "the plump grey body of a rock dove";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Meat()),
        new ItemElement(new Bone()),
    };
}

public class SwiftArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "swift";
    public override string TypeDisplayName => "Swift";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "sickle-winged", "small" },
        colors: new[] { "sooty", "dark brown" },
        noun:   "swift",
        traits: new[] { "screaming down the canyon in a band", "flickering against the rock", "gone into a crack in the wall" });
    protected override string CorpseBodyDescription => "the sickle-winged body of a swift";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Bone()),
    };
}

public class RattlesnakeArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "rattlesnake";
    public override string TypeDisplayName => "Rattlesnake";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "thick", "diamond-backed" },
        colors: new[] { "dun", "sandy", "grey-banded" },
        noun:   "rattlesnake",
        traits: new[] { "rattling a warning from under a ledge", "coiled in the sun, tail up", "sliding off between stones" });
    protected override string CorpseBodyDescription => "the limp banded body of a rattlesnake, the rattle still";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Skin()),
        new ItemElement(new Skin()),
        new ItemElement(new Meat()),
        new ItemElement(new Fang()),
        new ItemElement(new Fang()),
        new ItemElement(new Tail()),
    };
}

public class RingtailArchetype : SmallMammalShallowArchetype
{
    public override string ArchetypeId     => "ringtail";
    public override string TypeDisplayName => "Ringtail";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "slender", "big-eyed" },
        colors: new[] { "buff", "grey", "banded-tailed" },
        noun:   "ringtail",
        traits: new[] { "peering out of a crevice", "bounding along a ledge", "curled in an alcove" });
    protected override string CorpseBodyDescription => "the slender body of a ringtail, the banded tail across it";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Pelt()),
        new ItemElement(new Meat()),
        new ItemElement(new Tail()),
        new ItemElement(new Bone()),
    };
}

public class GeckoArchetype : TinyShallowArchetype
{
    public override string ArchetypeId     => "gecko";
    public override string TypeDisplayName => "Gecko";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "small", "flat-toed" },
        colors: new[] { "pale", "translucent", "speckled" },
        noun:   "gecko",
        traits: new[] { "stuck to the underside of a ledge", "chirping from a crack", "running straight up the rock" });
    public override List<Item> BuildCatchYield() => new() { new Tail(), new Skin() };
}

// ── Sea ice ─────────────────────────────────────────────────────────────────

public class WalrusArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "walrus";
    public override string TypeDisplayName => "Walrus";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "huge", "tusked" },
        colors: new[] { "pink-brown", "wrinkled", "grey" },
        noun:   "walrus",
        traits: new[] { "hauled out on the ice with its fellows", "levering itself along on its tusks", "bellowing at the water" });
    protected override string CorpseBodyDescription => "the vast wrinkled bulk of a walrus, tusks driven into the ice";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Hide()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Suet()),
        new ItemElement(new Tusk()),
        new ItemElement(new Tusk()),
    };
}

public class ArcticFoxArchetype : SmallMammalShallowArchetype
{
    public override string ArchetypeId     => "arctic_fox";
    public override string TypeDisplayName => "Arctic Fox";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "small", "thick-furred" },
        colors: new[] { "white", "blue-grey", "smoke" },
        noun:   "arctic fox",
        traits: new[] { "trotting after something it smells", "pouncing on the snow", "curled with its tail over its nose" });
    protected override string CorpseBodyDescription => "the small white body of an arctic fox";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Pelt()),
        new ItemElement(new Meat()),
        new ItemElement(new Tail()),
        new ItemElement(new Bone()),
    };
}

public class SkuaArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "skua";
    public override string TypeDisplayName => "Skua";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "heavy", "hook-billed" },
        colors: new[] { "dark brown", "mottled" },
        noun:   "skua",
        traits: new[] { "harrying a gull for its catch", "standing on the ice, watching", "diving at you with a harsh cry" });
    protected override string CorpseBodyDescription => "the dark heavy body of a skua";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Meat()),
        new ItemElement(new Claw()),
    };
}

public class ArcticTernArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "arctic_tern";
    public override string TypeDisplayName => "Arctic Tern";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "slender", "fork-tailed" },
        colors: new[] { "white", "black-capped", "pale grey" },
        noun:   "arctic tern",
        traits: new[] { "hovering over the open water", "plunging into a lead", "screaming at an intruder" });
    protected override string CorpseBodyDescription => "the slender white body of a tern";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Meat()),
        new ItemElement(new Bone()),
    };
}

public class PuffinArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "puffin";
    public override string TypeDisplayName => "Puffin";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "stubby", "bright-billed" },
        colors: new[] { "black-and-white", "orange-footed" },
        noun:   "puffin",
        traits: new[] { "whirring past low over the water", "standing at the ice edge", "with a beak full of little fish" });
    protected override string CorpseBodyDescription => "the stubby body of a puffin, the bright bill already dulling";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Meat()),
        new ItemElement(new Bone()),
    };
}

// ── Glacier ─────────────────────────────────────────────────────────────────

public class IceWormArchetype : TinyShallowArchetype
{
    public override string ArchetypeId     => "ice_worm";
    public override string TypeDisplayName => "Ice Worm";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "thread-thin", "tiny" },
        colors: new[] { "black", "dark" },
        noun:   "ice worm",
        traits: new[] { "writhing on the wet ice", "sinking back into the ice as the light hits it", "one of a dark scatter on the surface" });
    public override List<Item> BuildCatchYield() => new() { new Grub(), new Grub() };
}

public class SnowFleaArchetype : TinyShallowArchetype
{
    public override string ArchetypeId     => "snow_flea";
    public override string TypeDisplayName => "Snow Flea";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "minute", "springing" },
        colors: new[] { "blue-black", "sooty" },
        noun:   "snow flea",
        traits: new[] { "peppering the snow like soot", "flicking away at a breath", "one of thousands in a footprint" });
    public override List<Item> BuildCatchYield() => new() { new Carapace(), new Carapace() };
}

public class PtarmiganArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "ptarmigan";
    public override string TypeDisplayName => "Ptarmigan";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "plump", "feather-footed" },
        colors: new[] { "white", "speckled grey", "mottled" },
        noun:   "ptarmigan",
        traits: new[] { "crouched in the snow, invisible until it moves", "croaking as it whirs away", "burrowed into a drift" });
    protected override string CorpseBodyDescription => "the plump white body of a ptarmigan";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Meat()),
        new ItemElement(new Bone()),
    };
}

public class SnowyOwlArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "snowy_owl";
    public override string TypeDisplayName => "Snowy Owl";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "great", "round-headed" },
        colors: new[] { "white", "barred white" },
        noun:   "snowy owl",
        traits: new[] { "sitting on a rock, golden eyes half shut", "gliding low and silent", "turning its head right round toward you" });
    protected override string CorpseBodyDescription => "the soft white body of a snowy owl";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Plume()),
        new ItemElement(new Plume()),
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Meat()),
        new ItemElement(new Claw()),
        new ItemElement(new Claw()),
    };
}

// ── Snowfield ───────────────────────────────────────────────────────────────

public class ErmineArchetype : SmallMammalShallowArchetype
{
    public override string ArchetypeId     => "ermine";
    public override string TypeDisplayName => "Ermine";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "sinuous", "small" },
        colors: new[] { "white", "black-tipped" },
        noun:   "ermine",
        traits: new[] { "bounding across the snow", "standing up to look", "pouring itself into a hole" });
    protected override string CorpseBodyDescription => "the sinuous white body of an ermine, the black tail-tip vivid";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Pelt()),
        new ItemElement(new Meat()),
        new ItemElement(new Tail()),
        new ItemElement(new Bone()),
    };
}

public class PikaArchetype : SmallMammalShallowArchetype
{
    public override string ArchetypeId     => "pika";
    public override string TypeDisplayName => "Pika";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "round", "tiny-eared" },
        colors: new[] { "grey", "brown", "buff" },
        noun:   "pika",
        traits: new[] { "whistling from the rocks", "carrying a mouthful of grass", "sitting hunched on a stone" });
    protected override string CorpseBodyDescription => "the round small body of a pika";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Skin()),
        new ItemElement(new Meat()),
        new ItemElement(new Bone()),
    };
}

// ── Cold steppe ─────────────────────────────────────────────────────────────

public class ReindeerArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "reindeer";
    public override string TypeDisplayName => "Reindeer";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "antlered", "heavy-coated" },
        colors: new[] { "grey-brown", "pale", "white-necked" },
        noun:   "reindeer",
        traits: new[] { "scraping through the snow for moss", "clicking as it walks", "moving with the herd" });
    protected override string CorpseBodyDescription => "the heavy-coated body of a reindeer, antlers in the moss";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Hide()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Antler()),
        new ItemElement(new Antler()),
        new ItemElement(new Liver()),
        new ItemElement(new Suet()),
    };
}

public class MuskOxArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "musk_ox";
    public override string TypeDisplayName => "Musk Ox";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "massive", "shaggy" },
        colors: new[] { "dark brown", "black", "grizzled" },
        noun:   "musk ox",
        traits: new[] { "standing in a ring with the others, horns out", "skirt of hair blowing", "pawing the snow" });
    protected override string CorpseBodyDescription => "the shaggy mountain of a dead musk ox";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Hide()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Horn()),
        new ItemElement(new Horn()),
        new ItemElement(new Hair()),
    };
}

public class SaigaArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "saiga";
    public override string TypeDisplayName => "Saiga";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "odd-nosed", "slender" },
        colors: new[] { "sandy", "pale", "grey" },
        noun:   "saiga",
        traits: new[] { "running with its swollen nose low", "standing in the wind", "grazing among the tussocks" });
    protected override string CorpseBodyDescription => "the slender body of a saiga, the strange nose";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Hide()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Horn()),
        new ItemElement(new Liver()),
    };
}

public class LemmingArchetype : SmallMammalShallowArchetype
{
    public override string ArchetypeId     => "lemming";
    public override string TypeDisplayName => "Lemming";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "fat", "tiny" },
        colors: new[] { "brown", "chestnut", "mottled" },
        noun:   "lemming",
        traits: new[] { "squeaking furiously from a tussock", "running along a runway in the moss", "vanishing down a hole" });
    protected override string CorpseBodyDescription => "the fat small body of a lemming";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Skin()),
        new ItemElement(new Meat()),
        new ItemElement(new Bone()),
    };
}

public class SnowBuntingArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "snow_bunting";
    public override string TypeDisplayName => "Snow Bunting";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "small", "stout-billed" },
        colors: new[] { "white", "black-and-white", "tawny" },
        noun:   "snow bunting",
        traits: new[] { "flickering up in a flock", "picking seeds from the snow", "singing from a stone" });
    protected override string CorpseBodyDescription => "the small pied body of a snow bunting";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Bone()),
    };
}

public class CraneArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "crane";
    public override string TypeDisplayName => "Crane";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "tall", "long-legged" },
        colors: new[] { "grey", "red-crowned", "ash" },
        noun:   "crane",
        traits: new[] { "bugling from the thaw pools", "stalking the shallows", "dancing with wings half open" });
    protected override string CorpseBodyDescription => "the tall grey body of a crane";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Plume()),
        new ItemElement(new Plume()),
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Meat()),
        new ItemElement(new Bone()),
    };
}

public class MosquitoArchetype : TinyShallowArchetype
{
    /// <summary>One of the tiny things with a voice: audible as well as visible.</summary>
    public override SensoryProfile Senses => new(Examine: true, Contemplate: true, Listen: true);

    public override string ArchetypeId     => "mosquito";
    public override string TypeDisplayName => "Mosquito";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "thin", "whining" },
        colors: new[] { "brown", "grey", "blood-dark" },
        noun:   "mosquito",
        traits: new[] { "whining at your ear", "one of a cloud over the pools", "settled on your wrist" });
    public override List<Item> BuildCatchYield() => new() { new Wing(), new Wing() };
}

public class BumblebeeArchetype : TinyShallowArchetype
{
    /// <summary>One of the tiny things with a voice: audible as well as visible.</summary>
    public override SensoryProfile Senses => new(Examine: true, Contemplate: true, Listen: true);

    public override string ArchetypeId     => "bumblebee";
    public override string TypeDisplayName => "Bumblebee";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "fat", "furry" },
        colors: new[] { "black-and-orange", "banded" },
        noun:   "bumblebee",
        traits: new[] { "bumbling from flower to flower", "droning past low", "warming itself on a stone" });
    public override List<Item> BuildCatchYield() => new() { new Wing(), new Sting(), new Wax() };
}
