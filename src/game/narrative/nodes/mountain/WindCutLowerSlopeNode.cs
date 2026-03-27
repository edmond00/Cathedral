using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class WindCutLowerSlopeNode : PyramidalFeatureNode
{
    public override int MinAltitude => 2;
    public override int MaxAltitude => 6;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(WindCutUpperSlopeNode);
    
    public override string NodeId => "wind_cut_lower_slope";
    public override string ContextDescription => "on the wind-cut lower slope";
    public override string TransitionDescription => "descend to the wind-cut lower slope";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the gentler <slope> below the wind-scoured crest"), KeywordInContext.Parse("a pocket of <shelter> behind the ridge"), KeywordInContext.Parse("the sparse <vegetation> surviving on the lee"), KeywordInContext.Parse("the natural <protection> from the cutting wind") };
    
    private static readonly string[] Moods = { "sheltered", "protected", "descending", "gentler" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} wind-cut lower slope";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"on a {mood} wind-cut lower slope";
    }
    
    public sealed class AccumulatedDebris : Item
    {
        public override string ItemId => "wind_cut_lower_slope_accumulated_debris";
        public override string DisplayName => "Accumulated Debris";
        public override string Description => "Material deposited by wind from above";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a heap of windblown <detritus> on the slope"), KeywordInContext.Parse("the deposited work of <wind> from above"), KeywordInContext.Parse("a <drift> of fine material against the stone") };
    }
    
    public sealed class ShelterStone : Item
    {
        public override string ItemId => "wind_cut_lower_slope_shelter_stone";
        public override string DisplayName => "Shelter Stone";
        public override string Description => "Boulder providing wind protection";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a sheltered <refuge> behind the boulder"), KeywordInContext.Parse("a broad <boulder> blocking the wind"), KeywordInContext.Parse("the effective <windbreak> of a single stone") };
    }
}
