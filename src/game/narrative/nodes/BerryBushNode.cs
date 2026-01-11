using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes;

/// <summary>
/// A berry bush in the forest - the origin of forest blueberries.
/// </summary>
public class BerryBushNode : NarrationNode
{
    public override string NodeId => "berry_bush";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "bush", "berries", "blue", "clustered", "ripe", "sweet", "leaves", "branches", "fruit", "wild" };
    
    public override List<OutcomeBase> PossibleOutcomes => new()
    {
        new ClearingNode()
    };
    
    private static readonly string[] Moods = { "laden", "abundant", "sparse", "thorny", "dense", "flowering", "healthy", "weathered" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} berry bush";
    }
    
    /// <summary>
    /// Forest blueberries - can only come from berry bushes in the forest.
    /// </summary>
    public sealed class ForestBlueberry : Item
    {
        public override string ItemId => "forest_blueberry";
        public override string DisplayName => "Forest Blueberry";
        public override string Description => "A small, dark blue berry picked from a wild forest bush. Sweet and nutritious.";
        public override List<string> OutcomeKeywords => new() { "berry", "fruit", "blue", "dark", "round", "small", "sweet", "ripe", "clustered", "wild" };
    }
}
