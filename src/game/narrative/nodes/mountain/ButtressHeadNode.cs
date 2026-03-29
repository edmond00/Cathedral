using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class ButtressHeadNode : PyramidalFeatureNode
{
    public override int MinAltitude => 2;
    public override int MaxAltitude => 5;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(ButtressFootNode);
    
    public override string NodeId => "buttress_head";
    public override string ContextDescription => "at the rock buttress head";
    public override string TransitionDescription => "climb to the buttress head";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a jutting rock <buttress> overhead"), KeywordInContext.Parse("a bare <outcrop> thrust from the cliff"), KeywordInContext.Parse("a weathered <crag> commanding the view"), KeywordInContext.Parse("a bold <prominence> rising above the slope") };
    
    private static readonly string[] Moods = { "jutting", "prominent", "protruding", "commanding" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} buttress head";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"at a {mood} buttress head";
    }
    
    public override List<Item> GetItems() => new() { new WeatheredGranite(), new StoneCap() };

    public sealed class WeatheredGranite : Item
    {
        public override string ItemId => "buttress_head_weathered_granite";
        public override string DisplayName => "Weathered Granite";
        public override string Description => "Exposed granite at the buttress top";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a pale <feldspar> crystal in the granite"), KeywordInContext.Parse("a glinting <crystal> embedded in the rock"), KeywordInContext.Parse("a coarse <grit> ground from the stone face") };
    }
    
    public sealed class StoneCap : Item
    {
        public override string ItemId => "buttress_head_stone_cap";
        public override string DisplayName => "Stone Cap";
        public override string Description => "Large capstone crowning the buttress";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("the flat <crown> of the buttress"), KeywordInContext.Parse("this small <summit> perched above"), KeywordInContext.Parse("a broad <boulder> capping the formation") };
    }
}
