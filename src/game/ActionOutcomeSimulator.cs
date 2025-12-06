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
    /// Uses RNG or forced outcome to determine success/failure and applies consequences from action data.
    /// </summary>
    /// <param name="selectedAction">The action to simulate</param>
    /// <param name="currentState">Current location state</param>
    /// <param name="blueprint">Location blueprint</param>
    /// <param name="forceSuccess">If specified, forces success/failure instead of using RNG</param>
    /// <param name="overrideFailureConsequence">If specified, overrides the failure consequence label</param>
    public ActionResult SimulateOutcome(
        ParsedAction selectedAction,
        LocationInstanceState currentState,
        LocationBlueprint blueprint,
        bool? forceSuccess = null,
        string? overrideFailureConsequence = null)
    {
        if (selectedAction == null)
            throw new ArgumentNullException(nameof(selectedAction));
        
        // Determine success based on forced value or RNG
        bool success = forceSuccess ?? (_random.NextDouble() < DefaultSuccessRate);
        
        if (success)
        {
            return CreateSuccessResult(selectedAction);
        }
        else
        {
            return CreateFailureResult(selectedAction, overrideFailureConsequence);
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
    /// <param name="action">The failed action</param>
    /// <param name="overrideConsequence">Optional override for failure consequence (from Critic evaluation)</param>
    private ActionResult CreateFailureResult(ParsedAction action, string? overrideConsequence = null)
    {
        var failureDescription = overrideConsequence ?? action.FailureConsequence ?? "Your action failed.";
        
        // Add failure type context if available (only if not overridden)
        if (overrideConsequence == null && !string.IsNullOrEmpty(action.FailureType) && action.FailureType != "none")
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
