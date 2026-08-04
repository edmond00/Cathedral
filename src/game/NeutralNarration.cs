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

    /// <summary>
    /// Trailing sentence appended to a threatening enemy's observation neutral text: it flags the
    /// just-described object as a present danger so the observation persona rewrites a note of caution
    /// into the block. Used only when the first observation of a phase leads with a same-area enemy
    /// (the "under threat" opener), and kept first-person so it merges into the observation voice.
    /// </summary>
    public static string ThreatCaution()
        => "This one means me harm, right here — I must stay wary and ready.";

    /// <summary>
    /// Neutral meaning for a failed observation: the Modus Mentis found nothing here worth its
    /// attention (every candidate object was graded "averse" in the persona evaluation). Re-expressed
    /// in the observation persona's voice as the whole observation block.
    /// </summary>
    public static string ObservationNothing(bool isReminescence = false)
        => isReminescence ? "Nothing surfaces from my memory here."
                          : "Nothing here draws my attention.";

    /// <summary>
    /// Neutral meaning for a refused focus: a (new) observation Modus Mentis was handed
    /// <paramref name="targetPhrase"/> (already articled) to focus on, and chose to lose interest
    /// instead of observing it. Re-expressed in that persona's voice as the whole focus block —
    /// no detail, no clickable keyword.
    /// </summary>
    public static string ObservationNotInterested(string targetPhrase, bool isReminescence = false)
        => isReminescence
            ? $"I am not interested in the memory of {targetPhrase}, and let it fade."
            : $"I am not interested in {targetPhrase}, and turn my attention away.";

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

    /// <summary>
    /// Neutral reasoning for the "ignore and move on" path: the thinking Modus Mentis was offered the
    /// goals this object affords and settled on none of them — either because the object affords
    /// nothing, or because its free reasoning wandered to something that was never on the list, which
    /// the match critic reports through its catch-all option (see <c>ThinkingExecutor.ChooseGoalAsync</c>).
    /// Both mean the same thing to the player: nothing here is worth acting on, look elsewhere.
    /// </summary>
    public static string ReasoningIgnore(string targetPhrase, bool isReminescence = false)
        => isReminescence
            ? $"I remember {NounPhrase(FirstPerson(targetPhrase))}, but there is nothing in it worth doing, and I would rather let my mind turn to something else."
            : $"I notice {NounPhrase(FirstPerson(targetPhrase))}, but there is nothing here worth doing, and I would rather turn my attention to something else.";

    /// <summary>
    /// Neutral reasoning for the "no way to do it" path: the goal was chosen but the thinking Modus
    /// Mentis judged that none of the available action skills fit (every one graded "averse" in the
    /// willingness evaluation), so the intent is dropped. Reasoning-only, no action follows.
    /// </summary>
    public static string ReasoningNoMeans(string targetPhrase, string goalPhrase, bool isReminescence = false)
    {
        var opener = isReminescence ? "I remember" : "I notice";
        return $"{opener} {NounPhrase(FirstPerson(targetPhrase))}. I want to {FirstPerson(goalPhrase)}, but I find no way to do it, and let it go.";
    }

    /// <summary>
    /// The intended action as a first-person "I will …" statement (e.g. "I will climb the tree").
    /// When <paramref name="discrete"/> is true the adverb "discretely" is inserted ("I will
    /// discretely climb the tree") to reflect a stealthy modus mentis. The styled rewrite is
    /// GBNF-forced to open with the "I will " prefix, which is then stripped to form the button label.
    /// </summary>
    public static string ActionIntent(string verbVerbatim, bool discrete = false)
        => discrete ? $"I will discretely {FirstPerson(verbVerbatim)}"
                    : $"I will {FirstPerson(verbVerbatim)}";

    /// <summary>
    /// First-person refusal used when the action modus mentis is too reluctant/opposed to attempt the
    /// action (persona-fit cancellation). Rewritten in the skill's voice as the outcome narration.
    /// </summary>
    public static string ActionRefusal(string verbVerbatim) => $"I don't want to {FirstPerson(verbVerbatim)}.";

    // ── Action outcomes ────────────────────────────────────────────────────────
    // actionDisplay is already a clean verb phrase (e.g. "climb the tree"), so it is used verbatim.

    public static string OutcomeSuccess(string actionDisplay, IReadOnlyList<string>? outcomeVerbatims = null)
    {
        var head = $"It is done! I succeeded to {FirstPerson(actionDisplay)}.";
        var tail = OutcomeConsequences(outcomeVerbatims);
        return tail.Length == 0 ? head : $"{head} Thanks to this success I {tail}.";
    }

    public static string OutcomeFailure(string actionDisplay, IReadOnlyList<string>? outcomeVerbatims = null)
    {
        var head = $"Alas, I failed to {FirstPerson(actionDisplay)}.";
        var tail = OutcomeConsequences(outcomeVerbatims);
        return tail.Length == 0 ? head : $"{head} Due to this failure I {tail}.";
    }

    /// <summary>
    /// Joins the outcome reports' <c>Verbatim</c> phrases into a single comma-separated clause that
    /// reads grammatically after "I " (e.g. "obtained a gold coin, learned Bargaining"). Empty
    /// verbatims (internal bookkeeping reports) are dropped; returns "" when nothing is left, so the
    /// caller can omit the consequence clause entirely.
    /// </summary>
    private static string OutcomeConsequences(IReadOnlyList<string>? outcomeVerbatims)
    {
        if (outcomeVerbatims == null || outcomeVerbatims.Count == 0) return "";
        var parts = outcomeVerbatims
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim());
        return string.Join(", ", parts);
    }

    public static string PlausibilityFailure(string actionDisplay)
        => $"I tried to {FirstPerson(actionDisplay)}, but it could not happen here.";

    /// <summary>
    /// Refusal for a coded-rule block: the character declines an action they cannot take, with a
    /// first-person reason (e.g. a witness present, an enemy at hand). Re-expressed in the acting
    /// modus mentis's voice as the [IMPOSSIBLE] narration.
    /// </summary>
    public static string ActionImpossible(string actionDisplay, string reason)
    {
        var r = (reason ?? "").Trim();
        if (r.Length == 0) return $"I cannot {FirstPerson(actionDisplay)} here.";
        return $"I cannot {FirstPerson(actionDisplay)}: {FirstPerson(r)}";
    }

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

    // Dialogue neutral text now lives on the dialogue tree nodes themselves (direct speech with
    // {scope:field} template tokens) and flows through Cathedral.Game.Dialogue.Runtime.DialogueTemplate
    // + DialogueReplicaWriter — not through this class.

    // ── Critic ─────────────────────────────────────────────────────────────────

    /// <summary>First-person critic verdict ("I think …"), matching the LLM reason's voice.</summary>
    public static string CriticFailureReason()
        => "I think this cannot be done as intended.";

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
}
