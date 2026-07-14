using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Upper limbs body part region. Contains: hands, arms.
/// </summary>
public class UpperLimbsBodyPart : BodyPart
{
    public override string Id => "upper_limbs";
    public override string DisplayName => "Upper Limbs";
    public override bool AcceptsWildcardWounds => true;
    public override string Description =>
        "The upper members, the arms and hands together, by which the body grasps, labours, and strikes. " +
        "Taken whole, they are the frame's readiest instrument of unarmed force.";

    private readonly List<Organ> _organs;
    public override List<Organ> Organs => _organs;
    
    public UpperLimbsBodyPart()
    {
        _organs = new List<Organ>
        {
            new HandsOrgan(),
            new ArmsOrgan()
        };
    }
}
