using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Beast legs organ (limbs). Multi-part organ: left/right foreleg and hindleg.
/// </summary>
public class BeastLegsOrgan : Organ
{
    public override string Id => "legs";
    public override string DisplayName => "Legs";
    public override string BodyPartId => "limbs";

    private readonly List<OrganPart> _parts;
    public override List<OrganPart> Parts => _parts;

    public BeastLegsOrgan()
    {
        _parts = new List<OrganPart>
        {
            new LeftForeleg(),
            new RightForeleg(),
            new LeftHindleg(),
            new RightHindleg(),
        };
    }

    public sealed class LeftForeleg : OrganPart
    {
        public override string Id => "left_foreleg";
        public override string DisplayName => "Left Foreleg";
    }

    public sealed class RightForeleg : OrganPart
    {
        public override string Id => "right_foreleg";
        public override string DisplayName => "Right Foreleg";
    }

    public sealed class LeftHindleg : OrganPart
    {
        public override string Id => "left_hindleg";
        public override string DisplayName => "Left Hindleg";
    }

    public sealed class RightHindleg : OrganPart
    {
        public override string Id => "right_hindleg";
        public override string DisplayName => "Right Hindleg";
    }
}
