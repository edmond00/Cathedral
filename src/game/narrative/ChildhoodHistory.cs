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
    /// Fragments remembered in REMEMBER order, paired with their reminescence id.
    /// Items are <c>(reminescenceId, fragmentName, fragmentSummary)</c>.
    /// </summary>
    public List<(string ReminescenceId, string FragmentName, string FragmentSummary)> RememberedFragments { get; } = new();

    /// <summary>
    /// Records a fragment that was just remembered. The summary is the natural-language
    /// content the player will recognise (one or two sentences from the fragment definition).
    /// </summary>
    public void RecordFragment(string reminescenceId, string fragmentName, string fragmentSummary)
    {
        RememberedFragments.Add((reminescenceId, fragmentName, fragmentSummary));
    }

    /// <summary>True when nothing has been remembered yet.</summary>
    public bool IsEmpty => RememberedFragments.Count == 0 && Location == null;

    /// <summary>
    /// Renders the currently filled childhood history as a compact prose summary suitable for
    /// inclusion in LLM prompts. Returns an empty string when nothing has been remembered.
    /// </summary>
    public string ToPromptSummary()
    {
        if (IsEmpty) return string.Empty;
        var sb = new StringBuilder();
        if (Location != null)
            sb.Append($"You spent your childhood at {Location}. ");
        if (RememberedFragments.Count > 0)
        {
            sb.Append("So far you have remembered: ");
            sb.Append(string.Join("; ", RememberedFragments.Select(f => f.FragmentSummary)));
            sb.Append('.');
        }
        return sb.ToString().Trim();
    }
}
