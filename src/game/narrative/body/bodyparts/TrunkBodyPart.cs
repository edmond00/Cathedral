using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Trunk body part region. Contains: backbone, heart, thorax, viscera, stomach, sex.
/// </summary>
public class TrunkBodyPart : BodyPart
{
    public override string Id => "trunk";
    public override string DisplayName => "Trunk";
    
    private readonly List<Organ> _organs;
    public override List<Organ> Organs => _organs;
    
    public TrunkBodyPart()
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
