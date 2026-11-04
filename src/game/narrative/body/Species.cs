using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Abstract base for a species definition.
/// Ties a species to an anatomy type, an art folder, and optional per-organ MaxScore overrides.
/// Each species has its own subclass; species sharing an anatomy (e.g. wolf/cat both use Beast)
/// share the same body-part/organ/wound structure but can differ in organ MaxScores.
/// </summary>
public abstract class Species
{
    /// <summary>The anatomy layout used by this species.</summary>
    public abstract AnatomyType AnatomyType { get; }

    /// <summary>Display name for the species (used in UI labels).</summary>
    public abstract string DisplayName { get; }

    /// <summary>
    /// What the choice judge reads in place of <see cref="DisplayName"/>, for a species whose name the
    /// shipped model knows as something no one could meet: it knows "triceratops" as a dinosaur extinct
    /// for ages, and every faculty — Beast Sense included — passed an appeased triceratops over for a
    /// tapir, a strangler fig or nothing at all. A plain noun ("three-horned beast") makes it a beast
    /// like any other. Judge-side only; the player and the narration keep the name. Null (the default)
    /// for every name the model reads as a living animal. See <see cref="ChoiceJudge"/>.
    /// </summary>
    public virtual string? JudgeNoun => null;

    /// <summary>
    /// Relative path (from the working directory) to the body art folder for this species.
    /// e.g. "assets/art/body/human"  or  "assets/art/body/beast"
    /// </summary>
    public abstract string ArtFolderPath { get; }

    /// <summary>
    /// Per species organ-part MaxScore overrides.
    /// Key = organ-part id (e.g. "fangs", "left_foreleg"). Value = new MaxScore.
    /// Organ parts not listed use the class default (5).
    /// </summary>
    public virtual IReadOnlyDictionary<string, int> OrganPartMaxScores =>
        _emptyScores;

    private static readonly IReadOnlyDictionary<string, int> _emptyScores =
        new Dictionary<string, int>();
}
