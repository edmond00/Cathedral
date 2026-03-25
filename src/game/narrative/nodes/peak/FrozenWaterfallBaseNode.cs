using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class FrozenWaterfallBaseNode : PyramidalFeatureNode
{
    public override int MinAltitude => 4;
    public override int MaxAltitude => 7;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(FrozenWaterfallLipNode);
    
    public override string NodeId => "frozen_waterfall_base";
    public override string ContextDescription => "standing at the frozen waterfall base";
    public override string TransitionDescription => "descend to the frozen waterfall base";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "waterfall", "ice", "pool", "cascade" };
    
    private static readonly string[] Moods = { "towering", "massive", "frozen", "spectacular" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} waterfall base";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing at a {mood} waterfall base";
    }
    
    public sealed class WaterfallBasalt : Item
    {
        public override string ItemId => "frozen_waterfall_base_waterfall_basalt";
        public override string DisplayName => "Waterfall Basalt";
        public override string Description => "Volcanic basalt collectible from the frozen pool";
        public override List<string> OutcomeKeywords => new() { "basalt", "column", "igneous" };
    }
    
    public sealed class PeakMoss : Item
    {
        public override string ItemId => "frozen_waterfall_base_peak_moss";
        public override string DisplayName => "Peak Moss";
        public override string Description => "Hardy alpine moss collectible from the base";
        public override List<string> OutcomeKeywords => new() { "moss", "alpine", "resilience" };
    }
}
