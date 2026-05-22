using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

public sealed class BearSpecies : Species
{
    public override AnatomyType AnatomyType => AnatomyType.Beast;
    public override string DisplayName => "Bear";
    public override string ArtFolderPath => "assets/art/body/beast";

    // Apex brute: massive physical power, resilient body, below-average wit.
    public override IReadOnlyDictionary<string, int> OrganPartMaxScores { get; } =
        new Dictionary<string, int>
        {
            // Encephalon: slow but not mindless — a dog is smarter
            { "anamnesis",      3 },
            { "cerebrum",       2 }, // below average reasoning
            { "cerebellum",     3 },
            { "hippocampus",    3 },
            { "pineal_gland",   2 },
            // Trunk: everything maxed — an unstoppable biological machine
            { "backbone",   5 },
            { "heart",      5 },
            { "pulmones",   5 },
            { "viscera",    5 }, // virtually immune to disease
            { "paunch",     5 }, // omnivore, digests anything
            { "hepar",      5 },
            { "spleen",     4 },
            // Body: strongest raw power among beasts
            { "fangs",           4 },
            { "left_foreleg",    5 },
            { "right_foreleg",   5 },
            { "left_hindleg",    5 },
            { "right_hindleg",   5 },
            { "left_foreclaws",  5 },
            { "right_foreclaws", 5 },
            { "left_hindclaws",  5 },
            { "right_hindclaws", 5 },
        };
}
