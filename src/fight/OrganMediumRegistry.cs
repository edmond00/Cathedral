using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Fight;

/// <summary>
/// Static registry of all organ medium categories.
/// Each category owns its ordered list of fighting skill IDs.
/// Skills appear in the list from easiest-to-learn (index 0) to hardest.
/// A single skill may appear in multiple categories (e.g. flesh_tear in both fangs and teeth).
/// </summary>
public static class OrganMediumRegistry
{
    private static readonly Dictionary<string, OrganMediumCategory> _byId;
    private static readonly IReadOnlyList<OrganMediumCategory>      _all;

    static OrganMediumRegistry()
    {
        var cats = new[]
        {
            // ── Beast body parts ──────────────────────────────────────────────────────
            new OrganMediumCategory("fangs", "Fangs",
                new[] { "flesh_tear", "flesh_clamp", "throat_grip" }),

            new OrganMediumCategory("claws", "Claws",
                new[] { "scratch", "lacerate", "gut_ripper" }),

            // ── Human body parts ─────────────────────────────────────────────────────
            new OrganMediumCategory("hands", "Hands",
                new[] { "punch", "seize", "uppercut", "chokehold", "palm_strike" }),

            new OrganMediumCategory("feet", "Feet",
                new[] { "high_kick", "trip", "back_kick" }),

            new OrganMediumCategory("viscera", "Viscera",
                new[] { "survival_instinct", "cold_blood_skill", "rage_skill", "blood_lust_skill", "iron_nerves_skill" }),

            new OrganMediumCategory("teeths", "Teeth",
                new[] { "bite", "flesh_tear" }),

            new OrganMediumCategory("legs", "Leg",
                new[] { "run", "knee_strike", "dodge", "jump", "defensive_posture" }),

            new OrganMediumCategory("arms", "Arm",
                new[] { "push", "elbow_strike" }),
        };

        _all  = cats;
        _byId = cats.ToDictionary(c => c.OrganId);
    }

    public static IReadOnlyList<OrganMediumCategory> GetAll() => _all;

    /// <summary>Returns the category with the given organ id, or null if not found.</summary>
    public static OrganMediumCategory? GetById(string organId) =>
        _byId.GetValueOrDefault(organId);

    /// <summary>Returns all categories whose skill list contains the given skill id.</summary>
    public static IEnumerable<OrganMediumCategory> GetCategoriesContaining(string skillId) =>
        _all.Where(c => c.SkillIds.Contains(skillId));
}
