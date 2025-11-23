using System;
using System.Collections.Generic;

namespace Cathedral.Game;

/// <summary>
/// Represents the outcome of an action taken at a location.
/// Contains all information needed to update the game state after an action.
/// </summary>
public record ActionResult
{
    /// <summary>
    /// Whether the action succeeded or failed.
    /// Failure ends the interaction loop and returns to world view.
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// Narrative description of what happened as a result of the action.
    /// This will be displayed to the player.
    /// </summary>
    public string NarrativeOutcome { get; init; }
    
    /// <summary>
    /// State changes to apply to the location.
    /// Key: State category ID (e.g., "time_of_day", "weather")
    /// Value: New state ID (e.g., "afternoon", "rainy")
    /// </summary>
    public Dictionary<string, string> StateChanges { get; init; }
    
    /// <summary>
    /// New sublocation to transition to (null if staying in same sublocation).
    /// </summary>
    public string? NewSublocation { get; init; }
    
    /// <summary>
    /// New actions that become available after this action.
    /// If null, actions should be regenerated based on new state.
    /// </summary>
    public List<string>? NewActions { get; init; }
    
    /// <summary>
    /// Items gained from this action (for future inventory system).
    /// </summary>
    public List<string>? ItemsGained { get; init; }
    
    /// <summary>
    /// Whether this action ends the interaction (success = return to world view).
    /// Different from failure - this is a successful exit.
    /// </summary>
    public bool EndsInteraction { get; init; }

    /// <summary>
    /// Creates a successful action result.
    /// </summary>
    public static ActionResult CreateSuccess(
        string narrative,
        Dictionary<string, string>? stateChanges = null,
        string? newSublocation = null,
        List<string>? newActions = null,
        List<string>? itemsGained = null,
        bool endsInteraction = false)
    {
        return new ActionResult
        {
            Success = true,
            NarrativeOutcome = narrative,
            StateChanges = stateChanges ?? new Dictionary<string, string>(),
            NewSublocation = newSublocation,
            NewActions = newActions,
            ItemsGained = itemsGained,
            EndsInteraction = endsInteraction
        };
    }

    /// <summary>
    /// Creates a failure action result.
    /// </summary>
    public static ActionResult CreateFailure(string narrative)
    {
        return new ActionResult
        {
            Success = false,
            NarrativeOutcome = narrative,
            StateChanges = new Dictionary<string, string>(),
            NewSublocation = null,
            NewActions = null,
            ItemsGained = null,
            EndsInteraction = true // Failure always ends interaction
        };
    }

    /// <summary>
    /// Creates an action result that successfully exits the location.
    /// </summary>
    public static ActionResult CreateExit(string narrative = "You leave the location and return to the world.")
    {
        return new ActionResult
        {
            Success = true,
            NarrativeOutcome = narrative,
            StateChanges = new Dictionary<string, string>(),
            NewSublocation = null,
            NewActions = null,
            ItemsGained = null,
            EndsInteraction = true
        };
    }

    public ActionResult()
    {
        Success = false;
        NarrativeOutcome = string.Empty;
        StateChanges = new Dictionary<string, string>();
        NewSublocation = null;
        NewActions = null;
        ItemsGained = null;
        EndsInteraction = false;
    }
}
