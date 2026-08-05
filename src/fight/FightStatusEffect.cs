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

    /// <summary>
    /// Called at the END of the affected fighter's turn.
    ///
    /// <para>
    /// Turn-scoped buffs expire here, and they have to: expiry used to run only in
    /// <see cref="Fighter.StartTurn"/>, so an effect applied on the owner's turn survived every
    /// other fighter's turn and died at the owner's <em>next</em> one — a full round, not a turn.
    /// An effect that genuinely wants the round (Cold Blood, which fires while enemies attack)
    /// simply does not expire here.
    /// </para>
    /// </summary>
    public virtual void OnTurnEnd(Fighter owner, FightState state, Random rng) { }

    // ── Passive queries ───────────────────────────────────────────────────────
    // Inert by default. Each is read at exactly one resolution site (named in its summary) so a
    // new effect never has to be threaded through the combat path by hand.

    /// <summary>Extra dice added to the owner's DEFENCE pool. Read by <c>SkillAction.Execute</c>.</summary>
    public virtual int BonusDefenseDice => 0;

    /// <summary>Extra dice added to the owner's ATTACK pool. Read by <c>SkillAction.Execute</c>.</summary>
    public virtual int BonusAttackDice => 0;

    /// <summary>
    /// When true, a wound this fighter suffers is the most severe one available rather than a
    /// uniform draw. Read by <c>FightResolver.PickWound</c> — note it is queried on the ATTACKER,
    /// since it is the attacker's ferocity that picks the wound.
    /// </summary>
    public virtual bool ForcesHighestSeverityWound => false;

    /// <summary>Lifts the once-per-turn rule. Read by <see cref="FightState.IsActionUsed"/>.</summary>
    public virtual bool BypassesUsedActions => false;

    /// <summary>Allows re-rolling a failed runaway check. Read by the RUN button guard.</summary>
    public virtual bool AllowsRunawayRetry => false;

    /// <summary>Multiplies the owner's tiles-per-CP. Read by <see cref="Fighter.EffectiveMoveSpeed"/>.</summary>
    public virtual int MoveSpeedMultiplier => 1;

    /// <summary>Lets the owner path through HardObstacle cells. Read by the movement passability test.</summary>
    public virtual bool AllowsHardObstacleCrossing => false;

    // ── Resolution events ─────────────────────────────────────────────────────

    /// <summary>
    /// Called on each of the ATTACKER's effects once an attack of theirs has resolved.
    /// </summary>
    public virtual void OnAttackResolved(Fighter owner, Fighter target, bool isHit,
                                         FightState state, Random rng) { }

    /// <summary>
    /// Called on each of the DEFENDER's effects once an attack against them has resolved.
    /// <paramref name="defenseSucceeded"/> is true when the attack missed.
    /// Returning true from <see cref="EndsAttackerTurnOnSuccessfulDefense"/> is what actually
    /// interrupts the attacker — this hook is for logging and bookkeeping.
    /// </summary>
    public virtual void OnDefended(Fighter owner, Fighter attacker, bool defenseSucceeded,
                                   FightState state, Random rng) { }

    /// <summary>
    /// When true and the owner successfully defends, the attacker's turn ends immediately.
    /// Surfaced as a flag on <c>FightResolver.AttackResult</c> rather than acted on in the
    /// resolver, which is static and must not drive turn order.
    /// </summary>
    public virtual bool EndsAttackerTurnOnSuccessfulDefense => false;

    /// <summary>Short display label shown in the UI status indicator (e.g. "B2" for Bleeding level 2).</summary>
    public abstract string DisplayLabel { get; }

    /// <summary>Color for the display label.</summary>
    public abstract Vector4 DisplayColor { get; }

    /// <summary>Human-readable name shown in the STATE list on the fighter info pan.</summary>
    public abstract string DisplayName { get; }

    /// <summary>One-paragraph explanation rendered when this effect's STATE row is hovered.</summary>
    public abstract string Description { get; }
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
    public override Vector4 DisplayColor => Config.Colors.BrightPurple;
    public override string DisplayName   => $"Bleeding ({Level})";
    public override string Description   => "Drains Level humors from the body's vital heat queue at the start of every turn. Stops when the fight ends or all humor queues are critical.";

    /// <summary>Cap on stacked bleed level — multiple cuts can't bleed faster than this.</summary>
    private const int MaxLevel = 9;

    /// <summary>Increase the level (e.g. another bleeding-causing hit lands). Clamped to <see cref="MaxLevel"/>.</summary>
    public void AddLevel(int extra)
    {
        Level = Math.Min(MaxLevel, Level + Math.Max(0, extra));
    }

    /// <summary>Humors actually drained on the last turn start (0 if queues were empty).</summary>
    public int LastDrainedHumors { get; private set; }
    /// <summary>Vital heat actually drained on the last turn start.</summary>
    public int LastDrainedVitalHeat { get; private set; }
    /// <summary>True when the most recent drain pushed the fighter's humor queues to fully critical.</summary>
    public bool LastDrainPushedToCritical { get; private set; }

    public override void OnApply(Fighter target, Fighter source, FightState state, Random rng)
    {
        state.AddLog($"{target.DisplayName} is bleeding! (level {Level})", LogEntryType.SpecialEffect);
    }

    public override void OnTurnStart(Fighter owner, FightState state, Random rng)
    {
        var humors = owner.Member.HumorQueues;
        bool wasCritical = humors.IsFullyCritical;

        int drained = 0;
        int humorCount = 0;
        for (int i = 0; i < Level; i++)
        {
            if (humors.IsFullyCritical) break;
            int heat = humors.ConsumeVitalHeatCycled(owner.Member, rng);
            drained += heat;
            humorCount++;
        }

        LastDrainedHumors    = humorCount;
        LastDrainedVitalHeat = drained;
        LastDrainPushedToCritical = !wasCritical && humors.IsFullyCritical;

        if (humorCount > 0)
            state.AddLog($"{owner.DisplayName} bleeds — {humorCount} humor(s) drained (vital heat: -{drained}).", LogEntryType.SpecialEffect);
        else
            state.AddLog($"{owner.DisplayName}'s bleeding slows — humor queues depleted.", LogEntryType.SpecialEffect);

        // Death from black-bile depletion: mark the fighter humor-depleted so IsAlive flips
        // to false. CheckFightEnd in the resolver will pick it up.
        if (humors.IsFullyCritical && !owner.IsHumorDepleted)
        {
            owner.IsHumorDepleted = true;
            state.AddLog($"{owner.DisplayName} collapses — humors fully depleted.", LogEntryType.Wound);
            IsExpired = true;
        }
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
    public override string DisplayName   => "Knockdown";
    public override string Description   => "Down on the ground. At the start of every turn, roll <heart> d6 — need at least one six to recover and act this turn. Otherwise the turn is skipped and the knockdown persists.";

    public override void OnApply(Fighter target, Fighter source, FightState state, Random rng)
    {
        target.IsKnockedDown = true;
        state.AddLog($"{target.DisplayName} is knocked down!", LogEntryType.SpecialEffect);
    }

    public override void OnTurnStart(Fighter owner, FightState state, Random rng)
    {
        // No auto-expire. The fight controller handles the recovery roll at turn start
        // — see FightModeAdapter.MaybeStartKnockdownRecovery.
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
    public override string DisplayName   => "Immobilized";
    public override string Description   => "Cannot take movement actions next turn. Expires at the start of the affected turn.";

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
/// Fall-over: applied when a fighter slips on Treacherous or Dangerous terrain mid-move.
/// Acts like a one-turn knockdown — blocks attacks and signals the player "you're getting up".
/// Auto-clears at the start of the affected fighter's next turn.
/// </summary>
public sealed class FallOverEffect : FightStatusEffect
{
    public override string EffectId      => "fall_over";
    public override string DisplayLabel  => "F";
    public override Vector4 DisplayColor => Config.Colors.LightPurple;
    public override string DisplayName   => "Fallen over";
    public override string Description   => "Lost footing on unstable terrain mid-move. The current turn was cut short. Effect clears at the start of the next turn.";

    public override void OnApply(Fighter target, Fighter source, FightState state, Random rng)
    {
        target.IsKnockedDown = true; // reuse no-attack flag
        state.AddLog($"{target.DisplayName} loses footing!", LogEntryType.SpecialEffect);
    }

    public override void OnTurnStart(Fighter owner, FightState state, Random rng)
    {
        owner.IsKnockedDown = false;
        IsExpired = true;
        state.AddLog($"{owner.DisplayName} gets back up.", LogEntryType.SpecialEffect);
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
    public override string DisplayName   => "Pushback";
    public override string Description   => "Shoved back along the attack vector. If stopped by a hard obstacle, adds a backbone wound.";

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
