using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using Cathedral.Game.Narrative;

namespace Cathedral.Fight;

/// <summary>
/// Which side of the fight a fighter belongs to.
/// </summary>
public enum FighterFaction { Party, Enemy }

/// <summary>
/// Wraps a <see cref="PartyMember"/> for the combat system.
/// Holds arena position, current action points, and turn-state bookkeeping.
/// </summary>
public class Fighter
{
    // ── Identity ─────────────────────────────────────────────────
    public PartyMember Member { get; }
    public FighterFaction Faction { get; init; }
    public bool IsPlayerControlled { get; init; }

    // ── Arena position ──────────────────────────────────────────
    public int X { get; set; }
    public int Y { get; set; }

    // ── Turn state ───────────────────────────────────────────────
    public int CurrentCineticPoints { get; set; }
    public bool HasActedThisTurn { get; set; }
    public int InitiativeRoll { get; set; }  // Set at fight start: rng.Next(1,7) + InitiativeValue

    // ── Status effects ────────────────────────────────────────────
    /// <summary>Active status effects currently affecting this fighter.</summary>
    public List<FightStatusEffect> ActiveEffects { get; } = new();
    /// <summary>True when a KnockdownEffect is active — no attack skills this turn.</summary>
    public bool IsKnockedDown { get; set; }
    /// <summary>True when an ImmobilizeEffect is active — no movement this turn.</summary>
    public bool IsImmobilized { get; set; }

    /// <summary>
    /// True once this fighter has turned a melee attack aside since their own last turn — the
    /// opening a riposte needs (<see cref="FightingSkill.RequiresSuccessfulDefense"/>).
    ///
    /// <para>
    /// Set while defending, which happens during someone <em>else's</em> turn, and cleared at the
    /// end of this fighter's own — so the counter has exactly one turn in which to be taken.
    /// </para>
    /// </summary>
    public bool HasDefendedMeleeSinceOwnTurn { get; set; }

    // ── Derived stat shortcuts ────────────────────────────────────
    public int MaxCineticPoints   => GetCombatStat("cinetic_points");
    public int MoveSpeed          => GetCombatStat("move_speed");

    /// <summary>
    /// Tiles per Cinetic Point actually available right now — the raw <see cref="MoveSpeed"/> stat
    /// floored at 1 and scaled by any active effect (Sprint doubles it).
    ///
    /// <para>
    /// Every movement budget must read this rather than <c>Math.Max(1, MoveSpeed)</c>: the range
    /// preview, the path cost, the AI's reachability search and the MOVE info panel all have to
    /// agree, or the player is shown a range they cannot walk.
    /// </para>
    /// </summary>
    public int EffectiveMoveSpeed
    {
        get
        {
            int speed = Math.Max(1, MoveSpeed);
            foreach (var e in ActiveEffects)
                speed *= Math.Max(1, e.MoveSpeedMultiplier);
            return speed;
        }
    }

    /// <summary>True when an active effect lets this fighter path through HardObstacle cells (Jump).</summary>
    public bool CanCrossHardObstacles => ActiveEffects.Any(e => e.AllowsHardObstacleCrossing);

    /// <summary>Total bonus defence dice granted by active effects (Parry, Dodge, postures).</summary>
    public int BonusDefenseDice => ActiveEffects.Sum(e => e.BonusDefenseDice);

    /// <summary>Total bonus attack dice granted by active effects (Feint's carry-over).</summary>
    public int BonusAttackDice => ActiveEffects.Sum(e => e.BonusAttackDice);

    public int BaseNaturalDefense => GetCombatStat("natural_defense");
    /// <summary>
    /// The defence pool before armour. A stance or a guard adds to it through
    /// <see cref="BonusDefenseDice"/> now — it used to be a flat +2 behind a bool on this class,
    /// which the STATE pane could not see and so never reported to the player.
    /// </summary>
    public int NaturalDefense     => BaseNaturalDefense;

    /// <summary>
    /// Bonus defence dice from worn armour covering <paramref name="sectionId"/>. A null section
    /// (a blow that lands nowhere in particular) is turned by nothing.
    /// </summary>
    public int ArmorDice(string? sectionId) =>
        sectionId is null ? 0 : Member.ArmorDiceForSection(sectionId);
    /// <summary>Bonus attack dice added to every offensive skill roll (genitories stat).</summary>
    public int NaturalAttack      => GetCombatStat("natural_attack");
    /// <summary>Number of d6 rolled in a runaway check (1 die per foot level). At least one six required to flee.</summary>
    public int RunawayDiceCount => GetCombatStat("runaway_dice");
    /// <summary>Equilibrium — feet stat. Higher = lower terrain-slip risk during movement.</summary>
    public int EquilibriumValue => GetCombatStat("equilibrium");
    /// <summary>Knockdown recovery dice count — heart stat. Need at least one 6 to recover.</summary>
    public int KnockdownRecoveryDiceCount => GetCombatStat("knockdown_recovery");
    public int InitiativeValue    => GetCombatStat("initiative");

