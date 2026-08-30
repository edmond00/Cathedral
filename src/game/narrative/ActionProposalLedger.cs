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
/// <para><b>Proposed, not merely thought of.</b> An action is recorded when its button reaches the
/// screen. A goal the thinking modus mentis settled on and the action modus mentis then refused was
/// never offered to the player, so it stays available — the refusal is the character's, and asking
/// again with a different mind is exactly the move that should work.</para>
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

    /// <summary>How many distinct actions have been proposed so far this phase.</summary>
    public int Count => _proposed.Count;

    /// <summary>Records an action as proposed. Called when its button reaches the screen.</summary>
    public void Propose(VerbAction action) => _proposed.Add(KeyOf(action));

    /// <summary>True when this verb has already been offered against this exact target this phase.</summary>
    public bool WasProposed(VerbAction action) => _proposed.Contains(KeyOf(action));

    /// <summary>
    /// The subset of <paramref name="goals"/> not yet proposed, in the given order. Anything that is
    /// not a <see cref="VerbAction"/> passes through untouched — the ledger only knows about actions.
    /// </summary>
    public List<NarrativeAnchor> Remaining(IEnumerable<NarrativeAnchor> goals)
        => goals.Where(g => g is not VerbAction va || !WasProposed(va)).ToList();

    /// <summary>Forgets everything — a new narration phase begins.</summary>
    public void Clear() => _proposed.Clear();

    private static (string, Element?, object?) KeyOf(VerbAction action)
        => (action.Verb.VerbId, action.Target, action.Variant);
}
