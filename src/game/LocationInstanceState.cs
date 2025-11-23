using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cathedral.Glyph.Microworld.LocationSystem;

namespace Cathedral.Game;

/// <summary>
/// Represents the runtime state of a specific location instance.
/// Tracks the player's progress, current position, and action history at a location.
/// </summary>
public record LocationInstanceState
{
    /// <summary>
    /// Unique identifier for this location (typically derived from vertex index or position hash).
    /// </summary>
    public string LocationId { get; init; }
    
    /// <summary>
    /// Type of location (forest, tavern, dungeon, etc.).
    /// </summary>
    public string LocationType { get; init; }
    
    /// <summary>
    /// Current sublocation within this location (e.g., "forest_edge", "tavern_main_hall").
    /// </summary>
    public string CurrentSublocation { get; init; }
    
    /// <summary>
    /// Current state values for each state category.
    /// Key: State category ID (e.g., "time_of_day", "weather")
    /// Value: Current state ID (e.g., "morning", "clear")
    /// </summary>
    public Dictionary<string, string> CurrentStates { get; init; }
    
    /// <summary>
    /// History of all actions taken at this location.
    /// </summary>
    public List<PlayerAction> ActionHistory { get; init; }
    
    /// <summary>
    /// Timestamp of when this location was last visited.
    /// </summary>
    public DateTime LastVisited { get; init; }
    
    /// <summary>
    /// Number of turns/actions taken during the current visit.
    /// Resets to 0 when leaving the location.
    /// </summary>
    public int CurrentTurnCount { get; init; }
    
    /// <summary>
    /// Total number of turns/actions taken at this location across all visits.
    /// </summary>
    public int TotalTurnCount { get; init; }
    
    /// <summary>
    /// Number of times this location has been visited.
    /// </summary>
    public int VisitCount { get; init; }

    /// <summary>
    /// Creates a new location instance state with default values.
    /// </summary>
    public LocationInstanceState(
        string locationId,
        string locationType,
        string currentSublocation,
        Dictionary<string, string> currentStates)
    {
        LocationId = locationId ?? throw new ArgumentNullException(nameof(locationId));
        LocationType = locationType ?? throw new ArgumentNullException(nameof(locationType));
        CurrentSublocation = currentSublocation ?? throw new ArgumentNullException(nameof(currentSublocation));
        CurrentStates = currentStates ?? throw new ArgumentNullException(nameof(currentStates));
        ActionHistory = new List<PlayerAction>();
        LastVisited = DateTime.UtcNow;
        CurrentTurnCount = 0;
        TotalTurnCount = 0;
        VisitCount = 1;
    }

    /// <summary>
    /// Creates an initial location state from a blueprint.
    /// </summary>
    public static LocationInstanceState FromBlueprint(
        string locationId,
        LocationBlueprint blueprint,
        string? startingSublocation = null)
    {
        if (blueprint == null)
            throw new ArgumentNullException(nameof(blueprint));

        // Determine starting sublocation (first one if not specified)
        string sublocation = startingSublocation ?? blueprint.Sublocations.Keys.First();
        
        // Initialize states with default values from blueprint
        var states = new Dictionary<string, string>();
        foreach (var (categoryId, category) in blueprint.StateCategories)
        {
            states[categoryId] = category.DefaultStateId;
        }

        return new LocationInstanceState(locationId, blueprint.LocationType, sublocation, states);
    }

    /// <summary>
    /// Creates a copy with updated sublocation.
    /// </summary>
    public LocationInstanceState WithSublocation(string newSublocation)
    {
        return this with { CurrentSublocation = newSublocation };
    }

    /// <summary>
    /// Creates a copy with an updated state value.
    /// </summary>
    public LocationInstanceState WithState(string categoryId, string stateId)
    {
        var newStates = new Dictionary<string, string>(CurrentStates)
        {
            [categoryId] = stateId
        };
        return this with { CurrentStates = newStates };
    }

    /// <summary>
    /// Creates a copy with multiple state updates applied.
    /// </summary>
    public LocationInstanceState WithStates(Dictionary<string, string> stateChanges)
    {
        var newStates = new Dictionary<string, string>(CurrentStates);
        foreach (var (category, state) in stateChanges)
        {
            newStates[category] = state;
        }
        return this with { CurrentStates = newStates };
    }

    /// <summary>
    /// Creates a copy with an action added to history and incremented turn counts.
    /// </summary>
    public LocationInstanceState WithAction(PlayerAction action)
    {
        var newHistory = new List<PlayerAction>(ActionHistory) { action };
        return this with
        {
            ActionHistory = newHistory,
            CurrentTurnCount = CurrentTurnCount + 1,
            TotalTurnCount = TotalTurnCount + 1,
            LastVisited = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a copy with the current turn count reset (for new visit).
    /// </summary>
    public LocationInstanceState WithNewVisit()
    {
        return this with
        {
            CurrentTurnCount = 0,
            VisitCount = VisitCount + 1,
            LastVisited = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Serializes this location state to JSON.
    /// </summary>
    public string ToJson()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };
        return JsonSerializer.Serialize(this, options);
    }

    /// <summary>
    /// Deserializes a location state from JSON.
    /// </summary>
    public static LocationInstanceState? FromJson(string json)
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        };
        return JsonSerializer.Deserialize<LocationInstanceState>(json, options);
    }

    /// <summary>
    /// Gets a summary string for debugging.
    /// </summary>
    public override string ToString()
    {
        return $"Location {LocationId} ({LocationType}) - {CurrentSublocation} - " +
               $"Turn {CurrentTurnCount}/{TotalTurnCount} - Visit #{VisitCount}";
    }
}
