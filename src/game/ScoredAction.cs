using System;
using System.Collections.Generic;

namespace Cathedral.Game;

/// <summary>
/// Represents an action with Critic evaluation scores from tree-based evaluation.
/// </summary>
public class ScoredAction
{
    public ParsedAction Action { get; set; } = new ParsedAction();
    
    /// <summary>
    /// The full tree evaluation result with trace information.
    /// </summary>
    public CriticTreeResult? TreeResult { get; set; }
    
    // Individual scores (extracted from tree for backward compatibility)
    public double SkillScore { get; set; }
    public double ConsequenceScore { get; set; }
    public double ContextScore { get; set; }
    public double LocationScore { get; set; }
    public double SpecificityScore { get; set; }
    
    public double TotalScore { get; set; }
    public double EvaluationDurationMs { get; set; }
    
    /// <summary>
    /// Number of failed checks in the evaluation tree.
    /// </summary>
    public int FailureCount => TreeResult?.FailureCount ?? 0;
    
    /// <summary>
    /// Whether all checks in the evaluation tree passed.
    /// </summary>
    public bool AllChecksPassed => TreeResult?.OverallSuccess ?? false;
    
    /// <summary>
    /// Error messages from failed checks.
    /// </summary>
    public IEnumerable<string> ErrorMessages => TreeResult?.AllErrorMessages ?? Array.Empty<string>();
}