    // ── HP delegation ─────────────────────────────────────────────
    public int MaxHp     => Member.MaxHp;
    public int CurrentHp => Member.CurrentHp;
    /// <summary>True once humor queues fully collapsed (e.g. terminal bleed) — fighter counts as dead.</summary>
    public bool IsHumorDepleted { get; set; }
    public bool IsAlive  => CurrentHp > 0 && !IsHumorDepleted;

    // ── AI bookkeeping ────────────────────────────────────────────
    /// <summary>Combat personality consulted by <see cref="FightAI"/>. Defaults to balanced;
    /// the fight builder assigns archetype-derived values for enemy fighters.</summary>
    public AiPersonality Personality { get; set; } = AiPersonality.Default;
    /// <summary>Initiative-list index of the fighter we attacked most recently. Used by
    /// the AI for short-term target focus so an enemy doesn't reshuffle priorities every turn.</summary>
    public int? LastAttackTargetIdx { get; set; }

    // ── Display ───────────────────────────────────────────────────
    public string DisplayName => Member.DisplayName;
    public char DisplayChar  => Faction == FighterFaction.Party ? '☻' : '☹';
    public Vector4 DisplayColor => Faction == FighterFaction.Party
        ? Config.Colors.White
        : Config.Colors.Purple;

    // ── Constructor ───────────────────────────────────────────────
    public Fighter(PartyMember member, int x, int y, bool isPlayerControlled, FighterFaction faction)
    {
        Member            = member ?? throw new ArgumentNullException(nameof(member));
        X                 = x;
        Y                 = y;
        IsPlayerControlled = isPlayerControlled;
        Faction           = faction;
    }

    // ── Turn management ───────────────────────────────────────────
    /// <summary>
    /// Called at the start of this fighter's turn: restore CP, reset per-turn flags,
    /// and process all active status effects (bleeding drain, knockdown expiry, etc.).
    /// Requires a <paramref name="state"/> and <paramref name="rng"/> for effect processing.
    /// </summary>
    public void StartTurn(FightState state, Random rng)
    {
        CurrentCineticPoints = Math.Max(1, MaxCineticPoints);
        HasActedThisTurn     = false;

        // Process status effects (bleeding, knockdown expiry, etc.)
        for (int i = ActiveEffects.Count - 1; i >= 0; i--)
        {
            ActiveEffects[i].OnTurnStart(this, state, rng);
            if (ActiveEffects[i].IsExpired)
                ActiveEffects.RemoveAt(i);
        }
    }

    /// <summary>
    /// Called as this fighter's turn ends, before the initiative order advances.
    ///
    /// <para>
    /// This is where "lasts this turn" actually means this turn. Without it the only expiry pass is
    /// <see cref="StartTurn"/>, which runs a full round later — so a buff taken on your turn would
    /// still be up while every enemy acted. Effects that DO want the round (Cold Blood defends
    /// during enemy turns) just leave <see cref="FightStatusEffect.OnTurnEnd"/> alone.
    /// </para>
    /// </summary>
    public void EndTurn(FightState state, Random rng)
    {
        // The riposte opening closes with the turn it was earned for.
        HasDefendedMeleeSinceOwnTurn = false;

        for (int i = ActiveEffects.Count - 1; i >= 0; i--)
        {
            ActiveEffects[i].OnTurnEnd(this, state, rng);
            if (ActiveEffects[i].IsExpired)
                ActiveEffects.RemoveAt(i);
        }
    }

    /// <summary>Parameterless overload for contexts without state/rng (legacy / init).</summary>
    public void StartTurn()
    {
        CurrentCineticPoints = Math.Max(1, MaxCineticPoints);
        HasActedThisTurn     = false;
    }

    // ── Skill access ──────────────────────────────────────────────
    /// <summary>
    /// All fighting skills this fighter can currently use: they know it, the medium is available,
    /// they can afford it, and any precondition the skill sets is met — a riposte only exists in
    /// the window after a blow has been turned aside.
    /// </summary>
    public IEnumerable<FightingSkill> GetUnlockedSkills(FightingSkillRegistry registry) =>
        registry.GetAll().Where(s => s.IsUnlocked(this)
                                  && CurrentCineticPoints >= s.CineticPointsCost
                                  && (!s.RequiresSuccessfulDefense || HasDefendedMeleeSinceOwnTurn));

