using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

public sealed class BoarSpecies : Species
{
    public override AnatomyType AnatomyType => AnatomyType.Beast;
    public override string DisplayName => "Boar";
    public override string ArtFolderPath => "assets/art/body/beast";

    // Mindless aggressor: brutish strength, lowest intellect, iron gut.
    public override IReadOnlyDictionary<string, int> OrganPartMaxScores { get; } =
        new Dictionary<string, int>
        {
            // Encephalon: barely any higher thought, pure aggression
            { "anamnesis",      2 },
            { "cerebrum",       2 },
            { "cerebellum",     3 },
            { "hippocampus",    2 },
            { "pineal_gland",   1 },
            // Trunk: tough charger with iron gut and charging backbone
            { "backbone",   5 }, // built for head-on charges
            { "heart",      4 },
            { "pulmones",   4 },
            { "viscera",    5 }, // tough, disease-resistant
            { "paunch",     5 }, // forager, digests roots and refuse
            { "hepar",      4 },
            { "spleen",     3 },
            // Body: powerful tusks and charging legs
            { "fangs",           5 },
            { "left_foreleg",    5 },
            { "right_foreleg",   5 },
            { "left_hindleg",    5 },
            { "right_hindleg",   5 },
            { "left_foreclaws",  3 },
            { "right_foreclaws", 3 },
            { "left_hindclaws",  3 },
            { "right_hindclaws", 3 },
        };
}
