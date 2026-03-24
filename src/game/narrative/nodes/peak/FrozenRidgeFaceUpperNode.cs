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
    
    public override string NodeId => "frozen_ridge_face_upper";
    public override string ContextDescription => "clinging to the upper frozen ridge face";
    public override string TransitionDescription => "climb to the upper frozen ridge face";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "face", "upper", "frozen", "vertical", "ridge", "ice", "steep", "exposed", "climbing", "wall" };
    
    private static readonly string[] Moods = { "vertical", "frozen", "challenging", "exposed" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} upper frozen ridge face";
    }
    
    public sealed class IceHold : Item
    {
        public override string ItemId => "frozen_ridge_face_upper_ice_hold";
        public override string DisplayName => "Ice Hold";
        public override string Description => "Frozen protrusion for climbing";
        public override List<string> OutcomeKeywords => new() { "ice", "hold", "protrusion", "grip", "frozen", "climbing", "knob", "handhold", "solid", "jutting" };
    }
    
    public sealed class FrozenCrack : Item
    {
        public override string ItemId => "frozen_ridge_face_upper_frozen_crack";
        public override string DisplayName => "Frozen Crack";
        public override string Description => "Ice-filled crack in the face";
        public override List<string> OutcomeKeywords => new() { "crack", "frozen", "ice", "fissure", "filled", "narrow", "vertical", "cold", "seam", "crevice" };
    }
    
    public sealed class GlacierPolishedRock : Item
    {
        public override string ItemId => "frozen_ridge_face_upper_glacier_polished_rock";
        public override string DisplayName => "Glacier-Polished Rock";
        public override string Description => "Stone embedded in ice, polished by glacial movement";
        public override List<string> OutcomeKeywords => new() { "rock", "polished", "glacier", "smooth", "embedded", "stone", "rounded", "hard", "ancient", "collectible" };
    }
}