    /// <summary>
    /// Fighting skills this fighter knows and has a limb for but cannot use right now — drawn greyed
    /// in the action list. Two reasons, and the row looks the same for both because from the
    /// player's side it is one fact: the skill is there and not available.
    ///
    /// <list type="bullet">
    ///   <item>the CP cost exceeds <see cref="CurrentCineticPoints"/> — a passing shortage;</item>
    ///   <item>wounds have broken the modi mentis behind it
    ///         (<see cref="FightingSkill.IsKnownButBroken"/>) — which lasts until they heal.</item>
    /// </list>
    ///
    /// <para>A broken skill has to come from its own test rather than from
    /// <see cref="FightingSkill.IsUnlocked"/>, because that method now returns false for exactly
    /// these — which is what keeps them out of <see cref="GetUnlockedSkills"/> and away from the AI.</para>
    /// </summary>
    public IEnumerable<FightingSkill> GetUnusableKnownSkills(FightingSkillRegistry registry) =>
        registry.GetAll().Where(s =>
            (s.IsUnlocked(this) && CurrentCineticPoints < s.CineticPointsCost)
            || s.IsKnownButBroken(this));

    /// <summary>
    /// Returns one learnable skill per available medium group:
    /// - For organ mediums: the lowest-MediumPosition unknown skill per organ.
    /// - For weapon mediums: the first unknown skill in each equipped weapon's category order.
    /// </summary>
    public IEnumerable<FightingSkill> GetLearnableSkills(FightingSkillRegistry registry)
    {
        // ── Organ-medium learnable skills ─────────────────────────────
        // Iterate each organ registry category in order. For each available organ, find
        // the first skill in its list whose primary MM is not yet known. Because a skill
        // can appear in multiple organ lists (e.g. flesh_tear in fangs AND teeths), we
        // deduplicate by SkillId so the same skill object is returned only once — the UI's
        // AddOrganSkillToGroups will place it under every applicable organ tab on its own.
        var seenOrganSkillIds = new HashSet<string>();
        var organLearnables   = new List<FightingSkill>();

        foreach (var cat in OrganMediumRegistry.GetAll())
        {
            if (Member.GetOrganById(cat.OrganId) == null) continue; // fighter lacks this organ
            foreach (var skillId in cat.SkillIds)
            {
                var skill = registry.GetById(skillId);
                if (skill == null) continue;
                if (CurrentCineticPoints < skill.CineticPointsCost) continue;
                if (Member.LearnedModiMentis.Any(m =>
                    m.ModusMentisId == skill.RequiredModusMentisId ||
                    skill.SecondaryModusMentisIds.Contains(m.ModusMentisId)))
                    continue; // at least one unlocking MM already known
                if (seenOrganSkillIds.Add(skill.SkillId))
                    organLearnables.Add(skill);
                break; // first unknown per organ
            }
        }

        // ── Body-part-medium learnable skills ─────────────────────────
        var seenBodyPartSkillIds = new HashSet<string>();
        var bodyPartLearnables   = new List<FightingSkill>();

        foreach (var cat in BodyPartMediumRegistry.GetAll())
        {
            if (Member.GetBodyPartById(cat.BodyPartId) == null) continue;
            foreach (var skillId in cat.SkillIds)
            {
                var skill = registry.GetById(skillId);
                if (skill == null) continue;
                if (CurrentCineticPoints < skill.CineticPointsCost) continue;
                if (Member.LearnedModiMentis.Any(m =>
                    m.ModusMentisId == skill.RequiredModusMentisId ||
                    skill.SecondaryModusMentisIds.Contains(m.ModusMentisId)))
                    continue;
                if (seenBodyPartSkillIds.Add(skill.SkillId))
                    bodyPartLearnables.Add(skill);
                break;
            }
        }

        // ── Weapon-medium learnable skills ────────────────────────────
        var equippedWeapons = Member.EquippedItems[EquipmentAnchor.RightHold]
            .Concat(Member.EquippedItems[EquipmentAnchor.LeftHold])
            .OfType<IWeaponItem>()
            .ToList();

        var weaponLearnables = new List<FightingSkill>();
        var seenCategories   = new HashSet<string>();

        foreach (var weapon in equippedWeapons)
        {
            var category = WeaponMediumRegistry.GetById(weapon.WeaponCategory);
            if (category == null || !seenCategories.Add(category.CategoryId)) continue;

            foreach (var skillId in category.SkillIds)
            {
                var skill = registry.GetById(skillId);
                if (skill == null) continue;
                if (Member.LearnedModiMentis.Any(m =>
                    m.ModusMentisId == skill.RequiredModusMentisId ||
                    skill.SecondaryModusMentisIds.Contains(m.ModusMentisId))) continue;
                if (CurrentCineticPoints < skill.CineticPointsCost) continue;
                weaponLearnables.Add(skill);
                break;
            }
        }

        return organLearnables.Concat(bodyPartLearnables).Concat(weaponLearnables);
    }

    /// <summary>Fight learning stat value — number of dice rolled when attempting to learn an unknown skill.</summary>
    public int FightLearningStat => GetCombatStat("fight_learning");

    // ── Helpers ───────────────────────────────────────────────────
    private int GetCombatStat(string name)
        => Member.DerivedStats.First(s => s.Name == name).GetValue(Member);
}
