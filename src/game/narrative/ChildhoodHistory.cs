using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Stores the protagonist's childhood biography as it is reconstructed during the
/// childhood reminescence phase. Each REMEMBER action populates one or more fields.
///
/// Used as part of the reminescence prompt context (so subsequent reminescences are
/// coherent with what the protagonist has already remembered) and reused later in the
/// game by features that reference the protagonist's origins.
/// </summary>
public class ChildhoodHistory
{
    /// <summary>
    /// The childhood location ("the stable", "the port city", ...). Set by the first
    /// fragment chosen in <c>sound_in_the_dark</c>. Null until decided.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Visited reminescence ids in REMEMBER order (e.g. ["sound_in_the_dark", "stable_childhood", ...]).
    /// </summary>
    public List<string> VisitedReminescences { get; } = new();

    /// <summary>
    /// Fragments remembered in REMEMBER order.
    /// </summary>
    public List<(string ReminescenceId, string FragmentName, string FragmentSummary, string ContextSummary)> RememberedFragments { get; } = new();

    /// <summary>
    /// Records a fragment that was just remembered.
    /// <paramref name="contextSummary"/> is the short biographical phrase used in history
    /// prompts ("living by your wits on the street"). Empty for location-setting fragments.
    /// </summary>
    public void RecordFragment(string reminescenceId, string fragmentName, string fragmentSummary, string contextSummary = "")
    {
        RememberedFragments.Add((reminescenceId, fragmentName, fragmentSummary, contextSummary));
    }

    /// <summary>True when nothing has been remembered yet.</summary>
    public bool IsEmpty => RememberedFragments.Count == 0 && Location == null;

    /// <summary>
    /// Renders the childhood history as a single sentence suitable for the top of an LLM
    /// prompt. Returns an empty string when nothing has been remembered yet.
    /// Format: "You spent your childhood at {location}, {context phrase 1}, {context phrase 2}."
    /// </summary>
    public string ToPromptSummary()
    {
        if (IsEmpty) return string.Empty;
        if (Location == null) return string.Empty;

        var outcomes = RememberedFragments
            .Where(f => !string.IsNullOrWhiteSpace(f.ContextSummary))
            .Select(f => f.ContextSummary)
            .ToList();

        var sb = new StringBuilder();
        sb.Append($"You spent your childhood at {Location}");
        if (outcomes.Count > 0)
            sb.Append($", {string.Join(", ", outcomes)}");
        sb.Append('.');
        return sb.ToString();
    }
}
