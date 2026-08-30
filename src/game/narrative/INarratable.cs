using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Something the outcome narrator can be told about.
///
/// <para>The whole of what the LLM path needs from a thing: a name for logs and menus, and a phrase
/// it can drop into a prompt. Deliberately an interface and deliberately this small — it is
/// implemented by three unrelated families (a click destination, a consequence, an inline snippet)
/// and inheriting from a common <i>base</i> is what used to imply kinship they do not have. A fight
/// mode's entry payload derived from a class called <c>INarratable</c> purely to be passable to
/// <see cref="OutcomeNarrator"/>, and so read as a consequence of an action for years.</para>
/// </summary>
public interface INarratable
{
    /// <summary>Name for UI and logging.</summary>
    string DisplayName { get; }

    /// <summary>A phrase the narrator can use in an LLM prompt.</summary>
    string ToNaturalLanguageString();
}

/// <summary>
/// A place a click can land: the narration menu hangs an interaction on it.
///
/// <para>Two kinds, and the distinction is the whole of the narration loop. An
/// <see cref="ObservationObject"/> is a thing you can look at — clicking its keyword focuses the
/// next observation on it. An <see cref="Cathedral.Game.Scene.Action"/> is a verb in context —
/// choosing it runs the verb. Both live in the same lists (<c>SubOutcomes</c>,
/// <c>PossibleOutcomes</c>) because the thinking phase draws from them together: "look closer at
/// that" and "grab the flower" are both answers to "what do you want to do?".</para>
///
/// <para>Do not confuse with <see cref="Outcome"/>. This is where a click <b>goes</b>; an
/// <c>Outcome</c> is what an action <b>did</b>. Both used to be called outcomes, which is why the
/// two were routinely mistaken for one another.</para>
/// </summary>
public abstract class NarrativeAnchor : INarratable
{
    public abstract string DisplayName { get; }
    public abstract string ToNaturalLanguageString();
}

/// <summary>
/// Uniform interface over the things that can hold anchors, so the graph can be traversed without
/// knowing whether it is looking at a node or an observation object.
/// </summary>
public interface IObservation
{
    /// <summary>Stable identifier for this observation (NodeId / ObservationId).</summary>
    string ObservationId { get; }

    /// <summary>The anchors reachable through this observation.</summary>
    IReadOnlyList<NarrativeAnchor> ObservationOutcomes { get; }
}

/// <summary>
/// A narratable whose two strings are simply given at construction. Used by the childhood
/// reminescence and get-up phases, where the concrete text is written by the phase itself and there
/// is no scene object behind it to name.
/// </summary>
public sealed class InlineNarratable : INarratable
{
    private readonly string _displayName;
    private readonly string _naturalLanguage;

    public InlineNarratable(string displayName, string naturalLanguage)
    {
        _displayName     = displayName;
        _naturalLanguage = naturalLanguage;
    }

    public string DisplayName               => _displayName;
    public string ToNaturalLanguageString() => _naturalLanguage;
}
