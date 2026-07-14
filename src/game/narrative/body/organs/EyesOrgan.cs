using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Eyes organ (visage). Multi-part organ: left eye, right eye.
/// </summary>
public class EyesOrgan : Organ
{
    public override string Id => "eyes";
    public override string DisplayName => "Eyes";
    public override string BodyPartId => "visage";
    public override string Description =>
        "The organs of vision, mediating perception at a distance. They are the seat of the disciplines " +
        "that rest upon the trained eye: observation and scrutiny, aim and marksmanship, the reading of " +
        "terrain and structure, and the exactitude of visual craft.";

    private readonly List<OrganPart> _parts;
    public override List<OrganPart> Parts => _parts;
    
    public EyesOrgan()
    {
        _parts = new List<OrganPart> { new LeftEyePart(), new RightEyePart() };
    }
    
    public sealed class LeftEyePart : OrganPart
    {
        public override string Id => "left_eye";
        public override string DisplayName => "Left Eye";
        public override int DefaultMaxScore => 3;
    }
    
    public sealed class RightEyePart : OrganPart
    {
        public override string Id => "right_eye";
        public override string DisplayName => "Right Eye";
        public override int DefaultMaxScore => 3;
    }
}
