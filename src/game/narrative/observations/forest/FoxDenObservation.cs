using System;
using System.Collections.Generic;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;

namespace Cathedral.Game.Narrative.Observations.Forest;

/// <summary>
/// Fox Den observation — an abandoned or active fox burrow.
/// Three sub-outcomes: FoxFur, BoneShard, FeatherRemains.
/// Associated encounter: WolfArchetype (50% spawn chance).
/// </summary>
public class FoxDenObservation : ObservationObject
{
    public override string ObservationId => "fox_den";

    public override List<KeywordInContext> ObservationKeywordsInContext => new()
    {
        KeywordInContext.Parse("the dark earthy <den> entrance in the bank"),
        KeywordInContext.Parse("a wide <burrow> dug into the hillside roots"),
        KeywordInContext.Parse("the sharp <musk> of fox clinging to the air"),
        KeywordInContext.Parse("the dark <tunnel> descending beneath the roots"),
    };

    private static readonly string[] Moods = { "musky", "hidden", "occupied", "abandoned", "earthy", "secretive", "dark", "sheltered" };

    public override List<NpcEncounterSlot> AssociatedEncounters => new()
    {
        new NpcEncounterSlot(new WolfArchetype(), spawnChance: 0.50f),
    };

    public FoxDenObservation()
    {
        SubOutcomes = new List<ConcreteOutcome>
        {
            new FoxFur(),
            new BoneShard(),
            new FeatherRemains(),
        };
    }

    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} fox den";
    }

    public sealed class FoxFur : Item
    {
        public override string ItemId => "fox_fur_tuft";
        public override string DisplayName => "Fox Fur Tuft";
        public override string Description => "A tuft of reddish fox fur caught on roots";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new()
        {
            KeywordInContext.Parse("a scrap of reddish <pelt> caught on a root"),
            KeywordInContext.Parse("a <russet> tuft of fur near the entrance"),
        };
    }

    public sealed class BoneShard : Item
    {
        public override string ItemId => "fox_den_bone_shard";
        public override string DisplayName => "Bone Shard";
        public override string Description => "A gnawed bone fragment from a fox meal";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new()
        {
            KeywordInContext.Parse("a gnawed bone split open for the <marrow>"),
            KeywordInContext.Parse("the remains of old <prey> near the den mouth"),
        };
    }

    public sealed class FeatherRemains : Item
    {
        public override string ItemId => "fox_den_feather_remains";
        public override string DisplayName => "Scattered Feathers";
        public override string Description => "Bird feathers scattered near the den entrance from a kill";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new()
        {
            KeywordInContext.Parse("a loose <plume> among scattered feathers"),
            KeywordInContext.Parse("the scattered evidence of a recent <kill>"),
        };
    }
}
