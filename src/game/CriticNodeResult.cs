namespace Cathedral.Game;

/// <summary>
/// Result of evaluating a single CriticNode.
/// Records which choice the LLM selected and whether it was a failure choice.
/// </summary>
public class CriticNodeResult
{
    /// <summary>The name of the node that was evaluated.</summary>
    public string NodeName { get; set; } = string.Empty;

    /// <summary>The question that was asked (without the choices list).</summary>
    public string Question { get; set; } = string.Empty;

    /// <summary>The choice id the LLM selected.</summary>
    public string ChosenId { get; set; } = string.Empty;

    /// <summary>True if the selected choice was a failure choice (tree stops here).</summary>
    public bool IsFailure { get; set; }

    /// <summary>Error message from the failure choice, if any.</summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>Duration of this evaluation in milliseconds.</summary>
    public double DurationMs { get; set; }

    public override string ToString()
    {
        var status = IsFailure ? "✗" : "✓";
        var error = IsFailure && !string.IsNullOrEmpty(ErrorMessage) ? $" — {ErrorMessage}" : "";
        return $"[{status}] {NodeName}: chose='{ChosenId}'{error} ({DurationMs:F0}ms)";
    }
}
