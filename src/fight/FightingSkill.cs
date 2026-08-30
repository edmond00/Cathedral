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
    /// <summary>Other utility effects.</summary>
    Utility,
    /// <summary>Alias for <see cref="Utility"/> — other utility effects.</summary>
    Other,
    /// <summary>
    /// A self-applied buff: no target, no dice, no wound. It costs vital heat instead — see
    /// <see cref="FightingSkill.VitalHeatCostFor"/> — and its whole effect is the
    /// <see cref="FightStatusEffect"/> returned by <see cref="FightingSkill.CreateBuffEffect"/>.
    /// Rolling dice for one would be meaningless: there is nothing to hit and nothing to resist.
    /// </summary>
    Buff,
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

    /// <summary>
    /// The kind of harm this skill does — cutting, piercing, blunt, or a combination.
    /// Only used when a blow lands somewhere with no authored wound: the generic wound it leaves
    /// then matches the weapon rather than being the same graze for everything. See
    /// <c>FightResolver.Wildcards</c>. <see cref="DamageType.None"/> (the default) means any.
    /// </summary>
    public virtual DamageType DamageTypes => DamageType.None;

    /// <summary>How the wound target is chosen.</summary>
    public virtual WoundTargetMode WoundTargetMode => WoundTargetMode.Random;

    /// <summary>
    /// Where this skill lands, when <see cref="WoundTargetMode"/> is
    /// <see cref="WoundTargetMode.FixedBodyPart"/>.
    ///
    /// <para>
    /// A comma-separated list means "one of these", drawn before the roll — "trunk,upper_limbs" for
    /// a bite that takes the body or an arm. Ids absent from the defender's anatomy are skipped, so
    /// a list can name both the human and beast forms of a location safely.
    /// </para>
    /// </summary>
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
    /// Buff, DefensePosture, Utility, Other, and Defense skills are self-targeted by default.
    /// </summary>
    public virtual bool IsSelfTargeting =>
        EffectType == FightingSkillEffect.Buff ||
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
    ///
    /// <para><b>Effective levels, not stored ones.</b> A wound lowers what a modus mentis may reach
    /// (<c>PartyMember.GetEffectiveModusMentisLevel</c>), and this sum is half of
    /// <see cref="TotalLevel"/> — so reading the stored level would let a ruined arm bring its full
    /// strength to every blow. It can therefore come out negative, which is what
    /// <see cref="IsBrokenFor"/> reads.</para>
    /// </summary>
    public int GetTotalMmLevel(Fighter f)
    {
        int total = 0;
        foreach (var mm in f.Member.LearnedModiMentis)
        {
            if (mm.ModusMentisId == RequiredModusMentisId
             || SecondaryModusMentisIds.Contains(mm.ModusMentisId))
                total += f.Member.GetEffectiveModusMentisLevel(mm);
        }
        return total;
    }

    /// <summary>
    /// Whether wounds have taken this skill out of the fighter's hands — the modi mentis behind it
    /// sum to <b>less than zero</b> once each is capped by what its organs can still reach.
    ///
    /// <para><b>The medium is deliberately not consulted</b>, and neither is
    /// <see cref="BaseDice"/>: a skill is a technique, and a body that can no longer hold the
    /// technique cannot swing it however sound the limb behind it. That is a separate question from
    /// <see cref="IsAnyMediumAvailable"/>, which asks whether there is a limb at all.</para>
    ///
    /// <para><b>Why below zero and not at it.</b> A sum of exactly 0 is what an <em>unlearned</em>
    /// skill reads as — no matching modus mentis contributes anything — and that state has to stay
    /// usable: <c>FirstBlow</c> draws from unlearned skills on purpose, since the first punch is
    /// where the punch is learned. Inside a fight <see cref="IsUnlocked"/> has already required the
    /// modus mentis to be known, so a 0 there means "known and worn down to nothing" and does slip
    /// through this gate — a skill at that point rolls <see cref="BaseDice"/> plus its medium and
    /// nothing else.</para>
    /// </summary>
    public bool IsBrokenFor(Fighter f) => GetTotalMmLevel(f) < 0;

    /// <summary>Whether this fighter knows the primary or any secondary modus mentis.</summary>
    private bool IsAnyModusMentisKnown(Fighter f) =>
        f.Member.LearnedModiMentis.Any(m =>
            m.ModusMentisId == RequiredModusMentisId ||
            SecondaryModusMentisIds.Contains(m.ModusMentisId));

    /// <summary>
    /// The itemised terms behind <see cref="TotalLevel"/>, so the info panel can show a player
    /// <em>where</em> a skill's strength comes from instead of one opaque number.
    /// <see cref="Total"/> is what the level formulas consume.
    /// </summary>
    /// <param name="MediumLabel">Display name of the medium the level was read from.</param>
    /// <param name="MediumLevel">Raw medium level (organ / organ-part / body-part score, or weapon level).</param>
    /// <param name="MediumMultiplicator">This skill's <see cref="MediumLevelMultiplicator"/>.</param>
    /// <param name="ModiMentis">Each contributing modus mentis and its level, in registry order.</param>
    /// <param name="SkillMultiplicator">This skill's <see cref="SkillLevelMultiplicator"/>.</param>
    public readonly record struct LevelBreakdown(
        string MediumLabel,
        int MediumLevel,
        int MediumMultiplicator,
        IReadOnlyList<(string Id, int Level)> ModiMentis,
        int SkillMultiplicator)
    {
        /// <summary>Summed modus-mentis levels, before the skill multiplicator.</summary>
        public int MmLevel => ModiMentis.Sum(m => m.Level);

        /// <summary>The single figure that feeds both the dice count and the buff vital-heat cost.</summary>
        public int Total => MediumLevel * MediumMultiplicator + MmLevel * SkillMultiplicator;
    }

    /// <summary>
    /// Itemised level terms for this fighter. Same arithmetic as <see cref="TotalLevel"/> — that
    /// method delegates here — so the panel can never drift from the roll.
    /// </summary>
    public LevelBreakdown GetLevelBreakdown(Fighter f, string? organPartId = null,
                                            FightingMedium? activeMedium = null)
    {
        var medium = activeMedium ?? Medium;
        var mms = f.Member.LearnedModiMentis
            .Where(mm => mm.ModusMentisId == RequiredModusMentisId
                      || SecondaryModusMentisIds.Contains(mm.ModusMentisId))
            .Select(mm => (mm.ModusMentisId, mm.Level))
            .ToList();
        return new LevelBreakdown(medium.DisplayName, medium.GetLevel(f, organPartId),
                                  MediumLevelMultiplicator, mms, SkillLevelMultiplicator);
    }

    /// <summary>
    /// This fighter's effective level with the skill:
    /// <c>medium_level × MediumLevelMultiplicator + mm_level × SkillLevelMultiplicator</c>.
    ///
    /// <para>
    /// Split out from <see cref="TotalDice"/> because level no longer only ever means dice: a
    /// <see cref="FightingSkillEffect.Buff"/> spends it in the opposite direction, reducing the
    /// vital heat the buff costs (<see cref="VitalHeatCostFor"/>). One formula, two readings.
    /// </para>
    ///
    /// When <paramref name="organPartId"/> is supplied, the medium level is that organ part's
    /// score (e.g. a left-hand punch uses only the left hand's level) instead of the whole organ.
    /// When <paramref name="activeMedium"/> is supplied it overrides <see cref="Medium"/> for the
    /// level lookup — used when a multi-medium skill is executed from a non-primary medium tab.
    /// </summary>
    public int TotalLevel(Fighter f, string? organPartId = null, FightingMedium? activeMedium = null)
    {
        int mediumLevel = (activeMedium ?? Medium).GetLevel(f, organPartId);
        int mmLevel     = GetTotalMmLevel(f);
        return mediumLevel * MediumLevelMultiplicator + mmLevel * SkillLevelMultiplicator;
    }

    /// <summary>
    /// Total dice for a given fighter: <c>BaseDice + <see cref="TotalLevel"/></c>.
    /// </summary>
    public int TotalDice(Fighter f, string? organPartId = null, FightingMedium? activeMedium = null)
        => BaseDice + TotalLevel(f, organPartId, activeMedium);

    /// <summary>Cheapest a buff can ever get, however skilled the fighter.</summary>
    public const int MinBuffVitalHeat = 1;
    /// <summary>What a buff costs a fighter with no relevant level at all.</summary>
    public const int MaxBuffVitalHeat = 10;

    /// <summary>
    /// Vital heat this skill drains from <paramref name="f"/>'s humor queues when used.
    ///
    /// <para>
    /// For a <see cref="FightingSkillEffect.Buff"/> this is the whole cost model, and it runs
    /// <em>backwards</em> from the dice one: level does not buy more dice, it buys a cheaper buff,
    /// from <see cref="MaxBuffVitalHeat"/> down to <see cref="MinBuffVitalHeat"/>. Every other
    /// skill keeps its flat authored <see cref="VitalHeatCost"/>.
    /// </para>
    /// </summary>
    public virtual int VitalHeatCostFor(Fighter f, string? organPartId = null,
                                        FightingMedium? activeMedium = null)
        => EffectType == FightingSkillEffect.Buff
            ? Math.Clamp(MaxBuffVitalHeat - TotalLevel(f, organPartId, activeMedium),
                         MinBuffVitalHeat, MaxBuffVitalHeat)
            : VitalHeatCost;

    /// <summary>
    /// The status effect a <see cref="FightingSkillEffect.Buff"/> applies to its user. Returning a
    /// fresh instance per call (as <see cref="SpecialEffects"/> does) keeps two fighters — or two
    /// uses — from sharing one mutable effect object.
    /// Null for everything that is not a buff.
    /// </summary>
    public virtual FightStatusEffect? CreateBuffEffect(Fighter owner) => null;

    /// <summary>
    /// The effect a SELF-TARGETED skill that still rolls dice grants, given how many sixes it got.
    /// Feint is the only such skill: it has something to roll against (someone must be convinced)
    /// but no one to wound.
    ///
    /// <para>
    /// A self-targeted roll must never produce a wound — that was exactly the old bug in which a
    /// parry could injure the person parrying — so <c>FinishAttackResolution</c> routes every
    /// self-targeted roll here instead of into wound selection. Returning null means the roll
    /// simply had no effect.
    /// </para>
    /// </summary>
    public virtual FightStatusEffect? CreateRolledEffect(Fighter owner, int sixes) => null;

    /// <summary>
    /// For a <see cref="FightingSkillEffect.DefensePosture"/> skill, whether the guard is beaten
    /// aside the first time a blow gets through it. True for Cover — a shield you have been driven
    /// off is no longer between you and anything — false for a braced stance, which holds for the
    /// turn regardless.
    /// </summary>
    public virtual bool GuardBreaksOnDamage => false;

    /// <summary>
    /// A riposte: the skill can only be used when the fighter has already turned a melee attack
    /// aside since their last turn. Counter Strike is the only one — it is what the skill IS, and
    /// the reason it hits as hard as it does for one Cinetic Point.
    /// </summary>
    public virtual bool RequiresSuccessfulDefense => false;

    /// <summary>
    /// How far this skill may charge: the attacker closes the distance and then strikes, in one
    /// action. Zero (the default) means an ordinary attack, which reaches only as far as
    /// <see cref="Range"/> without moving.
    ///
    /// <para>
    /// The lunges declare it. It is why they can be aimed at someone several cells away when every
    /// other melee skill is limited to a neighbour: the reach is the run-up, not the weapon.
    /// </para>
    /// </summary>
    public virtual int ChargeDistance => 0;

    /// <summary>
    /// Returns true when the fighter can use this skill in the current combat state.
    /// Checks: primary OR any secondary ModusMentis known, that modus mentis still carryable by this
    /// body, medium available, organ not disabled by wounds.
    /// (CP cost check is handled separately in GetUnlockedSkills.)
    ///
    /// <para>The broken test lives here rather than at the three call sites so the fight AI is gated
    /// by construction — it filters its own candidates through this method, and a skill an injured
    /// enemy could still swing while the player could not would be a difficulty bug nobody would
    /// trace back to a wound.</para>
    /// </summary>
    public bool IsUnlocked(Fighter f)
    {
        // Check primary OR any secondary ModusMentis known
        if (!IsAnyModusMentisKnown(f))
            return false;

        // Known, but wounds have worn the modus mentis behind it past usable.
        if (IsBrokenFor(f))
            return false;

        // At least one medium must be available and undisabled.
        return IsAnyMediumAvailable(f);
    }

    /// <summary>
    /// A skill the fighter knows and has a limb for, but whose modi mentis wounds have broken. Drawn
    /// greyed in the action list rather than dropped, so a player can see that a skill they had is
    /// gone rather than hunting a list for something that silently vanished.
    /// </summary>
    public bool IsKnownButBroken(Fighter f) =>
        IsAnyModusMentisKnown(f) && IsBrokenFor(f) && IsAnyMediumAvailable(f);

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
