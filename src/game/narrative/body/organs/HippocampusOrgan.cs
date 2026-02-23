using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Hippocampus organ (brain). Single-part organ.
/// </summary>
public class HippocampusOrgan : Organ
{
    public override string Id => "hippocampus";
    public override string DisplayName => "Hippocampus";
    public override string BodyPartId => "brain";
    
    private readonly List<OrganPart> _parts;
    public override List<OrganPart> Parts => _parts;
    
    public HippocampusOrgan()
    {
        _parts = new List<OrganPart> { new HippocampusPart() };
    }
    
    public sealed class HippocampusPart : OrganPart
    {
        public override string Id => "hippocampus";
        public override string DisplayName => "Hippocampus";
    }
}
