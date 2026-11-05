using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Limbs body part (reptile anatomy): the four legs and nothing else. A great reptile's toes are
/// blunt — it has no claws, and so none of the claw arts. A biped (the carnotaurus) is the same four
/// limbs with its forelegs capped low, not another anatomy.
/// </summary>
public class ReptileLimbsBodyPart : BodyPart
{
    public override string Id => "limbs";
    public override string DisplayName => "Limbs";
    public override bool AcceptsWildcardWounds => true;
    public override string Description =>
        "The four sprawling members of the reptile, thick-boned and blunt-toed, by which it carries its " +
        "weight and drives its charge. Taken whole, they serve the advance rather than the chase.";

    private readonly List<Organ> _organs;
    public override List<Organ> Organs => _organs;

    public ReptileLimbsBodyPart()
    {
        _organs = new List<Organ>
        {
            new BeastLegsOrgan(),
        };
    }
}
