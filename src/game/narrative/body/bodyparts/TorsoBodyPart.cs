using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Torso body part region. Contains: backbone, heart, thorax, viscera, stomach, sex.
/// </summary>
public class TorsoBodyPart : BodyPart
{
    public override string Id => "torso";
    public override string DisplayName => "Torso";
    
    private readonly List<Organ> _organs;
    public override List<Organ> Organs => _organs;
    
    public TorsoBodyPart()
    {
        _organs = new List<Organ>
        {
            new BackboneOrgan(),
            new HeartOrgan(),
            new ThoraxOrgan(),
            new VisceraOrgan(),
            new StomachOrgan(),
            new SexOrgan()
        };
    }
}
