using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Viscera organ (trunk). Single-part organ.
/// </summary>
public class VisceraOrgan : Organ
{
    public override string Id => "viscera";
    public override string DisplayName => "Viscera";
    public override string BodyPartId => "trunk";
    
    private readonly List<OrganPart> _parts;
    public override List<OrganPart> Parts => _parts;
    
    public VisceraOrgan()
    {
        _parts = new List<OrganPart> { new VisceraPart() };
    }
    
    public sealed class VisceraPart : OrganPart
    {
        public override string Id => "viscera";
        public override string DisplayName => "Viscera";
    }
}
