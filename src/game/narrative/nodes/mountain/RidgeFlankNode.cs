using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class RidgeFlankNode : PyramidalFeatureNode
{
    public override int MinAltitude => 0;
    public override int MaxAltitude => 3;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(RidgeSpineNode);
    
    public override string NodeId => "ridge_flank";
    public override string ContextDescription => "on the exposed ridge flank";
    public override string TransitionDescription => "descend to the ridge flank";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the angled <flank> of the ridge dropping below"), KeywordInContext.Parse("a steep <slope> of mixed grass and stone"), KeywordInContext.Parse("the last thin <grass> clinging to the flank"), KeywordInContext.Parse("a loose <rock> skittering down the side") };
    
    private static readonly string[] Moods = { "sloping", "angled", "steep", "descending" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} ridge flank";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"on a {mood} ridge flank";
    }
    
    public sealed class AlpineGrass : Item
    {
        public override string ItemId => "ridge_flank_alpine_grass";
        public override string DisplayName => "Alpine Grass";
        public override string Description => "Hardy grass clinging to the ridge side";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a rigid <culm> bending in the alpine wind"), KeywordInContext.Parse("a hardy <montane> grass clump on the ridge"), KeywordInContext.Parse("the stubborn <persistence> of life on stone") };
    }
    
    public sealed class SlopeDebris : Item
    {
        public override string ItemId => "ridge_flank_slope_debris";
        public override string DisplayName => "Slope Debris";
        public override string Description => "Loose rock and gravel on the flank";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a scatter of rocky <detritus> on the slope"), KeywordInContext.Parse("a shifting bed of loose <gravel> underfoot"), KeywordInContext.Parse("the unnerving <instability> of the flank") };
    }
}
