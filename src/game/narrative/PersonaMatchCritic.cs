using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cathedral.LLM;

namespace Cathedral.Game.Narrative;

/// <summary>
/// The PERSONA-MATCH CRITIC: a single shared LLM instance whose system prompt makes it a neutral,
/// literal matcher — the opposite of the flavoured Modus Mentis slots. Every persona choice runs in
/// two stages (see <see cref="PersonaChoiceSelector"/>): the Modus Mentis answers an open question in
/// its own voice (free text), then this critic reads that reasoning and the lettered option list and
/// returns the one letter whose option best fits it. The persona supplies the <i>want</i>; the critic
/// does the cold <i>which one</i>, so the persona slot never has to reason logically against a schema.
///
/// <para>Mirrors <see cref="ItemUseCritic"/> and the static-singleton shape of
/// <see cref="Sanitizer.TextSanitizationPipeline"/>: one slot, created once with a fixed system
/// prompt, reset after every pick. Initialised from
/// <see cref="LlamaServerManager"/> once the server is up; degrades to a safe fallback (first option)
/// when unavailable, and to a random pick in playground mode.</para>
/// </summary>
public static class PersonaMatchCritic
{
    /// <summary>Hard ceiling: one letter A–Z, so at most 26 options may be presented at once.</summary>
    public const int MaxOptions = 26;

    private static LlamaServerManager? _llm;
    private static int _slotId = -1;
    private static bool _initialized;

    /// <summary>
    /// The playground stand-in for the critic's judgement. Master-seeded, so a <c>--playground</c>
    /// run picks the same options in the same order every time — without this, no scripted run is
    /// reproducible, because every persona choice in the game funnels through here. Locked because
    /// picks are made from whichever background thread is generating narration, and a shared
    /// <see cref="Random"/> is not thread-safe.
    /// </summary>
    private static readonly Random _playgroundRng = GameRng.Stream("persona-match-playground");

    public static bool IsReady => _initialized && _slotId >= 0;

