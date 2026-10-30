using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

// ─────────────────────────────────────────────────────────────────────────────
//  The great reptiles — one anatomy (Reptile), one art (assets/art/body/reptile), eight bodies.
//  What makes a crocodile a crocodile and a triceratops a triceratops is only these caps: where the
//  horns and the thagomizer are a species' real weapons they run to 5, and where they are no more
//  than bony bosses or a heavy tail they sit at 1–3. A biped (the carnotaurus) is the same four
//  limbs with the forelegs capped at 1.
//
//  Cold blood is the common thread: small brains, slow hearts, guts that go a long time between
//  meals and digest anything — and spines built to carry armour.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Crocodile — the river's ambusher: the strongest jaw of any body, a gut that waits months.</summary>
public sealed class CrocodileSpecies : Species
{
    public override AnatomyType AnatomyType => AnatomyType.Reptile;
    public override string DisplayName => "Crocodile";
    public override string ArtFolderPath => "assets/art/body/reptile";

    public override IReadOnlyDictionary<string, int> OrganPartMaxScores { get; } =
        new Dictionary<string, int>
        {
            { "anamnesis",   3 }, // remembers the drinking places and the season they are used
            { "cerebrum",    1 },
            { "cerebellum",  3 },
            { "hippocampus", 3 },
            { "backbone",    5 }, // the death roll
            { "heart",       3 },
            { "pulmones",    5 }, // an hour under water
            { "viscera",     5 }, // eats carrion with impunity
            { "paunch",      5 },
            { "hepar",       4 },
            { "spleen",      4 },
            { "fangs",       5 },
            { "horns",       1 }, // bony bosses behind the eyes, nothing more
            { "thagomizer",  3 }, // a heavy keeled tail, no spikes
            { "left_foreleg",  3 }, { "right_foreleg", 3 },
            { "left_hindleg",  3 }, { "right_hindleg", 3 },
        };
}

/// <summary>Giant monitor — the desert's hunter: tireless, tongue-scenting, a septic bite.</summary>
public sealed class GiantMonitorSpecies : Species
{
    public override AnatomyType AnatomyType => AnatomyType.Reptile;
    public override string DisplayName => "Giant Monitor";
    public override string ArtFolderPath => "assets/art/body/reptile";

    public override IReadOnlyDictionary<string, int> OrganPartMaxScores { get; } =
        new Dictionary<string, int>
        {
            { "anamnesis",   2 },
            { "cerebrum",    2 },
            { "cerebellum",  3 },
            { "hippocampus", 4 }, // follows a wounded quarry for days
            { "backbone",    4 },
            { "heart",       4 },
            { "pulmones",    4 },
            { "viscera",     5 },
            { "paunch",      4 },
            { "hepar",       4 },
            { "spleen",      3 },
            { "snout",       4 },
            { "tongue",      5 }, // tastes the air
            { "fangs",       4 },
            { "horns",       1 },
            { "thagomizer",  3 }, // the whip of the tail
            { "left_foreleg",  4 }, { "right_foreleg", 4 },
            { "left_hindleg",  4 }, { "right_hindleg", 4 },
        };
}

/// <summary>Horned lizard — a lizard grown to the size of an ox, crowned with spines.</summary>
public sealed class HornedLizardSpecies : Species
{
    public override AnatomyType AnatomyType => AnatomyType.Reptile;
    public override string DisplayName => "Horned Lizard";
    public override string ArtFolderPath => "assets/art/body/reptile";

    public override IReadOnlyDictionary<string, int> OrganPartMaxScores { get; } =
        new Dictionary<string, int>
        {
            { "anamnesis",   2 },
            { "cerebrum",    1 },
            { "cerebellum",  2 },
            { "hippocampus", 2 },
            { "backbone",    3 },
            { "heart",       3 },
            { "pulmones",    3 },
            { "viscera",     4 },
            { "paunch",      4 },
            { "hepar",       3 },
            { "spleen",      4 }, // the blood it squirts from its eyes is its temper
            { "fangs",       2 },
            { "horns",       4 }, // the crown of spines
            { "thagomizer",  2 },
            { "left_foreleg",  3 }, { "right_foreleg", 3 },
            { "left_hindleg",  3 }, { "right_hindleg", 3 },
        };
}

/// <summary>Cave lizard — pale and near-blind, it hunts the dark by ear and tongue.</summary>
public sealed class CaveLizardSpecies : Species
{
    public override AnatomyType AnatomyType => AnatomyType.Reptile;
    public override string DisplayName => "Cave Lizard";
    public override string ArtFolderPath => "assets/art/body/reptile";

    public override IReadOnlyDictionary<string, int> OrganPartMaxScores { get; } =
        new Dictionary<string, int>
        {
            { "anamnesis",   3 },
            { "cerebrum",    1 },
            { "cerebellum",  3 },
            { "hippocampus", 4 }, // knows every gallery in the dark
            { "left_eye",    1 }, { "right_eye", 1 }, // filmed over
            { "left_ear",    4 }, { "right_ear", 4 },
            { "tongue",      5 },
            { "backbone",    3 },
            { "heart",       2 }, // slow, cold, patient
            { "pulmones",    3 },
            { "viscera",     4 },
            { "paunch",      5 }, // a meal a season
            { "hepar",       3 },
            { "spleen",      3 },
            { "fangs",       3 },
            { "horns",       2 },
            { "thagomizer",  2 },
            { "left_foreleg",  3 }, { "right_foreleg", 3 },
            { "left_hindleg",  3 }, { "right_hindleg", 3 },
        };
}

