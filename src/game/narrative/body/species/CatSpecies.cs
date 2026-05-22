using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

public sealed class CatSpecies : Species
{
    public override AnatomyType AnatomyType => AnatomyType.Beast;
    public override string DisplayName => "Cat";
    public override string ArtFolderPath => "assets/art/body/beast";

    // Precise and instinctive: exceptional balance, deadly claws, light body.
    public override IReadOnlyDictionary<string, int> OrganPartMaxScores { get; } =
        new Dictionary<string, int>
        {
            // Encephalon: body mastery and sharp instinct
            { "anamnesis",      3 },
            { "cerebrum",       3 },
            { "cerebellum",     4 }, // exceptional agility, balance and precision
            { "hippocampus",    3 },
            { "pineal_gland",   3 }, // mysterious, sharp instincts
            // Trunk: small sprinter, not built for endurance
            { "backbone",   3 }, // flexible spine, not powerful
            { "heart",      4 },
            { "pulmones",   3 }, // sprinter bursts, not long-chase
            { "viscera",    3 },
            { "paunch",     3 },
            { "hepar",      4 },
            { "spleen",     3 },
            // Body: deadly claws, weak bite
            { "fangs",           3 },
            { "left_foreclaws",  5 },
            { "right_foreclaws", 5 },
            { "left_hindclaws",  5 },
            { "right_hindclaws", 5 },
        };
}
