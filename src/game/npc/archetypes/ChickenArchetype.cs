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
    public override bool   DefaultHostile  => false;

    protected override KeywordInContext[] BuildNarrationKeywords() => new[]
    {
        KeywordInContext.Parse("a speckled <chicken> pecking at the ground"),
        KeywordInContext.Parse("the bobbing red <comb> of a hen"),
        KeywordInContext.Parse("the clucking <bird> scratching in the dirt"),
    };

    protected override string BuildObservationHint(string nodeContext)
        => "a speckled hen clucks and scratches in the yard, paying you no mind";

    public override CorpseSpot CreateCorpse(ShallowNpcEntity entity, Area area)
    {
        var bodyParts = new List<PointOfInterest>
        {
            new CorpseBodyPartPoI(
                "Body",
                new() { "the limp feathered body of the chicken" },
                new()
                {
                    KeywordInContext.Parse("the limp <carcass> of the dead hen"),
                    KeywordInContext.Parse("the pale <breast> visible under ruffled feathers"),
                },
                new()
                {
                    new ItemElement(new ChickenMeat()),
                    new ItemElement(new ChickenMeat()),
                }),

            new CorpseBodyPartPoI(
                "Wings",
                new() { "the outstretched wings of the dead chicken" },
                new()
                {
                    KeywordInContext.Parse("the outstretched <wing> of the dead bird"),
                    KeywordInContext.Parse("the cream <feather>s still soft against the ground"),
                },
                new()
                {
                    new ItemElement(new ChickenFeather()),
                    new ItemElement(new ChickenFeather()),
                    new ItemElement(new ChickenFeather()),
                }),
        };

        return CorpseRegistry.CreateForShallowNpc(
            entity, area,
            displayName:  "Dead Chicken",
            descriptions: new() { "A limp chicken, its neck broken, feathers already going flat" },
            keywords: new()
            {
                KeywordInContext.Parse("the limp dead <chicken> on the ground"),
                KeywordInContext.Parse("the still <carcass> of a freshly killed hen"),
            },
            bodyParts);
    }
}
