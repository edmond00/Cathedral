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
}
