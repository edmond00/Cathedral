namespace Cathedral.Fight;

/// <summary>
/// Describes a body-part region medium (e.g. "upper_limbs") and owns the ordered list of
/// fighting skill IDs available when that body part is present and undisabled.
/// The skill order reflects increasing learning difficulty: index 0 is the easiest to learn.
/// Mirrors <see cref="OrganMediumCategory"/> but keys on a body-part id rather than an organ id.
/// </summary>
public sealed class BodyPartMediumCategory
{
    public string BodyPartId  { get; }
    public string DisplayName { get; }

    /// <summary>
    /// Skill IDs in learning-difficulty order (easiest first, hardest last).
    /// A fighter learns skills from the front of the list; each earlier skill
    /// unlocked reduces the difficulty for the next one.
    /// </summary>
    public IReadOnlyList<string> SkillIds { get; }

    public BodyPartMediumCategory(string bodyPartId, string displayName, IReadOnlyList<string> skillIds)
    {
        BodyPartId  = bodyPartId;
        DisplayName = displayName;
        SkillIds    = skillIds;
    }
}
