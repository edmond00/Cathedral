using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cathedral;
using Cathedral.LLM;
using Cathedral.LLM.JsonConstraints;

namespace Cathedral.Game.Narrative;

/// <summary>
/// The situational + framing pieces a graded persona evaluation needs, on top of the option list
/// (which <see cref="PersonaChoiceSelector"/> renders itself, shuffled):
/// <list type="bullet">
/// <item><see cref="ContextText"/> — situational preamble; may already end with its own spacing, or "".</item>
/// <item><see cref="ListHeader"/> — the line introducing the options (e.g. "Around you, you notice:").</item>
/// <item><see cref="InterestPhrase"/> — completes "grade how strongly it …" (e.g. "draws your attention").</item>
/// </list>
/// </summary>
public readonly record struct GradeEvalPromptParts(string ContextText, string ListHeader, string InterestPhrase);

/// <summary>
/// Persona-driven option selection via a per-option graded evaluation, replacing the single-pick enum
/// choice (which suffered from prose-funnel and positional/first-token bias). The flow, per call:
/// <list type="number">
/// <item>shuffle the option order (prompt list and GBNF key order together) to defeat order bias;</item>
/// <item>ask the Modus Mentis to grade every option <c>high</c>/<c>medium</c>/<c>low</c> for how well
///       it matches the persona;</item>
/// <item>sample the final choice(s) from the <c>high</c> options; if there are none, from the
///       <c>medium</c> options — so a scene rarely comes up empty.</item>
/// </list>
/// Returns an empty list only when <b>every</b> option is graded <c>low</c> — the caller treats that
/// as its site-specific failure (nothing to observe / ignore &amp; move on / no way to act). In
/// playground mode it behaves as a neutral random selector that never declines (no LLM consulted).
/// </summary>
public class PersonaChoiceSelector
{
    private readonly LlamaServerManager _llm;
    private readonly Random _random = new();

    public PersonaChoiceSelector(LlamaServerManager llm) => _llm = llm ?? throw new ArgumentNullException(nameof(llm));

    /// <summary>
    /// Runs the graded evaluation on <paramref name="slotId"/> (whose system prompt carries
    /// <paramref name="evaluator"/>'s persona) over <paramref name="options"/>, and returns up to
    /// <paramref name="pick"/> option strings sampled at random from the best-graded tier: the
    /// <c>high</c> options, or the <c>medium</c> options when there are no <c>high</c>. Empty result ⇒
    /// every option was <c>low</c> (failure). <paramref name="keepHistory"/> preserves the slot's
    /// conversation for a follow-up rewrite (maps to <c>skipReset</c>).
    /// </summary>
    public async Task<IReadOnlyList<string>> SelectAsync(
        int slotId,
        ModusMentis evaluator,
        IReadOnlyList<string> options,
        GradeEvalPromptParts parts,
        int pick = 1,
        bool keepHistory = false,
        CancellationToken ct = default)
    {
        if (options == null || options.Count == 0) return Array.Empty<string>();

        // Sanitize option → JSON key (strip characters that would break the GBNF key literal),
        // de-duplicating collisions and keeping a map back to the original option string.
        var keyToOption = new Dictionary<string, string>(StringComparer.Ordinal);
        var orderedKeys = new List<string>();
        foreach (var opt in options)
        {
            var key = SanitizeKey(opt);
            if (key.Length == 0 || keyToOption.ContainsKey(key)) continue;
            keyToOption[key] = opt;
            orderedKeys.Add(key);
        }
        if (orderedKeys.Count == 0) return Array.Empty<string>();

        // Playground: no LLM — behave as a neutral random selector that never declines.
        if (PlaygroundMode.IsActive)
            return SampleDistinct(orderedKeys.Select(k => keyToOption[k]).ToList(), pick);

        // Shuffle key order (prompt list + GBNF key order move together) to defeat position bias.
        Shuffle(orderedKeys);

        string prompt = BuildPrompt(orderedKeys, evaluator, parts);
        string gbnf = JsonConstraintGenerator.GenerateGBNF(LLMSchemaConfig.CreateGradeEvalSchema(orderedKeys));
        int maxTokens = Math.Clamp(orderedKeys.Count * 16 + 24, 32, 512);

        string json = await _llm.GenerateConstrainedStringAsync(slotId, prompt, gbnf, maxTokens, skipReset: keepHistory);

        var (high, medium) = ParseGrades(json, orderedKeys, keyToOption);
        var pool = high.Count > 0 ? high : medium;          // prefer "high", fall back to "medium"
        if (pool.Count == 0) return Array.Empty<string>();  // all "low" → failure; caller decides

        return SampleDistinct(pool, pick);
    }

    // ── Prompt ──────────────────────────────────────────────────────────────────

    private static string BuildPrompt(IReadOnlyList<string> orderedKeys, ModusMentis evaluator, GradeEvalPromptParts parts)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(parts.ContextText)) sb.Append(parts.ContextText); // carries its own trailing spacing
        if (!string.IsNullOrWhiteSpace(parts.ListHeader)) sb.Append(parts.ListHeader.TrimEnd()).Append('\n');
        foreach (var k in orderedKeys) sb.Append("- ").Append(k).Append('\n');
        sb.Append('\n');
        sb.Append(Config.Narrative.GradeEvalInstruction(evaluator.PersonaReminder, parts.InterestPhrase));
        return sb.ToString();
    }

    // ── Parsing ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Reads the graded JSON and returns the original option strings bucketed into the "high" and
    /// "medium" tiers (in option order); "low" (or missing/unknown) options are dropped.
    /// </summary>
    private static (List<string> high, List<string> medium) ParseGrades(
        string? json, IReadOnlyList<string> orderedKeys, IReadOnlyDictionary<string, string> keyToOption)
    {
        var high = new List<string>();
        var medium = new List<string>();
        if (string.IsNullOrWhiteSpace(json)) return (high, medium);
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            foreach (var key in orderedKeys)
            {
                if (!root.TryGetProperty(key, out var v) || v.ValueKind != JsonValueKind.String) continue;
                var grade = v.GetString();
                if (string.Equals(grade, "high", StringComparison.OrdinalIgnoreCase))
                    high.Add(keyToOption[key]);
                else if (string.Equals(grade, "medium", StringComparison.OrdinalIgnoreCase))
                    medium.Add(keyToOption[key]);
                // "low" (and anything unexpected) → dropped
            }
        }
        catch (JsonException) { /* GBNF should prevent this; treat as all-low if it happens */ }
        return (high, medium);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Removes characters that would break the JSON-key GBNF literal (backslash, double-quote) and
    /// collapses whitespace. Options rarely contain these, so the sanitized key stays readable; the
    /// selector still returns the ORIGINAL option string so the caller's mapping is unaffected.
    /// </summary>
    private static string SanitizeKey(string option)
    {
        if (string.IsNullOrWhiteSpace(option)) return string.Empty;
        var cleaned = option.Replace("\\", " ").Replace("\"", " ").Trim();
        return Regex.Replace(cleaned, @"\s+", " ");
    }

    private void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary>Returns up to <paramref name="pick"/> distinct entries drawn uniformly at random.</summary>
    private IReadOnlyList<string> SampleDistinct(List<string> pool, int pick)
    {
        Shuffle(pool);
        return pick >= pool.Count ? pool : pool.Take(pick).ToList();
    }
}
