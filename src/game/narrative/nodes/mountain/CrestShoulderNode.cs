using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class CrestShoulderNode : PyramidalFeatureNode
{
    public override int MinAltitude => 0;
    public override int MaxAltitude => 1;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(CrestRidgeNode);
    
    public override string NodeId => "crest_shoulder";
    public override string ContextDescription => "on the summit crest shoulder";
    public override string TransitionDescription => "descend to the crest shoulder";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "shoulder", "slope", "wind", "barrenness" };
    
    private static readonly string[] Moods = { "steep", "exposed", "wind-battered", "barren" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} crest shoulder";
    }
    
    public sealed class LooseScree : Item
    {
        public override string ItemId => "crest_shoulder_loose_scree";
        public override string DisplayName => "Loose Scree";
        public override string Description => "Unstable rock debris on the shoulder";
        public override List<string> OutcomeKeywords => new() { "scree", "gravel", "instability" };
    }
    
    public sealed class AlpineQuartz : Item
    {
        public override string ItemId => "crest_shoulder_alpine_quartz";
        public override string DisplayName => "Alpine Quartz";
        public override string Description => "Clear quartz crystal collectible from the shoulder";
        public override List<string> OutcomeKeywords => new() { "quartz", "crystal", "mineral" };
    }
}
