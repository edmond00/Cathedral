using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Beehive - A wild bee colony in a tree hollow.
/// </summary>
public class BeehiveNode : NarrationNode
{
    public override string NodeId => "beehive";
    public override string ContextDescription => "carefully observing the beehive";
    public override string TransitionDescription => "approach the hive cautiously";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "buzzing", "honey", "wax", "insects", "flying", "entrance", "busy", "golden", "swarm", "colony" };
    
    private static readonly string[] Moods = { "buzzing", "busy", "active", "thriving", "humming", "industrious", "protective", "organized" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} beehive";
    }
    
    public sealed class WildHoneycomb : Item
    {
        public override string ItemId => "wild_honeycomb";
        public override string DisplayName => "Wild Honeycomb";
        public override string Description => "A small piece of honeycomb carefully extracted";
        public override List<string> OutcomeKeywords => new() { "golden", "hexagonal", "sweet", "wax", "honey", "cells", "sticky", "aromatic", "comb", "nectar" };
    }
    
    public sealed class BeeswaxFragment : Item
    {
        public override string ItemId => "beehive_beeswax_fragment";
        public override string DisplayName => "Beeswax Fragment";
        public override string Description => "A small piece of pure beeswax from the hive";
        public override List<string> OutcomeKeywords => new() { "yellow", "waxy", "smooth", "pliable", "aromatic", "pure", "malleable", "soft", "fragrant", "golden" };
    }
    
    public sealed class RoyalJelly : Item
    {
        public override string ItemId => "beehive_royal_jelly";
        public override string DisplayName => "Royal Jelly";
        public override string Description => "Precious milky substance secreted by worker bees";
        public override List<string> OutcomeKeywords => new() { "milky", "white", "creamy", "nutritious", "precious", "protein", "secretion", "glandular", "queen", "rich" };
    }
}
