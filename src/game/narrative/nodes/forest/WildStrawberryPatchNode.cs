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
    
    public override List<string> NodeKeywords => new() { "red", "tiny", "sweet", "ground", "runners", "white", "flowers", "berries", "fragrant", "delicate" };
    
    private static readonly string[] Moods = { "tiny", "sweet", "abundant", "ground-hugging", "fragrant", "delicate", "wild", "productive" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} wild strawberry patch";
    }
    
    public sealed class WildStrawberry : Item
    {
        public override string ItemId => "wild_strawberry";
        public override string DisplayName => "Wild Strawberry";
        public override string Description => "Tiny, intensely sweet wild strawberries";
        public override List<string> OutcomeKeywords => new() { "red", "tiny", "sweet", "seeds", "fragrant", "berries", "fruit", "intense", "delicate", "wild" };
    }
}
