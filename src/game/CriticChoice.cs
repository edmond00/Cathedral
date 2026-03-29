namespace Cathedral.Game;

/// <summary>
/// A single choice option in a CriticNode enum-style evaluation.
/// The Id is the exact token the LLM outputs, constrained via GBNF grammar.
/// </summary>
public class CriticChoice
{
    /// <summary>The exact token the LLM outputs (constrained via GBNF).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Optional description shown in the prompt next to the choice id.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// When true, selecting this choice terminates the tree as a plausibility failure.
    /// Used by plausibility nodes where picking this choice means the action is rejected.
    /// </summary>
    public bool IsFailure { get; set; }

    /// <summary>Error message returned to the player when IsFailure is true and this choice is selected.</summary>
    public string? ErrorMessage { get; set; }

    public CriticChoice(string id, string? description = null, bool isFailure = false, string? errorMessage = null)
    {
        Id = id;
        Description = description;
        IsFailure = isFailure;
        ErrorMessage = errorMessage;
    }
}
