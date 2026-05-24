using System;
using System.Linq;
using OpenTK.Mathematics;
using Cathedral.Game.Narrative;

namespace Cathedral.Fight;

/// <summary>
/// Abstract base for all in-fight status effects (bleeding, pushback, knockdown, immobilize).
/// Effects are stored on <see cref="Fighter.ActiveEffects"/> and processed at turn start.
/// </summary>
public abstract class FightStatusEffect
{
    /// <summary>Unique string id for this effect type (e.g. "bleeding").</summary>
    public abstract string EffectId { get; }

    /// <summary>Severity level of this effect (1–3 for bleeding, 1 for others).</summary>
    public int Level { get; protected set; } = 1;

    /// <summary>Whether this effect has expired and should be removed.</summary>
    public bool IsExpired { get; protected set; }

    /// <summary>Called immediately when the effect is applied to a target from a skill hit.</summary>
    public virtual void OnApply(Fighter target, Fighter source, FightState state, Random rng) { }

    /// <summary>Called at the start of the affected fighter's turn.</summary>
    public virtual void OnTurnStart(Fighter owner, FightState state, Random rng) { }

    /// <summary>Short display label shown in the UI status indicator (e.g. "B2" for Bleeding level 2).</summary>
    public abstract string DisplayLabel { get; }

    /// <summary>Color for the display label.</summary>
    public abstract Vector4 DisplayColor { get; }
}

// ─────────────────────────────────────────────────────────────────────────────
// Concrete effects
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Bleeding: drains <see cref="FightStatusEffect.Level"/> humors per turn from the
/// fighter's humor queue, degrading organ scores over time.
/// Does NOT directly reduce HP — the humor system handles long-term degradation.
/// Stops when the fight ends or when all queues are fully critical.
/// </summary>
public sealed class BleedingEffect : FightStatusEffect
{
    public BleedingEffect() { }
    public BleedingEffect(int level) { Level = level; }

    public override string EffectId      => "bleeding";
    public override string DisplayLabel  => $"B{Level}";
    public override Vector4 DisplayColor => Config.Colors.BrightRed;

    public override void OnApply(Fighter target, Fighter source, FightState state, Random rng)
    {
        state.AddLog($"{target.DisplayName} is bleeding! (level {Level})", LogEntryType.SpecialEffect);
    }

    public override void OnTurnStart(Fighter owner, FightState state, Random rng)
    {
        var humors = owner.Member.HumorQueues;
        int drained = 0;
        for (int i = 0; i < Level; i++)
        {
            int heat = humors.ConsumeVitalHeatCycled(owner.Member, rng);
            drained += heat;
        }

        if (drained > 0)
            state.AddLog($"{owner.DisplayName} bleeds — {Level} humor(s) drained (vital heat: -{drained}).", LogEntryType.SpecialEffect);
        else
            state.AddLog($"{owner.DisplayName}'s bleeding slows — humor queues depleted.", LogEntryType.SpecialEffect);

        if (humors.IsFullyCritical)
            IsExpired = true;
    }
}

/// <summary>
/// Knockdown: the affected fighter cannot use attack skills on their next turn.
/// Expires at the start of the affected turn (single-turn duration).
/// </summary>
public sealed class KnockdownEffect : FightStatusEffect
{
    public override string EffectId      => "knockdown";
    public override string DisplayLabel  => "K";
    public override Vector4 DisplayColor => Config.Colors.Orange;

    public override void OnApply(Fighter target, Fighter source, FightState state, Random rng)
    {
        target.IsKnockedDown = true;
        state.AddLog($"{target.DisplayName} is knocked down!", LogEntryType.SpecialEffect);
    }

    public override void OnTurnStart(Fighter owner, FightState state, Random rng)
    {
        // The knockdown was applied last turn; now it clears
        owner.IsKnockedDown = false;
        IsExpired = true;
        state.AddLog($"{owner.DisplayName} recovers from knockdown.", LogEntryType.SpecialEffect);
    }
}

/// <summary>
/// Immobilize: the affected fighter cannot take movement actions on their next turn.
/// Expires at the start of the affected turn (single-turn duration).
/// </summary>
public sealed class ImmobilizeEffect : FightStatusEffect
{
    public override string EffectId      => "immobilize";
    public override string DisplayLabel  => "I";
    public override Vector4 DisplayColor => Config.Colors.Yellow;

    public override void OnApply(Fighter target, Fighter source, FightState state, Random rng)
    {
        target.IsImmobilized = true;
        state.AddLog($"{target.DisplayName} is immobilized!", LogEntryType.SpecialEffect);
    }

    public override void OnTurnStart(Fighter owner, FightState state, Random rng)
    {
        owner.IsImmobilized = false;
        IsExpired = true;
        state.AddLog($"{owner.DisplayName} breaks free.", LogEntryType.SpecialEffect);
    }
}

/// <summary>
/// Pushback: applied instantly in <see cref="OnApply"/>, moves the target away from
/// the attacker. If stopped by a hard obstacle, applies a bonus backbone wound.
/// The effect resolves immediately and always expires after apply.
/// </summary>
public sealed class PushbackEffect : FightStatusEffect
{
    private readonly int _steps;

    public PushbackEffect(int steps = 1) => _steps = steps;

    public override string EffectId      => "pushback";
    public override string DisplayLabel  => "P";
    public override Vector4 DisplayColor => Config.Colors.LightPurple;

    public override void OnApply(Fighter target, Fighter source, FightState state, Random rng)
    {
        // Direction: from source toward target (push away)
        int dx = target.X - source.X;
        int dy = target.Y - source.Y;
        int ndx = dx == 0 ? 0 : (dx > 0 ? 1 : -1);
        int ndy = dy == 0 ? 0 : (dy > 0 ? 1 : -1);

        if (ndx == 0 && ndy == 0) { IsExpired = true; return; }

        int distance = _steps > 0 ? _steps : rng.Next(1, 7);
        int actualMoved = 0;
        bool hitWall = false;

        for (int i = 0; i < distance; i++)
        {
            int nx = target.X + ndx;
            int ny = target.Y + ndy;
            if (!FightResolver.CanMoveTo(state.Area, nx, ny, state.Fighters, target))
            {
                hitWall = true;
                break;
            }
            target.X = nx;
            target.Y = ny;
            actualMoved++;
        }

        if (actualMoved > 0)
            state.AddLog($"{target.DisplayName} is pushed back {actualMoved} tile(s)!", LogEntryType.SpecialEffect);

        // Slam into wall: apply bonus backbone wound
        if (hitWall)
        {
            var backboneWounds = WoundRegistry.All.Values
                .Where(w => w.AffectsOrgan("backbone", "trunk"))
                .ToList();
            if (backboneWounds.Count > 0)
            {
                var bonus = backboneWounds[rng.Next(backboneWounds.Count)];
                FightResolver.ApplyWound(target, bonus);
                state.AddLog($"{target.DisplayName} slams the wall — {bonus.WoundName}!", LogEntryType.Wound);
            }
        }

        IsExpired = true;
    }
}
