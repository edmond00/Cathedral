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
    public override string Description =>
        "The central vital organ and index of the constitution's warmth, its span of years, and its " +
        "capacity for attachment; the wellspring of ardour and courage. It is the seat of the affective " +
        "disciplines: the bonds of fellowship, the moving of sentiment, and the passions of desire.";

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
        public override int DefaultMaxScore => 5;
    }
}
