using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Face body part region. Contains: ears, eyes, nose, tongue, teeths.
/// </summary>
public class FaceBodyPart : BodyPart
{
    public override string Id => "face";
    public override string DisplayName => "Face";
    
    private readonly List<Organ> _organs;
    public override List<Organ> Organs => _organs;
    
    public FaceBodyPart()
    {
        _organs = new List<Organ>
        {
            new EarsOrgan(),
            new EyesOrgan(),
            new NoseOrgan(),
            new TongueOrgan(),
            new TeethsOrgan()
        };
    }
}
