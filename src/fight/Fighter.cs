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

    // ── Derived stat shortcuts ────────────────────────────────────
    public int MaxCineticPoints   => GetCombatStat("cinetic_points");
    public int MoveSpeed          => GetCombatStat("move_speed");
    public int BaseNaturalDefense => GetCombatStat("natural_defense");
    /// <summary>Natural defense including active posture bonus.</summary>
    public int NaturalDefense     => BaseNaturalDefense + (IsDefensePostureActive ? 2 : 0);
    public int RunawayChancePercent => GetCombatStat("runaway_chance");
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
        : Config.Colors.Orange;

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
    /// <summary>Called at the start of this fighter's turn: restore CP, reset per-turn flags.</summary>
    public void StartTurn()
    {
        CurrentCineticPoints  = Math.Max(1, MaxCineticPoints);
        HasActedThisTurn      = false;
        IsDefensePostureActive = false;
    }

    // ── Skill access ──────────────────────────────────────────────
    /// <summary>All fighting skills this fighter can currently use (ModusMentis + medium available + CP cost met).</summary>
    public IEnumerable<FightingSkill> GetUnlockedSkills(FightingSkillRegistry registry) =>
        registry.GetAll().Where(s => s.IsUnlocked(this) && CurrentCineticPoints >= s.CineticPointsCost);

    // ── Helpers ───────────────────────────────────────────────────
    private int GetCombatStat(string name)
    {
        var stat = Member.DerivedStats.FirstOrDefault(s => s.Name == name);
        return stat?.GetValue(Member) ?? 0;
    }
}
