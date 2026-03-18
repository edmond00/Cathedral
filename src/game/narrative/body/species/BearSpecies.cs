using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

public sealed class BearSpecies : Species
{
    public override AnatomyType AnatomyType => AnatomyType.Beast;
    public override string DisplayName => "Bear";
    public override string ArtFolderPath => "assets/art/body/beast";

    // Bears have powerful limbs and strong claws; moderate bite.
    public override IReadOnlyDictionary<string, int> OrganPartMaxScores { get; } =
        new Dictionary<string, int>
        {
            { "fangs",           6 },
            { "left_foreleg",    9 },
            { "right_foreleg",   9 },
            { "left_hindleg",    8 },
            { "right_hindleg",   8 },
            { "left_foreclaws",  8 },
            { "right_foreclaws", 8 },
            { "left_hindclaws",  7 },
            { "right_hindclaws", 7 },
        };
}
