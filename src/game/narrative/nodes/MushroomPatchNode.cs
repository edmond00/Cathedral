using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes;

/// <summary>
/// A patch of mushrooms growing in the forest - the origin of forest mushrooms.
/// </summary>
public class MushroomPatchNode : NarrationNode
{
    public override string NodeId => "mushroom_patch";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "fungus", "cap", "pale", "white", "stem", "gills", "ground", "damp", "earthy", "round" };
    
    public override List<OutcomeBase> PossibleOutcomes => new()
    {
        new ClearingNode()
    };
    
    private static readonly string[] Moods = { "clustered", "solitary", "sprawling", "hidden", "fresh", "old", "colorful", "shadowy" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} mushroom patch";
    }
    
    /// <summary>
    /// Forest mushrooms - can only come from mushroom patches in the forest.
    /// </summary>
    public sealed class ForestMushroom : Item
    {
        public override string ItemId => "forest_mushroom";
        public override string DisplayName => "Forest Mushroom";
        public override string Description => "A wild mushroom with a pale cap, picked from the forest floor. Potentially edible or medicinal.";
        public override List<string> OutcomeKeywords => new() { "fungus", "cap", "pale", "white", "stem", "gills", "earthy", "round", "spores", "wild" };
    }
}
