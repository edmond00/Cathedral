using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene;

/// <summary>
/// Point of View — represents where and when the agent is interacting with the scene,
/// and what element they are currently focusing on.
/// </summary>
public class PoV
{
    /// <summary>The area the agent is currently in.</summary>
    public Area Where { get; set; }

    /// <summary>The current time period.</summary>
    public TimePeriod When { get; set; }

    /// <summary>
    /// The element the agent is currently focusing on (keyword click target).
    /// Null when no specific element is in focus.
    /// </summary>
    public Element? Focus { get; set; }

    public PoV(Area where, TimePeriod when, Element? focus = null)
    {
        Where = where;
        When  = when;
        Focus = focus;
    }
}
