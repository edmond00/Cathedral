using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Narrative.World.Items;   // Shell — the snail leaves the same object the tideline does
using Cathedral.Game.Npc.Corpse;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>
/// The small life that ought to be underfoot everywhere and until now was nowhere: insects, snails,
/// spiders, mice, lizards. Every one of them is a <see cref="ShallowNpcArchetype.IsTiny"/> creature,
/// so the verbs offered on them are <c>catch</c> and <c>crush</c> rather than <c>attack</c> and
/// <c>slay</c> — a distinction that costs nothing mechanically and says a great deal in play.
///
/// <para>They also cost almost nothing to place, which matters: a village with three people in it
/// and no insects reads as a stage set. These are the cheapest possible way to make a scene feel
/// inhabited, and because they are shallow they carry no anatomy, no affinity and no dialogue.</para>
///
/// <para>Crushing one leaves nothing. Catching one yields whatever
/// <see cref="ShallowNpcArchetype.BuildCatchYield"/> gives — <b>two parts, or three</b>, every one
/// of insignificant weight. Two is the floor because a single wing off a butterfly is a reward that
/// reads as a rounding error against the difficulty 3 the catch costs; three is the ceiling because
/// nothing this size has more than three parts worth naming, and the whole yield is granted at once
/// by <c>CatchVerb</c> rather than cut out one at a time the way a carcass is.</para>
/// </summary>
public abstract class TinyShallowArchetype : ShallowNpcArchetype
{
    public override bool IsTiny => true;

    /// <summary>
    /// Small enough that a close look and a long look are all it offers. The two that make a noise
    /// (cricket, bee) widen this themselves; nothing this size is worth smelling.
    /// </summary>
    public override SensoryProfile Senses => new(Examine: true, Contemplate: true);

    /// <summary>The naturalist's lessons rather than the object ones: this is a life, not a mechanism.</summary>
    public override System.Collections.Generic.IReadOnlyDictionary<string, string>? VerbModiMentis
        => TinyCreatureSenses;

    /// <summary><see cref="NpcArchetype.CreatureSenses"/> without the smell, which nothing this small has.</summary>
    private static readonly System.Collections.Generic.Dictionary<string, string> TinyCreatureSenses = new()
    {
        ["examine"]     = "creature_lore",
        ["contemplate"] = "fellow_feeling",
    };

    /// <summary>
    /// A tiny creature leaves no body worth crossing a room for, so it leaves none at all: an empty
    /// list, and nothing is added to the area. Stepping on a beetle does not furnish a room with a
    /// carcass, and an empty corpse PoI would be an observation offering only IGNORE.
    ///
    /// <para>Unreachable in practice — <c>catch</c> and <c>crush</c> remove a tiny creature through
    /// <c>TinyCreatureRemovedOutcome</c>, and both <c>slay</c> and <c>attack</c> refuse them — but the
    /// contract has to be answered, and this is the honest answer.</para>
    /// </summary>
    public override List<PointOfInterest> CreateCorpse(ShallowNpcEntity entity) => new();
}

// ── Insects ──────────────────────────────────────────────────────────────────

public class ButterflyArchetype : TinyShallowArchetype
{
    public override string ArchetypeId     => "butterfly";
    public override string TypeDisplayName => "Butterfly";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "small", "broad-winged", "ragged" },
        colors: new[] { "white", "tawny", "chalk-blue", "orange-barred" },
        noun:   "butterfly",
        traits: new[] { "opening and closing its wings in the sun", "lifting away on nothing", "settled with its wings shut like a leaf" });
    public override List<Item> BuildCatchYield() => new() { new Wing(), new Wing() };
}

public class MothArchetype : TinyShallowArchetype
{
    public override string ArchetypeId     => "moth";
    public override string TypeDisplayName => "Moth";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "fat", "dusty", "small" },
        colors: new[] { "grey", "buff", "mottled brown" },
        noun:   "moth",
        traits: new[] { "battering itself against the light", "still against the wall, wings flat", "leaving powder where it touched" });
    public override List<Item> BuildCatchYield() => new() { new Wing(), new Wing() };
}

public class DragonflyArchetype : TinyShallowArchetype
{
    public override string ArchetypeId     => "dragonfly";
    public override string TypeDisplayName => "Dragonfly";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "long", "hand-span" },
        colors: new[] { "blue-bodied", "green-bodied", "red-bodied" },
        noun:   "dragonfly",
        traits: new[] { "hanging still in the air, then gone", "quartering the water in straight lines", "wings a blur of glass" });
    public override List<Item> BuildCatchYield() => new() { new Wing(), new Wing() };
}

