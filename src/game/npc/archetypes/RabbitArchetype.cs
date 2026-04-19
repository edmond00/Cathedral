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
                    new ItemElement(new RabbitMeat()),
                    new ItemElement(new RabbitMeat()),
                }),

            new CorpseBodyPartPoI(
                "Pelt",
                new() { "the soft grey pelt of the rabbit" },
                new()
                {
                    new ItemElement(new RabbitPelt()),
                }),
        };

        return CorpseRegistry.CreateForShallowNpc(
            entity, area,
            displayName:  "Dead Rabbit",
            descriptions: new() { "A small dead rabbit, its eyes already glazing" },
            bodyParts);
    }
}
