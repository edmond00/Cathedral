using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Brain body part region. Contains: anamnesis, cerebellum, cerebrum, hippocampus, pineal_gland.
/// </summary>
public class BrainBodyPart : BodyPart
{
    public override string Id => "brain";
    public override string DisplayName => "Brain";
    
    private readonly List<Organ> _organs;
    public override List<Organ> Organs => _organs;
    
    public BrainBodyPart()
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
