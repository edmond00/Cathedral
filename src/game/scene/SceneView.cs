using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Scene;

/// <summary>
/// A single entry in a <see cref="SceneView"/>: one visible element with its
/// applicable verb descriptions.
/// </summary>
public class SceneViewEntry
{
    /// <summary>The element being observed.</summary>
    public Element Source { get; }

    /// <summary>Natural-language descriptions of possible actions (from applicable verbs).</summary>
    public List<VerbAction> ApplicableVerbs { get; }

    public SceneViewEntry(Element source, List<VerbAction> applicableVerbs)
    {
        Source           = source;
        ApplicableVerbs  = applicableVerbs;
    }
}

/// <summary>
/// A verb presented to the frontend: its natural-language description and a reference
/// back to the <see cref="Verb"/> that generated it.
/// </summary>
public class VerbAction : Cathedral.Game.Narrative.NarrativeAnchor
{
    /// <summary>The verb instance that generated this action.</summary>
    public Verb Verb { get; }

    /// <summary>Natural-language description of the action (e.g. "grab the apple").</summary>
    public string Verbatim { get; }

    /// <summary>The element this verb targets.</summary>
    public Element? Target { get; }

    /// <summary>
    /// Optional per-action payload for verbs that expand a single target into several actions
    /// (e.g. <c>RequestJobVerb</c> emits one per offered job, each carrying its <c>Job</c> here).
    /// Null for ordinary single-action verbs.
    /// </summary>
    public object? Variant { get; }

    /// <summary>
    /// Contextual NPC label substituted for the target's proper name in the LLM-facing goal phrase.
    /// Null unless the parent observation object stamps it (named NPCs only); the human-facing
    /// <see cref="DisplayName"/> always keeps the verbatim verb text.
    /// </summary>
    public string? ContextLabel { get; set; }

    public VerbAction(Verb verb, string verbatim, Element? target = null, object? variant = null)
    {
        Verb     = verb;
        Verbatim = verbatim;
        Target   = target;
        Variant  = variant;
    }

    public override string DisplayName => Verbatim;

    public override string ToNaturalLanguageString()
    {
        if (ContextLabel == null || Target is not SceneNpc n) return Verbatim;

        string name = n.DisplayName;

        // Some verbs prefix the bare name with a determiner and lower-case it (e.g. SlayVerb's
        // "slay the edmund sheaf"). Drop that determiner when swapping in the contextual label —
        // it already carries its own article ("the reaper of the field (a man)"), so we must not
        // produce "slay the the reaper …". Case-insensitive so a lower-cased name still matches.
        string withArticle = Verbatim.Replace($"the {name}", ContextLabel, System.StringComparison.OrdinalIgnoreCase);
        if (withArticle != Verbatim) return withArticle;

        return Verbatim.Replace(name, ContextLabel, System.StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// The "frontend view" produced by <see cref="Scene.View(PoV)"/>.
/// Contains only the elements and verbs relevant to the current PoV,
/// in a format the LLM pipeline and UI can consume.
/// </summary>
public class SceneView
{
    /// <summary>The area the PoV is in (for context).</summary>
    public Area CurrentArea { get; }

    /// <summary>The current time period.</summary>
    public TimePeriod CurrentPeriod { get; }

    /// <summary>One entry per visible element at the current PoV.</summary>
    public List<SceneViewEntry> Entries { get; }

    /// <summary>The focused element, if any.</summary>
    public Element? Focus { get; }

    public SceneView(Area currentArea, TimePeriod currentPeriod, List<SceneViewEntry> entries, Element? focus)
    {
        CurrentArea    = currentArea;
        CurrentPeriod  = currentPeriod;
        Entries        = entries;
        Focus          = focus;
    }
}
