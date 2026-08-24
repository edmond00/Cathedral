using System.Collections.Generic;
using Cathedral.Game.Narrative.Routines;

namespace Cathedral.Game.Narrative;

/// <summary>
/// What an action's consequences did to the person who caused them: N instances of one humor,
/// pushed into the spleen.
///
/// <para><b>An emotion is not a consequence, and this is still an <see cref="Outcome"/>.</b> The word
/// in this codebase does not mean "something the verb did" — <c>StateCaptureOutcome</c> and
/// <c>GetUpTransitionOutcome</c> are pure bookkeeping and are Outcomes too. What the base class
/// actually means is "a thing that applies its own change and can show a chip", which is exactly
/// what this is. Being one buys three things that would otherwise each need building: the chip
/// renderer already draws <c>block.OutcomeReports</c>, <c>ApplyTo</c> is already the single door
/// every state change goes through, and <c>expect-outcome emotion</c> works in the CLI on the day it
/// is written.</para>
///
/// <para>The chip's colour comes from the humor's own <see cref="BodyHumor.VitalHeat"/> sign rather
/// than from a hand-written severity, so a humor added later is coloured correctly without anyone
/// remembering to say so. Zero heat reads Neutral — no mind state has zero today, but Phlegm does,
/// and the rule should not depend on that staying true.</para>
///
/// <para><see cref="RoutineChainEffect"/> is deliberately <c>None</c>. An emotion moves neither the
/// point of view nor the clock, so a routine recorded around one is still valid — and a routine
/// REPLAY does not raise emotions at all (there is no narration there to carry the text), which is
/// why nothing here needs to survive one.</para>
/// </summary>
public sealed class EmotionOutcome : Outcome
{
    /// <summary>The organ whose queue an emotion lands in. Emotions are a mind state, so: the spleen.</summary>
    public const string TargetOrganId = "spleen";

    /// <summary>The modus mentis that felt it — the persona the narration was written in.</summary>
    public ModusMentis Source { get; }

    /// <summary>A representative instance, for the chip and for <see cref="BodyHumor.FeelsLike"/>.</summary>
    public BodyHumor Humor { get; }

    /// <summary>How many instances reach the queue. 1d6, rolled by the caller.</summary>
    public int Count { get; }

    private readonly System.Func<BodyHumor> _factory;

    public EmotionOutcome(ModusMentis source, System.Func<BodyHumor> humorFactory, int count)
        : base(Chip(humorFactory(), count), SeverityFor(humorFactory()), verbatim: string.Empty)
    {
        Source   = source;
        _factory = humorFactory;
        Humor    = humorFactory();
        Count    = count;
    }

    private static string Chip(BodyHumor humor, int count) => $"{humor.Name} × {count}";

    private static OutcomeSeverity SeverityFor(BodyHumor humor)
        => humor.VitalHeat > 0 ? OutcomeSeverity.Positive
         : humor.VitalHeat < 0 ? OutcomeSeverity.Negative
         : OutcomeSeverity.Neutral;

    /// <summary>
    /// Pushes <see cref="Count"/> fresh instances into the acting member's spleen — the same door
    /// eating uses (<c>ConsumableItem.Consume</c> loops <c>ProduceHumor</c> over the paunch).
    ///
    /// <para>A new instance per slot, from the trigger's factory, because a queue holds one object
    /// per slot; pushing one shared instance six times would put the same object in six places and
    /// make every later per-slot read ambiguous.</para>
    ///
    /// <para>A full spleen is not an error. <c>ProduceHumor</c> returns false when the queue is
    /// critical (entirely black bile) and the emotion is simply not felt — a mind already that far
    /// gone has no room left to react.</para>
    /// </summary>
    protected override void Apply(OutcomeContext ctx)
    {
        var member = ctx.Actor;
        if (member == null) return;
        for (int i = 0; i < Count; i++)
            member.HumorQueues.ProduceHumor(TargetOrganId, _factory());
    }

    public override RoutineChainEffect RoutineChainEffect => RoutineChainEffect.None;
}
