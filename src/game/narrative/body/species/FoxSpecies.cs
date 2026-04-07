using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

public sealed class FoxSpecies : Species
{
    public override AnatomyType AnatomyType => AnatomyType.Beast;
    public override string DisplayName => "Fox";
    public override string ArtFolderPath => "assets/art/body/beast";

    // Foxes are nimble and bite sharply but are lightly built.
    public override IReadOnlyDictionary<string, int> OrganPartMaxScores { get; } =
        new Dictionary<string, int>
        {
            { "fangs",           5 },
            { "left_foreleg",    5 },
            { "right_foreleg",   5 },
            { "left_hindleg",    5 },
            { "right_hindleg",   5 },
            { "left_foreclaws",  4 },
            { "right_foreclaws", 4 },
            { "left_hindclaws",  4 },
            { "right_hindclaws", 4 },
        };
}
