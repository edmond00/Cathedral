using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Glyph.Microworld.LocationSystem;

namespace Cathedral.Game;

/// <summary>
/// Simulates action outcomes programmatically using RNG.
/// Replaces LLM-based outcome generation with deterministic simulation.
/// </summary>
public class ActionOutcomeSimulator
{
    private readonly Random _random;
    
    // Success rate configuration
    private const double DefaultSuccessRate = 0.70; // 70% success rate
    
    public ActionOutcomeSimulator()
    {
        _random = new Random();
    }
    
    public ActionOutcomeSimulator(int seed)
    {
        _random = new Random(seed);
    }
    
    /// <summary>
    /// Simulates the outcome of a parsed action.
    /// Uses RNG to determine success/failure and applies consequences from action data.
    /// </summary>
    public ActionResult SimulateOutcome(
        ParsedAction selectedAction,
        LocationInstanceState currentState,
        LocationBlueprint blueprint)
    {
        if (selectedAction == null)
            throw new ArgumentNullException(nameof(selectedAction));
        
        // Determine success based on RNG
        // Future: could factor in difficulty, skill levels, etc.
        bool success = _random.NextDouble() < DefaultSuccessRate;
        
        if (success)
        {
            return CreateSuccessResult(selectedAction);
        }
        else
        {
            return CreateFailureResult(selectedAction);
        }
    }
    
    /// <summary>
    /// Creates an ActionResult for a successful action.
    /// </summary>
    private ActionResult CreateSuccessResult(ParsedAction action)
    {
        var stateChanges = new Dictionary<string, string>();
        
        // Apply state changes from success consequences
        if (action.SuccessStateChanges != null)
        {
            foreach (var (category, newState) in action.SuccessStateChanges)
            {
                if (!string.IsNullOrEmpty(category) && !string.IsNullOrEmpty(newState) &&
                    category != "none" && newState != "none")
                {
                    stateChanges[category] = newState;
                }
            }
        }
        
        // Extract items gained
        var itemsGained = new List<string>();
        if (action.SuccessItemsGained != null)
        {
            itemsGained.AddRange(action.SuccessItemsGained
                .Where(item => !string.IsNullOrEmpty(item) && item != "none"));
        }
        
        // Extract companions gained
        var companionsGained = new List<string>();
        if (action.SuccessCompanionsGained != null)
        {
            companionsGained.AddRange(action.SuccessCompanionsGained
                .Where(companion => !string.IsNullOrEmpty(companion) && companion != "none"));
        }
        
        // Determine sublocation change
        string? newSublocation = null;
        if (!string.IsNullOrEmpty(action.SuccessSublocationChange) && 
            action.SuccessSublocationChange != "none")
        {
            newSublocation = action.SuccessSublocationChange;
        }
        
        return ActionResult.CreateSuccess(
            narrative: action.SuccessConsequence ?? "Your action succeeded!",
            stateChanges: stateChanges,
            newSublocation: newSublocation,
            itemsGained: itemsGained.Count > 0 ? itemsGained : null,
            endsInteraction: false);
    }
    
    /// <summary>
    /// Creates an ActionResult for a failed action.
    /// Failures end the interaction.
    /// </summary>
    private ActionResult CreateFailureResult(ParsedAction action)
    {
        var failureDescription = action.FailureConsequence ?? "Your action failed.";
        
        // Add failure type context if available
        if (!string.IsNullOrEmpty(action.FailureType) && action.FailureType != "none")
        {
            failureDescription = $"{failureDescription} ({action.FailureType})";
        }
        
        return ActionResult.CreateFailure(
            narrative: failureDescription);
    }
    
    /// <summary>
    /// Configurable success check for future enhancement.
    /// Can factor in difficulty, player stats, etc.
    /// </summary>
    public bool RollSuccess(int difficulty = 3, double baseRate = DefaultSuccessRate)
    {
        // Simple implementation for now
        // Future: difficulty could modify success rate
        return _random.NextDouble() < baseRate;
    }
}
