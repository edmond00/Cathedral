using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Drainage Hollow - A transversal feature of a natural water channel.
/// </summary>
public class DrainageHollowNode : NarrationNode
{
    public override string NodeId => "drainage_hollow";
    public override string ContextDescription => "descending through the drainage hollow";
    public override string TransitionDescription => "follow the drainage";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "hollow", "channel", "mud", "depression" };
    
    private static readonly string[] Moods = { "damp", "muddy", "slippery", "wet", "eroded", "carved", "soggy", "moist" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} drainage hollow";
    }
    
    public sealed class ClayDeposit : Item
    {
        public override string ItemId => "drainage_clay";
        public override string DisplayName => "Clay Deposit";
        public override string Description => "Sticky clay accumulated in the drainage channel";
        public override List<string> OutcomeKeywords => new() { "clay", "mineral", "plasticity" };
    }
    
    public sealed class StagnantWater : Item
    {
        public override string ItemId => "drainage_hollow_stagnant_water";
        public override string DisplayName => "Stagnant Water";
        public override string Description => "Still water pooled in the hollow";
        public override List<string> OutcomeKeywords => new() { "water", "stagnation", "pool" };
    }
}
