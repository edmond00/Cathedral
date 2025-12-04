namespace Cathedral.Game;

/// <summary>
/// Represents an action with Critic evaluation scores.
/// Used for sorting and filtering actions based on quality.
/// </summary>
public class ScoredAction
{
    public ParsedAction Action { get; set; } = null!;
    
    /// <summary>
    /// Score for action-skill coherence (0.0-1.0).
    /// How well does the action match the assigned skill?
    /// </summary>
    public double SkillScore { get; set; }
    
    /// <summary>
    /// Score for action-consequence plausibility (0.0-1.0).
    /// How plausible is the success consequence given the action?
    /// </summary>
    public double ConsequenceScore { get; set; }
    
    /// <summary>
    /// Score for context coherence with previous action (0.0-1.0).
    /// Does this action make sense given what just happened?
    /// Set to 1.0 for first turn (no previous action).
    /// </summary>
    public double ContextScore { get; set; }
    
    /// <summary>
    /// Composite score combining all evaluation dimensions.
    /// Calculated as weighted average: skill*0.4 + consequence*0.4 + context*0.2
    /// </summary>
    public double TotalScore { get; set; }
    
    /// <summary>
    /// Duration of all evaluations for this action (milliseconds).
    /// </summary>
    public double EvaluationDurationMs { get; set; }
}
