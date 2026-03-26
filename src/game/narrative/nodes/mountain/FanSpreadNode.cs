using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class FanSpreadNode : PyramidalFeatureNode
{
    public override int MinAltitude => 7;
    public override int MaxAltitude => 10;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(FanApexNode);
    
    public override string NodeId => "fan_spread";
    public override string ContextDescription => "on the alluvial fan spread";
    public override string TransitionDescription => "descend to the fan spread";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "fan", "gravel", "deposit", "distributary" };
    
    private static readonly string[] Moods = { "wide", "gentle", "spreading", "deposited" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} fan spread";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"on a {mood} fan spread";
    }
    
    public sealed class FanGravel : Item
    {
        public override string ItemId => "fan_gravel";
        public override string DisplayName => "Fine Gravel";
        public override string Description => "Small sorted stones across the fan";
        public override List<string> OutcomeKeywords => new() { "fragment", "deposit", "layer" };
    }
    
}
