using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

// ─────────────────────────────────────────────────────────────────────────────
//  The beasts of the hot and cold country. All of them are the Beast anatomy the wolf and the bear
//  already use — four legs, four sets of claws, fangs — and differ, as those do, only in what each
//  organ part can reach. A white bear is still a bear and a white wolf still a wolf (their
//  archetypes reuse BearSpecies and WolfSpecies, as the black bear does); these are the animals with
//  no temperate counterpart.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>The great maned cat of the hot steppe. Strength before stamina; hunts in short rushes.</summary>
public sealed class LionSpecies : Species
{
    public override AnatomyType AnatomyType => AnatomyType.Beast;
    public override string DisplayName => "Lion";
    public override string ArtFolderPath => "assets/art/body/beast";

    public override IReadOnlyDictionary<string, int> OrganPartMaxScores { get; } =
        new Dictionary<string, int>
        {
            // Encephalon: a social hunter, patient in the ambush
            { "anamnesis",      3 },
            { "cerebrum",       3 },
            { "cerebellum",     4 },
            { "hippocampus",    3 },
            { "pineal_gland",   2 },
            // Trunk: enormous power, short wind
            { "backbone",   5 },
            { "heart",      4 },
            { "pulmones",   3 }, // a rush, not a chase
            { "viscera",    4 },
            { "paunch",     4 }, // gorges, then fasts
            { "hepar",      4 },
            { "spleen",     4 },
            // Body: the heaviest bite and paw of the cats
            { "fangs",           5 },
            { "left_foreleg",    5 },
            { "right_foreleg",   5 },
            { "left_hindleg",    5 },
            { "right_hindleg",   5 },
            { "left_foreclaws",  5 },
            { "right_foreclaws", 5 },
            { "left_hindclaws",  4 },
            { "right_hindclaws", 4 },
        };
}

/// <summary>The striped cat of the jungle: the largest and the most solitary of them.</summary>
public sealed class TigerSpecies : Species
{
    public override AnatomyType AnatomyType => AnatomyType.Beast;
    public override string DisplayName => "Tiger";
    public override string ArtFolderPath => "assets/art/body/beast";

