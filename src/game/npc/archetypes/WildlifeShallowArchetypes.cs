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
    protected override string BuildObservationHint(string nodeContext)
        => "a long-eared hare freezes in the open, then bounds away in three great leaps";
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
    protected override string BuildObservationHint(string nodeContext)
        => "a white-furred snow hare watches from a rock ledge, motionless against the stone";
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
    protected override string BuildObservationHint(string nodeContext)
        => "a russet squirrel races up a trunk and pauses, tail flicking";
    protected override string CorpseBodyDescription => "the small russet body of a squirrel";
    protected override List<ItemElement> BuildCorpseDrops() => new();
}

public class WoodMouseArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "wood_mouse";
    public override string TypeDisplayName => "Wood Mouse";
    protected override string BuildObservationHint(string nodeContext)
        => "a wood mouse darts under a fallen log, gone before the eye can follow";
    protected override string CorpseBodyDescription => "the tiny body of a wood mouse";
    protected override List<ItemElement> BuildCorpseDrops() => new();
}

public class MarmotArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "marmot";
    public override string TypeDisplayName => "Marmot";
    protected override string BuildObservationHint(string nodeContext)
        => "a fat marmot whistles a sharp warning from a flat rock";
    protected override string CorpseBodyDescription => "the round still body of a marmot";
    protected override List<ItemElement> BuildCorpseDrops() => new();
}

public class BatArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "bat";
    public override string TypeDisplayName => "Bat";
    protected override string BuildObservationHint(string nodeContext)
        => "a leathery bat wheels overhead, vanishing into a deeper pocket of dark";
    protected override string CorpseBodyDescription => "the leathery body of a bat";
    protected override List<ItemElement> BuildCorpseDrops() => new();
}

public class RatArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "rat";
    public override string TypeDisplayName => "Rat";
    protected override string BuildObservationHint(string nodeContext)
        => "a long-tailed rat scuttles along the wall, eyes catching the lantern-light";
    protected override string CorpseBodyDescription => "the matted body of a rat";
    protected override List<ItemElement> BuildCorpseDrops() => new();
}

public class BadgerArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "badger";
    public override string TypeDisplayName => "Badger";
    protected override string BuildObservationHint(string nodeContext)
        => "a striped badger snuffles at the leaf litter, paying no attention to you";
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
    protected override string BuildObservationHint(string nodeContext)
        => "a black crow perches on a fence-post, sizing you up with one bright eye";
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
    protected override string BuildObservationHint(string nodeContext)
        => "a lark spirals up out of the grass, song tumbling down behind it";
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
    protected override string BuildObservationHint(string nodeContext)
        => "a small sparrow flits from one perch to another, barely noticing you";
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
    protected override string BuildObservationHint(string nodeContext)
        => "a red-breasted robin hops down a branch and tilts its head at you";
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
    protected override string BuildObservationHint(string nodeContext)
        => "a woodpecker hammers somewhere up the trunk, the sound skittering away through the wood";
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
    protected override string BuildObservationHint(string nodeContext)
        => "an owl regards you from a high branch, eyes round and patient";
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
    protected override string BuildObservationHint(string nodeContext)
        => "an eagle wheels high above, casting a slow shadow across the rocks";
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
    protected override string BuildObservationHint(string nodeContext)
        => "a glossy raven perches on a high stone, watching you with grave attention";
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
    protected override string BuildObservationHint(string nodeContext)
        => "a seagull cries somewhere overhead, a long pale shape sliding across the sky";
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
    protected override string BuildObservationHint(string nodeContext)
        => "a grey heron stands motionless at the water's edge, neck folded in on itself";
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
    protected override string BuildObservationHint(string nodeContext)
        => "a sandpiper pelts along the water's edge, legs a brown blur";
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
    protected override string BuildObservationHint(string nodeContext)
        => "a damp green frog blinks once and slips into the water";
    protected override string CorpseBodyDescription => "the small wet body of a frog";
    protected override List<ItemElement> BuildCorpseDrops() => new();
}

public class ToadArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "toad";
    public override string TypeDisplayName => "Toad";
    protected override string BuildObservationHint(string nodeContext)
        => "a warty toad sits half-buried in mud, watching you with patient eyes";
    protected override string CorpseBodyDescription => "the squat body of a toad";
    protected override List<ItemElement> BuildCorpseDrops() => new();
}

public class AdderArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "adder";
    public override string TypeDisplayName => "Adder";
    public override bool   DefaultHostile  => true;
    protected override string BuildObservationHint(string nodeContext)
        => "an adder coils on a sun-warmed rock, zigzag pattern dark along its back";
    protected override string CorpseBodyDescription => "the limp coiled body of an adder";
    protected override List<ItemElement> BuildCorpseDrops() => new();
}

public class CrabArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "crab";
    public override string TypeDisplayName => "Crab";
    protected override string BuildObservationHint(string nodeContext)
        => "a brown crab sidles through a tide-pool, claws raised in warning";
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
    public override bool   DefaultHostile  => true;
    protected override string BuildObservationHint(string nodeContext)
        => "a long-legged cave spider rears in the lantern-light, palps working";
    protected override string CorpseBodyDescription => "the curled-leg body of a cave spider";
    protected override List<ItemElement> BuildCorpseDrops() => new();
}

// ── Larger livestock and beasts ──────────────────────────────────────────────

public class SheepArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "sheep";
    public override string TypeDisplayName => "Sheep";
    protected override string BuildObservationHint(string nodeContext)
        => "a soot-faced sheep crops grass beside the fence, paying you no attention";
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
    protected override string BuildObservationHint(string nodeContext)
        => "a heavy red cow stares at you with wet brown eyes, jaws moving";
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
    protected override string BuildObservationHint(string nodeContext)
        => "a tawny deer lifts its head from the grass and watches, ears swivelling";
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
    protected override string BuildObservationHint(string nodeContext)
        => "a horned mountain goat balances on a narrow ledge, unconcerned by the drop";
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
    public override bool   DefaultHostile  => true;
    protected override string BuildObservationHint(string nodeContext)
        => "a spotted lynx crouches on a high rock, tufted ears pricked forward";
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
    protected override string BuildObservationHint(string nodeContext)
        => "a dark seal hauls itself onto a flat rock, watching you with liquid eyes";
    protected override string CorpseBodyDescription => "the sleek dark body of a seal";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new SealPelt()),
    };
}
