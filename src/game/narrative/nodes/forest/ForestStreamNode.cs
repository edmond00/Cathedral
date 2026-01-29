using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Forest Stream - A transversal feature connecting areas with flowing water.
/// </summary>
public class ForestStreamNode : NarrationNode
{
    public override string NodeId => "forest_stream";
    public override string ContextDescription => "following the forest stream";
    public override string TransitionDescription => "follow the stream";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "flowing", "water", "babbling", "current", "stones", "banks", "ripples", "fresh", "clear", "cool" };
    
    private static readonly string[] Moods = { "babbling", "rushing", "trickling", "murmuring", "gurgling", "flowing", "cascading", "meandering" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} forest stream";
    }
    
    public sealed class StreamWater : Item
    {
        public override string ItemId => "stream_water";
        public override string DisplayName => "Stream Water";
        public override string Description => "Clear, cold water flowing from upstream";
        public override List<string> OutcomeKeywords => new() { "clear", "cold", "fresh", "flowing", "drinkable", "cool", "pure", "clean", "transparent", "crisp" };
    }
}
