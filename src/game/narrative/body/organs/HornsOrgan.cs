using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Horns organ (muzzle). Single-part organ unique to reptile anatomy: every horn of the head taken
/// together, brow and nose, as the fangs are every tooth. A species with no more than bony bosses
/// keeps the organ at a low cap rather than losing it, so the anatomy stays one anatomy.
/// </summary>
public class HornsOrgan : Organ
{
    public override string Id => "horns";
    public override string DisplayName => "Horns";
    public override string BodyPartId => "muzzle";
    public override string Description =>
        "The bony armament of the reptilian head, sheathed in horn and borne before it: the instrument " +
        "of the goring charge and of the rival's measure. The seat of the arts of the thrust and the toss.";

    private readonly List<OrganPart> _parts;
    public override List<OrganPart> Parts => _parts;

    public HornsOrgan()
    {
        _parts = new List<OrganPart> { new HornsPart() };
    }

    public sealed class HornsPart : OrganPart
    {
        public override string Id => "horns";
        public override string DisplayName => "Horns";
    }
}
