using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class GullyLipNode : PyramidalFeatureNode
{
    public override int MinAltitude => 5;
    public override int MaxAltitude => 8;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(GullyBottomNode);
    
    public override string NodeId => "gully_lip";
    public override string ContextDescription => "at the shaded gully lip";
    public override string TransitionDescription => "approach the gully lip";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "brink", "rim", "edge", "drop" };
    
    private static readonly string[] Moods = { "shaded", "dark", "shadowy", "dim" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} gully lip";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"at a {mood} gully lip";
    }
    
    public sealed class OverhangingFern : Item
    {
        public override string ItemId => "gully_lip_overhanging_fern";
        public override string DisplayName => "Overhanging Fern";
        public override string Description => "Lush fern growing at the gully edge";
        public override List<string> OutcomeKeywords => new() { "frond", "pinnule", "droop" };
    }
    
}
