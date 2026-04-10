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

    protected override KeywordInContext[] BuildNarrationKeywords() => new[]
    {
        KeywordInContext.Parse("a fat pink <pig> rooting in the mud"),
        KeywordInContext.Parse("the heavy <grunt> of a sow shifting in her pen"),
        KeywordInContext.Parse("the pale barrel <body> of a farm pig"),
    };

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
                    KeywordInContext.Parse("the heavy <carcass> of the dead pig"),
                    KeywordInContext.Parse("the pale pink <flesh> visible at the slaughter wound"),
                },
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
                    KeywordInContext.Parse("the thick <haunch>es of the pig"),
                    KeywordInContext.Parse("the marbled <pork> of the hindquarters"),
                },
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
            keywords: new()
            {
                KeywordInContext.Parse("the heavy dead <pig>, collapsed in the mud"),
                KeywordInContext.Parse("the fat <carcass> of the slaughtered animal"),
            },
            bodyParts);
    }
}
