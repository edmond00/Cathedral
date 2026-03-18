using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

public sealed class WolfSpecies : Species
{
    public override AnatomyType AnatomyType => AnatomyType.Beast;
    public override string DisplayName => "Wolf";
    public override string ArtFolderPath => "assets/art/body/beast";

    // Wolves have stronger jaws and more powerful limbs than smaller beasts.
    public override IReadOnlyDictionary<string, int> OrganPartMaxScores { get; } =
        new Dictionary<string, int>
        {
            { "fangs",           8 },
            { "left_foreleg",    7 },
            { "right_foreleg",   7 },
            { "left_hindleg",    7 },
            { "right_hindleg",   7 },
            { "left_foreclaws",  6 },
            { "right_foreclaws", 6 },
            { "left_hindclaws",  6 },
            { "right_hindclaws", 6 },
        };
}
