using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class WallTopNode : PyramidalFeatureNode
{
    public override int MinAltitude => 6;
    public override int MaxAltitude => 9;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(WallBaseNode);
    
    public override string NodeId => "wall_top";
    public override string ContextDescription => "at the lower cliff wall top";
    public override string TransitionDescription => "climb to the wall top";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "wall", "precipice", "overlook", "drop" };
    
    private static readonly string[] Moods = { "exposed", "vertical", "precipitous", "commanding" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} wall top";
    }
    
    public sealed class LooseLedge : Item
    {
        public override string ItemId => "wall_top_loose_ledge";
        public override string DisplayName => "Loose Ledge";
        public override string Description => "Unstable rock at the wall edge";
        public override List<string> OutcomeKeywords => new() { "ledge", "instability", "edge" };
    }
    
    public sealed class RaptorPerch : Item
    {
        public override string ItemId => "wall_top_raptor_perch";
        public override string DisplayName => "Raptor Perch";
        public override string Description => "Bird lookout point on the wall";
        public override List<string> OutcomeKeywords => new() { "perch", "raptor", "vantage" };
    }
    
    public sealed class CliffEdge : Item
    {
        public override string ItemId => "wall_top_cliff_edge";
        public override string DisplayName => "Cliff Edge";
        public override string Description => "Sharp boundary at the wall top";
        public override List<string> OutcomeKeywords => new() { "edge", "void", "precipice" };
    }
}