    /// <summary>
    /// Creates the critic's slot with its neutral system prompt. Idempotent; safe to call once the
    /// LLM server is ready (e.g. from <c>SetLlamaServer</c>). A failure is logged and leaves the critic
    /// unready, which <see cref="PickAsync"/> then reports as a failure rather than working around.
    /// </summary>
    public static async Task InitializeAsync(LlamaServerManager llamaServer)
    {
        if (_initialized) return;
        _llm = llamaServer ?? throw new ArgumentNullException(nameof(llamaServer));
        try
        {
            _slotId = await _llm.CreateInstanceAsync(SystemPrompt);
            _initialized = true;
            Console.WriteLine($"PersonaMatchCritic: created slot {_slotId}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"PersonaMatchCritic: failed to initialize: {ex.Message}");
            _initialized = false;
        }
    }

    /// <summary>
    /// Returns the index into <paramref name="options"/> of the one that best fits
    /// <paramref name="reasoning"/> (the Modus Mentis's free-text answer), given the shared
    /// <paramref name="context"/>. The critic is an exterior observer, so the whole prompt is
    /// rendered in the <b>third person</b>: <paramref name="personaTitle"/> introduces who the
    /// character is ("a curious wanderer"; blank to omit), <paramref name="reportedQuestion"/> is
    /// the question in reported speech ("what they want to focus on"), and the second-person
    /// context and option labels the callers share with the persona stage are converted
    /// mechanically ("You are…" → "They are…", "your" → "their") — quoted speech excepted, since a
    /// "you" inside quotes addresses someone real. Options are presented as a lettered list A, B,
    /// C … and the reply is GBNF-constrained to a single one of those letters.
    ///
    /// <para>Set <paramref name="lastOptionIsCatchAll"/> when the final option is a "none of these"
    /// fallback the persona was never shown (see <see cref="PersonaChoiceSelector"/>'s hidden decline).
    /// The closing instruction then names that letter as the answer for an intention that matches no
    /// listed target — without it the critic must map every want onto a real option, and a persona that
    /// wandered off ("I choose to mark the boy by the wall instead") is silently forced onto option A.</para>
    ///
    /// <para>Callers must pass at most <see cref="MaxOptions"/> options (sample beforehand). In
    /// playground mode a random index is returned, there being no model to ask.</para>
    ///
    /// <para><b>It throws rather than falling back, and that is deliberate.</b> This used to catch
    /// everything and return <c>0</c>, which reads as robustness and is not: option A is a real
    /// narrative choice, attributed to the character, indistinguishable from one they made. A run
    /// that reported "failed to generate observations" turned out to have hit the same transport
    /// fault eight minutes earlier here — where it was swallowed, the player was told nothing, and
    /// the persona's decision was quietly replaced. One line in the log is not a report. A failure
    /// the player can see is worth more than a scene that keeps moving on a substituted answer.</para>
    /// </summary>
    public static async Task<int> PickAsync(
        string context,
        string personaTitle,
        string reportedQuestion,
        string reasoning,
        IReadOnlyList<string> options,
        bool lastOptionIsCatchAll = false,
        CancellationToken ct = default)
    {
        if (options == null || options.Count == 0) return -1;
        if (options.Count > MaxOptions)
            throw new ArgumentException($"PersonaMatchCritic.PickAsync: {options.Count} options exceeds {MaxOptions}; sample before calling.");

        if (PlaygroundMode.IsActive)
            lock (_playgroundRng) return _playgroundRng.Next(options.Count);

        // Not ready is a failure, not a condition to work around. See the note on this method.
        if (!IsReady || _llm == null || !_llm.IsServerReady)
            throw new InvalidOperationException(
                $"PersonaMatchCritic is not ready (initialized={_initialized}, slot={_slotId}, " +
                $"serverReady={_llm?.IsServerReady.ToString() ?? "no server"}).");

        string prompt  = BuildPrompt(context, personaTitle, reportedQuestion, reasoning, options, lastOptionIsCatchAll);
        string grammar = BuildLetterGrammar(options.Count);

        string reply = await _llm.GenerateConstrainedStringAsync(_slotId, prompt, grammar, maxTokens: 4, skipReset: false);
        int idx = LetterToIndex(reply);
        if (idx < 0 || idx >= options.Count)
            throw new InvalidOperationException(
                $"PersonaMatchCritic: reply '{reply}' is not one of the {options.Count} offered letters. " +
                "The grammar constrains the answer to exactly those, so this means the reply was " +
                "truncated or the grammar was not applied.");

        return idx;
    }

    // ── Prompt / grammar ─────────────────────────────────────────────────────────

    /// <summary>
    /// Renders the whole pick prompt from the critic's exterior point of view — "The character is a
    /// curious wanderer. They are in a field… Asked what they want to focus on, they just said:" —
    /// converting the shared second-person context and option labels via <see cref="ToThirdPerson"/>.
    /// The persona intro sentence is omitted when <paramref name="personaTitle"/> is blank.
    /// </summary>
    private static string BuildPrompt(string context, string personaTitle, string reportedQuestion, string reasoning, IReadOnlyList<string> options, bool lastOptionIsCatchAll)
    {
        var sb = new StringBuilder();

        // Opening paragraph: who the character is, then where they are / what they attend to.
        bool hasTitle   = !string.IsNullOrWhiteSpace(personaTitle);
        bool hasContext = !string.IsNullOrWhiteSpace(context);
        if (hasTitle) sb.Append("The character is ").Append(personaTitle.Trim()).Append('.');
        if (hasContext)
        {
            if (hasTitle) sb.Append(' ');
            sb.Append(ToThirdPerson(context.TrimEnd()));
        }
        if (hasTitle || hasContext) sb.Append("\n\n");

        sb.Append("Asked ").Append(reportedQuestion.Trim()).Append(", the character just said:\n");
        sb.Append('"').Append(reasoning.Trim()).Append("\"\n\n");
        sb.Append("Here are the options they can choose from:\n");
        for (int i = 0; i < options.Count; i++)
            sb.Append(Letter(i)).Append(" - ").Append(ToThirdPerson(options[i].Trim())).Append('\n');
        sb.Append("\nExtract the target of the character's intention, then select the option referring to that target. ");
        if (lastOptionIsCatchAll)
            sb.Append("If their intention is about none of the targets named above, select ")
              .Append(Letter(options.Count - 1)).Append(" instead. ");
        sb.Append("Answer with that option's letter and nothing else.");
        return sb.ToString();
    }

    /// <summary>
    /// Mechanically converts the second-person prompt fragments the persona stage uses into third
    /// person for the critic ("You are" → "They are", "your" → "their", "yourself" → "themselves").
    /// Text inside double quotes is left verbatim — a "you" in quoted speech addresses someone real
    /// (an NPC line, the persona's own words), not the character being described.
    /// </summary>
    private static string ToThirdPerson(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var parts = text.Split('"');
        for (int i = 0; i < parts.Length; i += 2)   // even indices are outside quotes
        {
            var s = parts[i];
            s = Regex.Replace(s, @"\bYourself\b", "Themselves");
            s = Regex.Replace(s, @"\byourself\b", "themselves");
            s = Regex.Replace(s, @"\bYour\b", "Their");
            s = Regex.Replace(s, @"\byour\b", "their");
            s = Regex.Replace(s, @"\bYou\b", "They");
            s = Regex.Replace(s, @"\byou\b", "they");
            parts[i] = s;
        }
        return string.Join("\"", parts);
    }

    /// <summary><c>root ::= "A" | "B" | … </c> over exactly the letters in range.</summary>
    private static string BuildLetterGrammar(int count)
    {
        var letters = Enumerable.Range(0, count).Select(i => $"\"{Letter(i)}\"");
        return "root ::= " + string.Join(" | ", letters) + "\n";
    }

    private static char Letter(int i) => (char)('A' + i);

    /// <summary>First A–Z letter in the reply → 0-based index, ignoring stray whitespace/case.</summary>
    private static int LetterToIndex(string? reply)
    {
        if (string.IsNullOrEmpty(reply)) return -1;
        foreach (char c in reply)
        {
            char u = char.ToUpperInvariant(c);
            if (u >= 'A' && u <= 'Z') return u - 'A';
        }
        return -1;
    }

    private const string SystemPrompt = @"You are the OPTION MATCHER. You are neutral, literal and logical — you have no personality and no preferences of your own.

You are given: some situational context, a short statement of what a character wants to do, and a lettered list of concrete options. Your only job is to decide which single option most faithfully carries out what the character said they want.

Guidelines:
- Match on meaning and intent, not on wording. Pick the option that does what they described.
- Do not judge whether the want is wise or in character — that is already decided. Just map it to the closest option.
- If the list ends with a catch-all option for an intention that fits none of the others, and what the character wants is about none of the listed targets, pick that catch-all rather than forcing a match.
- Answer with EXACTLY one letter from the list and nothing else.";
}
