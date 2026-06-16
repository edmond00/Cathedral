using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Cathedral.Game;

/// <summary>
/// Fixed, neutral, first-person sentence templates used by playground mode in place of
/// LLM-generated narration. No persona, no Modus Mentis name — just properly-formed English
/// built from the structured data already available at each call site.
///
/// The raw neutral descriptions these draw on are free-form (bare names without articles,
/// fully-capitalized phrases, mood-prefixed sentences, proper nouns), so <see cref="NounPhrase"/>
/// cleans them into an embeddable noun phrase before they go into a template.
/// </summary>
public static class PlaygroundNarration
{
    // ── Observation ────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds an observation sentence around a neutral description (e.g. "wind-blown A
    /// straw-stuffed figure", "wheat strip", "Hugh Furrow"), normalised to a clean noun phrase.
    /// </summary>
    public static string Observation(bool isFirst, bool isTransition, string subject)
    {
        var s = NounPhrase(subject);
        return isFirst      ? $"My attention is drawn to {s}."
             : isTransition ? $"My attention shifts to {s}."
                            : $"I look more closely at {s}.";
    }

    // ── Action outcomes ────────────────────────────────────────────────────────
    // actionDisplay is already a clean verb phrase (e.g. "climb the tree"), so it is used verbatim.

    public static string OutcomeSuccess(string actionDisplay)
        => $"I try to {actionDisplay}, and succeed.";

    public static string OutcomeFailure(string actionDisplay)
        => $"I try to {actionDisplay}, but fail.";

    public static string PlausibilityFailure(string actionDisplay)
        => $"I try to {actionDisplay}, but it cannot happen here.";

    public static string ItemCombinationFailure(string actionDisplay, string itemWithArticle)
        => $"Using {itemWithArticle} to {actionDisplay} does not work.";

    // ── Thinking ───────────────────────────────────────────────────────────────

    public static string Reasoning(string targetPhrase, string actionDisplay)
        => $"I weigh up {NounPhrase(targetPhrase)}, and resolve to {actionDisplay}.";

    // ── Speaking (3-part address to a companion) ───────────────────────────────

    public static string Attention(string companionName)
        => $"{companionName}, come and look at this.";

    public static string Description(string subject)
        => $"I noticed {NounPhrase(subject)}.";

    public static string Question()
        => "What do you make of it?";

    // ── Critic ─────────────────────────────────────────────────────────────────

    /// <summary>Impersonal — this is a critic's verdict, not character narration.</summary>
    public static string CriticFailureReason()
        => "That cannot be done as intended.";

    // ── Keyword helper ─────────────────────────────────────────────────────────

    /// <summary>
    /// Picks a keyword from a neutral description: the last whitespace-delimited word,
    /// lower-cased and stripped of surrounding punctuation. Returns null for empty input.
    /// Because observation templates embed the description (via <see cref="NounPhrase"/>, which
    /// preserves the final word), this keyword is guaranteed to appear in the rendered sentence
    /// so the case-insensitive keyword renderer can highlight it.
    /// </summary>
    public static string? KeywordFromPhrase(string? phrase)
    {
        if (string.IsNullOrWhiteSpace(phrase)) return null;
        var words = phrase.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return null;
        return words[^1].ToLowerInvariant().Trim('.', ',', '!', '?', '"', '\'', '(', ')');
    }

    // ── Noun-phrase normalisation ──────────────────────────────────────────────

    private static readonly HashSet<string> Determiners = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "some", "his", "her", "its", "their", "your", "my", "our",
        "this", "that", "these", "those", "no", "one", "two", "three", "four", "five",
        "several", "many", "few", "each", "every", "another",
    };

    /// <summary>
    /// Turns a raw neutral description into a clean noun phrase suitable for embedding mid-sentence:
    /// strips trailing punctuation, keeps proper nouns verbatim, lower-cases a sentence-initial
    /// capital, repairs mood-prefixed articles ("wind-blown a straw figure" → "a wind-blown straw
    /// figure"), and adds "a"/"an" when the phrase has no determiner and is not plural.
    /// </summary>
    public static string NounPhrase(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "something";
        var s = raw.Trim().TrimEnd('.', ',', ';', ':', '!', '?', ' ');
        if (s.Length == 0) return "something";

        var words0 = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Proper name (e.g. "Hugh Furrow", "Godric Reeve"): up to 3 words, each starting with a
        // capital and otherwise alphabetic. Kept verbatim — no lower-casing, no article.
        if (words0.Length <= 3 && words0.All(IsCapitalizedWord))
            return s;

        // Lower-case a sentence-initial capital (this is a common-noun phrase, not a name).
        s = char.ToLowerInvariant(s[0]) + s.Substring(1);

        // Lower-case stray capitalised articles left mid-phrase by a mood prefix.
        s = Regex.Replace(s, @"\b(A|An|The)\b", m => m.Value.ToLowerInvariant());

        // Mood-prefix repair: "<adj> a|an|the <rest>" → "a|an|the <adj> <rest>".
        var mood = Regex.Match(s, @"^([a-z][a-z-]*)\s+(a|an|the)\s+(.+)$");
        if (mood.Success && !Determiners.Contains(mood.Groups[1].Value))
            s = $"{mood.Groups[2].Value} {mood.Groups[1].Value} {mood.Groups[3].Value}";

        // Decide whether a leading article is needed.
        var words = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        bool hasDeterminer = words.Take(3).Any(w => Determiners.Contains(w.Trim('-')));
        string last = words[^1].ToLowerInvariant();
        bool looksPlural = last.Length > 2 && last.EndsWith("s") && !last.EndsWith("ss");

        if (!hasDeterminer && !looksPlural)
        {
            string article = "aeiou".IndexOf(char.ToLowerInvariant(s[0])) >= 0 ? "an" : "a";
            s = $"{article} {s}";
        }
        return s;
    }

    private static bool IsCapitalizedWord(string w)
        => w.Length > 0 && char.IsUpper(w[0]) && w.All(c => char.IsLetter(c) || c == '-' || c == '\'');
}
