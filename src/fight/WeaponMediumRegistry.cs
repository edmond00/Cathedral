using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Fight;

/// <summary>
/// Static registry of all weapon medium categories.
/// Each category owns its ordered list of fighting skill IDs.
/// Skills appear in the list from easiest-to-learn (index 0) to hardest.
/// A single skill may appear in multiple categories.
/// </summary>
public static class WeaponMediumRegistry
{
    private static readonly Dictionary<string, WeaponMediumCategory> _byId;
    private static readonly IReadOnlyList<WeaponMediumCategory>      _all;

    static WeaponMediumRegistry()
    {
        var cats = new[]
        {
            new WeaponMediumCategory("long_blade",  "Long Blade",
                new[] { "cleaving_strike", "counter_strike", "forward_lunge", "feint" }),

            new WeaponMediumCategory("short_blade", "Short Blade",
                new[] { "snap_thrust", "needle_thrust", "parry", "deep_pierce" }),

            new WeaponMediumCategory("saber",       "Saber",
                new[] { "snap_thrust", "feint", "needle_thrust", "counter_strike" }),

            new WeaponMediumCategory("blunt",       "Blunt",
                new[] { "smash", "crushing_blow", "heavy_strike", "mighty_swing" }),

            new WeaponMediumCategory("axe",         "Axe",
                new[] { "chop", "heavy_strike", "cleaving_strike", "driving_lunge" }),

            new WeaponMediumCategory("pickaxe",     "Pickaxe",
                new[] { "piercing_blow", "deep_pierce", "mighty_swing", "crushing_blow" }),

            new WeaponMediumCategory("spear",       "Spear",
                new[] { "forward_lunge", "piercing_blow", "driving_lunge", "deep_pierce" }),

            new WeaponMediumCategory("bow",         "Bow",
                new[] { "quickshot", "pinpoint_shot", "longshot" }),

            new WeaponMediumCategory("crossbow",    "Crossbow",
                new[] { "sighted_shot", "pinpoint_shot", "deadeye_shot" }),

            new WeaponMediumCategory("shield",      "Shield",
                new[] { "cover", "parry", "shield_bash" }),
        };

        _all  = cats;
        _byId = cats.ToDictionary(c => c.CategoryId);
    }

    public static IReadOnlyList<WeaponMediumCategory> GetAll() => _all;

    /// <summary>Returns the category with the given id, or null if not found.</summary>
    public static WeaponMediumCategory? GetById(string categoryId) =>
        _byId.GetValueOrDefault(categoryId);

    /// <summary>Returns all categories whose skill list contains the given skill id.</summary>
    public static IEnumerable<WeaponMediumCategory> GetCategoriesContaining(string skillId) =>
        _all.Where(c => c.SkillIds.Contains(skillId));
}
