namespace Cathedral.Game;

/// <summary>
/// Represents an action with Critic evaluation scores from the first pass.
/// </summary>
public class ScoredAction
{
    public ParsedAction Action { get; set; } = new ParsedAction();
    
    // First pass evaluation scores
    public double SkillScore { get; set; }
    public double ConsequenceScore { get; set; }
    public double ContextScore { get; set; }
    public double LocationScore { get; set; }
    public double SpecificityScore { get; set; }
    
    public double TotalScore { get; set; }
    public double EvaluationDurationMs { get; set; }
}
