using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class IcyGullyRunNode : PyramidalFeatureNode
{
    public override int MinAltitude => 4;
    public override int MaxAltitude => 7;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(IcyGullyHeadNode);
    
    public override string NodeId => "icy_gully_run";
    public override string ContextDescription => "standing in the icy gully run";
    public override string TransitionDescription => "descend into the icy gully run";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "gully", "run", "ice", "channel", "narrow", "frozen", "descent", "cold", "confined", "flowing" };
    
    private static readonly string[] Moods = { "narrow", "flowing", "confined", "icy" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} gully run";
    }
    
    public sealed class GullyObsidian : Item
    {
        public override string ItemId => "icy_gully_run_gully_obsidian";
        public override string DisplayName => "Gully Obsidian";
        public override string Description => "Volcanic glass collectible from the gully";
        public override List<string> OutcomeKeywords => new() { "obsidian", "volcanic", "glass", "black", "shiny", "sharp", "glassy", "lustrous", "smooth", "collectible" };
    }
}
