using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Npc.Corpse;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>
/// Domestic chicken — shallow NPC, non-hostile, found in the chicken coop and courtyard.
/// Can be slayed to harvest feathers and meat.
/// </summary>
public class ChickenArchetype : ShallowNpcArchetype
{
    public override string ArchetypeId     => "chicken";
    public override string TypeDisplayName => "Chicken";

    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "plump", "scrawny", "small" },
        colors: new[] { "speckled", "russet", "white", "black-and-white" },
        noun:   "hen",
        traits: new[] { "clucking and scratching in the dirt", "pecking at the ground, oblivious", "fluffed up and strutting" });

    public override List<PointOfInterest> CreateCorpse(ShallowNpcEntity entity)
        => CorpseRegistry.CreateForShallowNpc(
            entity,
            displayName:  "Dead Chicken",
            descriptions: new() { "a limp chicken, its neck broken, wings splayed and the feathers already going flat" },
            parts: new()
            {
                new ItemElement(new ChickenMeat()),
                new ItemElement(new ChickenMeat()),
                new ItemElement(new ChickenFeather()),
                new ItemElement(new ChickenFeather()),
                new ItemElement(new ChickenFeather()),
            });
}
