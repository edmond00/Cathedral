using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Narrative.World.Items;
using Cathedral.Game.Npc.Corpse;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>
/// Convenience base for simple shallow wildlife archetypes.
/// Subclass and supply a hint, type name, and a small corpse drop list.
/// </summary>
public abstract class GenericShallowArchetype : ShallowNpcArchetype
{
    /// <summary>One-line description of the corpse "body" PoI.</summary>
    protected abstract string CorpseBodyDescription { get; }

    /// <summary>Items dropped when this creature is killed (may be empty).</summary>
    protected abstract List<ItemElement> BuildCorpseDrops();

    public override CorpseSpot CreateCorpse(ShallowNpcEntity entity, Area area)
    {
        var bodyParts = new List<PointOfInterest>
        {
            new CorpseBodyPartPoI(
                "Body",
                new() { CorpseBodyDescription },
                BuildCorpseDrops()),
        };
        return CorpseRegistry.CreateForShallowNpc(
            entity, area,
            displayName:  $"Dead {TypeDisplayName}",
            descriptions: new() { $"The still body of a {TypeDisplayName.ToLowerInvariant()}" },
            bodyParts);
    }
}

// ── Small mammals ────────────────────────────────────────────────────────────

public class HareArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "hare";
    public override string TypeDisplayName => "Hare";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "lean", "long-eared" },
        colors: new[] { "brown", "grey-brown", "sandy" },
        noun:   "hare",
        traits: new[] { "frozen in the open, ready to bolt", "bounding away in great leaps", "ears flat, nose working the air" });
    protected override string CorpseBodyDescription => "the lean still body of a hare";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new RabbitMeat()),
        new ItemElement(new RabbitPelt()),
    };
}

public class SnowHareArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "snow_hare";
    public override string TypeDisplayName => "Snow Hare";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "lean", "thick-coated" },
        colors: new[] { "white", "snow-white", "pale grey" },
        noun:   "snow hare",
        traits: new[] { "motionless against the stone", "watching from a rock ledge", "all but lost against the snow" });
    protected override string CorpseBodyDescription => "the white-furred body of a snow hare";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new RabbitMeat()),
        new ItemElement(new RabbitPelt()),
    };
}

public class SquirrelArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "squirrel";
    public override string TypeDisplayName => "Squirrel";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "small", "quick" },
        colors: new[] { "russet", "red-brown", "grey" },
        noun:   "squirrel",
        traits: new[] { "racing up a trunk, tail flicking", "pausing to watch, tail curled", "darting along a branch" });
    protected override string CorpseBodyDescription => "the small russet body of a squirrel";
    protected override List<ItemElement> BuildCorpseDrops() => new();
}

public class WoodMouseArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "wood_mouse";
    public override string TypeDisplayName => "Wood Mouse";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "tiny", "small" },
        colors: new[] { "brown", "sandy", "grey-brown" },
        noun:   "wood mouse",
        traits: new[] { "darting under a fallen log", "nose twitching in the leaf litter", "gone before the eye can follow" });
    protected override string CorpseBodyDescription => "the tiny body of a wood mouse";
    protected override List<ItemElement> BuildCorpseDrops() => new();
}

public class MarmotArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "marmot";
    public override string TypeDisplayName => "Marmot";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "fat", "stout" },
        colors: new[] { "brown", "grizzled", "sandy" },
        noun:   "marmot",
        traits: new[] { "whistling a sharp warning from a flat rock", "sitting up on its haunches", "sunning itself on the stones" });
    protected override string CorpseBodyDescription => "the round still body of a marmot";
    protected override List<ItemElement> BuildCorpseDrops() => new();
}

public class BatArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "bat";
    public override string TypeDisplayName => "Bat";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "small", "leathery" },
        colors: new[] { "dark", "brown", "sooty" },
        noun:   "bat",
        traits: new[] { "wheeling overhead", "hanging folded in the dark", "flitting into a deeper pocket of shadow" });
    protected override string CorpseBodyDescription => "the leathery body of a bat";
    protected override List<ItemElement> BuildCorpseDrops() => new();
}

public class RatArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "rat";
    public override string TypeDisplayName => "Rat";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "long-tailed", "lean" },
        colors: new[] { "brown", "grey", "black" },
        noun:   "rat",
        traits: new[] { "scuttling along the wall", "eyes catching the lantern-light", "nosing at something in the dark" });
    protected override string CorpseBodyDescription => "the matted body of a rat";
    protected override List<ItemElement> BuildCorpseDrops() => new();
}

public class BadgerArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "badger";
    public override string TypeDisplayName => "Badger";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "heavy", "stocky" },
        colors: new[] { "striped", "grey", "grizzled" },
        noun:   "badger",
        traits: new[] { "snuffling at the leaf litter", "indifferent to the world", "lumbering toward its sett" });
    protected override string CorpseBodyDescription => "the heavy striped body of a badger";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new AnimalHide()),
    };
}