public class BeetleArchetype : TinyShallowArchetype
{
    public override string ArchetypeId     => "beetle";
    public override string TypeDisplayName => "Beetle";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "fat", "thumbnail-sized", "long" },
        colors: new[] { "black", "bronze", "iridescent" },
        noun:   "beetle",
        traits: new[] { "labouring across open ground", "upended and working its legs", "burrowing into the leaf litter" });
    public override List<Item> BuildCatchYield() => new() { new Carapace(), new Carapace() };
}

public class CockroachArchetype : TinyShallowArchetype
{
    public override string ArchetypeId     => "cockroach";
    public override string TypeDisplayName => "Cockroach";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "flat", "quick", "long-feelered" },
        colors: new[] { "brown", "chestnut", "dark" },
        noun:   "cockroach",
        traits: new[] { "running for the dark the moment it is seen", "still against the skirting, feelers working", "vanishing under the boards" });
    public override List<Item> BuildCatchYield() => new() { new Carapace(), new Wing() };
}

public class CricketArchetype : TinyShallowArchetype
{
    /// <summary>One of the two tiny things with a voice: audible as well as visible.</summary>
    public override SensoryProfile Senses => new(Examine: true, Contemplate: true, Listen: true);

    public override string ArchetypeId     => "cricket";
    public override string TypeDisplayName => "Cricket";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "small", "long-legged" },
        colors: new[] { "black", "straw-coloured", "brown" },
        noun:   "cricket",
        traits: new[] { "sawing away somewhere close and impossible to find", "silent the instant anything moves", "springing off at a touch" });
    public override List<Item> BuildCatchYield() => new() { new Grub(), new Wing() };
}

public class BeeArchetype : TinyShallowArchetype
{
    /// <summary>One of the two tiny things with a voice: audible as well as visible.</summary>
    public override SensoryProfile Senses => new(Examine: true, Contemplate: true, Listen: true);

    public override string ArchetypeId     => "bee";
    public override string TypeDisplayName => "Bee";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "fat", "small", "furred" },
        colors: new[] { "banded", "gold-dusted", "dark" },
        noun:   "bee",
        traits: new[] { "working over the flowers one at a time", "heavy with pollen and slow with it", "gone into the blossom head-first" });
    public override List<Item> BuildCatchYield() => new() { new Wax(), new Sting() };
}

public class SnailArchetype : TinyShallowArchetype
{
    public override string ArchetypeId     => "snail";
    public override string TypeDisplayName => "Snail";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "fat", "small" },
        colors: new[] { "banded", "amber", "chalk-white" },
        noun:   "snail",
        traits: new[] { "drawing a wet line up the stone", "shut into its shell and waiting", "feelers out, going nowhere in particular" });
    public override List<Item> BuildCatchYield() => new() { new Shell(), new Meat() };
}

public class GardenSpiderArchetype : TinyShallowArchetype
{
    public override string ArchetypeId     => "garden_spider";
    public override string TypeDisplayName => "Spider";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "long-legged", "fat-bodied", "small" },
        colors: new[] { "brown", "cross-marked", "pale" },
        noun:   "spider",
        traits: new[] { "sitting dead centre of its web", "dropping on a thread and hanging there", "gone still the moment the web is touched" });
    public override List<Item> BuildCatchYield() => new() { new Silk(), new Silk(), new Fang() };
}

// ── Small vertebrates ────────────────────────────────────────────────────────

public class HouseMouseArchetype : TinyShallowArchetype
{
    public override string ArchetypeId     => "house_mouse";
    public override string TypeDisplayName => "Mouse";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "small", "thin" },
        colors: new[] { "grey", "brown", "dust-coloured" },
        noun:   "mouse",
        traits: new[] { "freezing mid-floor with its head up", "working at something in the corner", "gone along the wall in a grey streak" });
    public override List<Item> BuildCatchYield() => new() { new Skin(), new Tail(), new Bone() };
}

public class LizardArchetype : TinyShallowArchetype
{
    public override string ArchetypeId     => "lizard";
    public override string TypeDisplayName => "Lizard";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "small", "quick", "finger-length" },
        colors: new[] { "brown", "green-flanked", "grey" },
        noun:   "lizard",
        traits: new[] { "flat on a warm stone and not moving", "gone into a crack before the eye finds it", "throat working as it watches" });
    public override List<Item> BuildCatchYield() => new() { new Tail(), new Skin() };
}
