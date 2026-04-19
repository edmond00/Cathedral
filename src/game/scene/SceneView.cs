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
    public List<VerbView> ApplicableVerbs { get; }

    public SceneViewEntry(Element source, List<VerbView> applicableVerbs)
    {
        Source           = source;
        ApplicableVerbs  = applicableVerbs;
    }
}

/// <summary>
/// A verb presented to the frontend: its natural-language description and a reference
/// back to the <see cref="Verb"/> that generated it.
/// </summary>
public class VerbView
{
    /// <summary>The verb instance that generated this view.</summary>
    public Verb Verb { get; }

    /// <summary>Natural-language description of the action (e.g. "grab the apple").</summary>
    public string Verbatim { get; }

    /// <summary>The element this verb targets (may differ from the SceneViewEntry source).</summary>
    public Element? Target { get; }

    public VerbView(Verb verb, string verbatim, Element? target = null)
    {
        Verb     = verb;
        Verbatim = verbatim;
        Target   = target;
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
