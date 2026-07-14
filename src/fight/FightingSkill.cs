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
    /// <summary>Reactive defense (parry/dodge) — adds defense dice to one or all incoming attacks.</summary>
    Defense,
    /// <summary>Allows jumping over HardObstacle cells.</summary>
    SpecialMovement,
    /// <summary>Other utility effects (rage, blood lust, run, etc.).</summary>
    Utility,
    /// <summary>Alias for <see cref="Utility"/> — other utility effects.</summary>
    Other,
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

    /// <summary>Primary ModusMentis id required to unlock this skill.</summary>
    public abstract string RequiredModusMentisId { get; }

    /// <summary>
    /// Secondary ModusMentis ids whose levels are also added to the skill level bonus.
    /// All known MMs (main + secondary) sum their levels when computing dice.
    /// </summary>
    public virtual string[] SecondaryModusMentisIds => Array.Empty<string>();

    /// <summary>
    /// All physical mediums this skill can be used through (organ or weapon).
    /// A skill may appear in multiple organ-medium lists (e.g. flesh_tear in fangs and teeth).
    /// </summary>
    public abstract FightingMedium[] Mediums { get; }

    /// <summary>Primary medium (first in <see cref="Mediums"/>). Convenience accessor for single-medium skills.</summary>
    public FightingMedium Medium => Mediums[0];

    /// <summary>Cinetic points spent to use this skill.</summary>
    public abstract int CineticPointsCost { get; }

    /// <summary>
    /// Vital heat drained from the attacker's humor queue when using this skill.
    /// Default 0. Visceral skills (Rage, Blood Lust, etc.) override this.
    /// </summary>
    public virtual int VitalHeatCost => 0;

    /// <summary>
    /// Extra dice independent of ModusMentis and medium level.
    /// Default 0 — most skills derive all dice from the multiplicator formula.
    /// Override for legacy skills or skills with a fixed base.
    /// </summary>
    public virtual int BaseDice => 0;

    /// <summary>
    /// Multiplier applied to the medium level when computing total dice.
    /// Formula: <c>BaseDice + medium_level × MediumLevelMultiplicator + mm_level × SkillLevelMultiplicator</c>
    /// </summary>
    public virtual int MediumLevelMultiplicator => 1;

    /// <summary>
    /// Multiplier applied to the summed ModusMentis level when computing total dice.
    /// </summary>
    public virtual int SkillLevelMultiplicator => 1;

    /// <summary>Status effects applied to the target on a successful hit.</summary>
    public virtual FightStatusEffect[] SpecialEffects => Array.Empty<FightStatusEffect>();

    /// <summary>Primary effect type.</summary>
    public abstract FightingSkillEffect EffectType { get; }

    /// <summary>How the wound target is chosen.</summary>
    public virtual WoundTargetMode WoundTargetMode => WoundTargetMode.Random;

    /// <summary>Body part id to target when <see cref="WoundTargetMode"/> is <see cref="WoundTargetMode.FixedBodyPart"/>.</summary>
    public virtual string? TargetBodyPartId => null;

    /// <summary>Maximum Euclidean distance from attacker to a valid target cell. Default 1 (adjacent melee).</summary>
    public virtual int Range => 1;

    /// <summary>
    /// Minimum Euclidean distance from attacker to a valid target cell.
    /// Default 1 (any non-self cell). Ranged skills like bow shots override this to forbid
    /// firing at point-blank range — the targetable area becomes a donut.
    /// </summary>
    public virtual int MinRange => 1;

    /// <summary>
    /// 1-based position of this skill in its medium's ordered skill list,
    /// derived from <see cref="OrganMediumRegistry"/> or <see cref="WeaponMediumRegistry"/>.
    /// Learning difficulty = MediumPosition - 1 (0-based index).
    /// For skills that appear in multiple categories the lowest position is used.
    /// </summary>
    public int MediumPosition
    {
        get
        {
            int best = int.MaxValue;
            foreach (var m in Mediums)
            {
                if (m.Type == MediumType.OrganMedium)
                {
                    foreach (var cat in OrganMediumRegistry.GetAll())
                        for (int i = 0; i < cat.SkillIds.Count; i++)
                            if (cat.SkillIds[i] == SkillId) { best = Math.Min(best, i + 1); break; }
                }
                else if (m.Type == MediumType.BodyPartMedium)
                {
                    foreach (var cat in BodyPartMediumRegistry.GetAll())
                        for (int i = 0; i < cat.SkillIds.Count; i++)
                            if (cat.SkillIds[i] == SkillId) { best = Math.Min(best, i + 1); break; }
                }
                else
                {
                    foreach (var cat in WeaponMediumRegistry.GetAll())
                        for (int i = 0; i < cat.SkillIds.Count; i++)
                            if (cat.SkillIds[i] == SkillId) { best = Math.Min(best, i + 1); break; }
                }
            }
            return best == int.MaxValue ? 1 : best;
        }
    }

    /// <summary>1-based position of this skill in the specified organ's medium list. Falls back to <see cref="MediumPosition"/> if not found.</summary>
    public int GetMediumPositionForOrganId(string organId)
    {
        var cat = OrganMediumRegistry.GetAll().FirstOrDefault(c => c.OrganId == organId);
        if (cat != null)
            for (int i = 0; i < cat.SkillIds.Count; i++)
                if (cat.SkillIds[i] == SkillId) return i + 1;
        return MediumPosition;
    }

    /// <summary>1-based position of this skill in the specified body-part's medium list. Falls back to <see cref="MediumPosition"/> if not found.</summary>
    public int GetMediumPositionForBodyPartId(string bodyPartId)
    {
        var cat = BodyPartMediumRegistry.GetAll().FirstOrDefault(c => c.BodyPartId == bodyPartId);
        if (cat != null)
            for (int i = 0; i < cat.SkillIds.Count; i++)
                if (cat.SkillIds[i] == SkillId) return i + 1;
        return MediumPosition;
    }

    /// <summary>Returns the medium from <see cref="Mediums"/> whose organ id matches, or null.</summary>
    public FightingMedium? GetMediumForOrganId(string organId) =>
        Mediums.FirstOrDefault(m => m.Type == MediumType.OrganMedium && m.OrganId == organId);

    /// <summary>Returns the medium from <see cref="Mediums"/> whose body-part id matches, or null.</summary>
    public FightingMedium? GetMediumForBodyPartId(string bodyPartId) =>
        Mediums.FirstOrDefault(m => m.Type == MediumType.BodyPartMedium && m.BodyPartId == bodyPartId);

    /// <summary>
    /// True when this skill targets only the user — clicking the action button
    /// executes it immediately without requiring an arena cell click.
    /// DefensePosture, Utility, Other, and Defense skills are self-targeted by default.
    /// </summary>
    public virtual bool IsSelfTargeting =>
        EffectType == FightingSkillEffect.DefensePosture ||
        EffectType == FightingSkillEffect.Utility ||
        EffectType == FightingSkillEffect.Other ||
        EffectType == FightingSkillEffect.Defense;

    // ── Derived calculations ──────────────────────────────────────

    /// <summary>
    /// All known MMs (main + secondary) this fighter brings to the skill — used to award XP on a hit.
    /// </summary>
    public IEnumerable<ModusMentis> GetContributingModiMentis(Fighter f) =>
        f.Member.LearnedModiMentis.Where(mm =>
            mm.ModusMentisId == RequiredModusMentisId ||
            SecondaryModusMentisIds.Contains(mm.ModusMentisId));

    /// <summary>
    /// Sum of levels from all known MMs (main + secondary) for this fighter.
    /// </summary>
    private int GetTotalMmLevel(Fighter f)
    {
        int total = 0;
        foreach (var mm in f.Member.LearnedModiMentis)
        {
            if (mm.ModusMentisId == RequiredModusMentisId)
                total += mm.Level;
            else if (SecondaryModusMentisIds.Contains(mm.ModusMentisId))
                total += mm.Level;
        }
        return total;
    }

    /// <summary>
    /// Total dice for a given fighter:
    /// <c>BaseDice + medium_level × MediumLevelMultiplicator + mm_level × SkillLevelMultiplicator</c>
    /// where mm_level is the sum of all known MM levels (main + secondary).
    /// When <paramref name="organPartId"/> is supplied, the medium level is that organ part's
    /// score (e.g. a left-hand punch uses only the left hand's level) instead of the whole organ.
    /// When <paramref name="activeMedium"/> is supplied it overrides <see cref="Medium"/> for the
    /// level lookup — used when a multi-medium skill is executed from a non-primary medium tab.
    /// </summary>
    public int TotalDice(Fighter f, string? organPartId = null, FightingMedium? activeMedium = null)
    {
        int mediumLevel = (activeMedium ?? Medium).GetLevel(f, organPartId);
        int mmLevel     = GetTotalMmLevel(f);
        return BaseDice + mediumLevel * MediumLevelMultiplicator + mmLevel * SkillLevelMultiplicator;
    }

    /// <summary>
    /// Returns true when the fighter can use this skill in the current combat state.
    /// Checks: primary OR any secondary ModusMentis known, medium available, organ not disabled by wounds.
    /// (CP cost check is handled separately in GetUnlockedSkills.)
    /// </summary>
    public bool IsUnlocked(Fighter f)
    {
        // Check primary OR any secondary ModusMentis known
        bool anyMmKnown = f.Member.LearnedModiMentis.Any(m =>
            m.ModusMentisId == RequiredModusMentisId ||
            SecondaryModusMentisIds.Contains(m.ModusMentisId));
        if (!anyMmKnown)
            return false;

        // At least one medium must be available and undisabled.
        return IsAnyMediumAvailable(f);
    }

    /// <summary>Returns true when at least one of <see cref="Mediums"/> is usable by this fighter.</summary>
    public bool IsAnyMediumAvailable(Fighter f)
    {
        var equippedWeapons = f.Member.EquippedItems[EquipmentAnchor.RightHold]
            .Concat(f.Member.EquippedItems[EquipmentAnchor.LeftHold])
            .OfType<IWeaponItem>();

        foreach (var m in Mediums)
        {
            if (m.Type == MediumType.OrganMedium)
            {
                var organId = m.OrganId;
                if (string.IsNullOrEmpty(organId)) continue;
                var organ = f.Member.GetOrganById(organId);
                if (organ == null) continue;
                bool disabled = f.Member.Wounds.Any(w =>
                    w.Handicap == WoundHandicap.High &&
                    (w.TargetKind == WoundTargetKind.Organ && w.TargetId == organId ||
                     w.TargetKind == WoundTargetKind.BodyPart && w.TargetId == organ.BodyPartId));
                if (!disabled) return true;
            }
            else if (m.Type == MediumType.BodyPartMedium)
            {
                var bodyPartId = m.BodyPartId;
                if (string.IsNullOrEmpty(bodyPartId)) continue;
                var bodyPart = f.Member.GetBodyPartById(bodyPartId);
                if (bodyPart == null) continue;
                bool disabled = f.Member.Wounds.Any(w =>
                    w.Handicap == WoundHandicap.High &&
                    w.TargetKind == WoundTargetKind.BodyPart && w.TargetId == bodyPartId);
                if (!disabled) return true;
            }
            else // WeaponMedium
            {
                if (equippedWeapons.Any(w =>
                    WeaponMediumRegistry.GetById(w.WeaponCategory)?.SkillIds.Contains(SkillId) == true))
                    return true;
            }
        }
        return false;
    }
}
