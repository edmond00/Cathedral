using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Seasonal Puddle - A temporary pool of water.
/// Associated with: Lowwood
/// </summary>
public class SeasonalPuddleNode : NarrationNode
{
    public override string NodeId => "seasonal_puddle";
    public override string ContextDescription => "examining the seasonal puddle";
    public override string TransitionDescription => "approach the puddle";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a shallow rain-filled <pool> in a dip"), KeywordInContext.Parse("the soft clinging <mud> around its edges"), KeywordInContext.Parse("a blurred <reflection> of the canopy above") };
    
    private static readonly string[] Moods = { "shallow", "murky", "still", "reflective", "temporary", "muddy", "stagnant", "rain-filled" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} seasonal puddle";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"examining a {mood} seasonal puddle";
    }
    
    public override List<Item> GetItems() => new() { new PuddleMud(), new TadpoleWater() };

    public sealed class PuddleMud : Item
    {
        public override string ItemId => "puddle_mud";
        public override string DisplayName => "Puddle Mud";
        public override string Description => "Wet, sticky mud from the puddle edge";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("some fine <silt> settled on the puddle bottom"), KeywordInContext.Parse("the sticky grey <clay> at the water's edge") };
    }
    
    public sealed class TadpoleWater : Item
    {
        public override string ItemId => "seasonal_puddle_tadpole_water";
        public override string DisplayName => "Tadpole Water";
        public override string Description => "Water sample teeming with tiny tadpoles";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a tiny dark <polliwog> darting near the edge"), KeywordInContext.Parse("a cluster of frog <spawn> floating near the centre") };
    }
}
