using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Ancient Tree Giant - A truly massive, centuries-old tree.
/// Associated with: Oldgrowth
/// </summary>
public class AncientTreeGiantNode : NarrationNode
{
    public override string NodeId => "ancient_tree_giant";
    public override string ContextDescription => "standing before the ancient giant";
    public override string TransitionDescription => "approach the giant tree";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a massive gnarled <trunk> rising above"), KeywordInContext.Parse("the enormous <girth> of the ancient tree"), KeywordInContext.Parse("a silent forest <patriarch> standing apart"), KeywordInContext.Parse("some <centuries> of wind and rain worn into the bark") };
    
    private static readonly string[] Moods = { "massive", "ancient", "venerable", "primeval", "enormous", "patriarch", "timeless", "monumental" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} ancient tree giant";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing before a {mood} ancient tree giant";
    }
    
    public override List<Item> GetItems() => new() { new AncientBark(), new TreeLichen() };

    public sealed class AncientBark : Item
    {
        public override string ItemId => "ancient_bark";
        public override string DisplayName => "Ancient Bark";
        public override string Description => "A piece of deeply furrowed bark from the ancient tree";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a deep <fissure> splitting the old bark"), KeywordInContext.Parse("some dark <furrow>s carved by time"), KeywordInContext.Parse("the visible <age> written into every groove") };
    }
    
    public sealed class TreeLichen : Item
    {
        public override string ItemId => "ancient_tree_lichen";
        public override string DisplayName => "Ancient Lichen Patch";
        public override string Description => "Centuries-old lichen growing on the weathered bark";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a grey <crust> of lichen on the bark"), KeywordInContext.Parse("an ancient <symbiosis> of fungus and alga"), KeywordInContext.Parse("a slow pale <growth> spreading across the weathered wood") };
    }
}
