using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Beast claws organ (limbs). Multi-part organ: left/right fore-claws and hind-claws.
/// </summary>
public class BeastClawsOrgan : Organ
{
    public override string Id => "claws";
    public override string DisplayName => "Claws";
    public override string BodyPartId => "limbs";

    private readonly List<OrganPart> _parts;
    public override List<OrganPart> Parts => _parts;

    public BeastClawsOrgan()
    {
        _parts = new List<OrganPart>
        {
            new LeftForeclaws(),
            new RightForeclaws(),
            new LeftHindclaws(),
            new RightHindclaws(),
        };
    }

    public sealed class LeftForeclaws : OrganPart
    {
        public override string Id => "left_foreclaws";
        public override string DisplayName => "Left Foreclaws";
    }

    public sealed class RightForeclaws : OrganPart
    {
        public override string Id => "right_foreclaws";
        public override string DisplayName => "Right Foreclaws";
    }

    public sealed class LeftHindclaws : OrganPart
    {
        public override string Id => "left_hindclaws";
        public override string DisplayName => "Left Hindclaws";
    }

    public sealed class RightHindclaws : OrganPart
    {
        public override string Id => "right_hindclaws";
        public override string DisplayName => "Right Hindclaws";
    }
}
