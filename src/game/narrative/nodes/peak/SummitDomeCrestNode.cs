using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class SummitDomeCrestNode : PyramidalFeatureNode
{
    public override int MinAltitude => 0;
    public override int MaxAltitude => 1;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(SummitDomeShoulderNode);
    
    public override string NodeId => "summit_dome_crest";
    public override string ContextDescription => "standing atop the summit dome crest";
    public override string TransitionDescription => "ascend to the summit dome crest";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "highest", "exposed", "windswept", "crest", "panoramic", "sky", "frozen", "summit", "peak", "crown" };
    
    private static readonly string[] Moods = { "windswept", "exposed", "majestic", "austere" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} summit dome crest";
    }
    
    public sealed class FrozenCrystal : Item
    {
        public override string ItemId => "summit_dome_crest_frozen_crystal";
        public override string DisplayName => "Frozen Crystal";
        public override string Description => "Ice crystal formed by extreme altitude";
        public override List<string> OutcomeKeywords => new() { "crystal", "ice", "frozen", "altitude", "pristine", "delicate", "geometric", "glittering", "cold", "pure" };
    }
    
    public sealed class SummitPolishedStone : Item
    {
        public override string ItemId => "summit_polished_stone";
        public override string DisplayName => "Summit-Polished Stone";
        public override string Description => "Stone smoothed by endless winds";
        public override List<string> OutcomeKeywords => new() { "stone", "polished", "smooth", "wind", "weathered", "ancient", "hard", "worn", "exposed", "enduring" };
    }
    
    public sealed class SummitGranite : Item
    {
        public override string ItemId => "summit_dome_crest_summit_granite";
        public override string DisplayName => "Summit Granite";
        public override string Description => "Peak granite collectible from the highest point";
        public override List<string> OutcomeKeywords => new() { "granite", "summit", "igneous", "crystalline", "speckled", "hard", "ancient", "dense", "peak", "collectible" };
    }
}
