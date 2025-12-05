using System.Collections.Generic;

namespace Cathedral.Game;

/// <summary>
/// Represents a parsed action from the Director's JSON response.
/// Contains all fields needed for Critic evaluation and outcome simulation.
/// </summary>
public class ParsedAction
{
    public string ActionText { get; set; } = "";
    public string Skill { get; set; } = "";
    public string Difficulty { get; set; } = "";
    public string Risk { get; set; } = "";
    
    // Success consequences
    public string SuccessConsequence { get; set; } = "";
    public Dictionary<string, string>? SuccessStateChanges { get; set; }
    public string? SuccessSublocationChange { get; set; }
    public List<string>? SuccessItemsGained { get; set; }
    public List<string>? SuccessCompanionsGained { get; set; }
    
    // Failure consequences
    public string FailureConsequence { get; set; } = "";
    public string FailureType { get; set; } = "";
    
    /// <summary>
    /// The original action index in the Director's response (for logging/debugging).
    /// </summary>
    public int OriginalIndex { get; set; }
}
