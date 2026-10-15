using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cathedral;
using Cathedral.LLM;

namespace Cathedral.Game.Narrative;

/// <summary>
/// A modus mentis's choice among a list of options, made by the neutral <see cref="ChoiceJudge"/>:
/// <list type="number">
/// <item>render the options — the real ones plus, where the site allows it, a <i>decline</i> option
///       ("let your focus go", "do something else entirely") — in one shuffled order, to defeat
///       position bias; the decline always last, and a willingness scale in its own order, since an
///       ordinal scale read out of order is harder to judge than one read in it;</item>
/// <item>hand the modus mentis, the situation and the lettered list to the judge for
///       <see cref="ChoiceKind"/>, which answers with one letter.</item>
/// </list>
/// If the judge lands on the decline option the call returns <c>null</c> — the caller's existing
/// failure path (nothing to observe / ignore &amp; move on / refusal) fires. In playground mode it
/// short-circuits to a random real option (never declines) with no LLM consulted.
///
/// <para>The modus mentis is not asked to answer in its own voice first, and nothing it might have
/// said is carried into the narration that follows: the rewrite of the chosen line is the only place
/// its voice is heard. See <see cref="ChoiceJudge"/> for why.</para>
///
/// <para>At most 26 options reach the judge (one letter each); if there are more, the real options
/// are sampled down first.</para>
/// </summary>
public class PersonaChoiceSelector
{
    private readonly Random _random = GameRng.Stream("persona-choice");

    public PersonaChoiceSelector(LlamaServerManager llm)
    {
        if (llm == null) throw new ArgumentNullException(nameof(llm));
    }

    /// <summary>
    /// Asks the judge which of <paramref name="items"/> <paramref name="evaluator"/> would choose,
    /// returning <c>null</c> when it chose to decline (<paramref name="declineOption"/>) or there was
    /// nothing to choose.
    ///
    /// <paramref name="describe"/> renders an item as a short option label in the second person
    /// ("grab a beechnut", "focus on an irrigation ditch", "greet them warmly"); items that describe
    /// identically collapse to one, so callers need no de-duplication pass.
    /// <paramref name="situation"/> is the second-person preamble ("You are in a field, in the wheat
    /// strip. Your attention is on …"). Pass no <paramref name="declineOption"/> to make the choice
    /// mandatory — the item is then never <c>null</c> (dialogue and means have no refusal).
    /// </summary>
    public async Task<T?> SelectAsync<T>(
        ChoiceKind kind,
        ModusMentis evaluator,
        IReadOnlyList<T> items,
        Func<T, string> describe,
        string situation,
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

        string decline = Normalize(declineOption ?? "");
        bool hasDecline = decline.Length > 0;

        // Keep the letter alphabet's worth of options — one slot reserved for decline when present.
        int maxReal = ChoiceJudge.MaxOptions - (hasDecline ? 1 : 0);
        if (candidates.Count > maxReal)
            candidates = candidates.OrderBy(_ => _random.Next()).Take(maxReal).ToList();

        if (kind != ChoiceKind.Willingness) Shuffle(candidates);

        // Real options, plus the decline option (a null-Item sentinel) last.
        var options = candidates.Select(c => (c.Label, Item: (T?)c.Item)).ToList();
        if (hasDecline) options.Add((Label: decline, Item: (T?)null));

        int idx = await ChoiceJudge.PickAsync(kind, evaluator, situation, options.Select(o => o.Label).ToList(), ct);
        return options[idx].Item;   // null item ⇒ decline was chosen
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

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
