using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Npc.Corpse;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>
/// Domestic pig — shallow NPC, non-hostile, found in the pigsty.
/// Can be slayed to harvest pork.
/// </summary>
public class PigArchetype : ShallowNpcArchetype
{
    public override string ArchetypeId     => "pig";
    public override string TypeDisplayName => "Pig";

    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "fat", "heavy", "muddy" },
        colors: new[] { "pink", "pink-and-black", "bristled grey" },
        noun:   "pig",
        traits: new[] { "snout twitching as it roots in the mire", "wallowing in the mud", "grunting over a trough" });

    public override CorpseSpot CreateCorpse(ShallowNpcEntity entity, Area area)
    {
        var bodyParts = new List<PointOfInterest>
        {
            new CorpseBodyPartPoI(
                "Body",
                new() { "the heavy pink carcass of the dead pig" },
                new()
                {
                    new ItemElement(new PorkMeat()),
                    new ItemElement(new PorkMeat()),
                    new ItemElement(new PorkMeat()),
                }),

            new CorpseBodyPartPoI(
                "Haunches",
                new() { "the thick haunches of the pig carcass" },
                new()
                {
                    new ItemElement(new PorkMeat()),
                    new ItemElement(new PorkMeat()),
                }),
        };

        return CorpseRegistry.CreateForShallowNpc(
            entity, area,
            displayName:  "Dead Pig",
            descriptions: new() { "A heavy pig carcass collapsed in the mire, still steaming faintly" },
            bodyParts);
    }
}
