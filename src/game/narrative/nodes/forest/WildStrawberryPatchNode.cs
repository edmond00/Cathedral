using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Wild Strawberry Patch - A ground-hugging carpet of wild strawberries.
/// </summary>
public class WildStrawberryPatchNode : NarrationNode
{
    public override string NodeId => "wild_strawberry_patch";
    public override string ContextDescription => "picking wild strawberries";
    public override string TransitionDescription => "approach the strawberries";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "berry", "runner", "flower", "fragrance" };
    
    private static readonly string[] Moods = { "tiny", "sweet", "abundant", "ground-hugging", "fragrant", "delicate", "wild", "productive" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} wild strawberry patch";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"picking from a {mood} wild strawberry patch";
    }
    
    public sealed class WildStrawberry : Item
    {
        public override string ItemId => "wild_strawberry";
        public override string DisplayName => "Wild Strawberry";
        public override string Description => "Tiny, intensely sweet wild strawberries";
        public override List<string> OutcomeKeywords => new() { "berry", "seed", "fragrance" };
    }
    
    public sealed class StrawberryRunner : Item
    {
        public override string ItemId => "wild_strawberry_runner";
        public override string DisplayName => "Strawberry Runner";
        public override string Description => "A thin stem sending out new plants";
        public override List<string> OutcomeKeywords => new() { "stolon", "stem", "propagation" };
    }
    
    public sealed class StrawberryLeaf : Item
    {
        public override string ItemId => "wild_strawberry_leaf";
        public override string DisplayName => "Strawberry Leaf";
        public override string Description => "Triple-leaflet leaves with serrated edges";
        public override List<string> OutcomeKeywords => new() { "leaflet", "serration", "vein" };
    }
}
