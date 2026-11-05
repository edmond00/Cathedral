using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Muzzle body part (reptile anatomy): the beast's muzzle with the horns added.
/// Contains: Ears, Eyes, Snout, Tongue, Fangs, Horns. Same id as the beast's, so the zone map, the
/// wounds and everything that reads the region by id are unchanged.
/// </summary>
public class ReptileMuzzleBodyPart : BodyPart
{
    public override string Id => "muzzle";
    public override string DisplayName => "Muzzle";
    public override bool AcceptsWildcardWounds => true;
    public override string Description =>
        "The reptile's armoured head, comprising the organs of its slow and patient senses together " +
        "with its two weapons, the jaw and the horn. Taken whole, it is at once the seat of perception " +
        "and the battering front of the charge.";

    private readonly List<Organ> _organs;
    public override List<Organ> Organs => _organs;

    public ReptileMuzzleBodyPart()
    {
        _organs = new List<Organ>
        {
            new EarsOrgan(),
            new EyesOrgan(),
            new SnoutOrgan(),
            new TongueOrgan(),
            new FangsOrgan(),
            new HornsOrgan(),
        };
    }
}
