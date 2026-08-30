using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Hippocampus organ (encephalon). Single-part organ.
/// </summary>
public class HippocampusOrgan : Organ
{
    public override string Id => "hippocampus";
    public override string DisplayName => "Hippocampus";
    public override string BodyPartId => "encephalon";
    public override string Description =>
        "The organ that first registers the impressions conveyed inward by the senses. It is the seat " +
        "of the receptive and imaginative faculties: the vivid recollection of experience, the composing " +
        "arts, and openness to what is newly met.";

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
