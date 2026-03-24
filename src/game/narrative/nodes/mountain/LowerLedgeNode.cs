using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class LowerLedgeNode : PyramidalFeatureNode
{
    public override int MinAltitude => 1;
    public override int MaxAltitude => 3;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(UpperLedgeNode);
    
    public override string NodeId => "lower_ledge";
    public override string ContextDescription => "on the lower stone ledge";
    public override string TransitionDescription => "descend to the lower ledge";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "ledge", "lower", "shelf", "narrow", "stone", "below", "shadowed", "protected", "alcove", "recess" };
    
    private static readonly string[] Moods = { "shadowed", "sheltered", "recessed", "protected" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} lower ledge";
    }
    
    public sealed class MossyStone : Item
    {
        public override string ItemId => "lower_ledge_mossy_stone";
        public override string DisplayName => "Mossy Stone";
        public override string Description => "Moisture-covered rock on the ledge";
        public override List<string> OutcomeKeywords => new() { "mossy", "stone", "damp", "green", "covered", "moist", "soft", "sheltered", "fuzzy", "wet" };
    }
    
    public sealed class CalciteFormation : Item
    {
        public override string ItemId => "lower_ledge_calcite_formation";
        public override string DisplayName => "Calcite Formation";
        public override string Description => "Limestone deposit collectible from the ledge";
        public override List<string> OutcomeKeywords => new() { "calcite", "limestone", "formation", "deposit", "white", "crystalline", "layered", "mineral", "carbonate", "collectible" };
    }
}
