using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class CrestRidgeNode : PyramidalFeatureNode
{
    public override int MinAltitude => 0;
    public override int MaxAltitude => 1;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(CrestShoulderNode);
    
    public override string NodeId => "crest_ridge";
    public override string ContextDescription => "standing on the summit crest ridge";
    public override string TransitionDescription => "ascend to the crest ridge";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "summit", "peak", "crest", "wind", "highest", "thin", "exposed", "sharp", "sky", "panoramic" };
    
    private static readonly string[] Moods = { "windswept", "exposed", "highest", "razor-sharp" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} crest ridge";
    }
    
    public sealed class FrostShatter : Item
    {
        public override string ItemId => "crest_ridge_frost_shatter";
        public override string DisplayName => "Frost Shatter";
        public override string Description => "Ice-fractured rock fragments at the summit";
        public override List<string> OutcomeKeywords => new() { "frost", "shattered", "ice", "fractured", "sharp", "crystalline", "frozen", "splintered", "brittle", "cold" };
    }
    
    public sealed class RidgePolishedStone : Item
    {
        public override string ItemId => "ridge_polished_stone";
        public override string DisplayName => "Ridge-Polished Stone";
        public override string Description => "Smooth stone shaped by constant wind";
        public override List<string> OutcomeKeywords => new() { "polished", "smooth", "wind", "worn", "glossy", "shaped", "weathered", "rounded", "sleek", "buffed" };
    }
    
    public sealed class SummitCairn : Item
    {
        public override string ItemId => "crest_ridge_summit_cairn";
        public override string DisplayName => "Summit Cairn";
        public override string Description => "Stacked stones marking the highest point";
        public override List<string> OutcomeKeywords => new() { "cairn", "stacked", "marker", "stones", "monument", "peak", "pillar", "memorial", "landmark", "tower" };
    }
}
