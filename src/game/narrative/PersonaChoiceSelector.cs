using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cathedral;
using Cathedral.LLM;

namespace Cathedral.Game.Narrative;

/// <summary>
/// The per-site framing a persona choice needs, around the options the selector renders itself:
/// <list type="bullet">
/// <item><see cref="ContextText"/> — the situational preamble ("You are in a field, in the wheat
///       strip. Your attention is on …"); ends with its own blank line, or is "".</item>
/// <item><see cref="OpenQuestion"/> — the second-person question put to the Modus Mentis, phrased for
///       the site ("What do you want to do?", "How do you want to do it?", "What do you want to focus
///       on?", "What do you want to say?").</item>
/// <item><see cref="ReportedQuestion"/> — the same question in reported speech, for the critic's
///       attribution line ("what they want to do", "how they want to go about it", "what they want to
///       focus on", "what they want to say").</item>
/// </list>
/// All three are constant across one selection, so the reasoning prompt's prefix stays cacheable.
/// </summary>
public readonly record struct PersonaChoicePrompt(string ContextText, string OpenQuestion, string ReportedQuestion);

/// <summary>
/// Persona-driven option selection, split into a persona stage and a neutral stage so the flavoured
/// Modus Mentis slots are never asked to reason logically against a schema (which they do poorly):
/// <list type="number">
/// <item>render the options — the real ones plus a site-specific <i>decline</i> option ("walk away",
///       "focus on nothing", "stay silent") — in one shuffled order, to defeat position bias;</item>
/// <item>ask the Modus Mentis the site's <see cref="PersonaChoicePrompt.OpenQuestion"/> over that
///       list, in its own voice, as <b>free text</b> (no grammar): this is the persona's <i>want</i>,
///       used only as a hint;</item>
/// <item>hand that reasoning, the context and the lettered list to the neutral
///       <see cref="PersonaMatchCritic"/>, which returns the single letter whose option best matches
///       the want.</item>
/// </list>
/// If the critic lands on the decline option the call returns <c>null</c> — the caller's existing
/// failure path (nothing to observe / ignore &amp; move on / no way to act / stay silent) fires. In
/// playground mode it short-circuits to a random real option (never declines) with no LLM consulted.
///
/// <para>At most 26 options reach the critic (one letter each); if there are more, the real options
/// are sampled down first. The Modus Mentis slot is reset before and after its reasoning call, so the
/// pass is self-contained: whatever the slot held on entry is cleared, and a follow-up rewrite starts
/// from the system prompt.</para>
/// </summary>
public class PersonaChoiceSelector
{
    /// <summary>Reasoning is a sentence or two — enough to name a want, not to monologue.</summary>
    private const int ReasoningMaxTokens = 120;

    private readonly LlamaServerManager _llm;
    private readonly Random _random = new();

    public PersonaChoiceSelector(LlamaServerManager llm) => _llm = llm ?? throw new ArgumentNullException(nameof(llm));

