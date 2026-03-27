using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Dry Creek Bed - A transversal feature of an empty watercourse.
/// </summary>
public class DryCreekBedNode : NarrationNode
{
    public override string NodeId => "dry_creek_bed";
    public override string ContextDescription => "walking along the dry creek bed";
    public override string TransitionDescription => "follow the dry creek";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("some smooth river <stone>s on the dry floor"), KeywordInContext.Parse("the winding <channel> carved by old floods"), KeywordInContext.Parse("the dry cracked <bed> of the empty creek") };
    
    private static readonly string[] Moods = { "parched", "barren", "empty", "desiccated", "arid", "waterless", "dusty", "desolate" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} dry creek bed";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"walking along a {mood} dry creek bed";
    }
    
    public sealed class SmoothStone : Item
    {
        public override string ItemId => "smooth_creek_stone";
        public override string DisplayName => "Smooth Creek Stone";
        public override string Description => "A water-polished stone from the ancient creek bed";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a smooth rounded <cobble> from the old creek"), KeywordInContext.Parse("a water-polished <pebble> lifted from the bed") };
    }
    
    public sealed class DriedAlgae : Item
    {
        public override string ItemId => "dry_creek_bed_dried_algae";
        public override string DisplayName => "Dried Algae";
        public override string Description => "Brittle algae left from when water flowed here";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a brittle <filament> of dried algae"), KeywordInContext.Parse("a crumbling <crust> left by old water") };
    }
}
