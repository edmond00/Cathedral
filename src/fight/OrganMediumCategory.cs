namespace Cathedral.Fight;

/// <summary>
/// Describes a body-part organ medium (e.g. "hands", "fangs", "legs") and owns the ordered list of
/// fighting skill IDs available when that organ is present and undisabled.
/// The skill order reflects increasing learning difficulty: index 0 is the easiest to learn.
/// </summary>
public sealed class OrganMediumCategory
{
    public string OrganId     { get; }
    public string DisplayName { get; }

    /// <summary>
    /// Skill IDs in learning-difficulty order (easiest first, hardest last).
    /// A fighter learns skills from the front of the list; each earlier skill
    /// unlocked reduces the difficulty for the next one.
    /// </summary>
    public IReadOnlyList<string> SkillIds { get; }

    public OrganMediumCategory(string organId, string displayName, IReadOnlyList<string> skillIds)
    {
        OrganId     = organId;
        DisplayName = displayName;
        SkillIds    = skillIds;
    }
}
