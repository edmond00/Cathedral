using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class IceCliffBaseNode : PyramidalFeatureNode
{
    public override int MinAltitude => 2;
    public override int MaxAltitude => 5;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(IceCliffTopNode);
    
    public override string NodeId => "ice_cliff_base";
    public override string ContextDescription => "standing at the ice cliff base";
    public override string TransitionDescription => "approach the ice cliff base";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a clear sheet of <ice> on the cliff face"), KeywordInContext.Parse("the vertical ice <cliff> rising above"), KeywordInContext.Parse("the glassy ice <wall> blocking the route"), KeywordInContext.Parse("the cold <glitter> of sunlight on the ice cliff") };
    
    private static readonly string[] Moods = { "frigid", "glittering", "towering", "ominous" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} ice cliff base";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing at a {mood} ice cliff base";
    }
    
    public sealed class GlacierDebris : Item
    {
        public override string ItemId => "ice_cliff_base_glacier_debris";
        public override string DisplayName => "Glacier Debris";
        public override string Description => "Rock fragments deposited by glacier movement";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("the mixed <detritus> at the glacier base"), KeywordInContext.Parse("the dark lateral <moraine> beside the cliff"), KeywordInContext.Parse("a jagged <rock> frozen into the glacier") };
    }
    
    public sealed class FrozenDebris : Item
    {
        public override string ItemId => "ice_cliff_base_frozen_debris";
        public override string DisplayName => "Frozen Debris";
        public override string Description => "Ice-encased debris at the cliff base";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("the brittle <detritus> locked in the cliff base"), KeywordInContext.Parse("a thick block of <ice> encasing the debris"), KeywordInContext.Parse("the perfect <encasement> of rock in glacier ice") };
    }
}