/// <summary>Carnotaurus — the bull-horned runner of the dry country: a biped, its arms all but gone.</summary>
public sealed class CarnotaurusSpecies : Species
{
    public override AnatomyType AnatomyType => AnatomyType.Reptile;
    public override string DisplayName => "Carnotaurus";
    public override string ArtFolderPath => "assets/art/body/reptile";

    public override IReadOnlyDictionary<string, int> OrganPartMaxScores { get; } =
        new Dictionary<string, int>
        {
            { "anamnesis",   2 },
            { "cerebrum",    2 },
            { "cerebellum",  4 }, // balances on two legs at a run
            { "hippocampus", 3 },
            { "backbone",    5 },
            { "heart",       4 },
            { "pulmones",    4 },
            { "viscera",     4 },
            { "paunch",      3 },
            { "hepar",       4 },
            { "spleen",      4 },
            { "fangs",       5 },
            { "horns",       4 }, // the two brow horns of a bull
            { "thagomizer",  2 },
            { "left_foreleg",  1 }, { "right_foreleg", 1 }, // vestigial
            { "left_hindleg",  5 }, { "right_hindleg", 5 },
        };
}

/// <summary>Stegosaurus — plated and slow-witted, and the thagomizer is named for its tail.</summary>
public sealed class StegosaurusSpecies : Species
{
    public override AnatomyType AnatomyType => AnatomyType.Reptile;
    public override string DisplayName => "Stegosaurus";
    public override string ArtFolderPath => "assets/art/body/reptile";

    public override IReadOnlyDictionary<string, int> OrganPartMaxScores { get; } =
        new Dictionary<string, int>
        {
            { "anamnesis",   1 },
            { "cerebrum",    1 }, // famously, a brain the size of a walnut
            { "cerebellum",  2 },
            { "hippocampus", 2 },
            { "backbone",    5 }, // carries the plates
            { "heart",       4 },
            { "pulmones",    4 },
            { "viscera",     5 },
            { "paunch",      5 }, // a browser's gut
            { "hepar",       4 },
            { "spleen",      3 },
            { "fangs",       1 }, // a beak and leaf-teeth
            { "horns",       1 },
            { "thagomizer",  5 },
            { "left_foreleg",  4 }, { "right_foreleg", 4 },
            { "left_hindleg",  5 }, { "right_hindleg", 5 },
        };
}

/// <summary>Triceratops — three horns and a frill, a herd beast that turns to meet a threat.</summary>
public sealed class TriceratopsSpecies : Species
{
    public override AnatomyType AnatomyType => AnatomyType.Reptile;
    public override string DisplayName => "Triceratops";
    public override string ArtFolderPath => "assets/art/body/reptile";

    public override IReadOnlyDictionary<string, int> OrganPartMaxScores { get; } =
        new Dictionary<string, int>
        {
            { "anamnesis",   2 },
            { "cerebrum",    1 },
            { "cerebellum",  2 },
            { "hippocampus", 3 },
            { "backbone",    5 },
            { "heart",       5 }, // the charge
            { "pulmones",    4 },
            { "viscera",     5 },
            { "paunch",      5 },
            { "hepar",       4 },
            { "spleen",      4 },
            { "fangs",       2 }, // a parrot's beak
            { "horns",       5 },
            { "thagomizer",  1 },
            { "left_foreleg",  5 }, { "right_foreleg", 5 },
            { "left_hindleg",  5 }, { "right_hindleg", 5 },
        };
}

/// <summary>Ankylosaurus — armoured from snout to tail, the tail ending in a bone club.</summary>
public sealed class AnkylosaurusSpecies : Species
{
    public override AnatomyType AnatomyType => AnatomyType.Reptile;
    public override string DisplayName => "Ankylosaurus";
    public override string ArtFolderPath => "assets/art/body/reptile";

    public override IReadOnlyDictionary<string, int> OrganPartMaxScores { get; } =
        new Dictionary<string, int>
        {
            { "anamnesis",   1 },
            { "cerebrum",    1 },
            { "cerebellum",  2 },
            { "hippocampus", 2 },
            { "backbone",    5 }, // the armour is borne on it
            { "heart",       3 },
            { "pulmones",    3 },
            { "viscera",     5 },
            { "paunch",      5 },
            { "hepar",       4 },
            { "spleen",      3 },
            { "fangs",       1 },
            { "horns",       3 }, // the horns at the corners of the skull
            { "thagomizer",  5 }, // the club
            { "left_foreleg",  4 }, { "right_foreleg", 4 },
            { "left_hindleg",  4 }, { "right_hindleg", 4 },
        };
}
