using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class UpperScreeNode : PyramidalFeatureNode
{
    public override int MinAltitude => 3;
    public override int MaxAltitude => 7;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(LowerScreeNode);
    
    public override string NodeId => "upper_scree";
    public override string ContextDescription => "on the upper scree slope";
    public override string TransitionDescription => "climb to the upper scree";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "scree", "rock", "instability", "danger" };
    
    private static readonly string[] Moods = { "treacherous", "unstable", "sliding", "precarious" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} upper scree";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"on a {mood} upper scree";
    }
    
    public sealed class LooseChips : Item
    {
        public override string ItemId => "upper_scree_loose_chips";
        public override string DisplayName => "Loose Chips";
        public override string Description => "Small angular rock fragments";
        public override List<string> OutcomeKeywords => new() { "chip", "fragment", "gravel" };
    }
    
    public sealed class SlideTrack : Item
    {
        public override string ItemId => "upper_scree_slide_track";
        public override string DisplayName => "Slide Track";
        public override string Description => "Path where rocks have recently slid";
        public override List<string> OutcomeKeywords => new() { "track", "slide", "scar" };
    }
    
    public sealed class UnstableRock : Item
    {
        public override string ItemId => "upper_scree_unstable_rock";
        public override string DisplayName => "Unstable Rock";
        public override string Description => "Larger stone perched in the scree";
        public override List<string> OutcomeKeywords => new() { "rock", "hazard", "instability" };
    }
}
