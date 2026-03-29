using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Wild Herbs - A patch of aromatic forest herbs.
/// </summary>
public class WildHerbsNode : NarrationNode
{
    public override string NodeId => "wild_herbs";
    public override string ContextDescription => "examining wild herbs";
    public override string TransitionDescription => "investigate the herbs";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a familiar aromatic <herb> growing low to the ground"), KeywordInContext.Parse("a small serrated <leaf> releasing scent when crushed"), KeywordInContext.Parse("a sharp <scent> rising from bruised stems underfoot"), KeywordInContext.Parse("a patch of plants known for their <medicine> in the towns") };
    
    private static readonly string[] Moods = { "fragrant", "aromatic", "pungent", "fresh", "medicinal", "wild", "potent", "green" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} wild herbs";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"examining a {mood} wild herbs";
    }
    
    public override List<Item> GetItems() => new() { new HerbBundle(), new HerbRoot() };

    public sealed class HerbBundle : Item
    {
        public override string ItemId => "wild_herb_bundle";
        public override string DisplayName => "Wild Herb Bundle";
        public override string Description => "A bundle of aromatic forest herbs";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("some strongly <aromatic> herb stems bound together"), KeywordInContext.Parse("a small <fascicle> of dried herb sprigs tied with a stem"), KeywordInContext.Parse("a bundle whose <medicine> is known to those who study plants") };
    }
    
    public sealed class HerbRoot : Item
    {
        public override string ItemId => "wild_herbs_herb_root";
        public override string DisplayName => "Herb Root";
        public override string Description => "A pungent root from a medicinal herb";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a knobbly <rhizome> dug from beneath the herb patch"), KeywordInContext.Parse("a root whose <medicine> is concentrated in its flesh"), KeywordInContext.Parse("a pungent smell speaking of its <potency> when prepared") };
    }
}
