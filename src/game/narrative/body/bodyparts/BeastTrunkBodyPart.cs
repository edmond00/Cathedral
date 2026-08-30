using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Trunk body part for beast anatomy. Same internal organs as the human trunk
/// except Genitories are absent (not applicable to beast companions).
/// Contains: Backbone, Heart, Pulmones, Viscera, Paunch, Hepar, Spleen.
/// </summary>
public class BeastTrunkBodyPart : BodyPart
{
    public override string Id => "trunk";
    public override string DisplayName => "Trunk";
    public override bool AcceptsWildcardWounds => true;
    public override string Description =>
        "The great central cavity, containing the vital and sanguific organs. Taken whole, it is the " +
        "vessel of life, the measure of injury the body may sustain before dissolution.";

    private readonly List<Organ> _organs;
    public override List<Organ> Organs => _organs;

    public BeastTrunkBodyPart()
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
        };
    }
}
