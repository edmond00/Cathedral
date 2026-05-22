using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

public sealed class WolfSpecies : Species
{
    public override AnatomyType AnatomyType => AnatomyType.Beast;
    public override string DisplayName => "Wolf";
    public override string ArtFolderPath => "assets/art/body/beast";

    // Apex pack predator: coordinated, territorial, lean and enduring.
    public override IReadOnlyDictionary<string, int> OrganPartMaxScores { get; } =
        new Dictionary<string, int>
        {
            // Encephalon: sharp coordination and pack-territory memory
            { "anamnesis",      3 },
            { "cerebrum",       3 },
            { "cerebellum",     4 }, // pack coordination, motor precision
            { "hippocampus",    4 }, // territorial routes, prey tracking
            { "pineal_gland",   2 },
            // Trunk: built for endurance, lean gut
            { "backbone",   5 }, // powerful spine for pursuit
            { "heart",      5 }, // long-chase stamina
            { "pulmones",   5 }, // endurance breathing
            { "viscera",    4 },
            { "paunch",     3 }, // lean predator, fasts between hunts
            { "hepar",      4 },
            { "spleen",     4 },
            // Body: strongest jaw and limbs
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
