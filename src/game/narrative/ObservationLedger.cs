using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Narrative;

/// <summary>
/// What the acting member has already looked at during the current narration phase.
///
/// <para>Every observation request (the phase opener, and each focus observation the player asks for
/// afterwards) lets its Modus Mentis choose what to attend to. Without a record of what came before,
/// those choices converge: the persona that found the anvil interesting finds it interesting again,
/// and a scene with a dozen things in it is explored three deep. Objects recorded here are removed
/// from every subsequent <i>choice list</i> of the phase, so each request has to reach for something
/// new.</para>
///
/// <para><b>Instances, not names.</b> Two birch trees are two things to look at, and observing the
/// first must leave the second available — so the record is keyed by object identity. This is why the
/// pool is filtered through <see cref="Remaining"/> <i>before</i>
/// <c>ObservationPhaseController.DeduplicateByName</c> collapses name-twins: collapsing first would
/// pick one birch to stand for all of them and retire the whole group with it.</para>
///
/// <para>The record covers only what the LLM is offered. An object the <i>player</i> hands to a focus
/// observation by clicking its keyword is always observed, whether or not it is in here — the keyword
/// necessarily came from an earlier block, so it always is.</para>
///
/// <para>Lifetime is one narration phase: <c>NarrativeController</c> clears it wherever the live text
/// greys into history (see <c>CloseNarrationSegment</c>), which is also where noetic points refill. A
/// new phase in the same place therefore sees the whole scene again.</para>
/// </summary>
public sealed class ObservationLedger
{
    // Reference identity: two outcomes that describe identically are still two separate things.
    private readonly HashSet<NarrativeAnchor> _observed = new(ReferenceEqualityComparer.Instance);

    /// <summary>How many distinct objects have been observed so far this phase.</summary>
    public int Count => _observed.Count;

    /// <summary>
    /// True before anything has been observed this phase — i.e. this is the request that opens it.
    /// The refusal option is withheld exactly then, so a phase can never begin with a block that
    /// offers no keyword to click.
    /// </summary>
    public bool IsEmpty => _observed.Count == 0;

    /// <summary>Records an object as observed. Called once its narration text actually exists.</summary>
    public void Observe(NarrativeAnchor outcome) => _observed.Add(outcome);

    /// <summary>True when this exact object has already been observed this phase.</summary>
    public bool WasObserved(NarrativeAnchor outcome) => _observed.Contains(outcome);

    /// <summary>The subset of <paramref name="outcomes"/> not yet observed, in the given order.</summary>
    public List<NarrativeAnchor> Remaining(IEnumerable<NarrativeAnchor> outcomes)
        => outcomes.Where(o => !_observed.Contains(o)).ToList();

    /// <summary>Forgets everything — a new narration phase begins.</summary>
    public void Clear() => _observed.Clear();
}
