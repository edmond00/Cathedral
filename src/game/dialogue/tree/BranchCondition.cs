namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// Determines when a branch or outcome fires relative to the skill check result.
/// </summary>
public enum BranchCondition
{
    /// <summary>Only fire when the skill check succeeds.</summary>
    Success,

    /// <summary>Only fire when the skill check fails.</summary>
    Failure,

    /// <summary>Always fire regardless of skill check result.</summary>
    Either,
}
