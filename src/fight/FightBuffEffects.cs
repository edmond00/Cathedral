using System;
using System.Linq;
using OpenTK.Mathematics;
using Cathedral.Game.Narrative;

namespace Cathedral.Fight;

// ─────────────────────────────────────────────────────────────────────────────
// Buff effects
//
// The self-applied half of the effect system. Everything in FightStatusEffect.cs is something
// done TO a fighter by an attack; everything here is something a fighter spends vital heat to do
// FOR themselves — see FightingSkillEffect.Buff and FightingSkill.CreateBuffEffect.
//
// They all follow the same shape: declare the inert query the resolution path already reads, or
// override the one event that matters, and expire in OnTurnEnd. None of them reaches into the
// fight state to change turn order or dice directly — the consult sites do that, so a new buff is
// a class and nothing else.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Base for buffs that last until the end of the turn they were taken on.
/// <para>
/// Expiring in <see cref="OnTurnEnd"/> rather than <see cref="OnTurnStart"/> is the whole point:
/// the start-of-turn pass only comes round again after every other fighter has acted, which would
/// silently stretch every "this turn" buff to a full round.
/// </para>
/// </summary>
public abstract class TurnScopedBuffEffect : FightStatusEffect
{
    public override void OnTurnEnd(Fighter owner, FightState state, Random rng) => IsExpired = true;
}

/// <summary>
/// Base for effects that must survive the owner's own turn ending, because what they answer happens
/// on somebody <em>else's</em> turn — anything defensive.
///
/// <para>
/// The distinction is not pedantry. A guard expiring at <see cref="OnTurnEnd"/> is gone before the
/// first enemy swings, which makes it literally unusable: you brace, your turn ends because bracing
/// cost your last Cinetic Point, and the brace evaporates. These expire at the owner's next
/// <see cref="OnTurnStart"/> instead — one full round, which is exactly the window in which blows
/// arrive.
/// </para>
/// </summary>
public abstract class RoundScopedBuffEffect : FightStatusEffect
{
    public override void OnTurnStart(Fighter owner, FightState state, Random rng) => IsExpired = true;
}

/// <summary>
/// Defensive stance — the old <c>Fighter.IsDefensePostureActive</c> flag, promoted to an effect.
/// <para>
/// It was a bool on the fighter, which meant the STATE pane could not see it: a player who took a
/// stance got no confirmation anywhere that they had. As an effect it lists itself.
/// </para>
/// </summary>
public sealed class DefensePostureEffect : RoundScopedBuffEffect
{
    private readonly int  _dice;
    private readonly bool _breaksOnDamage;
    private readonly string _label;

    /// <param name="breaksOnDamage">
    /// Cover only. A shield holds against everything until something gets through it, and then it
    /// is out of position — unlike a braced stance, which holds for the turn regardless.
    /// </param>
    public DefensePostureEffect(string label, int dice, bool breaksOnDamage = false)
    {
        _label          = label;
        _dice           = Math.Max(0, dice);
        _breaksOnDamage = breaksOnDamage;
    }

    public override string EffectId      => "defense_posture";
    public override string DisplayLabel  => $"D{_dice}";
    public override Vector4 DisplayColor => Config.Colors.Yellow;
    public override string DisplayName   => $"{_label} (+{_dice})";
    public override string Description   =>
        $"Braced. Adds {_dice} dice to the defence pool against every attack until your next turn"
        + (_breaksOnDamage ? ", and breaks the moment a blow gets through." : ".");

    public override int BonusDefenseDice => _dice;

    public override void OnApply(Fighter target, Fighter source, FightState state, Random rng) =>
        state.AddLog($"{target.DisplayName} braces — +{_dice} defence dice.", LogEntryType.SpecialEffect);

    public override void OnDefended(Fighter owner, Fighter attacker, bool defenseSucceeded,
                                    FightState state, Random rng)
    {
        if (!_breaksOnDamage || defenseSucceeded) return;
        IsExpired = true;
        state.AddLog($"{owner.DisplayName}'s {_label.ToLowerInvariant()} is beaten aside.",
            LogEntryType.SpecialEffect);
    }
}

/// <summary>
/// Parry / Dodge — a reactive guard bought up front, and spent on ONE incoming attack.
///
/// <para>
/// Defence in this game IS a number of dice, so a guard skill has no reason to roll on use: it
/// hands its level straight to the pool the next attack is measured against. (Previously these went
/// down the attack path against the user's own defence, which is how a parry could wound the person
/// parrying.)
/// </para>
///
/// <para>
/// Unlike a stance, a guard covers a single blow — you parry <em>a</em> thrust — so it expires as
/// soon as one attack has been measured against it, hit or miss.
/// </para>
/// </summary>
public sealed class GuardEffect : RoundScopedBuffEffect
{
    private int _dice;
    private readonly string _label;

