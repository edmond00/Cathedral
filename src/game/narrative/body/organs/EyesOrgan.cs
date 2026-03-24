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
    }
    
    public sealed class RightEyePart : OrganPart
    {
        public override string Id => "right_eye";
        public override string DisplayName => "Right Eye";
    }
}
