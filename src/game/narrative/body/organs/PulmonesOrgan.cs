using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Pulmones organ (trunk). Single-part organ.
/// </summary>
public class PulmonesOrgan : Organ
{
    public override string Id => "pulmones";
    public override string DisplayName => "Pulmones";
    public override string BodyPartId => "trunk";
    
    private readonly List<OrganPart> _parts;
    public override List<OrganPart> Parts => _parts;
    
    public PulmonesOrgan()
    {
        _parts = new List<OrganPart> { new PulmonesPart() };
    }
    
    public sealed class PulmonesPart : OrganPart
    {
        public override string Id => "pulmones";
        public override string DisplayName => "Pulmones";
    }
}
