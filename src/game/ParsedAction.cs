namespace Cathedral.Game;

/// <summary>
/// Represents a parsed action from the Director's JSON response.
/// Contains all fields needed for Critic evaluation and player presentation.
/// </summary>
public class ParsedAction
{
    public string ActionText { get; set; } = "";
    public string Skill { get; set; } = "";
    public string Difficulty { get; set; } = "";
    public string SuccessConsequence { get; set; } = "";
    public string FailureConsequence { get; set; } = "";
    
    /// <summary>
    /// The original action index in the Director's response (for logging/debugging).
    /// </summary>
    public int OriginalIndex { get; set; }
}
