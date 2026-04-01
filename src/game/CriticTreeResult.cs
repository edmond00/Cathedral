using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cathedral.Game;

/// <summary>
/// Result of evaluating an entire CriticNode tree.
/// </summary>
public class CriticTreeResult
{
    /// <summary>The complete trace of all nodes evaluated, in order.</summary>
    public List<CriticNodeResult> Trace { get; set; } = new();

    /// <summary>True if no failure choices were encountered during traversal.</summary>
    public bool OverallSuccess => Trace.All(r => !r.IsFailure);

    /// <summary>The choice id selected at the final (deepest) node evaluated.</summary>
    public string FinalChosenId => Trace.Count > 0 ? Trace[^1].ChosenId : string.Empty;

    /// <summary>Error message from the first failed node, if any.</summary>
    public string FirstErrorMessage =>
        Trace.FirstOrDefault(r => r.IsFailure)?.ErrorMessage ?? string.Empty;

    /// <summary>Total duration of all evaluations in milliseconds.</summary>
    public double TotalDurationMs => Trace.Sum(r => r.DurationMs);

    /// <summary>All error messages from failure nodes.</summary>
    public IEnumerable<string> AllErrorMessages =>
        Trace.Where(r => r.IsFailure && !string.IsNullOrEmpty(r.ErrorMessage))
             .Select(r => r.ErrorMessage);

    /// <summary>
    /// Concatenated critic reasoning for all failed nodes.
    /// Combines the structured error message with the free-text reason where available.
    /// </summary>
    public string CombinedFailureReason
    {
        get
        {
            var parts = Trace
                .Where(r => r.IsFailure)
                .Select(r =>
                {
                    if (!string.IsNullOrEmpty(r.FailureReason)) return r.FailureReason;
                    if (!string.IsNullOrEmpty(r.ErrorMessage))  return r.ErrorMessage;
                    return null;
                })
                .Where(s => s != null);
            return string.Join(" ", parts);
        }
    }

    public string GetTraceString()
    {
        if (Trace.Count == 0)
            return "No nodes evaluated.";

        var sb = new StringBuilder();
        sb.AppendLine($"Critic Tree ({Trace.Count} nodes, {TotalDurationMs:F0}ms):");
        for (int i = 0; i < Trace.Count; i++)
            sb.AppendLine($"  {i + 1}. {Trace[i]}");
        sb.AppendLine($"Result: {(OverallSuccess ? "SUCCESS" : $"FAILURE — {FirstErrorMessage}")}");
        return sb.ToString();
    }

    public override string ToString() =>
        $"CriticTreeResult: {(OverallSuccess ? "SUCCESS" : "FAILURE")} ({Trace.Count} nodes)";
}
