using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Beech Stand - A grove of smooth-barked beech trees.
/// Associated with: Brightwood
/// </summary>
public class BeechStandNode : NarrationNode
{
    public override string NodeId => "beech_stand";
    public override string ContextDescription => "walking through the beech stand";
    public override string TransitionDescription => "enter the beeches";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a rough <bark> of the beech"), KeywordInContext.Parse("some scattered <mast> on the ground"), KeywordInContext.Parse("some copper <leaves> drifting down"), KeywordInContext.Parse("a tall smooth-barked <beech> ahead") };
    
    private static readonly string[] Moods = { "elegant", "silvery", "stately", "smooth-barked", "graceful", "noble", "towering", "luminous" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} beech stand";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"walking through a {mood} beech stand";
    }
    
    public sealed class Beechnut : Item
    {
        public override string ItemId => "beechnut";
        public override string DisplayName => "Beechnut";
        public override string Description => "A triangular beechnut in its spiny husk";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a triangular <nut> in its spiny case"), KeywordInContext.Parse("the prickly <husk> split open on the ground"), KeywordInContext.Parse("a pale oily <kernel> inside the shell") };
    }
    
    public sealed class BeechLeaf : Item
    {
        public override string ItemId => "beech_stand_copper_leaf";
        public override string DisplayName => "Copper Beech Leaf";
        public override string Description => "A copper-colored beech leaf with delicate veins";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a delicate <vein> tracing through the copper leaf"), KeywordInContext.Parse("the deep <ochre> colour of an autumn beech leaf"), KeywordInContext.Parse("a thin <petiole> still attached to the branch") };
    }
}
