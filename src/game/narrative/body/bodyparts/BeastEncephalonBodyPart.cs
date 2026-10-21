using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// A beast's encephalon: the human's region without the pineal gland. Contains: anamnesis, cerebellum,
/// cerebrum, hippocampus.
///
/// <para>The pineal gland is the organ of inward regard — self-awareness, the contemplative and
/// intuitive arts — and a beast has none. Leaving it out is the whole of what bars a beast from the
/// modi mentis that name it (<see cref="ModusMentisAnatomy"/>'s structural test: every organ a modus
/// mentis names must exist on the body), and it also puts the pineal measures out of reach:
/// iterative thinking and doubt are absent, the experience a level costs is the full 12, and a fallen
/// beast regains its feet on a single die. Same id as the human's, so the zone map, the wounds and
/// everything that reads the region by id are unchanged.</para>
/// </summary>
public class BeastEncephalonBodyPart : BodyPart
{
    public override string Id => "encephalon";
    public override string DisplayName => "Encephalon";
    public override string Description =>
        "The cephalic mass, lodged within the cranium, comprising the organs of the sensitive and " +
        "instinctive faculties. Taken whole, it determines the capacity of the attentive faculty, the sum " +
        "of impressions the mind may hold under consideration at once.";

    private readonly List<Organ> _organs;
    public override List<Organ> Organs => _organs;

    public BeastEncephalonBodyPart()
    {
        _organs = new List<Organ>
        {
            new AnamnesisOrgan(),
            new CerebellumOrgan(),
            new CerebrumOrgan(),
            new HippocampusOrgan(),
        };
    }
}
