using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class ArchCrestNode : PyramidalFeatureNode
{
    public override int MinAltitude => 5;
    public override int MaxAltitude => 7;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(ArchPassageNode);
    
    public override string NodeId => "arch_crest";
    public override string ContextDescription => "atop the rock arch crest";
    public override string TransitionDescription => "climb to the arch crest";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "arch", "crest", "formation", "span" };
    
    private static readonly string[] Moods = { "graceful", "curved", "spectacular", "precarious" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} arch crest";
    }
    
    public sealed class ArchKeystone : Item
    {
        public override string ItemId => "arch_crest_keystone";
        public override string DisplayName => "Arch Keystone";
        public override string Description => "Central supporting stone at arch peak";
        public override List<string> OutcomeKeywords => new() { "keystone", "apex", "structure" };
    }
    
    public sealed class ViewPoint : Item
    {
        public override string ItemId => "arch_crest_view_point";
        public override string DisplayName => "View Point";
        public override string Description => "Commanding vantage from arch top";
        public override List<string> OutcomeKeywords => new() { "vista", "vantage", "panorama" };
    }
    
    public sealed class WeatheredSurface : Item
    {
        public override string ItemId => "arch_crest_weathered_surface";
        public override string DisplayName => "Weathered Surface";
        public override string Description => "Eroded rock on the arch spine";
        public override List<string> OutcomeKeywords => new() { "surface", "erosion", "weathering" };
    }
}
