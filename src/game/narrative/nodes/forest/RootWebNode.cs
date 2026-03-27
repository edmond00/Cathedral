using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Root Web - An intricate network of exposed surface roots.
/// Associated with: RootedForest
/// </summary>
public class RootWebNode : NarrationNode
{
    public override string NodeId => "root_web";
    public override string ContextDescription => "navigating the root web";
    public override string TransitionDescription => "step through the root web";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("an exposed surface <root> crossing the path"), KeywordInContext.Parse("the intricate <network> of roots in the soil"), KeywordInContext.Parse("this branching <maze> underfoot"), KeywordInContext.Parse("the spreading <web> of interconnected roots") };
    
    private static readonly string[] Moods = { "intricate", "interwoven", "complex", "network", "maze-like", "tangled", "spreading", "interconnected" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} root web";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"navigating a {mood} root web";
    }
    
    public sealed class WebRootFiber : Item
    {
        public override string ItemId => "root_fiber";
        public override string DisplayName => "Root Fibers";
        public override string Description => "Thin, tough fibers from surface roots";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a thin wiry <tendril> of surface root"), KeywordInContext.Parse("a tough root <fiber> pulled from the tangle") };
    }
    
    public sealed class RootBark : Item
    {
        public override string ItemId => "root_web_root_bark";
        public override string DisplayName => "Root Bark";
        public override string Description => "Papery bark peeling from exposed roots";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("the green <cambium> beneath the root bark"), KeywordInContext.Parse("a thin <peel> of papery bark from the root surface") };
    }
}