    public override IReadOnlyDictionary<string, int> OrganPartMaxScores { get; } =
        new Dictionary<string, int>
        {
            // Encephalon: a lone stalker with a long memory for ground
            { "anamnesis",      3 },
            { "cerebrum",       3 },
            { "cerebellum",     5 }, // silent placement of every foot
            { "hippocampus",    4 },
            { "pineal_gland",   3 },
            // Trunk: heavy and powerful, swims willingly
            { "backbone",   5 },
            { "heart",      5 },
            { "pulmones",   4 },
            { "viscera",    4 },
            { "paunch",     4 },
            { "hepar",      4 },
            { "spleen",     4 },
            // Body: everything at the ceiling
            { "fangs",           5 },
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

/// <summary>The spotted cat of the jungle floor and the river: a skull-crushing bite on a compact body.</summary>
public sealed class JaguarSpecies : Species
{
    public override AnatomyType AnatomyType => AnatomyType.Beast;
    public override string DisplayName => "Jaguar";
    public override string ArtFolderPath => "assets/art/body/beast";

    public override IReadOnlyDictionary<string, int> OrganPartMaxScores { get; } =
        new Dictionary<string, int>
        {
            { "anamnesis",      3 },
            { "cerebrum",       3 },
            { "cerebellum",     5 }, // climber and swimmer both
            { "hippocampus",    3 },
            { "pineal_gland",   3 },
            { "backbone",   4 },
            { "heart",      4 },
            { "pulmones",   4 },
            { "viscera",    4 },
            { "paunch",     3 },
            { "hepar",      4 },
            { "spleen",     3 },
            // Body: the bite is the weapon — it goes through skull and shell
            { "fangs",           5 },
            { "left_foreleg",    4 },
            { "right_foreleg",   4 },
            { "left_hindleg",    4 },
            { "right_hindleg",   4 },
            { "left_foreclaws",  5 },
            { "right_foreclaws", 5 },
            { "left_hindclaws",  4 },
            { "right_hindclaws", 4 },
        };
}

/// <summary>The tawny cat of the canyons and the high rock: a leaper, lighter than the others.</summary>
public sealed class PumaSpecies : Species
{
    public override AnatomyType AnatomyType => AnatomyType.Beast;
    public override string DisplayName => "Puma";
    public override string ArtFolderPath => "assets/art/body/beast";

    public override IReadOnlyDictionary<string, int> OrganPartMaxScores { get; } =
        new Dictionary<string, int>
        {
            { "anamnesis",      3 },
            { "cerebrum",       3 },
            { "cerebellum",     5 }, // leaps a man's height from a standstill
            { "hippocampus",    3 },
            { "pineal_gland",   3 },
            { "backbone",   4 },
            { "heart",      4 },
            { "pulmones",   3 },
            { "viscera",    3 },
            { "paunch",     3 },
            { "hepar",      4 },
            { "spleen",     3 },
            { "fangs",           4 },
            { "left_foreleg",    4 },
            { "right_foreleg",   4 },
            { "left_hindleg",    5 }, // the spring is in the hind legs
            { "right_hindleg",   5 },
            { "left_foreclaws",  5 },
            { "right_foreclaws", 5 },
            { "left_hindclaws",  4 },
            { "right_hindclaws", 4 },
        };
}

/// <summary>The pale cat of the snowfields: thick-furred, long-tailed, and built for cold air.</summary>
public sealed class SnowLeopardSpecies : Species
{
    public override AnatomyType AnatomyType => AnatomyType.Beast;
    public override string DisplayName => "Snow Leopard";
    public override string ArtFolderPath => "assets/art/body/beast";

    public override IReadOnlyDictionary<string, int> OrganPartMaxScores { get; } =
        new Dictionary<string, int>
        {
            { "anamnesis",      3 },
            { "cerebrum",       3 },
            { "cerebellum",     5 }, // balance on ice and ledge
            { "hippocampus",    4 },
            { "pineal_gland",   3 },
            { "backbone",   4 },
            { "heart",      4 },
            { "pulmones",   5 }, // thin air, all its life
            { "viscera",    4 },
            { "paunch",     3 },
            { "hepar",      4 },
            { "spleen",     4 },
            { "fangs",           4 },
            { "left_foreleg",    4 },
            { "right_foreleg",   4 },
            { "left_hindleg",    5 },
            { "right_hindleg",   5 },
            { "left_foreclaws",  4 },
            { "right_foreclaws", 4 },
            { "left_hindclaws",  4 },
            { "right_hindclaws", 4 },
        };
}

/// <summary>The laughing scavenger of the desert and the steppe: a crushing jaw and an iron gut.</summary>
public sealed class HyenaSpecies : Species
{
    public override AnatomyType AnatomyType => AnatomyType.Beast;
    public override string DisplayName => "Hyena";
    public override string ArtFolderPath => "assets/art/body/beast";

    public override IReadOnlyDictionary<string, int> OrganPartMaxScores { get; } =
        new Dictionary<string, int>
        {
            // Encephalon: clan-clever, the brightest of the hot country's beasts
            { "anamnesis",      4 },
            { "cerebrum",       3 },
            { "cerebellum",     3 },
            { "hippocampus",    4 },
            { "pineal_gland",   2 },
            // Trunk: tireless, and eats what nothing else will
            { "backbone",   4 },
            { "heart",      5 },
            { "pulmones",   5 },
            { "viscera",    5 },
            { "paunch",     5 }, // bone, hide and carrion
            { "hepar",      5 },
            { "spleen",     4 },
            // Body: jaws that crack thigh bones; blunt claws
            { "fangs",           5 },
            { "left_foreleg",    4 },
            { "right_foreleg",   4 },
            { "left_hindleg",    3 },
            { "right_hindleg",   3 },
            { "left_foreclaws",  3 },
            { "right_foreclaws", 3 },
            { "left_hindclaws",  3 },
            { "right_hindclaws", 3 },
        };
}

/// <summary>The warty pig of the hot steppe: the boar's cousin, quick-tempered and tusked.</summary>
public sealed class WarthogSpecies : Species
{
    public override AnatomyType AnatomyType => AnatomyType.Beast;
    public override string DisplayName => "Warthog";
    public override string ArtFolderPath => "assets/art/body/beast";

    public override IReadOnlyDictionary<string, int> OrganPartMaxScores { get; } =
        new Dictionary<string, int>
        {
            { "anamnesis",      2 },
            { "cerebrum",       2 },
            { "cerebellum",     3 },
            { "hippocampus",    3 }, // knows every burrow in its range
            { "pineal_gland",   1 },
            { "backbone",   4 },
            { "heart",      4 },
            { "pulmones",   4 },
            { "viscera",    5 },
            { "paunch",     4 },
            { "hepar",      4 },
            { "spleen",     3 },
            // Body: the tusks are longer than a boar's, the body lighter
            { "fangs",           5 },
            { "left_foreleg",    4 },
            { "right_foreleg",   4 },
            { "left_hindleg",    4 },
            { "right_hindleg",   4 },
            { "left_foreclaws",  2 },
            { "right_foreclaws", 2 },
            { "left_hindclaws",  2 },
            { "right_hindclaws", 2 },
        };
}
