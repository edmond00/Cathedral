using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class IceCrustedLedgeUpperNode : PyramidalFeatureNode
{
    public override int MinAltitude => 1;
    public override int MaxAltitude => 3;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(IceCrustedLedgeLowerNode);
    
    public override string NodeId => "upper_ice_crusted_ledge";
    public override string ContextDescription => "standing on the upper ice-crusted ledge";
    public override string TransitionDescription => "climb to the upper ice-crusted ledge";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "ledge", "ice", "crust", "precipice" };
    
    private static readonly string[] Moods = { "narrow", "icy", "precarious", "frozen" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} upper ice-crusted ledge";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing on a {mood} upper ice-crusted ledge";
    }
    
    public sealed class LedgeLichen : Item
    {
        public override string ItemId => "ice_crusted_ledge_upper_ledge_lichen";
        public override string DisplayName => "Ledge Lichen";
        public override string Description => "Hardy lichen collectible from the icy ledge";
        public override List<string> OutcomeKeywords => new() { "thallus", "symbiosis", "survival" };
    }
    
    public sealed class Icicle : Item
    {
        public override string ItemId => "ice_crusted_ledge_upper_icicle";
        public override string DisplayName => "Icicle";
        public override string Description => "Hanging icicle from the ledge";
        public override List<string> OutcomeKeywords => new() { "pendant", "crystal", "drip" };
    }
    
    public sealed class FrozenMoss : Item
    {
        public override string ItemId => "ice_crusted_ledge_upper_frozen_moss";
        public override string DisplayName => "Frozen Moss";
        public override string Description => "Moss encased in ice";
        public override List<string> OutcomeKeywords => new() { "bryophyte", "ice", "dormancy" };
    }
}
