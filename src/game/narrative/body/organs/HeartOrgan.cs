using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Heart organ (trunk). Single-part organ.
/// </summary>
public class HeartOrgan : Organ
{
    public override string Id => "heart";
    public override string DisplayName => "Heart";
    public override string BodyPartId => "trunk";
    
    private readonly List<OrganPart> _parts;
    public override List<OrganPart> Parts => _parts;
    
    public HeartOrgan()
    {
        _parts = new List<OrganPart> { new HeartPart() };
    }
    
    public sealed class HeartPart : OrganPart
    {
        public override string Id => "heart";
        public override string DisplayName => "Heart";
    }
}
