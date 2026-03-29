using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class WallBaseNode : PyramidalFeatureNode
{
    public override int MinAltitude => 6;
    public override int MaxAltitude => 9;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(WallTopNode);
    
    public override string NodeId => "wall_base";
    public override string ContextDescription => "at the lower cliff wall base";
    public override string TransitionDescription => "descend to the wall base";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a towering rock <wall> filling the sky"), KeywordInContext.Parse("the sheer <cliff> face above"), KeywordInContext.Parse("the deep <shadow> pooled at the wall base"), KeywordInContext.Parse("the broad <foundation> of the cliff wall") };
    
    private static readonly string[] Moods = { "towering", "shadowed", "massive", "imposing" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} wall base";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"at a {mood} wall base";
    }
    
    public override List<Item> GetItems() => new() { new FallenStone(), new ClimbingCracks() };

    public sealed class FallenStone : Item
    {
        public override string ItemId => "wall_base_fallen_stone";
        public override string DisplayName => "Fallen Stone";
        public override string Description => "Large rock that has dropped from above";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a large <fragment> that broke from above"), KeywordInContext.Parse("the evidence of a past <rockfall>"), KeywordInContext.Parse("the scattered <debris> at the wall foot") };
    }
    
    public sealed class ClimbingCracks : Item
    {
        public override string ItemId => "wall_base_climbing_cracks";
        public override string DisplayName => "Climbing Cracks";
        public override string Description => "Fissures offering handholds";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a narrow <crevice> in the cliff face"), KeywordInContext.Parse("a deep <fissure> splitting the wall rock"), KeywordInContext.Parse("a solid <handhold> in the cracked stone") };
    }
}
