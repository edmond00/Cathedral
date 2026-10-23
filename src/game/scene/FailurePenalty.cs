using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene;

/// <summary>The four kinds of thing a failed act can cost the body that attempted it.</summary>
public enum FailurePenaltyKind
{
    /// <summary>An injury: <see cref="WoundInflictionOutcome"/>.</summary>
    Wound,
    /// <summary>The way is lost and the place with it: <see cref="LostWayOutcome"/>.</summary>
    GetLost,
    /// <summary>The attempt eats the hour: <see cref="TimeWastedOutcome"/>.</summary>
    WasteTime,
    /// <summary>The mind loses its thread: <see cref="FocusLostOutcome"/>.</summary>
    LoseFocus,
}

/// <summary>
/// One entry of a verb's failure table (<see cref="Verbs.Verb.FailurePenalties"/>): what a failed
/// attempt costs, drawn uniformly from the table, so an entry repeated is an entry weighted.
///
/// <para><b>There is no "nothing" entry.</b> The table used to be a list of wounds padded with
/// <c>null</c>s, and most failures in the game drew one: a miss cost the attempt and not a thing
/// more. Every entry now names a cost, and the three that are not injuries are what fills the room
/// the nulls took — the way lost, the hour wasted, the thread of thought dropped.</para>
///
/// <para>A wound converts implicitly, so a table reads as a list of costs:
/// <c>new FailurePenalty[] { FailurePenalty.WasteTime, new CutWound() }</c>.</para>
/// </summary>
public sealed class FailurePenalty
{
    public FailurePenaltyKind Kind { get; }

    /// <summary>The injury, when <see cref="Kind"/> is <see cref="FailurePenaltyKind.Wound"/>.</summary>
    public Wound? Wound { get; }

    private FailurePenalty(FailurePenaltyKind kind, Wound? wound)
    {
        Kind  = kind;
        Wound = wound;
    }

    public static readonly FailurePenalty GetLost   = new(FailurePenaltyKind.GetLost,   null);
    public static readonly FailurePenalty WasteTime = new(FailurePenaltyKind.WasteTime, null);
    public static readonly FailurePenalty LoseFocus = new(FailurePenaltyKind.LoseFocus, null);

    public static FailurePenalty Of(Wound wound) => new(FailurePenaltyKind.Wound, wound);

    public static implicit operator FailurePenalty(Wound wound) => Of(wound);

    /// <summary>The outcome this entry becomes. Read by <c>--outcome-audit</c>, which cannot sample.</summary>
    public Type OutcomeType => Kind switch
    {
        FailurePenaltyKind.Wound     => typeof(WoundInflictionOutcome),
        FailurePenaltyKind.GetLost   => typeof(LostWayOutcome),
        FailurePenaltyKind.WasteTime => typeof(TimeWastedOutcome),
        _                            => typeof(FocusLostOutcome),
    };

    /// <summary>
    /// The consequence this entry has in <paramref name="s"/>, or null when it cannot take hold there —
    /// in which case the draw is made again from the rest of the table
    /// (<see cref="Verbs.Verb.DrawFailurePenalty"/>):
    ///
    /// <list type="bullet">
    /// <item>a wound the body does not own. A <see cref="WildcardWound"/> is the exception: a graze
    ///   belongs to no organ, so it is the acting anatomy's own graze of the same kind that lands —
    ///   a wolf scraped on the scree has a wolf's scrape. A named wound is never translated: the
    ///   table named one injury, and inventing a counterpart for it is content, not plumbing;</item>
    /// <item>losing the way when somebody saw or heard the failure. A confrontation outranks it: the
    ///   man who caught you does not let you wander off, and the one who heard you comes to a room
    ///   you are still standing in;</item>
    /// <item>losing the way where there is no way to lose — the opening phases, which have no
    ///   exit;</item>
    /// <item>wasting the hour when the failure begins a fight or a confrontation. The quarrel is had
    ///   at once, not an hour later, and the hour passing would be played out over the top of it;</item>
    /// <item>losing focus with no noetic point left to lose.</item>
    /// </list>
    /// </summary>
    public Outcome? Realise(FailureSituation s) => Kind switch
    {
        FailurePenaltyKind.Wound     => RealiseWound(s.Actor),
        FailurePenaltyKind.GetLost   => s.CanLoseTheWay && !s.Confronted ? new LostWayOutcome() : null,
        FailurePenaltyKind.WasteTime => s.Pov != null && !s.Caught ? new TimeWastedOutcome(s.Pov.When.Next()) : null,
        _                            => s.NoeticRemaining > 0 ? new FocusLostOutcome() : null,
    };

