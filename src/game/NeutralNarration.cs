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
    /// any later object is "shifts to". During the childhood reminescence phase
    /// (<paramref name="isReminescence"/>) both are reworded as memory surfacing/drifting rather
    /// than attention being drawn/shifted. The richer detail follows in
    /// <see cref="ObservationDetail"/>.
    /// </summary>
    public static string ObservationAttention(bool isFirst, string simpleName, bool isReminescence = false)
    {
        var s = NounPhrase(FirstPerson(simpleName));
        if (isFirst)
            return isReminescence ? $"A memory surfaces: {s}."
                                  : $"My attention is drawn to {s}.";
        return isReminescence ? $"My memory drifts to {s}."
                              : $"My attention shifts to {s}.";
    }

    /// <summary>
    /// Builds the detail sentence that follows the attention line, giving the object's richer
    /// description (e.g. "a wind-blown straw-stuffed figure"), normalised to a clean noun phrase.
    /// During the childhood reminescence phase (<paramref name="isReminescence"/>) the fragment is
    /// a recollection, so the sentence is put in the past tense ("This was …").
    /// </summary>
    public static string ObservationDetail(string description, bool isReminescence = false)
        => isReminescence ? $"This was {NounPhrase(FirstPerson(description))}."
                          : $"This is {NounPhrase(FirstPerson(description))}.";

    /// <summary>
    /// Builds the full neutral meaning of an observation as one text: the attention line naming the
    /// object (<see cref="ObservationAttention"/>) followed by the richer detail line
    /// (<see cref="ObservationDetail"/>). Merged so the persona rewrite can be done in a single
    /// request that yields two or three short styled sentences.
    /// </summary>
    public static string Observation(bool isFirst, string simpleName, string description, bool isReminescence = false)
        => $"{ObservationAttention(isFirst, simpleName, isReminescence)} {ObservationDetail(description, isReminescence)}";

    // ── Thinking ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Neutral chain-of-thought once the goal and skill have been chosen: what is noticed, what is
    /// intended, and the means that will be used. <paramref name="goalPhrase"/> is a verb phrase
    /// ("climb the tree"); <paramref name="skillMeans"/> is a Modus Mentis means description.
    /// During the childhood reminescence phase (<paramref name="isReminescence"/>) the opener is
    /// "I remember …" rather than "I notice …", since the object is a surfacing memory.
    /// </summary>
    public static string ReasoningChain(string targetPhrase, string goalPhrase, string skillMeans, bool isReminescence = false)
    {
        var opener = isReminescence ? "I remember" : "I notice";
        var parts = new List<string> { $"{opener} {NounPhrase(FirstPerson(targetPhrase))}." };
        if (!string.IsNullOrWhiteSpace(goalPhrase)) parts.Add($"I want to {FirstPerson(goalPhrase)}.");
        if (!string.IsNullOrWhiteSpace(skillMeans)) parts.Add($"I will rely on {FirstPerson(skillMeans)}.");
        return string.Join(" ", parts);
    }

    /// <summary>Neutral reasoning for the "ignore and move on" path.</summary>
    public static string ReasoningIgnore(string targetPhrase, bool isReminescence = false)
        => isReminescence
            ? $"I remember {NounPhrase(FirstPerson(targetPhrase))}, but I let it be and move on."
            : $"I notice {NounPhrase(FirstPerson(targetPhrase))}, but I let it be and move on.";

    /// <summary>
    /// The intended action as a first-person "I will …" statement (e.g. "I will climb the tree").
    /// The styled rewrite is GBNF-forced to open with the same prefix, which is then stripped to
    /// form the button label.
    /// </summary>
    public static string ActionIntent(string verbVerbatim) => $"I will {FirstPerson(verbVerbatim)}";

    // ── Action outcomes ────────────────────────────────────────────────────────
    // actionDisplay is already a clean verb phrase (e.g. "climb the tree"), so it is used verbatim.

    public static string OutcomeSuccess(string actionDisplay)
        => $"I tried to {FirstPerson(actionDisplay)}, and succeeded.";

    public static string OutcomeFailure(string actionDisplay)
        => $"I tried to {FirstPerson(actionDisplay)}, but failed.";

    public static string PlausibilityFailure(string actionDisplay)
        => $"I tried to {FirstPerson(actionDisplay)}, but it could not happen here.";

    public static string ItemCombinationFailure(string actionDisplay, string itemWithArticle)
        => $"I tried to use {FirstPerson(itemWithArticle)} to {FirstPerson(actionDisplay)}, but it did not work.";

    // ── Reminescence outcome ───────────────────────────────────────────────────

    /// <summary>
    /// Neutral success sentence for the childhood reminescence REMEMBER action. Unlike a normal
    /// action outcome (which templates the styled action label), this uses a plain "I tried to
    /// remember …, and succeeded." framing and then states the concrete memory that surfaces —
    /// <paramref name="memoryEvent"/> is the fragment's <c>OutcomeText</c>, converted to first
    /// person so the whole recollection reads in the protagonist's own voice.
    /// </summary>
    public static string ReminescenceOutcome(string fragmentName, string memoryEvent)
    {
        var name   = FirstPerson((fragmentName ?? "").Trim());
        var memory = FirstPerson((memoryEvent ?? "").Trim().TrimEnd('.'));
        return $"I tried to remember {name} from my childhood, and succeeded. It came back to me: {memory}.";
    }

    // ── First-person normalisation ─────────────────────────────────────────────

    /// <summary>Prepositions/particles that mark a following-or-preceding "you" as an object ("me").</summary>
    private static readonly string[] ObjectMarkers =
    {
        "to", "toward", "towards", "with", "into", "onto", "for", "from", "at", "of", "on",
        "upon", "around", "near", "behind", "below", "beneath", "beside", "against", "through",
        "over", "under", "about", "before", "after", "past", "beyond",
    };

    /// <summary>
    /// Rewrites second-person content into the protagonist's first-person voice
    /// ("you spent your childhood" → "I spent my childhood", "pulling you toward sleep" →
    /// "pulling me toward sleep"). Much of the authored content that fills neutral self-narration
    /// templates — most of all the childhood reminescence catalog — is written in the second person
    /// for prompt-framing, so every self-POV neutral sentence is normalised through here before it is
    /// shown or handed to the persona rewriter. It is deliberately NOT applied to dialogue/speaking
    /// templates, where "you" correctly addresses another character.
    ///
    /// The subject/object distinction is heuristic: a "you" adjacent to a preposition (either "below
    /// you" or "you toward …") is treated as an object ("me"); any other "you" is a subject ("I").
    /// </summary>
    public static string FirstPerson(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return s ?? "";

        // Be-verb agreement must run before the generic you→I so we don't produce "I are".
        s = Regex.Replace(s, @"\byou['’]re\b", "I'm",  RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"\byou are\b",   "I am",  RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"\byou were\b",  "I was", RegexOptions.IgnoreCase);

        // Possessives / reflexive first (leaves subject/object "you" for the passes below).
        s = Regex.Replace(s, @"\byourself\b", "myself", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"\byours\b",    "mine",   RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"\byour\b",     "my",     RegexOptions.IgnoreCase);

        // Object "you" → "me": trailing ("below you") or immediately before a preposition
        // ("you toward sleep", "watching you with suspicion").
        var markers = string.Join("|", ObjectMarkers);
        s = Regex.Replace(s, @"\byou\b(?=\s*(?:[.,;:!?)\]—-]|$))",         "me", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, $@"\byou\b(?=\s+(?:{markers})\b)",            "me", RegexOptions.IgnoreCase);

        // Any remaining "you" is a subject → "I".
        s = Regex.Replace(s, @"\byou\b", "I", RegexOptions.IgnoreCase);
        return s;
    }

    // ── Speaking (3-part address to a companion) ───────────────────────────────

    public static string Attention(string companionName)
        => $"{companionName}, come and look at this.";

    public static string Description(string subject)
        => $"I noticed {NounPhrase(FirstPerson(subject))}.";

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
