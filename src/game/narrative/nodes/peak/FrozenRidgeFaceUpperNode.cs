using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class FrozenRidgeFaceUpperNode : PyramidalFeatureNode
{
    public override int MinAltitude => 1;
    public override int MaxAltitude => 3;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(FrozenRidgeFaceLowerNode);
    
    public override string NodeId => "upper_frozen_ridge_face";
    public override string ContextDescription => "clinging to the upper frozen ridge face";
    public override string TransitionDescription => "climb to the upper frozen ridge face";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a clear sheet of <ice> on the upper face"), KeywordInContext.Parse("the knife-edge <ridge> locked in frozen rock"), KeywordInContext.Parse("the vertical ice <face> above the lip"), KeywordInContext.Parse("the technical <climbing> demanded by the ice") };
    
    private static readonly string[] Moods = { "vertical", "frozen", "challenging", "exposed" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} upper frozen ridge face";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"clinging to a {mood} upper frozen ridge face";
    }
    
    public sealed class GlacierPolishedRock : Item
    {
        public override string ItemId => "frozen_ridge_face_upper_glacier_polished_rock";
        public override string DisplayName => "Glacier-Polished Rock";
        public override string Description => "Stone embedded in ice, polished by glacial movement";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a smooth <cobble> polished by glacial grinding"), KeywordInContext.Parse("the fine <polish> left by moving ice"), KeywordInContext.Parse("the glass-like <smoothness> of the glacier surface") };
    }
}
