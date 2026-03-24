using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

public sealed class BoarSpecies : Species
{
    public override AnatomyType AnatomyType => AnatomyType.Beast;
    public override string DisplayName => "Boar";
    public override string ArtFolderPath => "assets/art/body/beast";

    // Boars are tough and have strong tusks (fangs) and sturdy legs.
    public override IReadOnlyDictionary<string, int> OrganPartMaxScores { get; } =
        new Dictionary<string, int>
        {
            { "fangs",           7 },
            { "left_foreleg",    8 },
            { "right_foreleg",   8 },
            { "left_hindleg",    7 },
            { "right_hindleg",   7 },
            { "left_foreclaws",  5 },
            { "right_foreclaws", 5 },
            { "left_hindclaws",  5 },
            { "right_hindclaws", 5 },
        };
}
