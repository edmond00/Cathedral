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
    
    public override string NodeId => "lower_ice_crusted_ledge";
    public override string ContextDescription => "standing on the lower ice-crusted ledge";
    public override string TransitionDescription => "descend to the lower ice-crusted ledge";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a narrow <ledge> crusted with old ice"), KeywordInContext.Parse("a clear sheet of <ice> over the ledge rock"), KeywordInContext.Parse("a small <shelter> beneath an icy overhang"), KeywordInContext.Parse("a shallow <recess> in the frozen cliff") };
    
    private static readonly string[] Moods = { "sheltered", "shadowed", "frozen", "narrow" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} lower ice-crusted ledge";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing on a {mood} lower ice-crusted ledge";
    }
    
    public override List<Item> GetItems() => new() { new FrostLichen() };

    public sealed class FrostLichen : Item
    {
        public override string ItemId => "ice_crusted_ledge_lower_frost_lichen";
        public override string DisplayName => "Frost Lichen";
        public override string Description => "Frost-resistant lichen collectible from the lower ledge";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a flat <crustose> lichen on the icy ledge"), KeywordInContext.Parse("a delicate <rime> of frost on the rock"), KeywordInContext.Parse("the tough <symbiosis> of lichen in extreme cold") };
    }
}
