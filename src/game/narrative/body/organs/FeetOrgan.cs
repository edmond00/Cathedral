using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Feet organ (lower_limbs). Multi-part organ: left foot, right foot.
/// </summary>
public class FeetOrgan : Organ
{
    public override string Id => "feet";
    public override string DisplayName => "Feet";
    public override bool AcceptsWildcardWounds => true;
    public override bool PartsAreIndependentMediums => true; // left & right foot strike independently
    public override string BodyPartId => "lower_limbs";
    public override string Description =>
        "The organs of stance and endurance in travel, bearing the body over distance and holding it " +
        "sure upon uncertain ground. They are the seat of the disciplines of quiet movement and the long " +
        "road: stealth, wayfaring, and sure footing.";

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
        public override int DefaultMaxScore => 3;
    }
    
    public sealed class RightFootPart : OrganPart
    {
        public override string Id => "right_foot";
        public override string DisplayName => "Right Foot";
        public override int DefaultMaxScore => 3;
    }
}
