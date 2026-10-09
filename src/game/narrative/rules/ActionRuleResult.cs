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

    /// <summary>
    /// The implement whose absence is the whole reason ("a knife", "a rod or a net"), when the
    /// refusal is one a tool would lift. Null for every other refusal. It changes two things: the
    /// refusal is narrated as a missing tool, said outright, and the action is left standing for
    /// "Use Tool" rather than greyed out as impossible.
    /// </summary>
    public string? NeededTool { get; private init; }

    public static ActionRuleResult Pass() => new() { Passed = true };
    public static ActionRuleResult Fail(string message) => new() { Passed = false, ErrorMessage = message };
    public static ActionRuleResult FailNeedsTool(string message, string tool)
        => new() { Passed = false, ErrorMessage = message, NeededTool = tool };
}
