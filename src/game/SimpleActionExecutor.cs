using System;
using System.Collections.Generic;
using Cathedral.Glyph.Microworld.LocationSystem;

namespace Cathedral.Game;

/// <summary>
/// Simple rule-based action executor for Phase 4.
/// This will be replaced with LLM-based execution in Phase 5.
/// </summary>
public class SimpleActionExecutor
{
    private readonly Random _random;
    private const double BASE_SUCCESS_RATE = 0.70; // 70% success rate
    private const double FAILURE_RATE = 0.15; // 15% chance of critical failure (ends interaction)

    public SimpleActionExecutor(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    /// <summary>
    /// Executes an action and returns the result.
    /// Uses simple RNG-based logic to determine outcomes.
    /// </summary>
    public ActionResult ExecuteAction(
        string actionText,
        LocationInstanceState currentState,
        LocationBlueprint blueprint)
    {
        // Special handling for "return" or "leave" actions
        if (IsExitAction(actionText))
        {
            return ActionResult.CreateExit("You leave the area and return to exploring the world.");
        }

        // Roll for outcome
        double roll = _random.NextDouble();

        // Critical failure (15% chance)
        if (roll < FAILURE_RATE)
        {
            return GenerateFailure(actionText, currentState, blueprint);
        }

        // Success (70% base chance, so 0.15 to 0.85)
        if (roll < (FAILURE_RATE + BASE_SUCCESS_RATE))
        {
            return GenerateSuccess(actionText, currentState, blueprint);
        }

        // Neutral/minor outcome (15% chance)
        return GenerateNeutralOutcome(actionText, currentState, blueprint);
    }

    private bool IsExitAction(string actionText)
    {
        var lowerAction = actionText.ToLowerInvariant();
        return lowerAction.Contains("return") ||
               lowerAction.Contains("leave") ||
               lowerAction.Contains("exit") ||
               lowerAction.Contains("go back");
    }

    private ActionResult GenerateFailure(
        string actionText,
        LocationInstanceState currentState,
        LocationBlueprint blueprint)
    {
        // Generate contextual failure narrative based on location type and action
        var narratives = new List<string>
        {
            "Your action has an unexpected consequence. You lose your footing and injure yourself, forcing you to retreat.",
            "Things go terribly wrong. You make too much noise and attract unwanted attention, forcing you to flee.",
            "Your attempt fails catastrophically. You disturb something dangerous and must escape immediately.",
            "The situation deteriorates rapidly. Your action backfires, leaving you no choice but to abandon this location.",
            "You miscalculate badly. The risk becomes too great and you're forced to withdraw to safety."
        };

        var narrative = narratives[_random.Next(narratives.Count)];
        return ActionResult.CreateFailure(narrative);
    }

    private ActionResult GenerateSuccess(
        string actionText,
        LocationInstanceState currentState,
        LocationBlueprint blueprint)
    {
        // Determine what changes
        var stateChanges = new Dictionary<string, string>();
        string? newSublocation = null;
        var itemsGained = new List<string>();

        // Possibly change time of day (30% chance)
        if (_random.NextDouble() < 0.30 && blueprint.StateCategories.ContainsKey("time_of_day"))
        {
            var timeCategory = blueprint.StateCategories["time_of_day"];
            var currentTimeId = currentState.CurrentStates.GetValueOrDefault("time_of_day", timeCategory.DefaultStateId);
            
            // Get all possible time states and pick a different one
            var otherTimes = timeCategory.PossibleStates.Keys
                .Where(k => k != currentTimeId)
                .ToList();
            
            if (otherTimes.Count > 0)
            {
                var nextTimeId = otherTimes[_random.Next(otherTimes.Count)];
                stateChanges["time_of_day"] = nextTimeId;
            }
        }

        // Possibly change sublocation (20% chance if action involves movement)
        if (_random.NextDouble() < 0.20 && ActionInvolvesMovement(actionText))
        {
            var otherSublocations = blueprint.Sublocations.Keys
                .Where(k => k != currentState.CurrentSublocation)
                .ToList();
            
            if (otherSublocations.Count > 0)
            {
                newSublocation = otherSublocations[_random.Next(otherSublocations.Count)];
            }
        }

        // Possibly gain item (25% chance if action involves searching/examining)
        if (_random.NextDouble() < 0.25 && ActionInvolvesSearching(actionText))
        {
            itemsGained.Add(GenerateRandomItem(blueprint));
        }

        // Generate success narrative
        var narrative = GenerateSuccessNarrative(
            actionText,
            currentState,
            blueprint,
            stateChanges,
            newSublocation,
            itemsGained);

        return ActionResult.CreateSuccess(
            narrative,
            stateChanges,
            newSublocation,
            newActions: null, // Will regenerate based on new state
            itemsGained: itemsGained.Count > 0 ? itemsGained : null);
    }

    private ActionResult GenerateNeutralOutcome(
        string actionText,
        LocationInstanceState currentState,
        LocationBlueprint blueprint)
    {
        var narratives = new List<string>
        {
            "You proceed cautiously. Nothing significant happens, but you've learned more about your surroundings.",
            "Your action yields minimal results. You gain some insight but make no substantial progress.",
            "Time passes uneventfully. You complete your action but the outcome is unremarkable.",
            "Your efforts are partially successful. You accomplish something, but not as much as you'd hoped.",
            "You act carefully and methodically. The immediate results are modest but you feel better prepared."
        };

        var narrative = narratives[_random.Next(narratives.Count)];
        
        return ActionResult.CreateSuccess(
            narrative,
            stateChanges: new Dictionary<string, string>(),
            newSublocation: null,
            newActions: null);
    }

    private bool ActionInvolvesMovement(string actionText)
    {
        var lowerAction = actionText.ToLowerInvariant();
        return lowerAction.Contains("continue") ||
               lowerAction.Contains("follow") ||
               lowerAction.Contains("path") ||
               lowerAction.Contains("deeper") ||
               lowerAction.Contains("explore") ||
               lowerAction.Contains("venture");
    }

    private bool ActionInvolvesSearching(string actionText)
    {
        var lowerAction = actionText.ToLowerInvariant();
        return lowerAction.Contains("search") ||
               lowerAction.Contains("examine") ||
               lowerAction.Contains("look for") ||
               lowerAction.Contains("investigate") ||
               lowerAction.Contains("gather");
    }

    private string GenerateRandomItem(LocationBlueprint blueprint)
    {
        // Generate contextual items based on location type
        var items = blueprint.LocationType.ToLowerInvariant() switch
        {
            "forest" => new[] { "medicinal herbs", "wooden branch", "wild berries", "mushrooms", "bird feather" },
            "mountain" => new[] { "smooth stone", "iron ore", "mountain flower", "crystal shard", "eagle feather" },
            "coast" => new[] { "seashell", "driftwood", "sea glass", "dried kelp", "colorful pebble" },
            "desert" => new[] { "desert flower", "scorpion shell", "ancient pottery shard", "dried cactus", "sand crystal" },
            _ => new[] { "curious trinket", "useful item", "interesting object", "valuable find", "strange artifact" }
        };

        return items[_random.Next(items.Length)];
    }

    private string GenerateSuccessNarrative(
        string actionText,
        LocationInstanceState currentState,
        LocationBlueprint blueprint,
        Dictionary<string, string> stateChanges,
        string? newSublocation,
        List<string> itemsGained)
    {
        var parts = new List<string>();

        // Base success message
        parts.Add("Your action succeeds.");

        // Add specific outcome based on what changed
        if (itemsGained.Count > 0)
        {
            parts.Add($"You find {string.Join(", ", itemsGained)}.");
        }

        if (newSublocation != null && blueprint.Sublocations.TryGetValue(newSublocation, out var subloc))
        {
            parts.Add($"You move to a new area: {subloc.Name}.");
        }

        if (stateChanges.ContainsKey("time_of_day"))
        {
            var newTimeId = stateChanges["time_of_day"];
            var timeCategory = blueprint.StateCategories["time_of_day"];
            if (timeCategory.PossibleStates.TryGetValue(newTimeId, out var newTime))
            {
                parts.Add($"Time passes - it is now {newTime.Name.ToLowerInvariant()}.");
            }
        }

        // Add encouraging message
        var encouragements = new[]
        {
            "You feel more confident as you continue.",
            "Your experience in this place grows.",
            "You're learning to navigate these challenges.",
            "Your skills are being put to good use.",
            "Progress feels tangible as you advance."
        };
        parts.Add(encouragements[_random.Next(encouragements.Length)]);

        return string.Join(" ", parts);
    }
}
