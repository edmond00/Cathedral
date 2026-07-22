using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Viscera organ (trunk). Single-part organ.
/// </summary>
public class VisceraOrgan : Organ
{
    public override string Id => "viscera";
    public override string DisplayName => "Viscera";
    public override string BodyPartId => "trunk";
    public override string Description =>
        "The deep viscera, in which the constitution's temper and fortitude reside, steadying the frame " +
        "under duress. They are the seat of the disciplines of hardihood: the ferocity of the fray, the " +
        "endurance of privation, and the coarse and bloody labours.";

    private readonly List<OrganPart> _parts;
    public override List<OrganPart> Parts => _parts;
    
    public VisceraOrgan()
    {
        _parts = new List<OrganPart> { new VisceraPart() };
    }
    
    public sealed class VisceraPart : OrganPart
    {
        public override string Id => "viscera";
        public override string DisplayName => "Viscera";
        public override int DefaultMaxScore => 5;
    }
}
