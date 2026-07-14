using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Nose organ (visage). Single-part organ.
/// </summary>
public class NoseOrgan : Organ
{
    public override string Id => "nose";
    public override string DisplayName => "Nose";
    public override string BodyPartId => "visage";
    public override string Description =>
        "The olfactory organ, the medium of caution and discernment. It is the seat of the disciplines " +
        "that turn upon scent: the lore of herbs and fungi, brewing and fermentation, and the tracking " +
        "of what cannot be seen.";

    private readonly List<OrganPart> _parts;
    public override List<OrganPart> Parts => _parts;
    
    public NoseOrgan()
    {
        _parts = new List<OrganPart> { new NosePart() };
    }
    
    public sealed class NosePart : OrganPart
    {
        public override string Id => "nose";
        public override string DisplayName => "Nose";
        public override int DefaultMaxScore => 3;
    }
}
