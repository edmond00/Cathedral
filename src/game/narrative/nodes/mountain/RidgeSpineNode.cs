using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class RidgeSpineNode : PyramidalFeatureNode
{
    public override int MinAltitude => 0;
    public override int MaxAltitude => 3;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(RidgeFlankNode);
    
    public override string NodeId => "ridge_spine";
    public override string ContextDescription => "traversing the exposed ridge spine";
    public override string TransitionDescription => "climb onto the ridge spine";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "spine", "ridge", "knife", "wind" };
    
    private static readonly string[] Moods = { "knife-edge", "precipitous", "vertiginous", "exposed" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} ridge spine";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"traversing a {mood} ridge spine";
    }
    
    public sealed class SharpRock : Item
    {
        public override string ItemId => "ridge_spine_sharp_rock";
        public override string DisplayName => "Sharp Rock";
        public override string Description => "Angular stone jutting from the ridge";
        public override List<string> OutcomeKeywords => new() { "rock", "edge", "angularity" };
    }
    
    public sealed class HornfelsChip : Item
    {
        public override string ItemId => "ridge_spine_hornfels_chip";
        public override string DisplayName => "Hornfels Chip";
        public override string Description => "Metamorphic rock fragment collectible from the ridge";
        public override List<string> OutcomeKeywords => new() { "hornfels", "chip", "metamorphic" };
    }
    
    public sealed class NarrowPath : Item
    {
        public override string ItemId => "ridge_spine_narrow_path";
        public override string DisplayName => "Narrow Path";
        public override string Description => "Thin walkable line along the ridge crest";
        public override List<string> OutcomeKeywords => new() { "path", "traverse", "passage" };
    }
}
