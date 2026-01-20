using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cathedral.Game;

/// <summary>
/// Result of evaluating an entire CriticNode tree.
/// </summary>
public class CriticTreeResult
{
    /// <summary>
    /// The complete trace of all nodes evaluated, in order.
    /// </summary>
    public List<CriticNodeResult> Trace { get; set; } = new();
    
    /// <summary>
    /// The number of failures encountered during tree traversal.
    /// 0 means no failures at all.
    /// </summary>
    public int FailureCount => Trace.Count(r => !r.Success);
    
    /// <summary>
    /// Whether the final node in the trace succeeded.
    /// </summary>
    public bool FinalSuccess => Trace.Count > 0 && Trace[^1].Success;
    
    /// <summary>
    /// The error message from the final node if it failed, empty otherwise.
    /// </summary>
    public string FinalErrorMessage => 
        Trace.Count > 0 && !Trace[^1].Success 
            ? Trace[^1].ErrorMessage 
            : string.Empty;
    
    /// <summary>
    /// Overall success: true only if there were no failures at all.
    /// </summary>
    public bool OverallSuccess => FailureCount == 0;
    
    /// <summary>
    /// Total duration of all evaluations in milliseconds.
    /// </summary>
    public double TotalDurationMs => Trace.Sum(r => r.DurationMs);
    
    /// <summary>
    /// Gets all error messages from failed nodes.
    /// </summary>
    public IEnumerable<string> AllErrorMessages => 
        Trace.Where(r => !r.Success && !string.IsNullOrEmpty(r.ErrorMessage))
             .Select(r => r.ErrorMessage);
    
    /// <summary>
    /// Gets all failed node names.
    /// </summary>
    public IEnumerable<string> FailedNodeNames => 
        Trace.Where(r => !r.Success).Select(r => r.NodeName);
    
    /// <summary>
    /// Gets a formatted string representation of the full trace.
    /// </summary>
    public string GetTraceString()
    {
        if (Trace.Count == 0)
            return "No nodes evaluated.";
        
        var sb = new StringBuilder();
        sb.AppendLine($"Critic Tree Evaluation ({Trace.Count} nodes, {FailureCount} failures, {TotalDurationMs:F0}ms):");
        
        for (int i = 0; i < Trace.Count; i++)
        {
            sb.AppendLine($"  {i + 1}. {Trace[i]}");
        }
        
        sb.AppendLine($"Result: {(OverallSuccess ? "SUCCESS" : $"FAILURE - {FinalErrorMessage}")}");
        
        return sb.ToString();
    }
    
    public override string ToString()
    {
        return $"CriticTreeResult: {(OverallSuccess ? "SUCCESS" : "FAILURE")} ({Trace.Count} nodes, {FailureCount} failures)";
    }
}
