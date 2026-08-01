using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene;

/// <summary>
/// Point of View — represents where and when the agent is interacting with the scene,
/// and what element they are currently focusing on.
///
/// <para><b><see cref="Where"/> and <see cref="When"/> are the same kind of thing</b>: coordinates
/// into the scene that a verb may move the agent along, and that decide together what is reachable
/// and who is present (<see cref="Scene.GetNpcsAt"/> takes both, and every NPC verb gates on it). A
/// verb changes either of them by returning a report that declares the matching
/// <see cref="Cathedral.Game.Narrative.Routines.RoutineChainEffect"/> —
/// <see cref="AreaMoveOutcome"/> for space, <see cref="TimeShiftOutcome"/> for time — never by
/// writing here directly. That declaration is what makes the step a <i>repositioning</i> step,
/// which the routine recorder keeps in every later routine's prefix rather than treating as a piece
/// of work in its own right.</para>
/// </summary>
public class PoV
{
    /// <summary>The area the agent is currently in.</summary>
    public Area Where { get; set; }

    /// <summary>
    /// The spot the agent is currently examining within the area.
    /// Null when the agent is observing the area at large (not inside any spot).
    /// </summary>
    public Spot? InSpot { get; set; }

    /// <summary>
    /// The current time period — the temporal half of the point of view.
    ///
    /// <para>Mirrored by <c>NarrationGraph.CurrentPeriod</c>, which is what NPC placement into graph
    /// nodes reads. <c>NarrativeController.ApplyTimePeriod</c> is the single writer of the pair and
    /// keeps them equal; letting them diverge is what once put NPCs in the wrong nodes.</para>
    /// </summary>
    public TimePeriod When { get; set; }

    /// <summary>
    /// The element the agent is currently focusing on (keyword click target).
    /// Null when no specific element is in focus.
    /// </summary>
    public Element? Focus { get; set; }

    public PoV(Area where, TimePeriod when, Element? focus = null, Spot? inSpot = null)
    {
        Where  = where;
        When   = when;
        Focus  = focus;
        InSpot = inSpot;
    }
}
