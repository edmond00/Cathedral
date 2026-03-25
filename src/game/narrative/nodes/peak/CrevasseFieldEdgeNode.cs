using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class CrevasseFieldEdgeNode : PyramidalFeatureNode
{
    public override int MinAltitude => 2;
    public override int MaxAltitude => 6;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(CrevasseFieldInteriorNode);
    
    public override string NodeId => "crevasse_field_edge";
    public override string ContextDescription => "standing at the crevasse field edge";
    public override string TransitionDescription => "approach the crevasse field edge";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "crevasse", "gap", "chasm", "danger" };
    
    private static readonly string[] Moods = { "dangerous", "fractured", "treacherous", "deep" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} crevasse field edge";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing at a {mood} crevasse field edge";
    }
    
    public sealed class CrevasseLip : Item
    {
        public override string ItemId => "crevasse_field_edge_crevasse_lip";
        public override string DisplayName => "Crevasse Lip";
        public override string Description => "Edge of the crevasse opening";
        public override List<string> OutcomeKeywords => new() { "lip", "crevasse", "ice" };
    }
    
}
