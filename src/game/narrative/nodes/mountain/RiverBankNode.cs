using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class RiverBankNode : PyramidalFeatureNode
{
    public override int MinAltitude => 5;
    public override int MaxAltitude => 9;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(RiverbedNode);
    
    public override string NodeId => "river_bank";
    public override string ContextDescription => "on the river cut bank";
    public override string TransitionDescription => "climb to the river bank";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "bank", "river", "embankment", "erosion" };
    
    private static readonly string[] Moods = { "eroded", "undercut", "steep", "crumbling" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} river bank";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"on a {mood} river bank";
    }
    
    public sealed class RootsExposed : Item
    {
        public override string ItemId => "river_bank_roots_exposed";
        public override string DisplayName => "Exposed Roots";
        public override string Description => "Tree roots hanging from eroded bank";
        public override List<string> OutcomeKeywords => new() { "root", "erosion", "network" };
    }
    
    public sealed class UndercutEdge : Item
    {
        public override string ItemId => "river_bank_undercut_edge";
        public override string DisplayName => "Undercut Edge";
        public override string Description => "Overhanging bank carved by water";
        public override List<string> OutcomeKeywords => new() { "overhang", "erosion", "instability" };
    }
    
    public sealed class RiverGrass : Item
    {
        public override string ItemId => "river_bank_river_grass";
        public override string DisplayName => "River Grass";
        public override string Description => "Tall grass growing at water's edge";
        public override List<string> OutcomeKeywords => new() { "grass", "reed", "waterside" };
    }
}
