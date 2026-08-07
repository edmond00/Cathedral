using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cathedral;
using Cathedral.Game.Narrative.Preview;
using Cathedral.Game.Narrative.Sanitizer;
using Cathedral.LLM;
using Cathedral.LLM.JsonConstraints;

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
public readonly record struct PersonaChoicePrompt(
    string ContextText, string OpenQuestion, string ReportedQuestion,
    PersonaOpening Opening = PersonaOpening.Intent);

/// <summary>
/// Which set of openings a site's free-text reasoning is constrained to (GBNF) and told to use (prompt).
/// <list type="bullet">
/// <item><see cref="Intent"/> — a site that asks <i>what</i> the persona chooses ("What do you want to
///       do?", "What do you want to say?"). The answer must state a want, not narrate a deed: asked
///       only to begin with "I", a small model answers "I attack Pig" — the present tense of something
///       already happening, when nothing has happened yet and the choice is still being made.</item>
/// <item><see cref="Willingness"/> — a site that asks <i>whether</i> the persona will ("Do you want to
///       do it?"), whose options are bare adjectives ("reluctant to do it"). Beginning with "I" alone,
///       the model glues the label straight on and produces "I reluctant to do it."; the copula and the
///       modals give it somewhere grammatical to land.</item>
/// </list>
/// </summary>
public enum PersonaOpening { Intent, Willingness }

/// <summary>The literal openings and the prompt wording behind each <see cref="PersonaOpening"/>.</summary>
public static class PersonaOpenings
{
    /// <summary>The GBNF alternatives, trailing space included — the output starts with exactly one.</summary>
    public static IReadOnlyList<string> Prefixes(PersonaOpening opening) => opening switch
    {
        PersonaOpening.Willingness => WillingnessPrefixes,
        _                          => IntentPrefixes,
    };

    /// <summary>The same set as the prompt asks for it, so instruction and grammar cannot disagree.</summary>
    public static string PromptPhrase(PersonaOpening opening) => Quote(Prefixes(opening));

    private static readonly string[] IntentPrefixes =
        { "I want ", "I would ", "I could ", "I can " };

    private static readonly string[] WillingnessPrefixes =
        { "I am ", "I want ", "I can ", "I could ", "I should ", "I don't ", "I can't ", "I shouldn't " };

    private static string Quote(IReadOnlyList<string> prefixes)
    {
        var quoted = prefixes.Select(p => $"\"{p.TrimEnd()}\"").ToList();
        return quoted.Count == 1
            ? quoted[0]
            : string.Join(", ", quoted.Take(quoted.Count - 1)) + " or " + quoted[^1];
    }
}

