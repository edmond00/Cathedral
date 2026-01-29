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
    
    public override List<string> NodeKeywords => new() { "aromatic", "fragrant", "green", "leaves", "medicinal", "scent", "pungent", "herbs", "plants", "fresh" };
    
    private static readonly string[] Moods = { "fragrant", "aromatic", "pungent", "fresh", "medicinal", "wild", "potent", "green" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} wild herbs";
    }
    
    public sealed class HerbBundle : Item
    {
        public override string ItemId => "wild_herb_bundle";
        public override string DisplayName => "Wild Herb Bundle";
        public override string Description => "A bundle of aromatic forest herbs";
        public override List<string> OutcomeKeywords => new() { "aromatic", "green", "fresh", "bundled", "fragrant", "tied", "herbs", "medicinal", "leaves", "pungent" };
    }
}
