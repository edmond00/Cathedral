using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class AvalanchePathRunoutNode : PyramidalFeatureNode
{
    public override int MinAltitude => 6;
    public override int MaxAltitude => 10;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(AvalanchePathReleaseNode);
    
    public override string NodeId => "avalanche_path_runout";
    public override string ContextDescription => "standing in the avalanche runout zone";
    public override string TransitionDescription => "descend to the avalanche runout zone";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "avalanche", "runout", "zone", "debris", "snow", "chaotic", "deposited", "jumbled", "scattered", "aftermath" };
    
    private static readonly string[] Moods = { "chaotic", "jumbled", "destructive", "deposited" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} runout zone";
    }
    
    public sealed class AvalancheDebris : Item
    {
        public override string ItemId => "avalanche_path_runout_avalanche_debris";
        public override string DisplayName => "Avalanche Debris";
        public override string Description => "Rock fragments collectible from avalanche deposit";
        public override List<string> OutcomeKeywords => new() { "debris", "avalanche", "rock", "fragments", "chaotic", "deposited", "mixed", "scattered", "angular", "collectible" };
    }
}
