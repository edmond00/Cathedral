using System;
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

    /// <summary>Livestock: seen, heard and — being livestock — smelled.</summary>
    public override SensoryProfile Senses => SensoryProfile.FullyAlive;

    /// <summary>An animal, so the naturalist's lessons rather than the object ones.</summary>
    public override System.Collections.Generic.IReadOnlyDictionary<string, string>? VerbModiMentis
        => CreatureSenses;

    public override string ArchetypeId     => "rabbit";
    public override string TypeDisplayName => "Rabbit";

    protected override string ComposeObservationHint(Random rng, string nodeContext) => Compose(rng,
        sizes:  new[] { "small", "lean" },
        colors: new[] { "grey", "brown", "grey-brown", "white" },
        noun:   "rabbit",
        traits: new[] { "nose twitching, eyes wide", "frozen mid-hop, ears upright", "nibbling at the grass, ready to bolt" });

    public override List<PointOfInterest> CreateCorpse(ShallowNpcEntity entity)
        => CorpseRegistry.CreateForShallowNpc(
            entity,
            displayName:  "Dead Rabbit",
            descriptions: new() { "a small dead rabbit, eyes already glazing, the soft grey pelt unmarked" },
            parts: new()
            {
                new ItemElement(new RabbitMeat()),
                new ItemElement(new RabbitMeat()),
                new ItemElement(new RabbitPelt()),
            });
}
