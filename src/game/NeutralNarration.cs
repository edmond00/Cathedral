using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Cathedral.Game;

/// <summary>
/// Builds neutral, first-person English describing the *meaning* of a piece of narration, from
/// structured game data. This is the single source of meaning for both narration paths:
///   • in playground mode it is shown verbatim (no LLM);
///   • otherwise it is handed to <see cref="Cathedral.Game.Narrative.PersonaRewriter"/>, which asks
///     the speaker's Modus Mentis / NPC LLM slot to re-express it in persona voice while keeping
///     the meaning.
///
/// Free-form descriptions are messy (bare names, capitalised phrases, mood prefixes, proper nouns),
/// so <see cref="NounPhrase"/> cleans them into an embeddable noun phrase before templating.
/// </summary>
public static class NeutralNarration
{
    // ── Observation ────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds the attention sentence that opens an observation, naming the object by a simple noun
    /// phrase (e.g. "a straw figure", "Hugh Furrow"). The first object of a phase is "drawn to";
    /// any later object is "shifts to". The richer detail follows in <see cref="ObservationDetail"/>.
    /// </summary>
    public static string ObservationAttention(bool isFirst, string simpleName)
    {
        var s = NounPhrase(simpleName);
        return isFirst ? $"My attention is drawn to {s}."
                       : $"My attention shifts to {s}.";
    }

    /// <summary>
    /// Builds the detail sentence that follows the attention line, giving the object's richer
    /// description (e.g. "a wind-blown straw-stuffed figure"), normalised to a clean noun phrase.
    /// </summary>
    public static string ObservationDetail(string description)
        => $"This is {NounPhrase(description)}.";

    /// <summary>
    /// Builds the full neutral meaning of an observation as one text: the attention line naming the
    /// object (<see cref="ObservationAttention"/>) followed by the richer detail line
    /// (<see cref="ObservationDetail"/>). Merged so the persona rewrite can be done in a single
    /// request that yields two or three short styled sentences.
    /// </summary>
    public static string Observation(bool isFirst, string simpleName, string description)
        => $"{ObservationAttention(isFirst, simpleName)} {ObservationDetail(description)}";

    // ── Thinking ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Neutral chain-of-thought once the goal and skill have been chosen: what is noticed, what is
    /// intended, and the means that will be used. <paramref name="goalPhrase"/> is a verb phrase
    /// ("climb the tree"); <paramref name="skillMeans"/> is a Modus Mentis means description.
    /// </summary>
    public static string ReasoningChain(string targetPhrase, string goalPhrase, string skillMeans)
    {
        var parts = new List<string> { $"I notice {NounPhrase(targetPhrase)}." };
        if (!string.IsNullOrWhiteSpace(goalPhrase)) parts.Add($"I want to {goalPhrase}.");
        if (!string.IsNullOrWhiteSpace(skillMeans)) parts.Add($"I will rely on {skillMeans}.");
        return string.Join(" ", parts);
    }

    /// <summary>Neutral reasoning for the "ignore and move on" path.</summary>
    public static string ReasoningIgnore(string targetPhrase)
        => $"I notice {NounPhrase(targetPhrase)}, but I let it be and move on.";

    /// <summary>
    /// The intended action as a first-person "Let me try to …" attempt (e.g. "Let me try to climb
    /// the tree"). The styled rewrite is GBNF-forced to open with the same prefix, which is then
    /// stripped to form the button label.
    /// </summary>
    public static string ActionIntent(string verbVerbatim) => $"Let me try to {verbVerbatim}";

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

    // ── Speaking (3-part address to a companion) ───────────────────────────────

    public static string Attention(string companionName)
        => $"{companionName}, come and look at this.";

    public static string Description(string subject)
        => $"I noticed {NounPhrase(subject)}.";

    public static string Question()
        => "What do you make of it?";

    // ── Dialogue ───────────────────────────────────────────────────────────────

    /// <summary>Neutral player line for a chosen dialogue node (its intent, as direct speech).</summary>
    public static string DialoguePlayerReplica(string nodeDescription)
        => $"I want to {LowerFirst(nodeDescription.Trim().TrimEnd('.'))}.";

    /// <summary>Neutral NPC opening line for a dialogue node (the step's intent stated plainly).</summary>
    public static string NpcOpening(string treeDescription, string nodeDescription)
        => $"{Capitalize(nodeDescription.Trim().TrimEnd('.'))}.";

    /// <summary>Neutral NPC reaction after the player's replica and skill check.</summary>
    public static string NpcReaction(bool succeeded, string nodeDescription)
        => succeeded
            ? $"Very well — {LowerFirst(nodeDescription.Trim().TrimEnd('.'))}."
            : "That does not move me.";

    // ── Critic ─────────────────────────────────────────────────────────────────

    /// <summary>Impersonal — this is a critic's verdict, not character narration.</summary>
    public static string CriticFailureReason()
        => "That cannot be done as intended.";

    // ── Keyword helper ─────────────────────────────────────────────────────────

    /// <summary>
    /// Picks a keyword from a neutral description: the last whitespace-delimited word,
    /// lower-cased and stripped of surrounding punctuation. Returns null for empty input.
    /// Used as the playground/fallback keyword when the LLM does not supply one.
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

    private static string Capitalize(string s)
        => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

    private static string LowerFirst(string s)
        => string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s.Substring(1);
}
