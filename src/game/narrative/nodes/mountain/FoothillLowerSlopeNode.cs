using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class FoothillLowerSlopeNode : PyramidalFeatureNode
{
    public override int MinAltitude => 9;
    public override int MaxAltitude => 10;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(FoothillUpperRiseNode);
    
    public override string NodeId => "foothill_lower_slope";
    public override string ContextDescription => "on the foothill lower slope";
    public override string TransitionDescription => "descend to the foothill lower slope";
    public override bool IsEntryNode => true;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the gentle <foothill> at the mountain edge"), KeywordInContext.Parse("a grassy <slope> descending to the plain"), KeywordInContext.Parse("the open <grassland> meeting the mountain"), KeywordInContext.Parse("the soft <boundary> between worlds") };
    
    private static readonly string[] Moods = { "gentle", "transitional", "peaceful", "welcoming" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} foothill lower slope";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"on a {mood} foothill lower slope";
    }
    
    public sealed class LowGrass : Item
    {
        public override string ItemId => "foothill_lower_slope_low_grass";
        public override string DisplayName => "Low Grass";
        public override string Description => "Short grass at the mountain's edge";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a single grass <blade> bending in the wind"), KeywordInContext.Parse("a wide <meadow> at the mountain foot"), KeywordInContext.Parse("the open <plain> visible far below") };
    }
    
}
