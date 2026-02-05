using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class DebrisFieldNode : PyramidalFeatureNode
{
    public override int MinAltitude => 3;
    public override int MaxAltitude => 7;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(RockfallCrownNode);
    
    public override string NodeId => "debris_field";
    public override string ContextDescription => "in the debris field";
    public override string TransitionDescription => "descend to the debris field";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "debris", "field", "scattered", "boulders", "rocks", "chaotic", "fallen", "spread", "jumbled", "broken" };
    
    private static readonly string[] Moods = { "chaotic", "scattered", "jumbled", "devastated" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} debris field";
    }
    
    public sealed class LargeBoulder : Item
    {
        public override string ItemId => "debris_field_large_boulder";
        public override string DisplayName => "Large Boulder";
        public override string Description => "Massive rock fallen from above";
        public override List<string> OutcomeKeywords => new() { "boulder", "large", "massive", "fallen", "heavy", "huge", "solid", "gray", "settled", "imposing" };
    }
    
    public sealed class RockDust : Item
    {
        public override string ItemId => "debris_field_rock_dust";
        public override string DisplayName => "Rock Dust";
        public override string Description => "Fine powder from the impact";
        public override List<string> OutcomeKeywords => new() { "dust", "powder", "fine", "gray", "covering", "settled", "impact", "pulverized", "coating", "residue" };
    }
}
