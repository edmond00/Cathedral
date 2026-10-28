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
