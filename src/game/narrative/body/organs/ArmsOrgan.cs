using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Arms organ (upper_limbs). Multi-part organ: left arm, right arm.
/// </summary>
public class ArmsOrgan : Organ
{
    public override string Id => "arms";
    public override string DisplayName => "Arms";
    public override string BodyPartId => "upper_limbs";
    
    private readonly List<OrganPart> _parts;
    public override List<OrganPart> Parts => _parts;
    
    public ArmsOrgan()
    {
        _parts = new List<OrganPart> { new LeftArmPart(), new RightArmPart() };
    }
    
    public sealed class LeftArmPart : OrganPart
    {
        public override string Id => "left_arm";
        public override string DisplayName => "Left Arm";
    }
    
    public sealed class RightArmPart : OrganPart
    {
        public override string Id => "right_arm";
        public override string DisplayName => "Right Arm";
    }
}
