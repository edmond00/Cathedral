using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Tongue organ (visage). Single-part organ.
/// </summary>
public class TongueOrgan : Organ
{
    public override string Id => "tongue";
    public override string DisplayName => "Tongue";
    public override string BodyPartId => "visage";
    
    private readonly List<OrganPart> _parts;
    public override List<OrganPart> Parts => _parts;
    
    public TongueOrgan()
    {
        _parts = new List<OrganPart> { new TonguePart() };
    }
    
    public sealed class TonguePart : OrganPart
    {
        public override string Id => "tongue";
        public override string DisplayName => "Tongue";
    }
}
