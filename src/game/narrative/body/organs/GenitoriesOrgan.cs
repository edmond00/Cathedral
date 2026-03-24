using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Genitories organ (trunk). Single-part organ.
/// </summary>
public class GenitoriesOrgan : Organ
{
    public override string Id => "genitories";
    public override string DisplayName => "Genitories";
    public override string BodyPartId => "trunk";
    
    private readonly List<OrganPart> _parts;
    public override List<OrganPart> Parts => _parts;
    
    public GenitoriesOrgan()
    {
        _parts = new List<OrganPart> { new GenitoriesPart() };
    }
    
    public sealed class GenitoriesPart : OrganPart
    {
        public override string Id => "genitories";
        public override string DisplayName => "Genitories";
    }
}
