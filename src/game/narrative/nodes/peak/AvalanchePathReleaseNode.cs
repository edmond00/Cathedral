using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class AvalanchePathReleaseNode : PyramidalFeatureNode
{
    public override int MinAltitude => 6;
    public override int MaxAltitude => 10;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(AvalanchePathRunoutNode);
    
    public override string NodeId => "avalanche_path_release";
    public override string ContextDescription => "standing in the avalanche release zone";
    public override string TransitionDescription => "reach the avalanche release zone";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the unbearable <tension> in the loaded snowpack"), KeywordInContext.Parse("a deep <fracture> line across the slope"), KeywordInContext.Parse("a packed <snow> slab ready to release"), KeywordInContext.Parse("the absolute <danger> of the release zone") };
    
    private static readonly string[] Moods = { "dangerous", "unstable", "steep", "threatening" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} release zone";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing in a {mood} release zone";
    }
    
    public override List<Item> GetItems() => new() { new FractureLine() };

    public sealed class FractureLine : Item
    {
        public override string ItemId => "avalanche_path_release_fracture_line";
        public override string DisplayName => "Fracture Line";
        public override string Description => "Line where avalanche released";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("the exposed <scarp> where the slab released"), KeywordInContext.Parse("a clean <crack> running through the snowpack"), KeywordInContext.Parse("the broken <snow> at the fracture line") };
    }
    
}
