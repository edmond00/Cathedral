using System;
using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Represents a discrete narrative context within a location.
/// A narration node is a specific scene with keywords, possible outcomes, and transitions.
/// </summary>
public record NarrationNode(
    string NodeId,                                      // "forest_clearing_01"
    string NodeName,                                    // "A Sun-Dappled Clearing"
    string NeutralDescription,                          // Objective scene description
    List<string> Keywords,                              // 5-10 notable elements ("moss", "ancient oaks")
    Dictionary<string, string> KeywordIntroExamples,    // Fallback for GBNF: "moss" => "There is moss"
    Dictionary<string, List<Outcome>> OutcomesByKeyword, // What can happen when interacting with each keyword
    bool IsEntryNode,                                   // Can this be the first node when entering location?
    List<string> PossibleTransitions                    // Node IDs this can lead to
);

/// <summary>
/// Represents a possible consequence of an action.
/// Outcomes are converted to/from natural language strings for LLM communication.
/// </summary>
public record Outcome(
    OutcomeType Type,                                   // Transition, Item, Skill, Companion, Humor
    string TargetId,                                    // NodeId, ItemId, SkillId, CompanionId
    Dictionary<string, int>? HumorChanges               // Optional humor deltas
)
{
    /// <summary>
    /// Convert outcome to natural language string for LLM.
    /// Examples: "transition hidden_stream", "acquire rare_mushroom", "learn mycology"
    /// </summary>
    public string ToNaturalLanguageString() => Type switch
    {
        OutcomeType.Transition => $"transition {TargetId}",
        OutcomeType.Item => $"acquire {TargetId}",
        OutcomeType.Skill => $"learn {TargetId}",
        OutcomeType.Companion => $"befriend {TargetId}",
        OutcomeType.Humor when HumorChanges != null => string.Join(", ", HumorChanges.Select(kvp => 
            $"{(kvp.Value > 0 ? "increase" : "decrease")} {kvp.Key} by {Math.Abs(kvp.Value)}")),
        _ => "unknown outcome"
    };
    
    /// <summary>
    /// Parse natural language string back to outcome by matching against possible outcomes.
    /// </summary>
    public static Outcome FromNaturalLanguageString(string outcomeStr, List<Outcome> possibleOutcomes)
    {
        return possibleOutcomes.FirstOrDefault(o => o.ToNaturalLanguageString() == outcomeStr)
            ?? throw new InvalidOperationException($"Could not parse outcome string: {outcomeStr}");
    }
}

/// <summary>
/// Types of outcomes that can result from actions.
/// </summary>
public enum OutcomeType
{
    Transition,      // Move to new narration node
    Item,            // Acquire item
    Skill,           // Learn new skill
    Companion,       // Gain animal companion
    Humor            // Only humor changes (no concrete outcome)
}
