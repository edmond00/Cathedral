using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class CrevasseFieldInteriorNode : PyramidalFeatureNode
{
    public override int MinAltitude => 2;
    public override int MaxAltitude => 6;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(CrevasseFieldEdgeNode);
    
    public override string NodeId => "crevasse_field_interior";
    public override string ContextDescription => "inside the crevasse field interior";
    public override string TransitionDescription => "enter the crevasse field interior";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "crevasse", "ice", "confinement", "depth" };
    
    private static readonly string[] Moods = { "narrow", "shadowed", "confining", "blue-lit" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} crevasse interior";
    }
    
}