    private Outcome? RealiseWound(PartyMember actor)
    {
        var template = Wound!;
        if (!WoundRegistry.CanBeSufferedBy(template, actor))
        {
            template = template is WildcardWound graze
                ? WoundRegistry.ForAnatomy(actor).OfType<WildcardWound>()
                               .FirstOrDefault(w => w.DamageType == graze.DamageType)
                : null;
            if (template == null) return null;
        }
        return new WoundInflictionOutcome(WoundInstance.Inflicted(template));
    }

    /// <summary>Parses a <c>--penalty-only</c> value: <c>wound</c>, <c>get_lost</c>, <c>waste_time</c>, <c>lose_focus</c>.</summary>
    public static FailurePenaltyKind? ParseKind(string? id) => id?.Trim().ToLowerInvariant() switch
    {
        "wound"      => FailurePenaltyKind.Wound,
        "get_lost"   => FailurePenaltyKind.GetLost,
        "waste_time" => FailurePenaltyKind.WasteTime,
        "lose_focus" => FailurePenaltyKind.LoseFocus,
        _            => null,
    };

    public override string ToString() => Kind == FailurePenaltyKind.Wound ? $"wound:{Wound!.WoundName}" : Kind.ToString();
}

/// <summary>
/// What a failure penalty needs to know to decide whether it can take hold. Built by the narrative
/// controller before the roll, because the executor that resolves the failure holds neither the
/// scene's phase nor the noetic pool.
/// </summary>
/// <param name="Actor">Whoever acted — a companion after a hand-off, so the wound is theirs.</param>
/// <param name="Pov">Where and when; the hour wasted is the one after <c>Pov.When</c>.</param>
/// <param name="NoeticRemaining">The pool as the roll found it, after the thinking that led to the act.</param>
/// <param name="CanLoseTheWay">False in the opening phases, which have no exit to wander out of.</param>
/// <param name="Confronted">Somebody saw or heard the failure. Set by the executor, not the controller.</param>
/// <param name="Caught">The failure begins a fight or a confrontation: somebody saw it, not merely heard
/// it. Set by the executor.</param>
public sealed record FailureSituation(
    PartyMember Actor,
    PoV?        Pov,
    int         NoeticRemaining,
    bool        CanLoseTheWay,
    bool        Confronted = false,
    bool        Caught     = false);

// ── The three costs that are not injuries ────────────────────────────────────────────────────

/// <summary>
/// The way is lost: on the CONTINUE that closes this action the visit ends, and the party is back on
/// the open map, wherever the location stands. Nothing found in the place is lost with it — only the
/// rest of the visit, and the journey back if it is wanted.
///
/// <para>Applied as a request (<see cref="Scene.PendingLostWay"/>) because leaving is the narrative
/// controller's business, and it must wait for the press that closes the segment: the chips are the
/// account of the failure, and the world map replaces the narration the frame it opens.</para>
/// </summary>
public sealed class LostWayOutcome : Outcome
{
    public LostWayOutcome()
        : base("Lost the way", OutcomeSeverity.Negative, "lost my way and wandered out of the place altogether") { }

    protected override void Apply(OutcomeContext ctx)
    {
        if (ctx.Scene != null) ctx.Scene.PendingLostWay = true;
    }
}

/// <summary>
/// The attempt eats the hour: the period moves on by one, as <see cref="TimeShiftOutcome"/> moves it,
/// and everything that follows the hour follows it — who is where, which doors are shut.
///
/// <para>A type of its own rather than a <see cref="TimeShiftOutcome"/>, because an emotion answers the
/// type: patience is settled by an hour chosen to be spent, and has no business being settled by one
/// fumbled away.</para>
/// </summary>
public sealed class TimeWastedOutcome : Outcome
{
    private readonly TimePeriod _destination;

    public TimeWastedOutcome(TimePeriod destination)
        : base($"Time wasted: {destination.Label()}", OutcomeSeverity.Negative,
               $"lost the time over it until {destination.Label().ToLowerInvariant()}")
    {
        _destination = destination;
    }

    protected override void Apply(OutcomeContext ctx)
    {
        if (ctx.PoV == null) return;
        ctx.PoV.When  = _destination;
        ctx.PoV.Focus = null;
    }
}

/// <summary>
/// The mind loses its thread: one more noetic point is gone, beyond the one the attempt already cost.
/// A failed act does not refill the pool, so the loss is carried into the next segment.
///
/// <para>Applied as a request (<see cref="Scene.PendingFocusLoss"/>) because the pool belongs to the
/// narration, not to the scene or the body.</para>
/// </summary>
public sealed class FocusLostOutcome : Outcome
{
    public FocusLostOutcome()
        : base("Focus lost: -1 noetic point", OutcomeSeverity.Negative, "lost the thread of my thoughts") { }

    protected override void Apply(OutcomeContext ctx)
    {
        if (ctx.Scene != null) ctx.Scene.PendingFocusLoss++;
    }
}
