using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

public sealed class DogSpecies : Species
{
    public override AnatomyType AnatomyType => AnatomyType.Beast;
    public override string DisplayName => "Dog";
    public override string ArtFolderPath => "assets/art/body/beast";

    // Domesticated and trainable: highest social intelligence, well-nourished.
    public override IReadOnlyDictionary<string, int> OrganPartMaxScores { get; } =
        new Dictionary<string, int>
        {
            // Encephalon: best learner — retains training, socially smart
            { "anamnesis",      4 }, // learns and remembers commands
            { "cerebrum",       4 }, // social and adaptive intelligence
            { "cerebellum",     4 }, // trained agility and coordination
            { "hippocampus",    3 },
            { "pineal_gland",   2 },
            // Trunk: healthy domestic, strong loyal heart
            { "backbone",   4 },
            { "heart",      5 }, // loyal companion, strong heart
            { "pulmones",   4 },
            { "viscera",    4 },
            { "paunch",     4 }, // well-fed domestic
            { "hepar",      4 },
            { "spleen",     4 },
            // Body: moderate, built for endurance
            { "fangs",        4 },
            { "left_foreleg",  4 },
            { "right_foreleg", 4 },
            { "left_hindleg",  4 },
            { "right_hindleg", 4 },
        };
}
