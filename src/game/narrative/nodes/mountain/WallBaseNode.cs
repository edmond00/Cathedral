using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class WallBaseNode : PyramidalFeatureNode
{
    public override int MinAltitude => 6;
    public override int MaxAltitude => 9;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(WallTopNode);
    
    public override string NodeId => "wall_base";
    public override string ContextDescription => "at the lower cliff wall base";
    public override string TransitionDescription => "descend to the wall base";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "wall", "base", "cliff", "towering", "shadow", "massive", "vertical", "imposing", "solid", "foundation" };
    
    private static readonly string[] Moods = { "towering", "shadowed", "massive", "imposing" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} wall base";
    }
    
    public sealed class FallenStone : Item
    {
        public override string ItemId => "wall_base_fallen_stone";
        public override string DisplayName => "Fallen Stone";
        public override string Description => "Large rock that has dropped from above";
        public override List<string> OutcomeKeywords => new() { "fallen", "stone", "large", "dropped", "debris", "rockfall", "heavy", "gray", "angular", "fresh" };
    }
    
    public sealed class ClimbingCracks : Item
    {
        public override string ItemId => "wall_base_climbing_cracks";
        public override string DisplayName => "Climbing Cracks";
        public override string Description => "Fissures offering handholds";
        public override List<string> OutcomeKeywords => new() { "cracks", "climbing", "fissures", "handholds", "vertical", "route", "grips", "lines", "openings", "passage" };
    }
}
