using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class FrozenOutwashPlainFlatsNode : PyramidalFeatureNode
{
    public override int MinAltitude => 9;
    public override int MaxAltitude => 10;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(FrozenOutwashPlainMarginNode);
    
    public override string NodeId => "frozen_outwash_plain_flats";
    public override string ContextDescription => "standing on the frozen outwash plain flats";
    public override string TransitionDescription => "descend to the frozen outwash plain flats";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "outwash", "flats", "plain", "frozen", "flat", "expansive", "ice", "barren", "cold", "open" };
    
    private static readonly string[] Moods = { "expansive", "flat", "barren", "open" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} frozen flats";
    }
    
    public sealed class OutwashGravel : Item
    {
        public override string ItemId => "frozen_outwash_plain_flats_outwash_gravel";
        public override string DisplayName => "Outwash Gravel";
        public override string Description => "Glacial outwash gravel collectible from the flats";
        public override List<string> OutcomeKeywords => new() { "gravel", "outwash", "glacial", "pebbles", "sediment", "rounded", "mixed", "sorted", "deposited", "collectible" };
    }
    
    public sealed class OutwashClay : Item
    {
        public override string ItemId => "frozen_outwash_plain_flats_outwash_clay";
        public override string DisplayName => "Outwash Clay";
        public override string Description => "Fine glacial clay collectible from the flats";
        public override List<string> OutcomeKeywords => new() { "clay", "outwash", "fine", "glacial", "sediment", "smooth", "grey", "compact", "mineral", "collectible" };
    }
}
