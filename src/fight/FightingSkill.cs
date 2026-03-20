using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;

namespace Cathedral.Fight;

/// <summary>
/// How a fighting skill selects which body part to wound on the target.
/// </summary>
public enum WoundTargetMode
{
    /// <summary>A wound is chosen randomly from the target's full anatomy.</summary>
    Random,
    /// <summary>The skill always targets the body part specified by <see cref="FightingSkill.TargetBodyPartId"/>.</summary>
    FixedBodyPart,
    /// <summary>The attacker (player) picks the target body part before the dice roll.</summary>
    PlayerChooses,
}

/// <summary>
/// General category of a fighting skill — governs primary effect logic.
/// </summary>
public enum FightingSkillEffect
{
    /// <summary>Deals a wound if dice succeed.</summary>
    Attack,
    /// <summary>Increases <c>NaturalDefense</c> until the start of the fighter's next turn.</summary>
    DefensePosture,
    /// <summary>Allows jumping over HardObstacle cells.</summary>
    SpecialMovement,
    /// <summary>Other utility effects.</summary>
    Utility,
}

/// <summary>
/// Abstract base class for all fighting skills.
/// A fighting skill is usable when:
///   (a) the fighter knows the required ModusMentis, and
///   (b) the required medium (organ or weapon) is available and undisabled.
/// </summary>
public abstract class FightingSkill
{
    /// <summary>Unique lowercase id string (for registry lookup).</summary>
    public abstract string SkillId { get; }

    /// <summary>Short human-readable name shown in the left panel.</summary>
    public abstract string DisplayName { get; }

    /// <summary>One-line flavour description.</summary>
    public abstract string Description { get; }

    /// <summary>ModusMentis id (must match <c>ModusMentis.ModusMentisId</c>) required to unlock.</summary>
    public abstract string RequiredModusMentisId { get; }

    /// <summary>Physical medium this skill uses (organ or weapon).</summary>
    public abstract FightingMedium Medium { get; }

    /// <summary>Cinetic points spent to use this skill.</summary>
    public abstract int CineticPointsCost { get; }

    /// <summary>Base number of dice rolled independently of ModusMentis level.</summary>
    public abstract int BaseDice { get; }

    /// <summary>Primary effect type.</summary>
    public abstract FightingSkillEffect EffectType { get; }

    /// <summary>How the wound target is chosen.</summary>
    public virtual WoundTargetMode WoundTargetMode => WoundTargetMode.Random;

    /// <summary>Body part id to target when <see cref="WoundTargetMode"/> is <see cref="WoundTargetMode.FixedBodyPart"/>.</summary>
    public virtual string? TargetBodyPartId => null;

    /// <summary>Maximum Manhattan distance from attacker to a valid target cell. Default 1 (adjacent melee).</summary>
    public virtual int Range => 1;

    // ── Derived calculations ──────────────────────────────────────

    /// <summary>Total dice for a given fighter: BaseDice + ModusMentis level bonus.</summary>
    public int TotalDice(Fighter f)
    {
        var mm = f.Member.LearnedModiMentis.FirstOrDefault(m => m.ModusMentisId == RequiredModusMentisId);
        int mmLevel = mm?.Level ?? 0;
        return BaseDice + mmLevel;
    }

    /// <summary>
    /// Returns true when the fighter can use this skill in the current combat state.
    /// Checks: ModusMentis known, medium available, CP sufficient (CP check done separately in GetUnlockedSkills).
    /// </summary>
    public bool IsUnlocked(Fighter f)
    {
        // Check ModusMentis known
        if (!f.Member.LearnedModiMentis.Any(m => m.ModusMentisId == RequiredModusMentisId))
            return false;

        // Check medium
        if (Medium.Type == MediumType.OrganMedium)
        {
            // Organ must exist and must not be fully disabled by wounds
            var organId = Medium.OrganId;
            if (string.IsNullOrEmpty(organId)) return false;

            var organ = f.Member.GetOrganById(organId);
            if (organ == null) return false;

            // Check if organ is disabled by a High-handicap wound (any organ part)
            bool disabled = f.Member.Wounds.Any(w =>
                w.Handicap == WoundHandicap.High &&
                (w.TargetKind == WoundTargetKind.Organ && w.TargetId == organId ||
                 w.TargetKind == WoundTargetKind.BodyPart && w.TargetId == organ.BodyPartId));
            if (disabled) return false;
        }
        else // WeaponMedium
        {
            // Any IWeaponItem in RightHold or LeftHold
            bool hasWeapon =
                f.Member.EquippedItems[EquipmentAnchor.RightHold].Concat(
                f.Member.EquippedItems[EquipmentAnchor.LeftHold])
                .Any(item => item is IWeaponItem);
            if (!hasWeapon) return false;
        }

        return true;
    }
}
