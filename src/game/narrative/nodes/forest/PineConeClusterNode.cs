using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Pine Cone Cluster - A collection of fallen pine cones.
/// </summary>
public class PineConeClusterNode : NarrationNode
{
    public override string NodeId => "pine_cone_cluster";
    public override string ContextDescription => "gathering pine cones";
    public override string TransitionDescription => "collect pine cones";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a woody <cone> with its scales wide open"), KeywordInContext.Parse("a thin curved <scale> from the pine cone"), KeywordInContext.Parse("a tiny winged <seed> tucked behind each scale"), KeywordInContext.Parse("a sticky drop of <resin> on the cone surface") };
    
    private static readonly string[] Moods = { "scattered", "abundant", "dry", "fallen", "plentiful", "numerous", "clustered", "collected" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} pine cone cluster";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"gathering from a {mood} pine cone cluster";
    }
    
    public sealed class PineCone : Item
    {
        public override string ItemId => "pine_cone";
        public override string DisplayName => "Pine Cone";
        public override string Description => "A woody pine cone with open scales";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a woody pine <strobilus> heavy with scales"), KeywordInContext.Parse("a sticky drop of <resin> on the cone surface") };
    }
    
    public sealed class PineNeedle : Item
    {
        public override string ItemId => "pine_cone_cluster_needles";
        public override string DisplayName => "Pine Needles";
        public override string Description => "A bundle of fallen pine needles";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a <fascicle> of paired pine needles"), KeywordInContext.Parse("a sharp resinous <bundle> of fallen needles") };
    }
}
