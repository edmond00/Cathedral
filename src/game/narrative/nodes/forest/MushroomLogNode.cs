using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Mushroom Log - A decaying log covered in shelf mushrooms.
/// </summary>
public class MushroomLogNode : NarrationNode
{
    public override string NodeId => "mushroom_log";
    public override string ContextDescription => "examining mushrooms on the log";
    public override string TransitionDescription => "inspect the mushroom log";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a large <bracket> fungus jutting from the log"), KeywordInContext.Parse("the earthy smell of <fungus> on the bark"), KeywordInContext.Parse("the soft inner <decay> of the log"), KeywordInContext.Parse("the crumbling <wood> riddled with fungal threads") };
    
    private static readonly string[] Moods = { "layered", "shelf-like", "overlapping", "decaying", "fungal", "rotting", "tiered", "clustered" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} mushroom log";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"examining a {mood} mushroom log";
    }
    
    public override List<Item> GetItems() => new() { new ShelfMushroom(), new DecayedLogWood(), new BeetleHole() };

    public sealed class ShelfMushroom : Item
    {
        public override string ItemId => "shelf_mushroom";
        public override string DisplayName => "Shelf Mushroom";
        public override string Description => "A tough, bracket-like shelf mushroom";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a tough <polypore> from the log surface"), KeywordInContext.Parse("a woody layered <conk> growing from the side") };
    }
    
    public sealed class DecayedLogWood : Item
    {
        public override string ItemId => "mushroom_log_rotten_wood";
        public override string DisplayName => "Rotten Wood";
        public override string Description => "Soft, decaying wood riddled with fungal threads";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("the soft <punky> rot at the log's core"), KeywordInContext.Parse("some white <mycelium> threads through the wood") };
    }
    
    public sealed class BeetleHole : Item
    {
        public override string ItemId => "mushroom_log_beetle_hole";
        public override string DisplayName => "Beetle Gallery";
        public override string Description => "Network of beetle tunnels carved through the decaying wood";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a network of beetle <gallery>s in the wood"), KeywordInContext.Parse("a winding <tunnel> carved by bark beetles") };
    }
}
