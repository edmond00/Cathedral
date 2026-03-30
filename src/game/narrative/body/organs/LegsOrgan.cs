using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Legs organ (lower_limbs). Multi-part organ: left leg, right leg.
/// </summary>
public class LegsOrgan : Organ
{
    public override string Id => "legs";
    public override string DisplayName => "Legs";
    public override bool AcceptsWildcardWounds => true;
    public override string BodyPartId => "lower_limbs";
    
    private readonly List<OrganPart> _parts;
    public override List<OrganPart> Parts => _parts;
    
    public LegsOrgan()
    {
        _parts = new List<OrganPart> { new LeftLegPart(), new RightLegPart() };
    }
    
    public sealed class LeftLegPart : OrganPart
    {
        public override string Id => "left_leg";
        public override string DisplayName => "Left Leg";
    }
    
    public sealed class RightLegPart : OrganPart
    {
        public override string Id => "right_leg";
        public override string DisplayName => "Right Leg";
    }
}
