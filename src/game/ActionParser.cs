using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Cathedral.Game;

/// <summary>
/// Parses Director LLM JSON responses into strongly-typed ParsedAction objects.
/// </summary>
public static class ActionParser
{
    /// <summary>
    /// Parses a Director JSON response into a list of ParsedAction objects.
    /// Returns null if parsing fails.
    /// </summary>
    public static List<ParsedAction>? ParseDirectorResponse(string jsonResponse)
    {
        if (string.IsNullOrWhiteSpace(jsonResponse))
            return null;
        
        try
        {
            using var doc = JsonDocument.Parse(jsonResponse);
            
            if (!doc.RootElement.TryGetProperty("actions", out var actionsArray))
            {
                Console.WriteLine("ActionParser: No 'actions' property found in JSON");
                return null;
            }
            
            var parsedActions = new List<ParsedAction>();
            int index = 0;
            
            foreach (var actionElement in actionsArray.EnumerateArray())
            {
                var parsed = ParseSingleAction(actionElement, index);
                if (parsed != null)
                {
                    parsedActions.Add(parsed);
                }
                index++;
            }
            
            return parsedActions.Count > 0 ? parsedActions : null;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"ActionParser: JSON parsing error - {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Parses a single action element from the JSON.
    /// </summary>
    private static ParsedAction? ParseSingleAction(JsonElement actionElement, int index)
    {
        try
        {
            var action = new ParsedAction
            {
                OriginalIndex = index
            };
            
            // Parse basic fields
            if (actionElement.TryGetProperty("action_text", out var actionText))
                action.ActionText = actionText.GetString() ?? "";
            
            // Try both "related_skill" (current format) and "skill" (fallback)
            if (actionElement.TryGetProperty("related_skill", out var relatedSkill))
                action.Skill = relatedSkill.GetString() ?? "";
            else if (actionElement.TryGetProperty("skill", out var skill))
                action.Skill = skill.GetString() ?? "";
            
            if (actionElement.TryGetProperty("difficulty", out var difficulty))
                action.Difficulty = difficulty.GetString() ?? "";
            
            if (actionElement.TryGetProperty("risk", out var risk))
                action.Risk = risk.GetString() ?? "";
            
            // Parse success consequence (flat string format)
            if (actionElement.TryGetProperty("success_consequence", out var successConseq))
            {
                action.SuccessConsequence = successConseq.GetString() ?? "";
            }
            // Fallback to nested object format if present
            else if (actionElement.TryGetProperty("success_consequences", out var successConseqObj))
            {
                ParseSuccessConsequences(successConseqObj, action);
            }
            
            // Parse failure consequence (flat string format)
            if (actionElement.TryGetProperty("failure_consequence", out var failureConseq))
            {
                action.FailureConsequence = failureConseq.GetString() ?? "";
                action.FailureType = failureConseq.GetString() ?? "";
            }
            // Fallback to nested object format if present
            else if (actionElement.TryGetProperty("failure_consequences", out var failureConseqObj))
            {
                ParseFailureConsequences(failureConseqObj, action);
            }
            
            return action;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ActionParser: Error parsing action at index {index} - {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Parses success consequences from the JSON element.
    /// </summary>
    private static void ParseSuccessConsequences(JsonElement successElement, ParsedAction action)
    {
        // Description
        if (successElement.TryGetProperty("description", out var desc))
            action.SuccessConsequence = desc.GetString() ?? "";
        
        // State changes
        if (successElement.TryGetProperty("state_changes", out var stateChanges))
        {
            if (stateChanges.TryGetProperty("category", out var category) &&
                stateChanges.TryGetProperty("new_state", out var newState))
            {
                var categoryStr = category.GetString();
                var newStateStr = newState.GetString();
                
                if (!string.IsNullOrEmpty(categoryStr) && !string.IsNullOrEmpty(newStateStr) &&
                    categoryStr != "none" && newStateStr != "none")
                {
                    action.SuccessStateChanges = new Dictionary<string, string>
                    {
                        [categoryStr] = newStateStr
                    };
                }
            }
        }
        
        // Sublocation change
        if (successElement.TryGetProperty("sublocation_change", out var sublocChange))
        {
            var subloc = sublocChange.GetString();
            if (!string.IsNullOrEmpty(subloc) && subloc != "none")
                action.SuccessSublocationChange = subloc;
        }
        
        // Item gained
        if (successElement.TryGetProperty("item_gained", out var itemGained))
        {
            var item = itemGained.GetString();
            if (!string.IsNullOrEmpty(item) && item != "none")
                action.SuccessItemsGained = new List<string> { item };
        }
        
        // Companion gained
        if (successElement.TryGetProperty("companion_gained", out var companionGained))
        {
            var companion = companionGained.GetString();
            if (!string.IsNullOrEmpty(companion) && companion != "none")
                action.SuccessCompanionsGained = new List<string> { companion };
        }
    }
    
    /// <summary>
    /// Parses failure consequences from the JSON element.
    /// </summary>
    private static void ParseFailureConsequences(JsonElement failureElement, ParsedAction action)
    {
        // Type
        if (failureElement.TryGetProperty("type", out var type))
            action.FailureType = type.GetString() ?? "";
        
        // Description
        if (failureElement.TryGetProperty("description", out var desc))
            action.FailureConsequence = desc.GetString() ?? "";
        
        // If no description, use type as fallback
        if (string.IsNullOrEmpty(action.FailureConsequence) && !string.IsNullOrEmpty(action.FailureType))
        {
            action.FailureConsequence = $"Your action failed: {action.FailureType}";
        }
    }
}
