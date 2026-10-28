using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Npc.Archetypes;

// ─────────────────────────────────────────────────────────────────────────────
//  The shallow life the wilderness overhaul added — the creatures that belong to one of the new kinds
//  of place (a dipper to falling water, a seal to the haul-out ice) rather than to a whole biome. Same
//  four bases and the same body-part vocabulary as everything else that does not fight.
// ─────────────────────────────────────────────────────────────────────────────

// ── Mountain ────────────────────────────────────────────────────────────────

public class DipperArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "dipper";
    public override string TypeDisplayName => "Dipper";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "small", "stout" },
        colors: new[] { "dark brown", "white-breasted", "slate" },
        noun:   "dipper",
        traits: new[] { "bobbing on a wet stone", "walking straight into the current and under it", "flying low along the water" });
    protected override string CorpseBodyDescription => "the small sodden body of a dipper, its white bib grey with water";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Meat()),
        new ItemElement(new Bone()),
    };
}

public class ChoughArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "chough";
    public override string TypeDisplayName => "Chough";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "glossy", "slender" },
        colors: new[] { "black", "red-legged", "blue-sheened" },
        noun:   "chough",
        traits: new[] { "tumbling in the updraught off the crag", "calling a high bright 'chee-ow'", "probing a turf with a curved red bill" });
    protected override string CorpseBodyDescription => "the glossy black body of a chough, its red bill and legs still bright";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Meat()),
        new ItemElement(new Bone()),
    };
}

public class SnowFinchArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "snow_finch";
    public override string TypeDisplayName => "Snow Finch";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "small", "plump" },
        colors: new[] { "grey-brown", "white-winged", "pale" },
        noun:   "snow finch",
        traits: new[] { "picking seed from the edge of the snow", "flashing white wings as it flies", "sheltering in a crack out of the wind" });
    protected override string CorpseBodyDescription => "the small cold body of a snow finch, its white wing-panels spread";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Meat()),
        new ItemElement(new Bone()),
    };
}

// ── Caves ───────────────────────────────────────────────────────────────────

public class OlmArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "olm";
    public override string TypeDisplayName => "Olm";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "slender", "eel-like" },
        colors: new[] { "pale pink", "fleshy white", "translucent" },
        noun:   "olm",
        traits: new[] { "lying on the bottom of the black water", "eyeless, its feathery red gills stirring", "turning slowly in the current" });
    protected override string CorpseBodyDescription => "the limp pink body of an olm, eyeless, small legs folded";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Skin()),
        new ItemElement(new Meat()),
        new ItemElement(new Bone()),
        new ItemElement(new Tail()),
    };
}

// ── Coast ───────────────────────────────────────────────────────────────────


public class OystercatcherArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "oystercatcher";
    public override string TypeDisplayName => "Oystercatcher";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "stout", "long-billed" },
        colors: new[] { "pied", "black and white", "red-billed" },
        noun:   "oystercatcher",
        traits: new[] { "piping shrilly as it runs along the tideline", "hammering a shell open with its bill", "flying low over the flats in a noisy flock" });
    protected override string CorpseBodyDescription => "the pied body of an oystercatcher, its long red bill still bright";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Feather()),
        new ItemElement(new Meat()),
        new ItemElement(new Wing()),
    };
}

// ── Forest ──────────────────────────────────────────────────────────────────

public class BeaverArchetype : SmallMammalShallowArchetype
{
    public override string ArchetypeId     => "beaver";
    public override string TypeDisplayName => "Beaver";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "heavy", "broad-tailed" },
        colors: new[] { "wet brown", "dark chestnut", "glossy" },
        noun:   "beaver",
        traits: new[] { "towing a branch across the pond", "gnawing a sapling with long orange teeth", "slapping the water with its flat tail and vanishing" });
    protected override string CorpseBodyDescription => "the heavy wet body of a beaver, its broad scaled tail lying flat";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Pelt()),
        new ItemElement(new Meat()),
        new ItemElement(new Meat()),
        new ItemElement(new Tail()),
        new ItemElement(new Tooth()),
    };
}

public class KingfisherArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "kingfisher";
    public override string TypeDisplayName => "Kingfisher";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "small", "dagger-billed" },
        colors: new[] { "electric blue", "orange-breasted", "jewel-bright" },
        noun:   "kingfisher",
        traits: new[] { "perched over the water, quite still", "a streak of blue low along the bank", "dropping into the water and coming up with a fish" });
    protected override string CorpseBodyDescription => "the tiny jewel-coloured body of a kingfisher, its blue as bright in death";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Plume()),
        new ItemElement(new Feather()),
        new ItemElement(new Bone()),
    };
}

// ── Plain ───────────────────────────────────────────────────────────────────

public class LapwingArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "lapwing";
    public override string TypeDisplayName => "Lapwing";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "crested", "round-winged" },
        colors: new[] { "green-black", "white-bellied", "iridescent" },
        noun:   "lapwing",
        traits: new[] { "tumbling over the wet grass crying 'pee-wit'", "running a few steps and stopping", "flopping along the ground as if hurt, to draw you from its nest" });
    protected override string CorpseBodyDescription => "the crested body of a lapwing, its dark back shot with green";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Plume()),
        new ItemElement(new Meat()),
        new ItemElement(new Bone()),
    };
}

public class BitternArchetype : BirdShallowArchetype
{
    public override string ArchetypeId     => "bittern";
    public override string TypeDisplayName => "Bittern";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "hunched", "dagger-billed" },
        colors: new[] { "streaked brown", "tawny", "reed-coloured" },
        noun:   "bittern",
        traits: new[] { "standing bill-up among the reeds, swaying with them", "booming somewhere out of sight like a bull in a barrel", "stalking the shallows a step at a time" });
    protected override string CorpseBodyDescription => "the streaked brown body of a bittern, long neck folded, the colour of dead reed";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Feather()),
        new ItemElement(new Plume()),
        new ItemElement(new Meat()),
        new ItemElement(new Bone()),
    };
}

// ── Desert ──────────────────────────────────────────────────────────────────

public class DesertMonitorArchetype : GenericShallowArchetype
{
    public override string ArchetypeId     => "desert_monitor";
    public override string TypeDisplayName => "Desert Monitor";
    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "long", "heavy-tailed" },
        colors: new[] { "sand-yellow", "banded", "dusty grey" },
        noun:   "monitor lizard",
        traits: new[] { "basking on a hot rock, tongue flickering", "swinging its head to watch you", "tail lashing as it backs into a crack" });
    protected override string CorpseBodyDescription => "the long banded body of a monitor lizard, its tail longer than the rest of it";
    protected override List<ItemElement> BuildCorpseDrops() => new()
    {
        new ItemElement(new Skin()),
        new ItemElement(new Skin()),
        new ItemElement(new Meat()),
        new ItemElement(new Tail()),
        new ItemElement(new Claw()),
    };
}
