using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Trunk body part (reptile anatomy): the beast's trunk with the thagomizer at the end of the tail.
/// Contains: Backbone, Heart, Pulmones, Viscera, Paunch, Hepar, Spleen, Thagomizer.
/// </summary>
public class ReptileTrunkBodyPart : BodyPart
{
    public override string Id => "trunk";
    public override string DisplayName => "Trunk";
    public override bool AcceptsWildcardWounds => true;
    public override string Description =>
        "The long scaled barrel and the tail drawn out behind it, containing the vital and sanguific " +
        "organs. Taken whole, it is the vessel of life, the measure of injury the body may sustain " +
        "before dissolution.";

    private readonly List<Organ> _organs;
    public override List<Organ> Organs => _organs;

    public ReptileTrunkBodyPart()
    {
        _organs = new List<Organ>
        {
            new BackboneOrgan(),
            new HeartOrgan(),
            new PulmonesOrgan(),
            new VisceraOrgan(),
            new PaunchOrgan(),
            new HeparOrgan(),
            new SpleenOrgan(),
            new ThagomizerOrgan(),
        };
    }
}
