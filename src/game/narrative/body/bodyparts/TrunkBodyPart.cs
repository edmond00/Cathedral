using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Trunk body part region. Contains: backbone, heart, pulmones, viscera, paunch, genitories, spleen, hepar.
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
            new PulmonesOrgan(),
            new VisceraOrgan(),
            new PaunchOrgan(),
            new GenitoriesOrgan(),
            new SpleenOrgan(),
            new HeparOrgan()
        };
    }
}
