using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative;

/// <summary>
/// What the acting member has already been <b>offered a button for</b> during the current narration
/// phase — the thinking-side counterpart of <see cref="ObservationLedger"/>, with the same lifetime
/// and the same purpose.
///
/// <para>The problem it solves is the one the observation ledger solves one step earlier. A thinking
/// request lets the modus mentis choose freely among everything the observed object affords, and
/// those choices converge: the mind that wanted to grab the apple wants to grab it again, so a
/// player trying to reach some *other* action on the same object spent point after point being
/// offered the same one. Actions recorded here are dropped from every later goal list of the phase,
/// so each request has to reach for something the player has not already been given.</para>
///
/// <para><b>Verb + target, not instance.</b> Unlike an observation object, a <see cref="VerbAction"/>
/// does not survive between requests: <c>NarrativeController.RefreshSceneVerbs</c> re-expands every
/// scene verb list immediately before each thinking request, so the object recorded and the object
/// filtered are never the same one. The identity is therefore the pair the player perceives — this
/// verb, on this thing — plus the <see cref="VerbAction.Variant"/>, so a verb that expands one target
/// into several actions (which job to ask for, which third party to be presented to) still offers the
/// rest of them. The target and variant are held by reference, which is what keeps two identical
/// PoIs, or two people of the same trade, separate.</para>
///
/// <para><b>And by what it says.</b> Two variants that read identically are one action to the player:
/// overhearing a merchant "talking with the clerk", when two clerks stand in the hall, expanded into two
/// acts with one sentence between them, and the second was offered again after the first was shown.
/// So an action is also proposed when its verb, target and wording match one already offered.</para>
///
/// <para><b>Proposed, not merely thought of.</b> An action is recorded when its button reaches the
/// screen. A goal the thinking modus mentis settled on and the action modus mentis then refused was
/// never offered to the player, so it stays available — the refusal is the character's, and asking
/// again with a different mind is exactly the move that should work.</para>
///
/// <para><b>Except to the mind that already asked.</b> A refused goal is recorded against the thinking
/// modus mentis that chose it (<see cref="Refuse"/>), and only that one stops being offered it: the
/// same mind asked about the same object would converge on the same goal, be refused the same way,
/// and spend a noetic point on nothing. Keyed by the <see cref="ModusMentis"/> instance, so a
/// companion's copy of the same modus mentis is a different mind.</para>
///
/// <para><b>And a second attempt is not left to be refused again.</b> Once a goal has been refused by
/// anyone this phase (<see cref="WasRefused"/>), the next mind that chooses it is held to what the
/// coded rules allow: the goal is withheld when no action modus mentis held could carry it out, and
/// otherwise only those that can are offered as the means, with no refusal left to them. That logic
/// lives in <c>ThinkingExecutor</c>; this class only remembers what was refused.</para>
///
/// <para><b>Nor is a mind asked twice whether it would rather not.</b> When a thinking modus mentis
/// declines everything an object offers (<see cref="Decline"/>), asking it again about the same object
/// can only be declined again: the goal judge reads scores rather than sampling, so one question has
/// one answer, and the second ask spent a noetic point to hear it twice. The second ask is therefore
/// the player insisting, and the decline is not offered (<see cref="HasDeclined"/>): the mind chooses
/// one of the goals. Keyed by mind and object instance, like the refusals; another mind asked about
/// the same object may still decline.</para>
///
/// <para>Cleared wherever the live text greys into history (<c>NarrativeController.CloseNarrationSegment</c>),
/// alongside the observation ledger: a new phase in the same place sees the whole scene, and every
/// action in it, again.</para>
/// </summary>
public sealed class ActionProposalLedger
{
    /// <summary>
    /// Verb id + target + variant. Tuple equality uses the default comparer per element, which for
    /// these two reference types (neither overrides Equals) is reference identity — the intent.
    /// </summary>
    private readonly HashSet<(string VerbId, Element? Target, object? Variant)> _proposed = new();

    /// <summary>The same actions by what they say — see the class remarks.</summary>
    private readonly HashSet<(string VerbId, Element? Target, string Verbatim)> _proposedWording = new();

    /// <summary>Actions an action modus mentis refused, by the thinking modus mentis that chose them.</summary>
    private readonly Dictionary<ModusMentis, HashSet<(string VerbId, Element? Target, object? Variant)>> _refusedFor = new();

