using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Rooted Forest - Level 12. Exposed root systems and erosion channels.
/// </summary>
public class RootedForestNode : NarrationNode
{
    public override string NodeId => "rooted_forest";
    public override string ContextDescription => "climbing through the rooted forest";
    public override string TransitionDescription => "enter the rooted forest";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("an exposed <root> like a ridgeline underfoot"), KeywordInContext.Parse("the interlaced <web> of surface roots"), KeywordInContext.Parse("the <erosion> channel cut between exposed roots"), KeywordInContext.Parse("the vast <network> of roots threading the ground") };
    
    private static readonly string[] Moods = { "gnarled", "exposed", "twisted", "contorted", "webbed", "interlaced", "serpentine", "convoluted" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} rooted forest";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"climbing through a {mood} rooted forest";
    }
    
    public sealed class ExposedRootFiber : Item
    {
        public override string ItemId => "rooted_forest_exposed_root_fiber";
        public override string DisplayName => "Exposed Root Fiber";
        public override string Description => "Fibrous strands from massive exposed roots";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a thick exposed <taproot> rising from below"), KeywordInContext.Parse("a fine <radicle> at the tip of the root") };
    }
    
    public sealed class RootGrip : Item
    {
        public override string ItemId => "rooted_forest_root_grip";
        public override string DisplayName => "Root Grip";
        public override string Description => "Sturdy root section useful for climbing";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a firm <holdfast> root section to grab onto"), KeywordInContext.Parse("a good natural <handhold> in the root curve") };
    }
}
