using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class ArchPassageNode : PyramidalFeatureNode
{
    public override int MinAltitude => 5;
    public override int MaxAltitude => 7;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(ArchCrestNode);
    
    public override string NodeId => "arch_passage";
    public override string ContextDescription => "through the rock arch passage";
    public override string TransitionDescription => "pass through the arch passage";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "arch", "passage", "gateway", "portal" };
    
    private static readonly string[] Moods = { "shadowed", "framed", "impressive", "sheltered" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} arch passage";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"through a {mood} arch passage";
    }
    
    public sealed class FramedSky : Item
    {
        public override string ItemId => "arch_passage_framed_sky";
        public override string DisplayName => "Framed Sky";
        public override string Description => "Sky visible through the arch opening";
        public override List<string> OutcomeKeywords => new() { "sky", "frame", "window" };
    }
    
    public sealed class EchoSpace : Item
    {
        public override string ItemId => "arch_passage_echo_space";
        public override string DisplayName => "Echo Space";
        public override string Description => "Acoustic chamber beneath the arch";
        public override List<string> OutcomeKeywords => new() { "echo", "resonance", "chamber" };
    }
}
