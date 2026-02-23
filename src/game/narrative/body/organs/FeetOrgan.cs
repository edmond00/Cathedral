using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Feet organ (lower_limbs). Multi-part organ: left foot, right foot.
/// </summary>
public class FeetOrgan : Organ
{
    public override string Id => "feet";
    public override string DisplayName => "Feet";
    public override string BodyPartId => "lower_limbs";
    
    private readonly List<OrganPart> _parts;
    public override List<OrganPart> Parts => _parts;
    
    public FeetOrgan()
    {
        _parts = new List<OrganPart> { new LeftFootPart(), new RightFootPart() };
    }
    
    public sealed class LeftFootPart : OrganPart
    {
        public override string Id => "left_foot";
        public override string DisplayName => "Left Foot";
    }
    
    public sealed class RightFootPart : OrganPart
    {
        public override string Id => "right_foot";
        public override string DisplayName => "Right Foot";
    }
}
