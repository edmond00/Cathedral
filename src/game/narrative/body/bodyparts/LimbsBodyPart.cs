using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Limbs body part (beast anatomy). Encompasses all four limbs (foreleg/hindleg + claws).
/// Equivalent to both human UpperLimbs and LowerLimbs combined.
/// </summary>
public class LimbsBodyPart : BodyPart
{
    public override string Id => "limbs";
    public override string DisplayName => "Limbs";
    public override bool AcceptsWildcardWounds => true;

    private readonly List<Organ> _organs;
    public override List<Organ> Organs => _organs;

    public LimbsBodyPart()
    {
        _organs = new List<Organ>
        {
            new BeastLegsOrgan(),
            new BeastClawsOrgan(),
        };
    }
}
