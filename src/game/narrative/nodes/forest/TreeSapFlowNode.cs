using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Tree Sap Flow - A wound in a tree oozing sticky sap.
/// </summary>
public class TreeSapFlowNode : NarrationNode
{
    public override string NodeId => "tree_sap_flow";
    public override string ContextDescription => "examining the sap flow";
    public override string TransitionDescription => "investigate the sap";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the sticky golden <resin> oozing from the bark"), KeywordInContext.Parse("some hardened <amber> sap on the old wound"), KeywordInContext.Parse("the sweet <sap> dripping down the trunk"), KeywordInContext.Parse("the raw <wound> in the bark still weeping") };
    
    private static readonly string[] Moods = { "oozing", "dripping", "sticky", "golden", "amber", "fresh", "crystallizing", "gleaming" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} tree sap flow";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"examining a {mood} tree sap flow";
    }
    
    public sealed class TreeResin : Item
    {
        public override string ItemId => "tree_resin";
        public override string DisplayName => "Tree Resin";
        public override string Description => "Sticky amber resin collected from the tree";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("the fresh <oleoresin> from the tree wound"), KeywordInContext.Parse("a sticky <exudate> pooling below the flow") };
    }
    
    public sealed class CrystallizedSap : Item
    {
        public override string ItemId => "tree_sap_crystallized";
        public override string DisplayName => "Crystallized Sap";
        public override string Description => "Hardened amber crystals of ancient sap";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a golden <crystal> of ancient hardened sap"), KeywordInContext.Parse("a lump of dark <copal> resin from the old wound") };
    }
}