/// <summary>
/// Result of a persona choice: the picked <see cref="Item"/> (<c>null</c> ⇒ the decline option was
/// chosen, or there was nothing to choose) and the persona's free-text <see cref="Reasoning"/> — its
/// in-voice answer to the open question, captured before the critic mapped it to an option. Callers
/// reuse the reasoning as an inner-thought hint in the follow-up rewrite, so the prose that narrates
/// the choice echoes the want that made it. <c>null</c> in playground mode and on no-LLM fallbacks.
/// </summary>
public readonly record struct PersonaChoice<T>(T? Item, string? Reasoning) where T : class;

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
/// failure path (nothing to observe / ignore &amp; move on / no way to act / stay silent) fires. That
/// decline can also be <i>hidden from the persona</i> and offered to the critic alone, as a catch-all
/// for a want that matches no real option; see the <c>declineHiddenFromPersona</c> parameter. In
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

    /// <summary>
    /// Character bounds on the (grammar-constrained) reasoning body after the forced "I " opening.
    /// The token limit above is the practical stop; the max is just a safe grammar ceiling.
    /// </summary>
    private const int ReasoningMinChars = 4;
    private const int ReasoningMaxChars = 600;

    /// <summary>
    /// GBNF for the persona's free-text reasoning: restricted to the same body charset as the persona
    /// rewrites — the double-quote among the characters it excludes, so the thought cannot come back
    /// quoting itself or one of the options it was shown. Forced to open with one of the site's
    /// <see cref="PersonaOpening"/> literals, so it reads as a first-person <i>want</i> or <i>stance</i>
    /// rather than as narration of a deed. Raw text (not JSON) — it is consumed directly as the
    /// reasoning string and streamed into the preview as inner thought.
    /// </summary>
    private static readonly Dictionary<PersonaOpening, string> ReasoningGrammars =
        Enum.GetValues<PersonaOpening>().ToDictionary(
            o => o,
            o => JsonConstraintGenerator.GenerateRawTextGrammar(
                     PersonaOpenings.Prefixes(o), ReasoningMinChars, ReasoningMaxChars));

    private readonly LlamaServerManager _llm;
    private readonly Random _random = GameRng.Stream("persona-choice");

    public PersonaChoiceSelector(LlamaServerManager llm) => _llm = llm ?? throw new ArgumentNullException(nameof(llm));

    /// <summary>
    /// Runs the persona reasoning on <paramref name="slotId"/> (whose system prompt carries
    /// <paramref name="evaluator"/>'s persona) and the neutral match on the shared critic, returning
    /// the chosen item — <c>null</c> when the persona chose to decline (<paramref name="declineOption"/>)
    /// — together with the persona's free-text reasoning (see <see cref="PersonaChoice{T}"/>).
    ///
    /// <paramref name="describe"/> renders an item as a short option label the persona and critic read
    /// (e.g. "grab a beechnut", "an irrigation ditch", "greet them warmly"); items that describe
    /// identically collapse to one, so callers need no de-duplication pass. <paramref name="declineOption"/>
    /// is the label for the "do none of these" choice; picking it yields a <c>null</c> item. Pass
    /// <c>null</c> (the default) to omit the decline option entirely — the persona must then choose one
    /// of the real items, and the item is never <c>null</c> (used by dialogue, which has no refusal).
    ///
    /// <para><paramref name="declineHiddenFromPersona"/> makes the decline option a <b>critic-only
    /// catch-all</b>: the persona is shown the real options alone and answers as if it had to choose
    /// one of them, but the critic gets the extra letter and is told to use it when the want matches no
    /// listed target. That covers the persona answering with something it was never offered ("I choose
    /// to mark the boy by the wall instead") — without it the critic must force that onto a real option,
    /// and the character acts on an intent nobody expressed. The visible form is for sites where
    /// declining is itself one of the persona's choices.</para>
    /// </summary>
    public async Task<PersonaChoice<T>> SelectAsync<T>(
        int slotId,
        ModusMentis evaluator,
        IReadOnlyList<T> items,
        Func<T, string> describe,
        PersonaChoicePrompt prompt,
        string? declineOption = null,
        bool declineHiddenFromPersona = false,
        ILlmPreviewSink? preview = null,
        CancellationToken ct = default) where T : class
    {
        if (items == null || items.Count == 0) return new PersonaChoice<T>(null, null);

        // Collapse items that read identically, keeping the first of each.
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var candidates = new List<(string Label, T Item)>();
        foreach (var item in items)
        {
            var label = Normalize(describe(item));
            if (label.Length == 0 || !seen.Add(label)) continue;
            candidates.Add((label, item));
        }
        if (candidates.Count == 0) return new PersonaChoice<T>(null, null);

        // Playground: no LLM — pick a random real option, never declining, so the scene flows.
        if (PlaygroundMode.IsActive)
            return new PersonaChoice<T>(candidates[_random.Next(candidates.Count)].Item, null);

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
        var criticLabels = options.Select(o => o.Label).ToList();

        // A hidden decline is never listed to the persona: it answers over the real options only, and
        // the extra letter exists solely so the critic can report "that matched none of them".
        bool hiddenDecline = hasDecline && declineHiddenFromPersona;
        var labels = hiddenDecline ? criticLabels.Take(candidates.Count).ToList() : criticLabels;

        // ── Persona stage: free-text reasoning on the Modus Mentis slot ──────────
        // Reset in and out so the reasoning neither inherits nor leaks slot history. When a preview
        // sink is supplied, the reasoning streams into the box as a dimmer parenthesized "inner thought"
        // preceding the rewrite it will hint.
        _llm.ResetInstance(slotId);
        string reasoningPrompt = BuildReasoningPrompt(prompt, labels, evaluator);
        // The option labels and the situation are game-authored, and the reasoning quotes them back
        // constantly ("I want to look at the square-mill lane"). Exempt them from the preview's live
        // sanitizer gate, or the box freezes on a name the game itself chose.
        preview?.OnSourceVocabulary(SourceVocabulary.From(labels.Append(prompt.ContextText)));
        string grammar = ReasoningGrammars[prompt.Opening];
        string reasoning = preview != null
            ? await _llm.GenerateConstrainedStringStreamingAsync(
                  slotId, reasoningPrompt, gbnfGrammar: grammar, ReasoningMaxTokens, skipReset: false,
                  onTokenStreamed: (t, _) => preview.OnToken(t))
            : await _llm.GenerateConstrainedStringAsync(
                  slotId, reasoningPrompt, gbnfGrammar: grammar, ReasoningMaxTokens, skipReset: false);
        reasoning = reasoning.Trim();
        preview?.OnComplete(reasoning);
        if (reasoning.Length == 0) return new PersonaChoice<T>(candidates[0].Item, null);   // no signal → first real option

        // ── Neutral stage: the critic maps the want to one lettered option ───────
        // The critic is an exterior observer: it introduces the persona in the third person and
        // converts the shared second-person context/labels itself (see PersonaMatchCritic).
        string title = string.IsNullOrWhiteSpace(evaluator.PersonaReminder) ? "" : $"a {evaluator.PersonaReminder}";

        int idx = await PersonaMatchCritic.PickAsync(
            prompt.ContextText, title, prompt.ReportedQuestion.Trim(), reasoning, criticLabels,
            lastOptionIsCatchAll: hiddenDecline, ct: ct);
        if (idx < 0 || idx >= options.Count) return new PersonaChoice<T>(candidates[0].Item, reasoning);
        return new PersonaChoice<T>(options[idx].Item, reasoning);   // null item ⇒ decline was chosen
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
        // Guard against confabulation: the small model tends to answer a "what do you want" question by
        // narrating a whole invented scene ("...asking about his day as he shares tales of the harvest").
        // Pin it to the choice itself; anything beyond that must be framed as private thought, not fact.
        sb.Append(
            "Your one purpose is to answer that question — name which of the options above you choose. " +
            "You may colour the answer with a brief inner thought (a feeling, a flicker of reasoning) to stay in character, " +
            "but do NOT invent facts or events: do not narrate anything you say or do, or anything that happens in the world " +
            "(no conversations, no actions, no outcomes). Whatever you imagine beyond the plain choice must be phrased as an " +
            "inner thought — what you privately think or feel — never stated as something that is actually happening. " +
            "Nothing has happened yet: you are still deciding, so say what you want or intend, never what you are doing. " +
            // The same set the grammar forces (PersonaOpenings), so instruction and constraint agree —
            // a model told "begin with I" and forced to begin with "I want " spends its first tokens
            // fighting the grammar.
            $"Answer in one short sentence, in your own voice, beginning with {PersonaOpenings.PromptPhrase(prompt.Opening)}.");

        if (!string.IsNullOrWhiteSpace(evaluator.PersonaReminder2))
            sb.Append("\n\nStay in the character of ").Append(evaluator.PersonaReminder2).Append('.');

        // Same closing setting reminder as the rewrite prompts (see Config.Narrative). The inner
        // thought this request produces is shown to the player and hints the rewrite that follows, so
        // a modern image invented here travels onward. The place is restated even though ContextText
        // opened with it — both read it from the scene's WorldContext, so they cannot disagree, and on
        // a small model the last line of the prompt is the one that holds.
        sb.Append(' ').Append(SceneSetting.Reminder());
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
