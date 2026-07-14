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
    public override string Description =>
        "The organs of locomotion, mediating the body's celerity. They are the seat of the disciplines " +
        "of movement: running and springing, agility, and the pursuits and evasions of the body in " +
        "motion.";

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
        public override int DefaultMaxScore => 4;
    }
    
    public sealed class RightLegPart : OrganPart
    {
        public override string Id => "right_leg";
        public override string DisplayName => "Right Leg";
        public override int DefaultMaxScore => 4;
    }
}
