using System;
using System.Collections.Generic;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class AvalanchePathRunoutNode : PyramidalFeatureNode
{
    public override int MinAltitude => 6;
    public override int MaxAltitude => 10;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(AvalanchePathReleaseNode);
    
    public override string NodeId => "avalanche_path_runout";

    public override List<NpcEncounterSlot> PossibleEncounters => new()
    {
        new NpcEncounterSlot(new WolfArchetype(), spawnChance: 0.35f),
        new NpcEncounterSlot(new BearArchetype(), spawnChance: 0.25f),
    };
    public override string ContextDescription => "standing in the avalanche runout zone";
    public override string TransitionDescription => "descend to the avalanche runout zone";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the total <wreckage> of snapped trees below"), KeywordInContext.Parse("the jumbled <debris> of the avalanche deposit"), KeywordInContext.Parse("the flat <runout> where the snow stopped"), KeywordInContext.Parse("the terrible <aftermath> of the slide") };
    
    private static readonly string[] Moods = { "chaotic", "jumbled", "destructive", "deposited" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} runout zone";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing in a {mood} runout zone";
    }
    
    public sealed class AvalancheDebris : Item
    {
        public override string ItemId => "avalanche_path_runout_avalanche_debris";
        public override string DisplayName => "Avalanche Debris";
        public override string Description => "Rock fragments collectible from avalanche deposit";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("the mixed <detritus> of rock and snow"), KeywordInContext.Parse("a jagged <rock> exposed by the avalanche"), KeywordInContext.Parse("a broken <fragment> embedded in the deposit") };
    }
}
