using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Spleen organ (trunk). Single-part organ.
/// </summary>
public class SpleenOrgan : Organ
{
    public override string Id => "spleen";
    public override string DisplayName => "Spleen";
    public override string BodyPartId => "trunk";
    public override string Description =>
        "Of the four sanguific organs, that of the melancholic temperament; it elaborates the humors as " +
        "its fellows do, yet withholds the atrabilious humor from ordinary circulation, which grave and " +
        "particular occasions alone provoke.";

    private readonly List<OrganPart> _parts;
    public override List<OrganPart> Parts => _parts;
    
    public SpleenOrgan()
    {
        _parts = new List<OrganPart> { new SpleenPart() };
    }
    
    public sealed class SpleenPart : OrganPart
    {
        public override string Id => "spleen";
        public override string DisplayName => "Spleen";
    }
}
