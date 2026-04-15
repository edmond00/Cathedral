namespace Cathedral.Game.Narrative.Rules;

/// <summary>
/// Result returned by a single <see cref="IActionRule"/> check.
/// </summary>
public class ActionRuleResult
{
    /// <summary>True when the rule is satisfied and the action may proceed.</summary>
    public bool Passed { get; private init; }

    /// <summary>
    /// Human-readable reason shown to the player as an [IMPOSSIBLE] block.
    /// Null when <see cref="Passed"/> is true.
    /// </summary>
    public string? ErrorMessage { get; private init; }

    public static ActionRuleResult Pass() => new() { Passed = true };
    public static ActionRuleResult Fail(string message) => new() { Passed = false, ErrorMessage = message };
}
