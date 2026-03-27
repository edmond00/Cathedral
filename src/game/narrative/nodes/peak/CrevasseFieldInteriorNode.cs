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
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the walls of a deep open <crevasse> on either side"), KeywordInContext.Parse("the blue-lit <ice> pressing in from the walls"), KeywordInContext.Parse("the physical <confinement> of the narrowing gap"), KeywordInContext.Parse("a <depth> of shadow below that refuses all light") };
    
    private static readonly string[] Moods = { "narrow", "shadowed", "confining", "blue-lit" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} crevasse interior";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"inside a {mood} crevasse interior";
    }
    
}
