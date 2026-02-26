using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Encephalon body part region. Contains: anamnesis, cerebellum, cerebrum, hippocampus, pineal_gland.
/// </summary>
public class EncephalonBodyPart : BodyPart
{
    public override string Id => "encephalon";
    public override string DisplayName => "Encephalon";
    
    private readonly List<Organ> _organs;
    public override List<Organ> Organs => _organs;
    
    public EncephalonBodyPart()
    {
        _organs = new List<Organ>
        {
            new AnamnesisOrgan(),
            new CerebellumOrgan(),
            new CerebrumOrgan(),
            new HippocampusOrgan(),
            new PinealGlandOrgan()
        };
    }
}
