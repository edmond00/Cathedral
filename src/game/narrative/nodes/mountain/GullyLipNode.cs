using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class GullyLipNode : PyramidalFeatureNode
{
    public override int MinAltitude => 5;
    public override int MaxAltitude => 8;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(GullyBottomNode);
    
    public override string NodeId => "gully_lip";
    public override string ContextDescription => "at the shaded gully lip";
    public override string TransitionDescription => "approach the gully lip";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the crumbling <brink> of the gully opening"), KeywordInContext.Parse("the rough <rim> where the gully cuts deep"), KeywordInContext.Parse("the uncertain <edge> above the drop"), KeywordInContext.Parse("a sudden <drop> into shadow below") };
    
    private static readonly string[] Moods = { "shaded", "dark", "shadowy", "dim" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} gully lip";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"at a {mood} gully lip";
    }
    
    public sealed class OverhangingFern : Item
    {
        public override string ItemId => "gully_lip_overhanging_fern";
        public override string DisplayName => "Overhanging Fern";
        public override string Description => "Lush fern growing at the gully edge";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a broad <frond> hanging over the edge"), KeywordInContext.Parse("a delicate <pinnule> brushing the stone"), KeywordInContext.Parse("the slow <droop> of the fern over the gully") };
    }
    
}