// ── Birds ───────────────────────────────────────────────────────────────────

public class CrowArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "crow";
    public override string TypeDisplayName => "Crow";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "sleek", "large" },
        colors: new[] { "black", "glossy black", "blue-black" },
        noun:   "crow",
        traits: new[] { "head cocked, one bright eye gleaming", "perched on a fence-post", "cawing once and settling" });
    protected override string CorpseBodyDescription => "the small black body of a crow";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
    };
}

public class LarkArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "lark";
    public override string TypeDisplayName => "Lark";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "small", "slight" },
        colors: new[] { "brown", "streaked brown", "dun" },
        noun:   "lark",
        traits: new[] { "spiralling up out of the grass", "song tumbling down behind it", "flitting low over the field" });
    protected override string CorpseBodyDescription => "the tiny body of a lark";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
    };
}

public class SparrowArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "sparrow";
    public override string TypeDisplayName => "Sparrow";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "small", "little" },
        colors: new[] { "brown", "dusty brown", "grey-brown" },
        noun:   "sparrow",
        traits: new[] { "flitting from one perch to another", "chirping in the hedge", "barely noticing you" });
    protected override string CorpseBodyDescription => "the small still body of a sparrow";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
    };
}

public class RobinArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "robin";
    public override string TypeDisplayName => "Robin";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "small", "round" },
        colors: new[] { "red-breasted", "russet-breasted" },
        noun:   "robin",
        traits: new[] { "tilting its head at you", "hopping down a branch", "bright-eyed and bold" });
    protected override string CorpseBodyDescription => "the small body of a robin";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
    };
}

public class WoodpeckerArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "woodpecker";
    public override string TypeDisplayName => "Woodpecker";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "small", "striking" },
        colors: new[] { "black-and-white", "green", "barred" },
        noun:   "woodpecker",
        traits: new[] { "hammering somewhere up the trunk", "clinging to the bark", "the sound skittering away through the wood" });
    protected override string CorpseBodyDescription => "the patterned body of a woodpecker";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
    };
}

public class OwlArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "owl";
    public override string TypeDisplayName => "Owl";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "large", "round-faced" },
        colors: new[] { "tawny", "pale", "barred" },
        noun:   "owl",
        traits: new[] { "watching from a high branch", "eyes round and patient", "turning its head slowly toward you" });
    protected override string CorpseBodyDescription => "the soft body of an owl";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
    };
}

public class EagleArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "eagle";
    public override string TypeDisplayName => "Eagle";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "great", "broad-winged" },
        colors: new[] { "dark", "golden-brown", "brown" },
        noun:   "eagle",
        traits: new[] { "wheeling high above", "casting a slow shadow across the rocks", "circling on still wings" });
    protected override string CorpseBodyDescription => "the great body of an eagle, wings outstretched";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new EagleFeather()),
        new ItemElement(new EagleFeather()),
        new ItemElement(new Feather()),
    };
}

public class RavenArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "raven";
    public override string TypeDisplayName => "Raven";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "large", "heavy" },
        colors: new[] { "glossy black", "blue-black", "black" },
        noun:   "raven",
        traits: new[] { "watching you with grave attention", "perched on a high stone", "croaking low" });
    protected override string CorpseBodyDescription => "the glossy black body of a raven";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
    };
}

public class SeagullArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "seagull";
    public override string TypeDisplayName => "Seagull";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "pale", "broad-winged" },
        colors: new[] { "white-grey", "white", "grey" },
        noun:   "seagull",
        traits: new[] { "crying somewhere overhead", "sliding across the sky", "wheeling over the water" });
    protected override string CorpseBodyDescription => "the white-grey body of a seagull";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
    };
}

public class HeronArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "heron";
    public override string TypeDisplayName => "Heron";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "tall", "long-necked" },
        colors: new[] { "grey", "blue-grey", "slate" },
        noun:   "heron",
        traits: new[] { "standing motionless at the water's edge", "neck folded in on itself", "stalking the shallows" });
    protected override string CorpseBodyDescription => "the long-necked body of a heron";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
    };
}

public class SandpiperArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "sandpiper";
    public override string TypeDisplayName => "Sandpiper";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "small", "slight" },
        colors: new[] { "brown", "mottled brown", "sandy" },
        noun:   "sandpiper",
        traits: new[] { "pelting along the water's edge", "legs a blur", "probing the wet sand" });
    protected override string CorpseBodyDescription => "the small body of a sandpiper";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
    };
}

// ── Reptiles, amphibians, water-life ─────────────────────────────────────────

public class FrogArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "frog";
    public override string TypeDisplayName => "Frog";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "small", "damp" },
        colors: new[] { "green", "olive", "brown" },
        noun:   "frog",
        traits: new[] { "blinking once and slipping into the water", "half-sunk in the shallows", "throat pulsing" });
    protected override string CorpseBodyDescription => "the small wet body of a frog";
    protected override List<ItemElement> BuildCorpseDrops() => new();
}

