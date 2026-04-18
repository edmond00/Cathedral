using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Sentinel outcome representing the thinking modusMentis choosing to ignore
/// an observation and move on. When selected as the GOAL, the pipeline stops
/// after WHY — no HOW/WHAT is generated and no action button is shown.
/// </summary>
public sealed class IgnoreOutcome : ConcreteOutcome
{
    public static readonly IgnoreOutcome Instance = new();
    public const string GoalString = "move on and find something else to focus on";

    private IgnoreOutcome() { }

    public override string DisplayName => GoalString;
    public override string ToNaturalLanguageString() => GoalString;
    public override List<KeywordInContext> OutcomeKeywordsInContext => new();
}
