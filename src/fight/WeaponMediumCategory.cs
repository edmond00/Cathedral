namespace Cathedral.Fight;

/// <summary>
/// Describes a weapon medium category (e.g. "long_blade", "bow") and owns the ordered list of
/// fighting skill IDs available to weapons of that category.
/// The skill order reflects increasing learning difficulty: index 0 is the easiest to learn.
/// </summary>
public sealed class WeaponMediumCategory
{
    public string CategoryId  { get; }
    public string DisplayName { get; }

    /// <summary>
    /// Skill IDs in learning-difficulty order (easiest first, hardest last).
    /// A fighter learns skills from the front of the list; each earlier skill
    /// unlocked reduces the difficulty for the next one.
    /// </summary>
    public IReadOnlyList<string> SkillIds { get; }

    public WeaponMediumCategory(string categoryId, string displayName, IReadOnlyList<string> skillIds)
    {
        CategoryId  = categoryId;
        DisplayName = displayName;
        SkillIds    = skillIds;
    }
}
