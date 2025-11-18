using System;

namespace Cathedral.Glyph.Microworld.LocationSystem;

/// <summary>
/// Abstract base class for generating location features with deterministic variance
/// Uses location ID as RNG seed to ensure consistent generation across visits
/// while providing rich variety between different locations of the same type
/// </summary>
public abstract class LocationFeatureGenerator
{
    /// <summary>
    /// Random number generator seeded with location ID for deterministic generation
    /// </summary>
    protected Random Rng { get; private set; } = new Random();
    
    /// <summary>
    /// Current location ID being processed
    /// </summary>
    protected string CurrentLocationId { get; private set; } = string.Empty;
    
    /// <summary>
    /// Generates a natural language context string describing the location's current state
    /// This string will be sent to the LLM to provide environmental context
    /// Should be optimized for small LLMs (under 200 words)
    /// </summary>
    /// <param name="locationId">Unique identifier for the location</param>
    /// <returns>Descriptive context string for LLM consumption</returns>
    public abstract string GenerateContext(string locationId);
    
    /// <summary>
    /// Generates the complete structural blueprint for a location including
    /// hierarchical sublocations, categorized states, and content mappings
    /// </summary>
    /// <param name="locationId">Unique identifier for the location</param>
    /// <returns>Complete location blueprint with all mechanical definitions</returns>
    public abstract LocationBlueprint GenerateBlueprint(string locationId);
    
    /// <summary>
    /// Sets the random number generator seed based on location ID
    /// Ensures deterministic generation - same location always produces same features
    /// Call this at the start of both GenerateContext and GenerateBlueprint
    /// </summary>
    /// <param name="locationId">Location identifier to use as seed</param>
    protected void SetSeed(string locationId)
    {
        if (string.IsNullOrEmpty(locationId))
            throw new ArgumentException("Location ID cannot be null or empty", nameof(locationId));
            
        CurrentLocationId = locationId;
        // Use location ID hash as seed for deterministic generation
        Rng = new Random(locationId.GetHashCode());
    }
    
    /// <summary>
    /// Helper method to generate varied sublocation connections
    /// Ensures hierarchical relationships are maintained
    /// </summary>
    /// <param name="sublocationIds">List of all sublocation IDs in the blueprint</param>
    /// <returns>Dictionary mapping sublocation IDs to their connected neighbors</returns>
    protected virtual Dictionary<string, List<string>> GenerateSublocationConnections(List<string> sublocationIds)
    {
        var connections = new Dictionary<string, List<string>>();
        
        foreach (var id in sublocationIds)
        {
            connections[id] = new List<string>();
        }
        
        // Default implementation creates minimal connectivity
        // Subclasses should override for location-specific connection logic
        return connections;
    }
    
    /// <summary>
    /// Helper method to generate content mappings based on sublocation and state combinations
    /// Subclasses should override to provide location-specific content logic
    /// </summary>
    /// <param name="sublocations">Dictionary of all sublocations in the blueprint</param>
    /// <returns>Content map keyed by sublocation+state combinations</returns>
    protected virtual Dictionary<string, Dictionary<string, LocationContent>> GenerateContentMap(
        Dictionary<string, Sublocation> sublocations)
    {
        var contentMap = new Dictionary<string, Dictionary<string, LocationContent>>();
        
        foreach (var (sublocationId, sublocation) in sublocations)
        {
            contentMap[sublocationId] = new Dictionary<string, LocationContent>
            {
                ["default"] = new LocationContent(
                    new List<string>(),
                    new List<string>(),
                    new List<string>(),
                    new List<string>(),
                    new List<string>()
                )
            };
        }
        
        return contentMap;
    }
    
    /// <summary>
    /// Validates that a blueprint has valid hierarchical structure and state consistency
    /// </summary>
    /// <param name="blueprint">Blueprint to validate</param>
    /// <returns>True if blueprint is structurally valid</returns>
    protected virtual bool ValidateBlueprint(LocationBlueprint blueprint)
    {
        if (blueprint == null)
            return false;
            
        // Check that all state categories have valid default states
        foreach (var (categoryId, category) in blueprint.StateCategories)
        {
            if (!category.PossibleStates.ContainsKey(category.DefaultStateId))
                return false;
        }
        
        // Check that sublocation parent references are valid
        foreach (var (sublocationId, sublocation) in blueprint.Sublocations)
        {
            if (sublocation.ParentSublocationId != null && 
                !blueprint.Sublocations.ContainsKey(sublocation.ParentSublocationId))
                return false;
        }
        
        // Check that sublocation connections reference valid sublocations
        foreach (var (sublocationId, connections) in blueprint.SublocationConnections)
        {
            if (!blueprint.Sublocations.ContainsKey(sublocationId))
                return false;
                
            foreach (var connectedId in connections)
            {
                if (!blueprint.Sublocations.ContainsKey(connectedId))
                    return false;
            }
        }
        
        return true;
    }
}