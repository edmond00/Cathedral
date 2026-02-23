using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Lower limbs body part region. Contains: feet, legs.
/// </summary>
public class LowerLimbsBodyPart : BodyPart
{
    public override string Id => "lower_limbs";
    public override string DisplayName => "Lower Limbs";
    
    private readonly List<Organ> _organs;
    public override List<Organ> Organs => _organs;
    
    public LowerLimbsBodyPart()
    {
        _organs = new List<Organ>
        {
            new FeetOrgan(),
            new LegsOrgan()
        };
    }
}
