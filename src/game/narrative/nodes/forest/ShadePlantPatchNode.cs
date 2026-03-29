using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Shade Plant Patch - Specialized plants adapted to deep shade.
/// Associated with: DeepCanopy
/// </summary>
public class ShadePlantPatchNode : NarrationNode
{
    public override string NodeId => "shade_plant_patch";
    public override string ContextDescription => "examining shade-loving plants";
    public override string TransitionDescription => "investigate the shade plants";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the permanent <shade> these plants have adapted to"), KeywordInContext.Parse("a broad dark-green <leaf> catching dim light"), KeywordInContext.Parse("the remarkable <adaptation> to lightless conditions") };
    
    private static readonly string[] Moods = { "adapted", "shade-loving", "dim", "specialized", "dark-green", "tolerant", "low-growing", "sparse" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} shade plant patch";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"examining a {mood} shade plant patch";
    }
    
    public override List<Item> GetItems() => new() { new BroadLeaf(), new ShadeFern() };

    public sealed class BroadLeaf : Item
    {
        public override string ItemId => "shade_broad_leaf";
        public override string DisplayName => "Broad Shade Leaf";
        public override string Description => "A wide, dark green leaf from a shade plant";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("the wide dark <lamina> of the shade plant leaf"), KeywordInContext.Parse("the pale <veining> spread across the dark surface") };
    }
    
    public sealed class ShadeFern : Item
    {
        public override string ItemId => "shade_plant_fern";
        public override string DisplayName => "Shade Fern";
        public override string Description => "A delicate shade-adapted fern frond";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a delicate <frond> adapted for deep shade"), KeywordInContext.Parse("a tiny <pinnule> at the frond's tip") };
    }
}
