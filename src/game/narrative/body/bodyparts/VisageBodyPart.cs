using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Visage body part region. Contains: ears, eyes, nose, tongue, teeths.
/// </summary>
public class VisageBodyPart : BodyPart
{
    public override string Id => "visage";
    public override string DisplayName => "Visage";
    public override bool AcceptsWildcardWounds => true;
    public override string Description =>
        "The countenance, comprising the organs of the special senses seated in the face. Taken whole, " +
        "it governs the comeliness of the face, the favour it procures upon a first acquaintance.";

    private readonly List<Organ> _organs;
    public override List<Organ> Organs => _organs;
    
    public VisageBodyPart()
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
