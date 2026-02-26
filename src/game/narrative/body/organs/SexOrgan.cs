using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Sex organ (trunk). Single-part organ.
/// </summary>
public class SexOrgan : Organ
{
    public override string Id => "sex";
    public override string DisplayName => "Sex";
    public override string BodyPartId => "trunk";
    
    private readonly List<OrganPart> _parts;
    public override List<OrganPart> Parts => _parts;
    
    public SexOrgan()
    {
        _parts = new List<OrganPart> { new SexPart() };
    }
    
    public sealed class SexPart : OrganPart
    {
        public override string Id => "sex";
        public override string DisplayName => "Sex";
    }
}
