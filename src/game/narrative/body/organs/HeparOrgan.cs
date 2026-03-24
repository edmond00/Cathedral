using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Hepar organ (trunk). Single-part organ.
/// </summary>
public class HeparOrgan : Organ
{
    public override string Id => "hepar";
    public override string DisplayName => "Hepar";
    public override string BodyPartId => "trunk";
    
    private readonly List<OrganPart> _parts;
    public override List<OrganPart> Parts => _parts;
    
    public HeparOrgan()
    {
        _parts = new List<OrganPart> { new HeparPart() };
    }
    
    public sealed class HeparPart : OrganPart
    {
        public override string Id => "hepar";
        public override string DisplayName => "Hepar";
    }
}
