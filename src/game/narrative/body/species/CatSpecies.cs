using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

public sealed class CatSpecies : Species
{
    public override AnatomyType AnatomyType => AnatomyType.Beast;
    public override string DisplayName => "Cat";
    public override string ArtFolderPath => "assets/art/body/beast";

    // Cats have sharp claws but weaker bite force than wolves.
    public override IReadOnlyDictionary<string, int> OrganPartMaxScores { get; } =
        new Dictionary<string, int>
        {
            { "fangs",           4 },
            { "left_foreclaws",  7 },
            { "right_foreclaws", 7 },
            { "left_hindclaws",  7 },
            { "right_hindclaws", 7 },
        };
}
