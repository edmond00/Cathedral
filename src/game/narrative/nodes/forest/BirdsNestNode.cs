using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Bird's Nest - A discovered nest with eggs or nestlings.
/// </summary>
public class BirdsNestNode : NarrationNode
{
    public override string NodeId => "birds_nest";
    public override string ContextDescription => "observing the bird's nest";
    public override string TransitionDescription => "examine the nest";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a woven <nest> tucked into the fork"), KeywordInContext.Parse("some pale blue <eggs> inside the cup"), KeywordInContext.Parse("some soft <feathers> lining the interior"), KeywordInContext.Parse("an interlaced lattice of <twigs> around the rim") };
    
    private static readonly string[] Moods = { "delicate", "woven", "hidden", "cozy", "occupied", "empty", "intricate", "abandoned" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} bird's nest";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"observing a {mood} bird's nest";
    }
    
    public sealed class BirdFeather : Item
    {
        public override string ItemId => "bird_feather";
        public override string DisplayName => "Bird Feather";
        public override string Description => "A small feather from the nest lining";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a fine <plume> from the nest lining"), KeywordInContext.Parse("a tiny <barb> split from the feather shaft"), KeywordInContext.Parse("some soft grey <down> still clinging to the cup") };
    }
    
    public sealed class EggshellFragment : Item
    {
        public override string ItemId => "birds_nest_eggshell";
        public override string DisplayName => "Eggshell Fragment";
        public override string Description => "A pale blue eggshell fragment from a hatched egg";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a thin <calcareous> shell fragment"), KeywordInContext.Parse("some blue <pigment> on the broken shell") };
    }
    
    public sealed class TwigBundle : Item
    {
        public override string ItemId => "birds_nest_twig_bundle";
        public override string DisplayName => "Twig Bundle";
        public override string Description => "Small interwoven twigs from the nest structure";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("some plant <fiber> woven into the structure"), KeywordInContext.Parse("a careful <weave> holding the cup together") };
    }
}
