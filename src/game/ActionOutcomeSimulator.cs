using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Cathedral.Glyph.Microworld.LocationSystem;

namespace Cathedral.Game;

/// <summary>
/// Handles programmatic simulation of action outcomes.
/// Determines success/failure via RNG and applies consequences from action data.
/// </summary>
public class ActionOutcomeSimulator
{
    private readonly Random _random;
    private readonly double _defaultSuccessRate;
    
    public ActionOutcomeSimulator(double defaultSuccessRate = 0.7, int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
        _defaultSuccessRate = defaultSuccessRate;
    }
    
    /// <summary>
    /// Simulates the outcome of a selected action.
    /// Uses RNG for success/failure, then parses consequences from the action JSON.
    /// </summary>
    public PlayerAction SimulateOutcome(
        ParsedAction selectedAction,
        string fullActionResponse,
        int actionIndex)
    {
        // Determine success/failure
        var success = _random.NextDouble() < _defaultSuccessRate;
        
        var outcome = new PlayerAction
        {
            ActionText = selectedAction.ActionText,
            WasSuccessful = success
        };

        // Parse the full action data for consequences
        try
        {
            using var doc = JsonDocument.Parse(fullActionResponse);
            if (doc.RootElement.TryGetProperty("actions", out var actionsArray))
            {
                var actions = actionsArray.EnumerateArray().ToList();
                if (actionIndex < actions.Count)
                {
                    var actionData = actions[actionIndex];
                    
                    // For the new schema, we have success_consequence and failure_consequence as labels
                    // Extract them directly from the action
                    if (success && actionData.TryGetProperty("success_consequence", out var successConseq))
                    {
                        outcome.Outcome = successConseq.GetString() ?? "";
                        
                        // Map consequence labels to state changes
                        var consequenceLabel = outcome.Outcome;
                        ApplyConsequenceLabel(consequenceLabel, outcome);
                    }
                    else if (!success && actionData.TryGetProperty("failure_consequence", out var failureConseq))
                    {
                        outcome.Outcome = failureConseq.GetString() ?? "";
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Warning: Failed to parse action consequences: {ex.Message}");
            // Fallback outcome if parsing fails
            outcome.Outcome = success ? "Your action succeeded!" : "Your action didn't go as planned.";
        }

        if (string.IsNullOrEmpty(outcome.Outcome))
        {
            outcome.Outcome = success ? "Your action was successful!" : "Your attempt failed.";
        }

        return outcome;
    }
    
    /// <summary>
    /// Applies consequences based on the consequence label.
    /// Maps labels like "beasts flee", "noon passes" to actual state changes.
    /// </summary>
    private void ApplyConsequenceLabel(string label, PlayerAction outcome)
    {
        // Map consequence labels to state changes
        switch (label.ToLower())
        {
            case "beasts flee":
                outcome.StateChanges["wildlife_state"] = "fled";
                break;
                
            case "beasts approach":
                outcome.StateChanges["wildlife_state"] = "approaching";
                break;
                
            case "noon passes":
                outcome.StateChanges["time_of_day"] = "noon";
                break;
                
            case "dusk falls":
                outcome.StateChanges["time_of_day"] = "dusk";
                break;
                
            case "night falls":
                outcome.StateChanges["time_of_day"] = "night";
                break;
                
            case "rain begins":
                outcome.StateChanges["weather"] = "rain";
                break;
                
            case "storm arrives":
                outcome.StateChanges["weather"] = "storm";
                break;
                
            case "fog thickens":
                outcome.StateChanges["weather"] = "fog";
                break;
                
            case "trail clears":
                outcome.StateChanges["trail_condition"] = "clear_trail";
                break;
                
            case "trail obscured":
                outcome.StateChanges["trail_condition"] = "obscured";
                break;
                
            // Add more mappings as needed
        }
    }
    
    /// <summary>
    /// Applies an action outcome to the game state.
    /// Updates sublocation and state categories based on the outcome.
    /// </summary>
    public void ApplyOutcome(
        PlayerAction outcome,
        ref string currentSublocation,
        Dictionary<string, string> currentStates,
        DirectorPromptConstructor director,
        NarratorPromptConstructor narrator)
    {
        // Apply state changes
        foreach (var (category, newState) in outcome.StateChanges)
        {
            currentStates[category] = newState;
        }

        // Apply sublocation change
        if (!string.IsNullOrEmpty(outcome.NewSublocation))
        {
            currentSublocation = outcome.NewSublocation;
        }

        // Update both prompt constructors with new state
        director.UpdateGameState(currentSublocation, currentStates);
        narrator.UpdateGameState(currentSublocation, currentStates);
    }
}
