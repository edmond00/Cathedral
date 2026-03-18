using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Snout organ (muzzle). Single-part organ unique to beast anatomy.
/// </summary>
public class SnoutOrgan : Organ
{
    public override string Id => "snout";
    public override string DisplayName => "Snout";
    public override string BodyPartId => "muzzle";

    private readonly List<OrganPart> _parts;
    public override List<OrganPart> Parts => _parts;

    public SnoutOrgan()
    {
        _parts = new List<OrganPart> { new SnoutPart() };
    }

    public sealed class SnoutPart : OrganPart
    {
        public override string Id => "snout";
        public override string DisplayName => "Snout";
    }
}
