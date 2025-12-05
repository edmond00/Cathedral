namespace Cathedral.Game;

/// <summary>
/// Represents an action with Critic evaluation scores.
/// </summary>
public class ScoredAction
{
    public ParsedAction Action { get; set; } = new ParsedAction();
    public double SkillScore { get; set; }
    public double ConsequenceScore { get; set; }
    public double ContextScore { get; set; }
    public double TotalScore { get; set; }
    public double EvaluationDurationMs { get; set; }
}
