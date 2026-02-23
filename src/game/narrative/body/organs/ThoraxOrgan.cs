using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Thorax organ (torso). Single-part organ.
/// </summary>
public class ThoraxOrgan : Organ
{
    public override string Id => "thorax";
    public override string DisplayName => "Thorax";
    public override string BodyPartId => "torso";
    
    private readonly List<OrganPart> _parts;
    public override List<OrganPart> Parts => _parts;
    
    public ThoraxOrgan()
    {
        _parts = new List<OrganPart> { new ThoraxPart() };
    }
    
    public sealed class ThoraxPart : OrganPart
    {
        public override string Id => "thorax";
        public override string DisplayName => "Thorax";
    }
}
