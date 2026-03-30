using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Hands organ (upper_limbs). Multi-part organ: left hand, right hand.
/// </summary>
public class HandsOrgan : Organ
{
    public override string Id => "hands";
    public override string DisplayName => "Hands";
    public override bool AcceptsWildcardWounds => true;
    public override string BodyPartId => "upper_limbs";
    
    private readonly List<OrganPart> _parts;
    public override List<OrganPart> Parts => _parts;
    
    public HandsOrgan()
    {
        _parts = new List<OrganPart> { new LeftHandPart(), new RightHandPart() };
    }
    
    public sealed class LeftHandPart : OrganPart
    {
        public override string Id => "left_hand";
        public override string DisplayName => "Left Hand";
    }
    
    public sealed class RightHandPart : OrganPart
    {
        public override string Id => "right_hand";
        public override string DisplayName => "Right Hand";
    }
}
