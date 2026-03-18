using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Fangs organ (muzzle). Single-part organ unique to beast anatomy.
/// MaxScore can be overridden per species (wolf has higher than cat).
/// </summary>
public class FangsOrgan : Organ
{
    public override string Id => "fangs";
    public override string DisplayName => "Fangs";
    public override string BodyPartId => "muzzle";

    private readonly List<OrganPart> _parts;
    public override List<OrganPart> Parts => _parts;

    public FangsOrgan()
    {
        _parts = new List<OrganPart> { new FangsPart() };
    }

    public sealed class FangsPart : OrganPart
    {
        public override string Id => "fangs";
        public override string DisplayName => "Fangs";
    }
}
