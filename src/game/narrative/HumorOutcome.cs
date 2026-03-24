using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Represents a pure humor change outcome (used for failures and emotional reactions).
/// Has no keywords as it's determined by the system, not selected by players.
/// </summary>
public class HumorOutcome : OutcomeBase
{
    public string HumorName { get; }
    public int Amount { get; }
    public string Description { get; }

    public HumorOutcome(string humorName, int amount, string description = "")
    {
        HumorName = humorName;
        Amount = amount;
        Description = description;
    }
    
    public override string DisplayName => $"{HumorName} {(Amount > 0 ? "+" : "")}{Amount}";

    public override string ToNaturalLanguageString()
    {
        return $"{(Amount > 0 ? "increase" : "decrease")} {HumorName} by {Math.Abs(Amount)}";
    }
}