    /// <summary>Every action refused this phase, whichever mind chose it.</summary>
    private readonly HashSet<(string VerbId, Element? Target, object? Variant)> _refused = new();

    /// <summary>The objects each thinking modus mentis has declined this phase.</summary>
    private readonly HashSet<(ModusMentis Thinking, NarrativeAnchor Observed)> _declined = new();

    /// <summary>How many distinct actions have been proposed so far this phase.</summary>
    public int Count => _proposed.Count;

    /// <summary>Records an action as proposed. Called when its button reaches the screen.</summary>
    public void Propose(VerbAction action)
    {
        _proposed.Add(KeyOf(action));
        _proposedWording.Add(WordingOf(action));
    }

    /// <summary>True when this verb has already been offered against this exact target this phase.</summary>
    public bool WasProposed(VerbAction action)
        => _proposed.Contains(KeyOf(action)) || _proposedWording.Contains(WordingOf(action));

    /// <summary>
    /// The subset of <paramref name="goals"/> not yet proposed, in the given order. Anything that is
    /// not a <see cref="VerbAction"/> passes through untouched — the ledger only knows about actions.
    /// </summary>
    public List<NarrativeAnchor> Remaining(IEnumerable<NarrativeAnchor> goals)
        => goals.Where(g => g is not VerbAction va || !WasProposed(va)).ToList();

    /// <summary>
    /// Records that the goal <paramref name="thinking"/> chose was refused by the action modus mentis
    /// asked to carry it out. Called when the refusal is decided, before it is narrated.
    /// </summary>
    public void Refuse(ModusMentis thinking, VerbAction action)
    {
        if (!_refusedFor.TryGetValue(thinking, out var refused))
            _refusedFor[thinking] = refused = new();
        refused.Add(KeyOf(action));
        _refused.Add(KeyOf(action));
    }

    /// <summary>True when this action has already been chosen and refused this phase, by any mind.</summary>
    public bool WasRefused(VerbAction action) => _refused.Contains(KeyOf(action));

    /// <summary>
    /// The subset of <paramref name="goals"/> that <paramref name="thinking"/> has not already chosen
    /// and seen refused this phase. Every other thinking modus mentis is still offered them.
    /// </summary>
    /// <remarks><paramref name="reoffer"/> lets a goal back in despite that: iterative thinking re-offers,
    /// within one chain of retries, a goal refused by one means while another means has not been tried
    /// (<c>ThinkingIterationsStat</c>).</remarks>
    public List<NarrativeAnchor> NotRefusedFor(ModusMentis thinking, IEnumerable<NarrativeAnchor> goals,
                                               System.Func<VerbAction, bool>? reoffer = null)
    {
        if (!_refusedFor.TryGetValue(thinking, out var refused)) return goals.ToList();
        return goals.Where(g => g is not VerbAction va || !refused.Contains(KeyOf(va)) || reoffer?.Invoke(va) == true)
                    .ToList();
    }

    /// <summary>
    /// Records that <paramref name="thinking"/>, asked about <paramref name="observed"/>, wanted none of
    /// what it offers. Called when the thinking ends on that refusal.
    /// </summary>
    public void Decline(ModusMentis thinking, NarrativeAnchor observed) => _declined.Add((thinking, observed));

    /// <summary>True when <paramref name="thinking"/> has already declined <paramref name="observed"/> this phase.</summary>
    public bool HasDeclined(ModusMentis thinking, NarrativeAnchor observed) => _declined.Contains((thinking, observed));

    /// <summary>
    /// The identity this ledger knows an action by (verb, target, variant), for callers that keep
    /// their own record of actions across a re-expansion of the verb lists — iterative thinking's
    /// crossed-off pairs.
    /// </summary>
    public static (string VerbId, Element? Target, object? Variant) IdentityOf(VerbAction action) => KeyOf(action);

    /// <summary>Forgets everything — a new narration phase begins.</summary>
    public void Clear()
    {
        _proposed.Clear();
        _proposedWording.Clear();
        _refusedFor.Clear();
        _refused.Clear();
        _declined.Clear();
    }

    private static (string, Element?, object?) KeyOf(VerbAction action)
        => (action.Verb.VerbId, action.Target, action.Variant);

    private static (string, Element?, string) WordingOf(VerbAction action)
        => (action.Verb.VerbId, action.Target, action.Verbatim);
}
