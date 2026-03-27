using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class RidgeSpineNode : PyramidalFeatureNode
{
    public override int MinAltitude => 0;
    public override int MaxAltitude => 3;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(RidgeFlankNode);
    
    public override string NodeId => "ridge_spine";
    public override string ContextDescription => "traversing the exposed ridge spine";
    public override string TransitionDescription => "climb onto the ridge spine";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the narrow rock <spine> traversing the ridge"), KeywordInContext.Parse("the knife-edge <ridge> exposed to everything"), KeywordInContext.Parse("the sharpness of a stone <knife> along the crest"), KeywordInContext.Parse("a relentless <wind> howling across the spine") };
    
    private static readonly string[] Moods = { "knife-edge", "precipitous", "vertiginous", "exposed" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} ridge spine";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"traversing a {mood} ridge spine";
    }
    
    public sealed class SharpRock : Item
    {
        public override string ItemId => "ridge_spine_sharp_rock";
        public override string DisplayName => "Sharp Rock";
        public override string Description => "Angular stone jutting from the ridge";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a glassy <shard> split from the ridge rock"), KeywordInContext.Parse("a razor-sharp <edge> on the broken stone"), KeywordInContext.Parse("the brutal <angularity> of unweathered rock") };
    }
    
    public sealed class HornfelsChip : Item
    {
        public override string ItemId => "ridge_spine_hornfels_chip";
        public override string DisplayName => "Hornfels Chip";
        public override string Description => "Metamorphic rock fragment collectible from the ridge";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a thin <fragment> of metamorphic rock"), KeywordInContext.Parse("a banded <metamorphic> chip from the ridge"), KeywordInContext.Parse("the extreme <hardness> of the hornfels under hand") };
    }
    
}
