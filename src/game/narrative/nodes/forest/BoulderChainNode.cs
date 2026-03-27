using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Boulder Chain - A transversal feature of large stones forming a path.
/// </summary>
public class BoulderChainNode : NarrationNode
{
    public override string NodeId => "boulder_chain";
    public override string ContextDescription => "climbing along the boulder chain";
    public override string TransitionDescription => "follow the boulders";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a massive mossy <boulder> blocking the way"), KeywordInContext.Parse("some crusty orange <lichen> across the stone face"), KeywordInContext.Parse("a worn ancient <rock> warming in the shade"), KeywordInContext.Parse("this long <chain> of stones leading onward") };
    
    private static readonly string[] Moods = { "ancient", "massive", "imposing", "weathered", "solid", "monolithic", "enduring", "timeless" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} boulder chain";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"climbing along a {mood} boulder chain";
    }
    
    public sealed class LichenSample : Item
    {
        public override string ItemId => "boulder_lichen";
        public override string DisplayName => "Boulder Lichen";
        public override string Description => "Crusty lichen growing on ancient stone";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a flaking <crust> of lichen on the stone"), KeywordInContext.Parse("an ancient <symbiosis> of alga and fungus"), KeywordInContext.Parse("a bright <pigment> colouring the rock surface") };
    }
    
    public sealed class StoneDust : Item
    {
        public override string ItemId => "boulder_chain_stone_dust";
        public override string DisplayName => "Stone Dust";
        public override string Description => "Fine mineral dust weathered from the boulders";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a fine <powder> of weathered stone"), KeywordInContext.Parse("some <mineral> dust catching the light"), KeywordInContext.Parse("a coarse <grit> of ground-down rock") };
    }
}
