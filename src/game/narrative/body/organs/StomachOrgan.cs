using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Stomach organ (trunk). Single-part organ.
/// </summary>
public class StomachOrgan : Organ
{
    public override string Id => "stomach";
    public override string DisplayName => "Stomach";
    public override string BodyPartId => "trunk";
    
    private readonly List<OrganPart> _parts;
    public override List<OrganPart> Parts => _parts;
    
    public StomachOrgan()
    {
        _parts = new List<OrganPart> { new StomachPart() };
    }
    
    public sealed class StomachPart : OrganPart
    {
        public override string Id => "stomach";
        public override string DisplayName => "Stomach";
    }
}
