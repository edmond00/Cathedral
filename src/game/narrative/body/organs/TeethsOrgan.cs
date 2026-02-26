using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Teeths organ (visage). Single-part organ.
/// </summary>
public class TeethsOrgan : Organ
{
    public override string Id => "teeths";
    public override string DisplayName => "Teeths";
    public override string BodyPartId => "visage";
    
    private readonly List<OrganPart> _parts;
    public override List<OrganPart> Parts => _parts;
    
    public TeethsOrgan()
    {
        _parts = new List<OrganPart> { new TeethsPart() };
    }
    
    public sealed class TeethsPart : OrganPart
    {
        public override string Id => "teeths";
        public override string DisplayName => "Teeths";
    }
}
