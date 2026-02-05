using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class IceCrustedLedgeLowerNode : PyramidalFeatureNode
{
    public override int MinAltitude => 1;
    public override int MaxAltitude => 3;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(IceCrustedLedgeUpperNode);
    
    public override string NodeId => "ice_crusted_ledge_lower";
    public override string ContextDescription => "standing on the lower ice-crusted ledge";
    public override string TransitionDescription => "descend to the lower ice-crusted ledge";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "ledge", "lower", "ice", "sheltered", "narrow", "frozen", "platform", "cold", "recessed", "shadowed" };
    
    private static readonly string[] Moods = { "sheltered", "shadowed", "frozen", "narrow" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} lower ice-crusted ledge";
    }
    
    public sealed class FrostLichen : Item
    {
        public override string ItemId => "ice_crusted_ledge_lower_frost_lichen";
        public override string DisplayName => "Frost Lichen";
        public override string Description => "Frost-resistant lichen collectible from the lower ledge";
        public override List<string> OutcomeKeywords => new() { "lichen", "frost", "resistant", "green-grey", "hardy", "symbiotic", "alpine", "tenacious", "growth", "collectible" };
    }
}
