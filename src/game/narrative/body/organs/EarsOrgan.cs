using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Ears organ (visage). Multi-part organ: left ear, right ear.
/// </summary>
public class EarsOrgan : Organ
{
    public override string Id => "ears";
    public override string DisplayName => "Ears";
    public override string BodyPartId => "visage";
    public override string Description =>
        "The organs of audition, and of the body's promptitude in answering what occurs about it. They " +
        "are the seat of the disciplines of attentive hearing: music, vigilance, and the finer " +
        "attunements of social exchange.";

    private readonly List<OrganPart> _parts;
    public override List<OrganPart> Parts => _parts;
    
    public EarsOrgan()
    {
        _parts = new List<OrganPart> { new LeftEarPart(), new RightEarPart() };
    }
    
    public sealed class LeftEarPart : OrganPart
    {
        public override string Id => "left_ear";
        public override string DisplayName => "Left Ear";
        public override int DefaultMaxScore => 3;
    }
    
    public sealed class RightEarPart : OrganPart
    {
        public override string Id => "right_ear";
        public override string DisplayName => "Right Ear";
        public override int DefaultMaxScore => 3;
    }
}
