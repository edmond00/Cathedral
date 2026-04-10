using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Npc.Corpse;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>
/// Farm rabbit — shallow NPC, non-hostile, found in the rabbit enclosure.
/// Can be slayed to harvest pelt and meat.
/// </summary>
public class RabbitArchetype : ShallowNpcArchetype
{
    public override string ArchetypeId     => "rabbit";
    public override string TypeDisplayName => "Rabbit";
    public override bool   DefaultHostile  => false;

    protected override KeywordInContext[] BuildNarrationKeywords() => new[]
    {
        KeywordInContext.Parse("a grey <rabbit> thumping softly in its hutch"),
        KeywordInContext.Parse("a pair of long grey <ears> twitching above the wire"),
        KeywordInContext.Parse("the soft thump of a <rabbit> shifting in the enclosure"),
    };

    protected override string BuildObservationHint(string nodeContext)
        => "a grey rabbit freezes as you approach, nose twitching, eyes wide";

    public override CorpseSpot CreateCorpse(ShallowNpcEntity entity, Area area)
    {
        var bodyParts = new List<PointOfInterest>
        {
            new CorpseBodyPartPoI(
                "Body",
                new() { "the small limp body of the dead rabbit" },
                new()
                {
                    KeywordInContext.Parse("the limp small <body> of the dead rabbit"),
                    KeywordInContext.Parse("the soft grey <fur> gone flat"),
                },
                new()
                {
                    new ItemElement(new RabbitMeat()),
                    new ItemElement(new RabbitMeat()),
                }),

            new CorpseBodyPartPoI(
                "Pelt",
                new() { "the soft grey pelt of the rabbit" },
                new()
                {
                    KeywordInContext.Parse("the loose grey <pelt> ready to be stripped"),
                    KeywordInContext.Parse("the fine dense <fur> of the rabbit skin"),
                },
                new()
                {
                    new ItemElement(new RabbitPelt()),
                }),
        };

        return CorpseRegistry.CreateForShallowNpc(
            entity, area,
            displayName:  "Dead Rabbit",
            descriptions: new() { "A small dead rabbit, its eyes already glazing" },
            keywords: new()
            {
                KeywordInContext.Parse("the small dead <rabbit>, legs splayed on the earth"),
                KeywordInContext.Parse("the still <carcass> of the rabbit"),
            },
            bodyParts);
    }
}