    public GuardEffect(string label, int dice)
    {
        _label = label;
        _dice  = Math.Max(0, dice);
    }

    public override string EffectId      => "guard";
    public override string DisplayLabel  => $"G{_dice}";
    public override Vector4 DisplayColor => Config.Colors.Yellow;
    public override string DisplayName   => $"{_label} (+{_dice})";
    public override string Description   =>
        $"Guarding. Adds {_dice} dice to the defence pool against the next attack, then is spent.";

    public override int BonusDefenseDice => _dice;

    public override void OnApply(Fighter target, Fighter source, FightState state, Random rng) =>
        state.AddLog($"{target.DisplayName} guards — +{_dice} defence dice on the next blow.",
            LogEntryType.SpecialEffect);

    /// <summary>Spent by the blow it answered, whether or not it turned it.</summary>
    public override void OnDefended(Fighter owner, Fighter attacker, bool defenseSucceeded,
                                    FightState state, Random rng)
    {
        _dice = 0;
        IsExpired = true;
        state.AddLog($"{owner.DisplayName}'s {_label.ToLowerInvariant()} is spent.",
            LogEntryType.SpecialEffect);
    }
}

/// <summary>Sprint — twice the ground per Cinetic Point for the rest of the turn.</summary>
public sealed class SprintEffect : TurnScopedBuffEffect
{
    public override string EffectId      => "sprint";
    public override string DisplayLabel  => "S";
    public override Vector4 DisplayColor => Config.Colors.LightGray;
    public override string DisplayName   => "Sprinting";
    public override string Description   => "Running flat out. Covers twice as many tiles per Cinetic Point until the end of this turn.";

    public override int MoveSpeedMultiplier => 2;

    public override void OnApply(Fighter target, Fighter source, FightState state, Random rng) =>
        state.AddLog($"{target.DisplayName} breaks into a run — double move speed.", LogEntryType.SpecialEffect);
}

/// <summary>Jump — clears the obstacles that ordinary movement has to walk around.</summary>
public sealed class JumpEffect : TurnScopedBuffEffect
{
    public override string EffectId      => "jump";
    public override string DisplayLabel  => "J";
    public override Vector4 DisplayColor => Config.Colors.LightGray;
    public override string DisplayName   => "Vaulting";
    public override string Description   => "Ready to vault. Movement may cross hard obstacles until the end of this turn.";

    public override bool AllowsHardObstacleCrossing => true;

    public override void OnApply(Fighter target, Fighter source, FightState state, Random rng) =>
        state.AddLog($"{target.DisplayName} readies to vault — obstacles no longer block movement.", LogEntryType.SpecialEffect);
}

/// <summary>Iron Nerves — the once-per-turn rule stops applying.</summary>
public sealed class IronNervesEffect : TurnScopedBuffEffect
{
    public override string EffectId      => "iron_nerves";
    public override string DisplayLabel  => "IN";
    public override Vector4 DisplayColor => Config.Colors.BrightPurple;
    public override string DisplayName   => "Iron nerves";
    public override string Description   => "Composed past exhaustion. Skills already used this turn may be used again — running away excepted.";

    public override bool BypassesUsedActions => true;

    public override void OnApply(Fighter target, Fighter source, FightState state, Random rng) =>
        state.AddLog($"{target.DisplayName} steadies — skills may be repeated this turn.", LogEntryType.SpecialEffect);
}

/// <summary>Survival Instinct — the runaway check may be retried as often as the turn allows.</summary>
public sealed class SurvivalInstinctEffect : TurnScopedBuffEffect
{
    public override string EffectId      => "survival_instinct";
    public override string DisplayLabel  => "SI";
    public override Vector4 DisplayColor => Config.Colors.BrightPurple;
    public override string DisplayName   => "Survival instinct";
    public override string Description   => "Every nerve set on getting out. A failed runaway check may be retried as many times as you like this turn.";

    public override bool AllowsRunawayRetry => true;

    public override void OnApply(Fighter target, Fighter source, FightState state, Random rng) =>
        state.AddLog($"{target.DisplayName} fixes on the way out — runaway checks may be retried.", LogEntryType.SpecialEffect);
}

/// <summary>
/// Rage — the first attack that lands this turn refills the Cinetic Point pool.
/// The once-per-turn latch lives on the effect instance, so it resets with the effect.
/// </summary>
public sealed class RageEffect : TurnScopedBuffEffect
{
    private bool _spent;

    public override string EffectId      => "rage";
    public override string DisplayLabel  => _spent ? "R·" : "R";
    public override Vector4 DisplayColor => Config.Colors.Purple;
    public override string DisplayName   => _spent ? "Rage (spent)" : "Rage";
    public override string Description   => "Fury feeding on contact. The first attack that lands this turn refills the Cinetic Point pool.";

