using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

public sealed class DogSpecies : Species
{
    public override AnatomyType AnatomyType => AnatomyType.Beast;
    public override string DisplayName => "Dog";
    public override string ArtFolderPath => "assets/art/body/beast";

    public override IReadOnlyDictionary<string, int> OrganPartMaxScores { get; } =
        new Dictionary<string, int>
        {
            { "fangs",        6 },
            { "left_foreleg",  6 },
            { "right_foreleg", 6 },
            { "left_hindleg",  6 },
            { "right_hindleg", 6 },
        };
}
