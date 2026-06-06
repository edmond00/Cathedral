using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Fight;

/// <summary>
/// Static registry of all body-part medium categories.
/// A body-part medium treats a whole anatomical region (e.g. upper_limbs) as a single
/// fighting medium whose level is the region's total score (sum of its organs).
/// Each category owns its ordered list of fighting skill IDs, easiest-to-learn first.
/// Mirrors <see cref="OrganMediumRegistry"/>.
/// </summary>
public static class BodyPartMediumRegistry
{
    private static readonly Dictionary<string, BodyPartMediumCategory> _byId;
    private static readonly IReadOnlyList<BodyPartMediumCategory>      _all;

    static BodyPartMediumRegistry()
    {
        var cats = new[]
        {
            new BodyPartMediumCategory("upper_limbs", "Upper Limbs",
                new[] { "seize", "chokehold" }),
        };

        _all  = cats;
        _byId = cats.ToDictionary(c => c.BodyPartId);
    }

    public static IReadOnlyList<BodyPartMediumCategory> GetAll() => _all;

    /// <summary>Returns the category with the given body-part id, or null if not found.</summary>
    public static BodyPartMediumCategory? GetById(string bodyPartId) =>
        _byId.GetValueOrDefault(bodyPartId);

    /// <summary>Returns all categories whose skill list contains the given skill id.</summary>
    public static IEnumerable<BodyPartMediumCategory> GetCategoriesContaining(string skillId) =>
        _all.Where(c => c.SkillIds.Contains(skillId));
}