    public override void OnApply(Fighter target, Fighter source, FightState state, Random rng) =>
        state.AddLog($"{target.DisplayName} gives in to rage — the next blow that lands restores Cinetic Points.", LogEntryType.SpecialEffect);

    public override void OnAttackResolved(Fighter owner, Fighter target, bool isHit,
                                          FightState state, Random rng)
    {
        if (_spent || !isHit) return;
        _spent = true;
        owner.CurrentCineticPoints = Math.Max(1, owner.MaxCineticPoints);
        state.AddLog($"{owner.DisplayName}'s rage feeds on the blow — Cinetic Points restored.", LogEntryType.SpecialEffect);
    }
}

/// <summary>
/// Cold Blood — a successful defence breaks the attacker off.
/// <para>
/// Scoped to the ROUND, not the turn: it fires while enemies attack, which happens after the
/// owner's turn has ended. It therefore does not override <see cref="FightStatusEffect.OnTurnEnd"/>
/// and expires on the owner's next <see cref="Fighter.StartTurn"/> instead.
/// </para>
/// </summary>
public sealed class ColdBloodEffect : FightStatusEffect
{
    private bool _armed;

    public override string EffectId      => "cold_blood";
    public override string DisplayLabel  => "CB";
    public override Vector4 DisplayColor => Config.Colors.LightPurple;
    public override string DisplayName   => "Cold blood";
    public override string Description   => "Reading the attack rather than fearing it. Any attack you turn aside ends the attacker's turn on the spot. Lasts until your next turn.";

    public override bool EndsAttackerTurnOnSuccessfulDefense => true;

    public override void OnApply(Fighter target, Fighter source, FightState state, Random rng)
    {
        _armed = true;
        state.AddLog($"{target.DisplayName} turns cold — a turned blow will break the attacker off.", LogEntryType.SpecialEffect);
    }

    public override void OnDefended(Fighter owner, Fighter attacker, bool defenseSucceeded,
                                    FightState state, Random rng)
    {
        if (defenseSucceeded)
            state.AddLog($"{owner.DisplayName} reads the blow — {attacker.DisplayName} is broken off.", LogEntryType.SpecialEffect);
    }

    // Expires at the owner's next turn start (a full round), unlike the turn-scoped buffs.
    public override void OnTurnStart(Fighter owner, FightState state, Random rng)
    {
        if (!_armed) return;
        IsExpired = true;
        state.AddLog($"{owner.DisplayName}'s cold blood fades.", LogEntryType.SpecialEffect);
    }
}

/// <summary>
/// Blood Lust — wounds this fighter inflicts are always the worst on offer.
/// <para>
/// The only buff scoped to the whole fight: it never expires, so it overrides neither
/// <see cref="FightStatusEffect.OnTurnEnd"/> nor <see cref="FightStatusEffect.OnTurnStart"/>.
/// </para>
/// </summary>
public sealed class BloodLustEffect : FightStatusEffect
{
    public override string EffectId      => "blood_lust";
    public override string DisplayLabel  => "BL";
    public override Vector4 DisplayColor => Config.Colors.BrightPurple;
    public override string DisplayName   => "Blood lust";
    public override string Description   => "Striking to ruin. Every wound you inflict is the most severe the blow could have caused. Lasts the whole fight.";

    public override bool ForcesHighestSeverityWound => true;

    public override void OnApply(Fighter target, Fighter source, FightState state, Random rng) =>
        state.AddLog($"{target.DisplayName} strikes to ruin — wounds will land at their worst.", LogEntryType.SpecialEffect);
}

/// <summary>
/// Feint — a landed feint buys dice for the next real attack.
/// <para>
/// Unlike the other buffs this one is bought with a dice roll, not vital heat: the feint has to
/// convince someone, so there is something to roll against. The sixes become the bonus, and the
/// bonus is consumed by the next attack rather than lasting the turn.
/// </para>
/// </summary>
public sealed class FeintEffect : TurnScopedBuffEffect
{
    private int _dice;

    public FeintEffect(int dice) => _dice = Math.Max(0, dice);

    public override string EffectId      => "feint";
    public override string DisplayLabel  => $"F{_dice}";
    public override Vector4 DisplayColor => Config.Colors.Yellow;
    public override string DisplayName   => $"Feint (+{_dice})";
    public override string Description   =>
        $"They have committed to the wrong line. The next attack this turn rolls {_dice} extra dice.";

    public override int BonusAttackDice => _dice;

    public override void OnApply(Fighter target, Fighter source, FightState state, Random rng) =>
        state.AddLog($"{target.DisplayName} feints — +{_dice} dice on the next attack.", LogEntryType.SpecialEffect);

    /// <summary>Spent by the attack that used it, not by the clock.</summary>
    public override void OnAttackResolved(Fighter owner, Fighter target, bool isHit,
                                          FightState state, Random rng)
    {
        _dice = 0;
        IsExpired = true;
    }
}
