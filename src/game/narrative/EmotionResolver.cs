using System;
using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Narrative;

/// <summary>What an action's consequences stirred in the body that caused them.</summary>
/// <param name="ModusMentis">The disposition that answered — whose voice narrates it.</param>
/// <param name="Trigger">The clause that matched, kept so <see cref="EmotionOutcome"/> can mint humors.</param>
/// <param name="Because">The outcomes that fired it, in the order they were applied.</param>
/// <param name="Count">How many humor instances reach the spleen. 1d6.</param>
public readonly record struct FeltEmotion(
    ModusMentis ModusMentis,
    EmotionTrigger Trigger,
    IReadOnlyList<Outcome> Because,
    int Count)
{
    /// <summary>A representative instance — for the chip's name and for the neutral sentence.</summary>
    public BodyHumor Humor => Trigger.Humor();

    /// <summary>The consequence half of the emotion's neutral line, first person, ready after "I ".</summary>
    public IReadOnlyList<string> Verbatims =>
        Because.Select(o => o.Verbatim).Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
}

/// <summary>
/// Decides which of the acting body's dispositions answers what an action just produced.
///
/// <para><b>Every modus mentis the body holds is asked, not the one that acted.</b> Avarice rejoices
/// at coin whether or not avarice was the modus mentis that earned it — keyed to the acting one the
/// emotion would fire in the rare case and stay silent in the common one, which is backwards. This is
/// why the resolver takes a <see cref="PartyMember"/> rather than the action's chain.</para>
///
/// <para><b>Exactly one emotion per action.</b> A wolf slain in a private room after a forced door can
/// match five dispositions at once, and five narration blocks plus five chips for one press would bury
/// the action that caused them. One is sampled uniformly from the matches — uniformly rather than by
/// modus mentis level, because a level is how well a thing is <i>done</i> and no one feels their
/// strongest feeling most often.</para>
///
/// <para>Purely synchronous and free of LLM calls: it is type matching and two draws. The one request
/// this system makes is the narration, which happens afterwards and only for the sampled winner.</para>
/// </summary>
public static class EmotionResolver
{
    /// <summary>Humor instances produced per triggered emotion — 1d6, the draft's own figure.</summary>
    public const int HumorDieFaces = 6;

    /// <summary>The RNG stream. Named so a scripted run reproduces the same emotion from the same seed.</summary>
    private const string RngStream = "emotion";

    /// <summary>
    /// Returns the one emotion these outcomes stir in <paramref name="actor"/>, or null when nothing
    /// they hold has anything to say about what happened — which is the common case, and is silent.
    /// </summary>
    /// <param name="outcomes">The outcomes about to be (or just) applied. Order is preserved into
    /// <see cref="FeltEmotion.Because"/> so the neutral sentence reads in the order the player saw.</param>
    public static FeltEmotion? Resolve(PartyMember? actor, IReadOnlyList<Outcome>? outcomes)
    {
        if (actor == null || outcomes == null || outcomes.Count == 0) return null;

        // One candidate per (modus mentis, trigger) pair that matched, carrying every outcome that
        // matched it. A disposition with two triggers hit by one action is two candidates, which is
        // right: greed answering both the coin and the item is two different feelings about one act,
        // and the sample should be able to land on either.
        var candidates = new List<(ModusMentis Mm, EmotionTrigger Trigger, List<Outcome> Because)>();

        foreach (var mm in actor.ModiMentis)
        {
            var triggers = mm.EmotionTriggers;
            if (triggers.Length == 0) continue;

            foreach (var trigger in triggers)
            {
                // An EmotionOutcome is itself an Outcome, so without this a disposition triggering on
                // a base type would feel its own feeling. Nothing declares such a trigger today; the
                // guard is here because the failure would be a silent loop rather than an error.
                var because = outcomes.Where(o => o is not EmotionOutcome && trigger.Matches(o)).ToList();
                if (because.Count > 0) candidates.Add((mm, trigger, because));
            }
        }

        if (candidates.Count == 0) return null;

        var rng    = GameRng.Stream(RngStream);
        var picked = candidates[rng.Next(candidates.Count)];
        int count  = rng.Next(1, HumorDieFaces + 1);

        return new FeltEmotion(picked.Mm, picked.Trigger, picked.Because, count);
    }

    /// <summary>
    /// The <see cref="EmotionOutcome"/> for a felt emotion — the chip, and the thing whose
    /// <c>ApplyTo</c> pushes the humors into the spleen.
    /// </summary>
    public static EmotionOutcome ToOutcome(this FeltEmotion felt)
        => new(felt.ModusMentis, felt.Trigger.Humor, felt.Count);
}
