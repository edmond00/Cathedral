using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Muzzle body part (beast anatomy). Equivalent to the human Visage.
/// Contains: Ears, Eyes, Snout, Tongue, Fangs.
/// </summary>
public class MuzzleBodyPart : BodyPart
{
    public override string Id => "muzzle";
    public override string DisplayName => "Muzzle";
    public override bool AcceptsWildcardWounds => true;

    private readonly List<Organ> _organs;
    public override List<Organ> Organs => _organs;

    public MuzzleBodyPart()
    {
        _organs = new List<Organ>
        {
            new EarsOrgan(),
            new EyesOrgan(),
            new SnoutOrgan(),
            new TongueOrgan(),
            new FangsOrgan(),
        };
    }
}
