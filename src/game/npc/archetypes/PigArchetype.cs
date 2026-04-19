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
    public override bool   DefaultHostile  => false;

    protected override string BuildObservationHint(string nodeContext)
        => "a fat sow looks up from the mire, snout twitching, then returns to rooting";

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
