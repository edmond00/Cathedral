using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class WindCutUpperSlopeNode : PyramidalFeatureNode
{
    public override int MinAltitude => 2;
    public override int MaxAltitude => 6;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(WindCutLowerSlopeNode);
    
    public override string NodeId => "wind_cut_upper_slope";
    public override string ContextDescription => "on the wind-cut upper slope";
    public override string TransitionDescription => "climb to the wind-cut upper slope";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the brutal <wind> cutting across the upper slope"), KeywordInContext.Parse("the exposed upper <slope> scoured bare"), KeywordInContext.Parse("the advanced <erosion> of the wind-cut face"), KeywordInContext.Parse("the total <barrenness> of the wind-scoured rock") };
    
    private static readonly string[] Moods = { "wind-scoured", "barren", "exposed", "harsh" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} wind-cut upper slope";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"on a {mood} wind-cut upper slope";
    }
    
    public override List<Item> GetItems() => new() { new ErodedRock() };

    public sealed class ErodedRock : Item
    {
        public override string ItemId => "wind_cut_upper_slope_eroded_rock";
        public override string DisplayName => "Eroded Rock";
        public override string Description => "Stone worn smooth by constant wind";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a smooth <cobble> shaped by years of wind"), KeywordInContext.Parse("the relentless <wind> that sculpts the slope"), KeywordInContext.Parse("the deep <erosion> cut into the upper face") };
    }
    
}
