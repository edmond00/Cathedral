using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class FrozenOutwashPlainMarginNode : PyramidalFeatureNode
{
    public override int MinAltitude => 9;
    public override int MaxAltitude => 10;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(FrozenOutwashPlainFlatsNode);
    
    public override string NodeId => "frozen_outwash_plain_margin";
    public override string ContextDescription => "standing at the frozen outwash plain margin";
    public override string TransitionDescription => "reach the frozen outwash plain margin";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "outwash", "margin", "glacier", "transition" };
    
    private static readonly string[] Moods = { "transitional", "frozen", "marginal", "boundary" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} ice margin";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing at a {mood} ice margin";
    }
    
    public sealed class GlacialCobble : Item
    {
        public override string ItemId => "frozen_outwash_plain_margin_glacial_cobble";
        public override string DisplayName => "Glacial Cobble";
        public override string Description => "Glacier-rounded stone collectible from the margin";
        public override List<string> OutcomeKeywords => new() { "cobble", "glacier", "stone" };
    }
}
