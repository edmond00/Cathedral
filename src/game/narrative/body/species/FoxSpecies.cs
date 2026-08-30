using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

public sealed class FoxSpecies : Species
{
    public override AnatomyType AnatomyType => AnatomyType.Beast;
    public override string DisplayName => "Fox";
    public override string ArtFolderPath => "assets/art/body/beast";

    // Cunning trickster: sharp instinct, spatial cunning, light frame.
    public override IReadOnlyDictionary<string, int> OrganPartMaxScores { get; } =
        new Dictionary<string, int>
        {
            // Encephalon: clever problem-solver with sharp gut feelings
            { "anamnesis",      3 },
            { "cerebrum",       4 }, // cunning and adaptive reasoning
            { "cerebellum",     3 },
            { "hippocampus",    4 }, // remembers escape routes and hiding spots
            { "pineal_gland",   3 }, // sharp instinct and premonition
            // Trunk: small and light, good at processing varied diet
            { "backbone",   3 }, // small frame
            { "heart",      4 },
            { "pulmones",   4 },
            { "viscera",    3 },
            { "paunch",     3 }, // lean opportunistic eater
            { "hepar",      4 }, // processes varied diet well
            { "spleen",     3 },
            // Body: nimble but not powerful
            { "fangs",           3 },
            { "left_foreleg",    4 },
            { "right_foreleg",   4 },
            { "left_hindleg",    4 },
            { "right_hindleg",   4 },
            { "left_foreclaws",  3 },
            { "right_foreclaws", 3 },
            { "left_hindclaws",  3 },
            { "right_hindclaws", 3 },
        };
}
