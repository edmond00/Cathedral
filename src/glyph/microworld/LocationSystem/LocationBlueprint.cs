using System;
using System.Collections.Generic;

namespace Cathedral.Glyph.Microworld.LocationSystem;

/// <summary>
/// Core data structure defining granular location mechanics with hierarchical sublocations
/// and categorized state system
/// </summary>
public record LocationBlueprint(
    string LocationId,
    string LocationType,
    Dictionary<string, StateCategory> StateCategories,
    Dictionary<string, Sublocation> Sublocations,
    Dictionary<string, List<string>> SublocationConnections,
    Dictionary<string, Dictionary<string, LocationContent>> ContentMap
)
{
    public string LocationId { get; init; } = LocationId ?? throw new ArgumentNullException(nameof(LocationId));
    public string LocationType { get; init; } = LocationType ?? throw new ArgumentNullException(nameof(LocationType));
    public Dictionary<string, StateCategory> StateCategories { get; init; } = StateCategories ?? throw new ArgumentNullException(nameof(StateCategories));
    public Dictionary<string, Sublocation> Sublocations { get; init; } = Sublocations ?? throw new ArgumentNullException(nameof(Sublocations));
    public Dictionary<string, List<string>> SublocationConnections { get; init; } = SublocationConnections ?? throw new ArgumentNullException(nameof(SublocationConnections));
    public Dictionary<string, Dictionary<string, LocationContent>> ContentMap { get; init; } = ContentMap ?? throw new ArgumentNullException(nameof(ContentMap));
    
    /// <summary>
    /// Maps simple 1-3 word labels to their full state resolution (category + state)
    /// Built automatically from all LocationState labels across all categories
    /// Used to resolve simple consequence strings like "night falls" back to {category: "time_of_day", state: "night"}
    /// </summary>
    public Dictionary<string, StateResolution> StateResolutionMap { get; init; } = BuildStateResolutionMap(StateCategories);
    
    private static Dictionary<string, StateResolution> BuildStateResolutionMap(Dictionary<string, StateCategory> categories)
    {
        var map = new Dictionary<string, StateResolution>();
        var seenLabels = new HashSet<string>();
        
        foreach (var (categoryId, category) in categories)
        {
            foreach (var (stateId, state) in category.PossibleStates)
            {
                if (seenLabels.Contains(state.Label))
                {
                    throw new InvalidOperationException(
                        $"Duplicate state label '{state.Label}' found. All state labels must be unique within a location.");
                }
                
                seenLabels.Add(state.Label);
                map[state.Label] = new StateResolution(categoryId, stateId, state.Label);
            }
        }
        
        return map;
    }
    
    /// <summary>
    /// Resolves a simple label string to its category and state ID
    /// </summary>
    public StateResolution? ResolveLabel(string label)
    {
        return StateResolutionMap.TryGetValue(label, out var resolution) ? resolution : null;
    }
}

/// <summary>
/// Resolution of a simple label to its full state information
/// </summary>
public record StateResolution(
    string CategoryId,
    string StateId,
    string Label
)
{
    public string CategoryId { get; init; } = CategoryId ?? throw new ArgumentNullException(nameof(CategoryId));
    public string StateId { get; init; } = StateId ?? throw new ArgumentNullException(nameof(StateId));
    public string Label { get; init; } = Label ?? throw new ArgumentNullException(nameof(Label));
}

/// <summary>
/// Categorized state system - each category can only have one active state
/// Prevents impossible combinations (e.g., can't be both "day" and "night")
/// </summary>
public record StateCategory(
    string CategoryId,
    string Name,
    Dictionary<string, LocationState> PossibleStates,
    string DefaultStateId,
    StateScope Scope
)
{
    public string CategoryId { get; init; } = CategoryId ?? throw new ArgumentNullException(nameof(CategoryId));
    public string Name { get; init; } = Name ?? throw new ArgumentNullException(nameof(Name));
    public Dictionary<string, LocationState> PossibleStates { get; init; } = PossibleStates ?? throw new ArgumentNullException(nameof(PossibleStates));
    public string DefaultStateId { get; init; } = DefaultStateId ?? throw new ArgumentNullException(nameof(DefaultStateId));
    public StateScope Scope { get; init; } = Scope;
}

/// <summary>
/// Individual state within a category with cross-category dependencies
/// </summary>
public record LocationState(
    string Id,
    string Name,
    string Description,
    string Label,
    List<string>? RequiredStates = null,
    List<string>? ForbiddenStates = null
)
{
    public string Id { get; init; } = Id ?? throw new ArgumentNullException(nameof(Id));
    public string Name { get; init; } = Name ?? throw new ArgumentNullException(nameof(Name));
    public string Description { get; init; } = Description ?? throw new ArgumentNullException(nameof(Description));
    public string Label { get; init; } = Label ?? throw new ArgumentNullException(nameof(Label));
    public List<string> RequiredStates { get; init; } = RequiredStates ?? new List<string>();
    public List<string> ForbiddenStates { get; init; } = ForbiddenStates ?? new List<string>();
}

/// <summary>
/// Hierarchical sublocation system with granular connections
/// Supports parent-child relationships for realistic spatial navigation
/// </summary>
public record Sublocation(
    string Id,
    string Name,
    string Description,
    string? ParentSublocationId,
    List<string> DirectConnections,
    List<string> RequiredStates,
    List<string> ForbiddenStates,
    Dictionary<string, string> LocalStates
)
{
    public string Id { get; init; } = Id ?? throw new ArgumentNullException(nameof(Id));
    public string Name { get; init; } = Name ?? throw new ArgumentNullException(nameof(Name));
    public string Description { get; init; } = Description ?? throw new ArgumentNullException(nameof(Description));
    public string? ParentSublocationId { get; init; } = ParentSublocationId;
    public List<string> DirectConnections { get; init; } = DirectConnections ?? new List<string>();
    public List<string> RequiredStates { get; init; } = RequiredStates ?? new List<string>();
    public List<string> ForbiddenStates { get; init; } = ForbiddenStates ?? new List<string>();
    public Dictionary<string, string> LocalStates { get; init; } = LocalStates ?? new Dictionary<string, string>();
}

/// <summary>
/// Content available in specific sublocation/state combinations
/// Supports granular, contextual interactions
/// </summary>
public record LocationContent(
    List<string> AvailableItems,
    List<string> AvailableCompanions,
    List<string> AvailableQuests,
    List<string> AvailableNPCs,
    List<string> AvailableActions
)
{
    public List<string> AvailableItems { get; init; } = AvailableItems ?? new List<string>();
    public List<string> AvailableCompanions { get; init; } = AvailableCompanions ?? new List<string>();
    public List<string> AvailableQuests { get; init; } = AvailableQuests ?? new List<string>();
    public List<string> AvailableNPCs { get; init; } = AvailableNPCs ?? new List<string>();
    public List<string> AvailableActions { get; init; } = AvailableActions ?? new List<string>();
}

/// <summary>
/// Defines whether a state affects the entire location or only specific sublocations
/// </summary>
public enum StateScope
{
    /// <summary>
    /// State affects the entire location (e.g., weather, time of day)
    /// </summary>
    Location,
    
    /// <summary>
    /// State affects only a specific sublocation (e.g., chest lock, room occupancy)
    /// </summary>
    Sublocation
}