public class ToadArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "toad";
    public override string TypeDisplayName => "Toad";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "squat", "warty" },
        colors: new[] { "brown", "mud-brown", "grey" },
        noun:   "toad",
        traits: new[] { "half-buried in mud", "watching you with patient eyes", "unmoving among the roots" });
    protected override string CorpseBodyDescription => "the squat body of a toad";
    protected override List<ItemElement> BuildCorpseDrops() => new();
}

public class AdderArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "adder";
    public override string TypeDisplayName => "Adder";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "coiled", "lean" },
        colors: new[] { "grey", "olive-brown", "coppery" },
        noun:   "adder",
        traits: new[] { "coiled on a sun-warmed rock", "tongue flickering", "dark zigzag along its back" });
    protected override string CorpseBodyDescription => "the limp coiled body of an adder";
    protected override List<ItemElement> BuildCorpseDrops() => new();
}

public class CrabArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "crab";
    public override string TypeDisplayName => "Crab";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "small", "broad", "lean" },
        colors: new[] { "brown", "pale green", "mottled grey", "dull red" },
        noun:   "crab",
        traits: new[] { "claws raised in warning", "sidling through a tide-pool", "one claw held high", "scuttling over the wet stones" });
    protected override string CorpseBodyDescription => "the upturned shell of a crab";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Crab()),
    };
}

public class CaveSpiderArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "cave_spider";
    public override string TypeDisplayName => "Cave Spider";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "long-legged", "pale" },
        colors: new[] { "grey", "translucent", "dark" },
        noun:   "cave spider",
        traits: new[] { "rearing in the lantern-light", "palps working", "frozen on the rock" });
    protected override string CorpseBodyDescription => "the curled-leg body of a cave spider";
    protected override List<ItemElement> BuildCorpseDrops() => new();
}

// ── Larger livestock and beasts ──────────────────────────────────────────────

public class SheepArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "sheep";
    public override string TypeDisplayName => "Sheep";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "heavy", "woolly" },
        colors: new[] { "dirty-white", "soot-faced", "grey" },
        noun:   "sheep",
        traits: new[] { "cropping the grass beside the fence", "wholly absorbed in grazing", "chewing steadily" });
    protected override string CorpseBodyDescription => "the heavy fleece-covered body of a sheep";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Wool()),
        new ItemElement(new Wool()),
        new ItemElement(new AnimalHide()),
    };
}

public class CowArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "cow";
    public override string TypeDisplayName => "Cow";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "heavy", "broad" },
        colors: new[] { "red", "brown", "black-and-white" },
        noun:   "cow",
        traits: new[] { "staring with wet brown eyes, jaws moving", "chewing steadily", "swishing its tail at the flies" });
    protected override string CorpseBodyDescription => "the great red body of a dead cow";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new AnimalHide()),
        new ItemElement(new AnimalHide()),
    };
}

public class DeerArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "deer";
    public override string TypeDisplayName => "Deer";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "lean", "long-limbed" },
        colors: new[] { "tawny", "red-brown", "dun" },
        noun:   "deer",
        traits: new[] { "lifting its head, ears swivelling", "watching, ready to bolt", "grazing at the tree-line" });
    protected override string CorpseBodyDescription => "the long-limbed body of a deer";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new DeerHide()),
        new ItemElement(new RabbitMeat()),
        new ItemElement(new RabbitMeat()),
    };
}

public class MountainGoatArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "mountain_goat";
    public override string TypeDisplayName => "Mountain Goat";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "horned", "wiry" },
        colors: new[] { "white", "shaggy grey", "pale" },
        noun:   "mountain goat",
        traits: new[] { "balanced on a narrow ledge", "unconcerned by the drop", "picking its way across the rock" });
    protected override string CorpseBodyDescription => "the wiry body of a mountain goat";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new GoatHide()),
    };
}

public class LynxArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "lynx";
    public override string TypeDisplayName => "Lynx";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "lean", "tufted-eared" },
        colors: new[] { "spotted", "grey", "tawny" },
        noun:   "lynx",
        traits: new[] { "crouched on a high rock", "ears pricked forward", "watching with pale eyes" });
    protected override string CorpseBodyDescription => "the spotted body of a lynx";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new LynxPelt()),
    };
}

public class SealArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "seal";
    public override string TypeDisplayName => "Seal";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "sleek", "heavy" },
        colors: new[] { "dark", "grey", "mottled" },
        noun:   "seal",
        traits: new[] { "hauled out on a flat rock", "watching with liquid eyes", "slipping into the water" });
    protected override string CorpseBodyDescription => "the sleek dark body of a seal";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new SealPelt()),
    };
}
