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
    
    public override List<string> NodeKeywords => new() { "cache", "acorn", "nut", "hoard" };
    
    private static readonly string[] Moods = { "hidden", "secretive", "buried", "hoarded", "stashed", "concealed", "protected", "stored" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} squirrel cache";
    }
    
    public sealed class CachedNuts : Item
    {
        public override string ItemId => "cached_nuts";
        public override string DisplayName => "Cached Nuts";
        public override string Description => "A handful of nuts from a squirrel's stash";
        public override List<string> OutcomeKeywords => new() { "nut", "acorn", "shell" };
    }
    
    public sealed class SquirrelFur : Item
    {
        public override string ItemId => "squirrel_cache_fur";
        public override string DisplayName => "Squirrel Fur Tuft";
        public override string Description => "A tuft of grey squirrel fur left at the cache";
        public override List<string> OutcomeKeywords => new() { "fur", "tail", "squirrel" };
    }
}
