using System;

namespace Cathedral.Game;

/// <summary>
/// Result of evaluating a single CriticNode.
/// </summary>
public class CriticNodeResult
{
    /// <summary>
    /// The name of the node that was evaluated.
    /// </summary>
    public string NodeName { get; set; } = string.Empty;
    
    /// <summary>
    /// The question that was asked.
    /// </summary>
    public string Question { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this node's check succeeded.
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// The raw probability of "yes" response (0.0 to 1.0).
    /// </summary>
    public double ProbabilityYes { get; set; }
    
    /// <summary>
    /// The raw probability of "no" response (0.0 to 1.0).
    /// </summary>
    public double ProbabilityNo { get; set; }
    
    /// <summary>
    /// The computed score: p(yes) / (p(yes) + p(no)).
    /// </summary>
    public double Score { get; set; }
    
    /// <summary>
    /// The threshold that was required for success.
    /// </summary>
    public double Threshold { get; set; }
    
    /// <summary>
    /// Whether "yes" was considered the success answer for this node.
    /// </summary>
    public bool YesIsSuccess { get; set; }
    
    /// <summary>
    /// Error message if the node failed, empty if succeeded.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
    
    /// <summary>
    /// Duration of this evaluation in milliseconds.
    /// </summary>
    public double DurationMs { get; set; }
    
    public override string ToString()
    {
        var status = Success ? "✓" : "✗";
        var answer = YesIsSuccess ? "yes" : "no";
        return $"[{status}] {NodeName}: score={Score:F3} (threshold={Threshold:F2}, {answer}=success) {(Success ? "" : $"- {ErrorMessage}")}";
    }
}
