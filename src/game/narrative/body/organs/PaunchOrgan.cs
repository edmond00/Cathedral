using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Paunch organ (trunk). Single-part organ.
/// </summary>
public class PaunchOrgan : Organ
{
    public override string Id => "paunch";
    public override string DisplayName => "Paunch";
    public override string BodyPartId => "trunk";
    public override string Description =>
        "Of the four sanguific organs, that of digestion; it renders the humors according to its " +
        "condition, chiefly the sanguine when sound, the bilious when disordered.";

    private readonly List<OrganPart> _parts;
    public override List<OrganPart> Parts => _parts;
    
    public PaunchOrgan()
    {
        _parts = new List<OrganPart> { new PaunchPart() };
    }
    
    public sealed class PaunchPart : OrganPart
    {
        public override string Id => "paunch";
        public override string DisplayName => "Paunch";
    }
}
