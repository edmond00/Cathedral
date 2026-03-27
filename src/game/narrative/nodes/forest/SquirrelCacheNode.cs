using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Squirrel Cache - A hidden stash of nuts and seeds.
/// </summary>
public class SquirrelCacheNode : NarrationNode
{
    public override string NodeId => "squirrel_cache";
    public override string ContextDescription => "discovering the squirrel cache";
    public override string TransitionDescription => "investigate the cache";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a hidden <cache> of nuts in a hollow"), KeywordInContext.Parse("some brown <acorn>s buried just below the surface"), KeywordInContext.Parse("a mixed <hoard> of seeds and nuts found here") };
    
    private static readonly string[] Moods = { "hidden", "secretive", "buried", "hoarded", "stashed", "concealed", "protected", "stored" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} squirrel cache";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"discovering a {mood} squirrel cache";
    }
    
    public sealed class CachedNuts : Item
    {
        public override string ItemId => "cached_nuts";
        public override string DisplayName => "Cached Nuts";
        public override string Description => "A handful of nuts from a squirrel's stash";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("some hard-shelled <caryopsis> nuts from the stash"), KeywordInContext.Parse("a cracked <shell> from a previously eaten nut") };
    }
    
    public sealed class SquirrelFur : Item
    {
        public override string ItemId => "squirrel_cache_fur";
        public override string DisplayName => "Squirrel Fur Tuft";
        public override string Description => "A tuft of grey squirrel fur left at the cache";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a tuft of grey <pelage> from a squirrel's flank"), KeywordInContext.Parse("some bushy <tail> hairs caught on the bark") };
    }
}
