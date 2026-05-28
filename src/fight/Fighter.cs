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
    public bool IsDefensePostureActive { get; set; }
    public int InitiativeRoll { get; set; }  // Set at fight start: rng.Next(1,7) + InitiativeValue

    // ── Status effects ────────────────────────────────────────────
    /// <summary>Active status effects currently affecting this fighter.</summary>
    public List<FightStatusEffect> ActiveEffects { get; } = new();
    /// <summary>True when a KnockdownEffect is active — no attack skills this turn.</summary>
    public bool IsKnockedDown { get; set; }
    /// <summary>True when an ImmobilizeEffect is active — no movement this turn.</summary>
    public bool IsImmobilized { get; set; }

    // ── Derived stat shortcuts ────────────────────────────────────
    public int MaxCineticPoints   => GetCombatStat("cinetic_points");
    public int MoveSpeed          => GetCombatStat("move_speed");
    public int BaseNaturalDefense => GetCombatStat("natural_defense");
    /// <summary>Natural defense including active posture bonus.</summary>
    public int NaturalDefense     => BaseNaturalDefense + (IsDefensePostureActive ? 2 : 0);
    /// <summary>Number of d6 rolled in a runaway check (1 die per foot level). At least one six required to flee.</summary>
    public int RunawayDiceCount => Math.Max(1, GetCombatStat("runaway_dice"));
    /// <summary>Equilibrium — feet stat. Higher = lower terrain-slip risk during movement.</summary>
    public int EquilibriumValue => Math.Max(1, GetCombatStat("equilibrium"));
    public int InitiativeValue    => GetCombatStat("initiative");

    // ── HP delegation ─────────────────────────────────────────────
    public int MaxHp     => Member.MaxHp;
    public int CurrentHp => Member.CurrentHp;
    public bool IsAlive  => CurrentHp > 0;

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
        CurrentCineticPoints   = Math.Max(1, MaxCineticPoints);
        HasActedThisTurn       = false;
        IsDefensePostureActive = false;

        // Process status effects (bleeding, knockdown expiry, etc.)
        for (int i = ActiveEffects.Count - 1; i >= 0; i--)
        {
            ActiveEffects[i].OnTurnStart(this, state, rng);
            if (ActiveEffects[i].IsExpired)
                ActiveEffects.RemoveAt(i);
        }
    }

    /// <summary>Parameterless overload for contexts without state/rng (legacy / init).</summary>
    public void StartTurn()
    {
        CurrentCineticPoints   = Math.Max(1, MaxCineticPoints);
        HasActedThisTurn       = false;
        IsDefensePostureActive = false;
    }

    // ── Skill access ──────────────────────────────────────────────
    /// <summary>All fighting skills this fighter can currently use (ModusMentis + medium available + CP cost met).</summary>
    public IEnumerable<FightingSkill> GetUnlockedSkills(FightingSkillRegistry registry) =>
        registry.GetAll().Where(s => s.IsUnlocked(this) && CurrentCineticPoints >= s.CineticPointsCost);

    /// <summary>Fighting skills this fighter knows but cannot currently afford (IsUnlocked but CP cost exceeds CurrentCineticPoints).</summary>
    public IEnumerable<FightingSkill> GetUnaffordableKnownSkills(FightingSkillRegistry registry) =>
        registry.GetAll().Where(s => s.IsUnlocked(this) && CurrentCineticPoints < s.CineticPointsCost);

    /// <summary>
    /// Returns one learnable skill per available medium group:
    /// - For organ mediums: the lowest-MediumPosition unknown skill per organ.
    /// - For weapon mediums: the first unknown skill in each equipped weapon's category order.
    /// </summary>
    public IEnumerable<FightingSkill> GetLearnableSkills(FightingSkillRegistry registry)
    {
        // ── Organ-medium learnable skills ─────────────────────────────
        var organLearnables = registry.GetAll()
            .Where(s => s.Medium.Type == MediumType.OrganMedium)
            .Where(s => !Member.LearnedModiMentis.Any(m => m.ModusMentisId == s.RequiredModusMentisId))
            .Where(s => IsMediumAvailable(s))
            .Where(s => CurrentCineticPoints >= s.CineticPointsCost)
            .GroupBy(s => s.Medium.OrganId ?? s.SkillId)
            .Select(g => g.OrderBy(s => s.MediumPosition).First());

        // ── Weapon-medium learnable skills ────────────────────────────
        // One learnable per equipped weapon's category; skill is the first
        // unknown entry in that category's ordered skill list.
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
                if (Member.LearnedModiMentis.Any(m => m.ModusMentisId == skill.RequiredModusMentisId)) continue;
                if (CurrentCineticPoints < skill.CineticPointsCost) continue;
                weaponLearnables.Add(skill);
                break; // one learnable per category
            }
        }

        return organLearnables.Concat(weaponLearnables);
    }

    private bool IsMediumAvailable(FightingSkill skill)
    {
        if (skill.Medium.Type == MediumType.OrganMedium)
        {
            var organId = skill.Medium.OrganId;
            if (string.IsNullOrEmpty(organId)) return false;
            return Member.GetOrganById(organId) != null;
        }
        // Weapon: at least one equipped weapon whose category contains this skill
        return Member.EquippedItems[EquipmentAnchor.RightHold]
            .Concat(Member.EquippedItems[EquipmentAnchor.LeftHold])
            .OfType<IWeaponItem>()
            .Any(w => WeaponMediumRegistry.GetById(w.WeaponCategory)?.SkillIds.Contains(skill.SkillId) == true);
    }

    /// <summary>Fight learning stat value — number of dice rolled when attempting to learn an unknown skill.</summary>
    public int FightLearningStat => GetCombatStat("fight_learning");

    // ── Helpers ───────────────────────────────────────────────────
    private int GetCombatStat(string name)
    {
        var stat = Member.DerivedStats.FirstOrDefault(s => s.Name == name);
        return stat?.GetValue(Member) ?? 0;
    }
}
