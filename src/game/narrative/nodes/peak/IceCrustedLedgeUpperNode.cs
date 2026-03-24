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
    
    public override string NodeId => "ice_crusted_ledge_upper";
    public override string ContextDescription => "standing on the upper ice-crusted ledge";
    public override string TransitionDescription => "climb to the upper ice-crusted ledge";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "ledge", "upper", "ice", "crusted", "narrow", "frozen", "exposed", "precipice", "cold", "shelf" };
    
    private static readonly string[] Moods = { "narrow", "icy", "precarious", "frozen" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} upper ice-crusted ledge";
    }
    
    public sealed class LedgeLichen : Item
    {
        public override string ItemId => "ice_crusted_ledge_upper_ledge_lichen";
        public override string DisplayName => "Ledge Lichen";
        public override string Description => "Hardy lichen collectible from the icy ledge";
        public override List<string> OutcomeKeywords => new() { "lichen", "hardy", "orange", "crusty", "symbiotic", "survivor", "growth", "alpine", "colorful", "collectible" };
    }
    
    public sealed class Icicle : Item
    {
        public override string ItemId => "ice_crusted_ledge_upper_icicle";
        public override string DisplayName => "Icicle";
        public override string Description => "Hanging icicle from the ledge";
        public override List<string> OutcomeKeywords => new() { "icicle", "hanging", "pointed", "frozen", "drip", "crystalline", "sharp", "translucent", "cold", "delicate" };
    }
    
    public sealed class FrozenMoss : Item
    {
        public override string ItemId => "ice_crusted_ledge_upper_frozen_moss";
        public override string DisplayName => "Frozen Moss";
        public override string Description => "Moss encased in ice";
        public override List<string> OutcomeKeywords => new() { "moss", "frozen", "encased", "ice", "green", "preserved", "cold", "embedded", "ancient", "dormant" };
    }
}
