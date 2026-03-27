using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Young Maple Group - A cluster of young maple saplings.
/// Associated with: Brightwood
/// </summary>
public class YoungMapleGroupNode : NarrationNode
{
    public override string NodeId => "young_maple_group";
    public override string ContextDescription => "examining the young maples";
    public override string TransitionDescription => "approach the maples";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a slender <sapling> reaching toward the canopy"), KeywordInContext.Parse("a fresh <leaf> trembling in the breeze"), KeywordInContext.Parse("a young <maple> with pointed lobes"), KeywordInContext.Parse("a tight <cluster> of stems pushing upward") };
    
    private static readonly string[] Moods = { "vigorous", "young", "thriving", "verdant", "growing", "clustered", "healthy", "vibrant" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} young maple group";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"examining a {mood} young maple group";
    }
    
    public sealed class MapleSeed : Item
    {
        public override string ItemId => "maple_seed";
        public override string DisplayName => "Maple Seed";
        public override string Description => "A winged maple seed, ready to spin";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a spinning <samara> drifting on the air"), KeywordInContext.Parse("a tiny <achene> clinging to the bark"), KeywordInContext.Parse("a papery <wing> catching the light") };
    }
    
    public sealed class MapleLeaf : Item
    {
        public override string ItemId => "young_maple_palmate_leaf";
        public override string DisplayName => "Palmate Maple Leaf";
        public override string Description => "A fresh young maple leaf with pointed lobes";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("the flat <lamina> of the maple leaf"), KeywordInContext.Parse("a pointed <lobe> at the leaf edge"), KeywordInContext.Parse("a fine <vein> branching across the surface") };
    }
}