    /// <summary>
    /// Runs the persona reasoning on <paramref name="slotId"/> (whose system prompt carries
    /// <paramref name="evaluator"/>'s persona) and the neutral match on the shared critic, returning
    /// the chosen item — or <c>null</c> when the persona chose to decline (<paramref name="declineOption"/>).
    ///
    /// <paramref name="describe"/> renders an item as a short option label the persona and critic read
    /// (e.g. "grab a beechnut", "an irrigation ditch", "greet them warmly"); items that describe
    /// identically collapse to one, so callers need no de-duplication pass. <paramref name="declineOption"/>
    /// is the label for the "do none of these" choice; picking it yields <c>null</c>. Pass
    /// <c>null</c> (the default) to omit the decline option entirely — the persona must then choose one
    /// of the real items, and the result is never <c>null</c> (used by dialogue, which has no refusal).
    /// </summary>
    public async Task<T?> SelectAsync<T>(
        int slotId,
        ModusMentis evaluator,
        IReadOnlyList<T> items,
        Func<T, string> describe,
        PersonaChoicePrompt prompt,
        string? declineOption = null,
        CancellationToken ct = default) where T : class
    {
        if (items == null || items.Count == 0) return null;

        // Collapse items that read identically, keeping the first of each.
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var candidates = new List<(string Label, T Item)>();
        foreach (var item in items)
        {
            var label = Normalize(describe(item));
            if (label.Length == 0 || !seen.Add(label)) continue;
            candidates.Add((label, item));
        }
        if (candidates.Count == 0) return null;

        // Playground: no LLM — pick a random real option, never declining, so the scene flows.
        if (PlaygroundMode.IsActive)
            return candidates[_random.Next(candidates.Count)].Item;

        string? decline = Normalize(declineOption ?? "");
        bool hasDecline = decline.Length > 0;

        // Keep the letter alphabet's worth of options — one slot reserved for decline when present.
        int maxReal = PersonaMatchCritic.MaxOptions - (hasDecline ? 1 : 0);
        if (candidates.Count > maxReal)
            candidates = candidates.OrderBy(_ => _random.Next()).Take(maxReal).ToList();

        Shuffle(candidates);

        // Real options, plus the decline option (a null-Item sentinel, distinct from every real T)
        // when one is offered — so the persona can genuinely opt out where the site allows it.
        var options = candidates.Select(c => (c.Label, Item: (T?)c.Item)).ToList();
        if (hasDecline) options.Add((Label: decline, Item: (T?)null));
        var labels = options.Select(o => o.Label).ToList();

        // ── Persona stage: free-text reasoning on the Modus Mentis slot ──────────
        // Reset in and out so the reasoning neither inherits nor leaks slot history.
        _llm.ResetInstance(slotId);
        string reasoning = await _llm.GenerateConstrainedStringAsync(
            slotId, BuildReasoningPrompt(prompt, labels, evaluator), gbnfGrammar: null, ReasoningMaxTokens, skipReset: false);
        reasoning = reasoning.Trim();
        if (reasoning.Length == 0) return candidates[0].Item;   // no signal → first real option

        // ── Neutral stage: the critic maps the want to one lettered option ───────
        // Attribute the reasoning to the persona and the question, so the critic knows who answered.
        string title = string.IsNullOrWhiteSpace(evaluator.PersonaReminder) ? "a character" : evaluator.PersonaReminder!;
        string attribution = $"Asking {title} {prompt.ReportedQuestion.Trim()}, they just said:";

        int idx = await PersonaMatchCritic.PickAsync(prompt.ContextText, attribution, reasoning, labels, ct);
        if (idx < 0 || idx >= options.Count) return candidates[0].Item;
        return options[idx].Item;   // null ⇒ decline was chosen
    }

    // ── Prompt ──────────────────────────────────────────────────────────────────

    private static string BuildReasoningPrompt(PersonaChoicePrompt prompt, IReadOnlyList<string> labels, ModusMentis evaluator)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(prompt.ContextText)) sb.Append(prompt.ContextText); // carries own spacing
        sb.Append("You could:\n");
        foreach (var label in labels)
            sb.Append("- ").Append(label).Append('\n');
        sb.Append('\n');

        // Persona reminder folded into the question, mirroring the rewrite prompts.
        string question = prompt.OpenQuestion.Trim();
        if (!string.IsNullOrWhiteSpace(evaluator.PersonaReminder))
            question = $"As a {evaluator.PersonaReminder}, {LowerFirst(question)}";
        sb.Append(question).Append(' ');
        sb.Append("Answer in one short sentence, in your own voice.");

        if (!string.IsNullOrWhiteSpace(evaluator.PersonaReminder2))
            sb.Append("\n\nStay in the character of ").Append(evaluator.PersonaReminder2).Append('.');
        return sb.ToString();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static string LowerFirst(string s)
        => string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s.Substring(1);

    /// <summary>Collapses whitespace so labels that differ only in spacing de-duplicate.</summary>
    private static string Normalize(string? label)
    {
        if (string.IsNullOrWhiteSpace(label)) return string.Empty;
        return Regex.Replace(label.Trim(), @"\s+", " ");
    }

    private void Shuffle<TItem>(IList<TItem> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
