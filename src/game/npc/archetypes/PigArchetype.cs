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

    public override List<PointOfInterest> CreateCorpse(ShallowNpcEntity entity)
        => CorpseRegistry.CreateForShallowNpc(
            entity,
            displayName:  "Dead Pig",
            descriptions: new() { "a heavy pink carcass collapsed in the mire, thick in the haunches and still steaming faintly" },
            parts: new()
            {
                new ItemElement(new PorkMeat()),
                new ItemElement(new PorkMeat()),
                new ItemElement(new PorkMeat()),
                new ItemElement(new PorkMeat()),
                new ItemElement(new PorkMeat()),
            });
}
