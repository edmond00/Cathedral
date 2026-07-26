using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// A dialogue effect that has already been applied by its <see cref="IDialogueOutcome"/>, described
/// for display. Deliberately inert: <see cref="OutcomeReport.Apply"/> stays a no-op, because unlike a
/// narration report (which both describes and performs its change) the dialogue outcome has already
/// done the work by the time the chip exists. Reusing <see cref="OutcomeReport"/> is what lets the
/// dialogue panel render these through exactly the same path as an action's outcome chips.
/// </summary>
public sealed class DialogueOutcomeReport : OutcomeReport
{
    // Dialogue chips are rendered in the dialogue panel, never listed in an action's outcome
    // sentence, so they carry no narration verbatim.
    public DialogueOutcomeReport(string text, OutcomeReportSeverity severity)
        : base(text, severity, verbatim: string.Empty) { }
}

/// <summary>Shared phrasing for the chips dialogue outcomes emit.</summary>
public static class DialogueOutcomeReports
{
    /// <summary>
    /// Describes a move along the affinity scale, or null when the level did not actually change.
    /// Severity follows <see cref="AffinityLevelExtensions.BonusDice"/> rather than the raw enum
    /// order: Suspicious sorts highest numerically but grants nothing, so it must read as a loss.
    /// </summary>
    public static OutcomeReport? AffinityChange(NpcEntity npc, AffinityLevel before, AffinityLevel after)
    {
        if (before == after) return null;

        int delta = after.BonusDice() - before.BonusDice();
        var severity = delta > 0 ? OutcomeReportSeverity.Positive
                     : delta < 0 ? OutcomeReportSeverity.Negative
                                 : OutcomeReportSeverity.Neutral;

        return new DialogueOutcomeReport(
            $"{npc.DisplayName}: {before.ToShortLabel()} → {after.ToShortLabel()}", severity);
    }

    /// <summary>A relationship change that is not a step on the affinity scale (enmity, crimes).</summary>
    public static OutcomeReport Relation(string text, OutcomeReportSeverity severity)
        => new DialogueOutcomeReport(text, severity);
}
