using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Isolated Oak - A single majestic oak tree standing alone.
/// </summary>
public class IsolatedOakNode : NarrationNode
{
    public override string NodeId => "isolated_oak";
    public override string ContextDescription => "examining the isolated oak";
    public override string TransitionDescription => "approach the oak";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "massive", "trunk", "branches", "bark", "acorns", "crown", "solitary", "ancient", "gnarled", "spreading" };
    
    private static readonly string[] Moods = { "majestic", "solitary", "towering", "ancient", "venerable", "impressive", "grand", "stalwart" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} isolated oak";
    }
    
    public sealed class Acorn : Item
    {
        public override string ItemId => "oak_acorn";
        public override string DisplayName => "Acorn";
        public override string Description => "A brown acorn with its cap intact";
        public override List<string> OutcomeKeywords => new() { "brown", "cap", "seed", "nut", "smooth", "oval", "woody", "hard", "acorn", "small" };
    }
}
