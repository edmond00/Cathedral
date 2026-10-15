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
/// The six bounded decisions a modus mentis makes. Each has its own judge slot, so each system
/// prompt is written for one question and stays cached between requests.
/// </summary>
public enum ChoiceKind
{
    /// <summary>Which object in view to attend to (observation).</summary>
    Focus,
    /// <summary>Whether to keep the object the attention was handed, or turn elsewhere (observation).</summary>
    KeepFocus,
    /// <summary>What to do about the observed object (thinking).</summary>
    Goal,
    /// <summary>Which action modus mentis carries the goal out (thinking).</summary>
    Means,
    /// <summary>How willingly the action modus mentis carries the goal out. An ordinal scale.</summary>
    Willingness,
    /// <summary>Which reply to say (speaking).</summary>
    Reply,
}

/// <summary>
/// The CHOICE JUDGE: one neutral LLM slot per <see cref="ChoiceKind"/>, which reads a modus mentis's
/// description, the situation and a lettered list, and answers with one letter — the option that
/// modus mentis would take. It replaced a two-stage pass in which the modus mentis first answered in
/// its own voice (free text, on its persona slot) and a neutral matcher then mapped that answer onto
/// the list. Measured on the shipped model on CPU, the judge alone is about three times faster
/// (~0.9 s against ~3.2 s a decision), more often right, and far more stable from run to run: the
/// persona's free text drifted toward whatever the model itself preferred, so a cruel mind chose to
/// bribe and a covetous one never stole, and the matcher faithfully carried that drift through.
///
/// <para><b>The modus mentis is described, not voiced.</b> The judge sees
/// <see cref="ModusMentis.DisplayName"/>, <see cref="ModusMentis.MenuDescription"/> (what it governs,
/// in neutral third person) and <see cref="ModusMentis.PersonaTone"/> (its manner). Its persona prompt
/// stays where it belongs, in the rewrite that narrates the choice.</para>
///
/// <para>Like the matcher it replaced, it <b>throws rather than falling back</b>: option A is a real
/// narrative choice attributed to the character, and a substituted answer the player cannot see is
/// worse than a failure they can. In playground mode it is never reached —
/// <see cref="PersonaChoiceSelector"/> draws a random option first.</para>
/// </summary>
public static class ChoiceJudge
{
    /// <summary>Hard ceiling: one letter A–Z, so at most 26 options may be presented at once.</summary>
    public const int MaxOptions = 26;

    private static LlamaServerManager? _llm;
    private static readonly Dictionary<ChoiceKind, int> _slots = new();
    private static bool _initialized;

    public static bool IsReady => _initialized;

    /// <summary>
    /// Creates one slot per kind with its system prompt. Idempotent; called once the LLM server is
    /// ready. A failure is logged and leaves the judge unready, which <see cref="PickAsync"/> then
    /// reports as a failure rather than working around.
    /// </summary>
    public static async Task InitializeAsync(LlamaServerManager llamaServer)
    {
        if (_initialized) return;
        _llm = llamaServer ?? throw new ArgumentNullException(nameof(llamaServer));
        try
        {
            foreach (var kind in Enum.GetValues<ChoiceKind>())
                _slots[kind] = await _llm.CreateInstanceAsync(SystemPrompts[kind]);
            _initialized = true;
            Console.WriteLine("ChoiceJudge: created slots " +
                              string.Join(", ", _slots.Select(s => $"{s.Key}={s.Value}")));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ChoiceJudge: failed to initialize: {ex.Message}");
            _initialized = false;
        }
    }

    /// <summary>
    /// Returns the index into <paramref name="options"/> that <paramref name="faculty"/> would take.
    /// <paramref name="situation"/> and the option labels are the second-person text the rest of the
    /// narration uses ("You are in a farm, in the yard."); they are converted to the third person here,
    /// since the judge is an observer of the character, not the character. For
    /// <see cref="ChoiceKind.Means"/> the labels are the candidate modi mentis themselves.
    /// </summary>
    public static async Task<int> PickAsync(
        ChoiceKind kind, ModusMentis faculty, string situation, IReadOnlyList<string> options,
        CancellationToken ct = default)
    {
        if (options == null || options.Count == 0) return -1;
        if (options.Count > MaxOptions)
            throw new ArgumentException($"ChoiceJudge.PickAsync: {options.Count} options exceeds {MaxOptions}; sample before calling.");

        if (!IsReady || _llm == null || !_llm.IsServerReady)
            throw new InvalidOperationException(
                $"ChoiceJudge is not ready (initialized={_initialized}, " +
                $"serverReady={_llm?.IsServerReady.ToString() ?? "no server"}).");

        string prompt  = BuildPrompt(kind, faculty, situation, options);
        string grammar = BuildLetterGrammar(options.Count);
        string reply = await _llm.GenerateConstrainedStringAsync(_slots[kind], prompt, grammar, maxTokens: 4, skipReset: false);
        int idx = LetterToIndex(reply);
        if (idx < 0 || idx >= options.Count)
            throw new InvalidOperationException(
                $"ChoiceJudge: reply '{reply}' is not one of the {options.Count} offered letters. " +
                "The grammar constrains the answer to exactly those, so this means the reply was " +
                "truncated or the grammar was not applied.");
        Console.WriteLine($"ChoiceJudge: {kind} for {faculty.DisplayName} -> {options[idx]}");
        return idx;
    }

    // ── Prompt / grammar ─────────────────────────────────────────────────────────

    /// <summary>"SNEAK ART - Holds part of the attention… Its manner: a cautious shadow…."</summary>
    public static string Describe(ModusMentis mm)
    {
        var sb = new StringBuilder(mm.DisplayName.ToUpperInvariant()).Append(" - ").Append(mm.MenuDescription.Trim());
        if (!string.IsNullOrWhiteSpace(mm.PersonaTone)) sb.Append(" Its manner: ").Append(mm.PersonaTone.Trim()).Append('.');
        return sb.ToString();
    }

    private static string BuildPrompt(ChoiceKind kind, ModusMentis faculty, string situation, IReadOnlyList<string> options)
    {
        var sb = new StringBuilder();
        sb.Append(kind == ChoiceKind.Means ? "The thinking faculty: " : "The faculty: ").Append(Describe(faculty)).Append("\n\n");
        if (!string.IsNullOrWhiteSpace(situation))
            sb.Append("The situation: ").Append(ToThirdPerson(Regex.Replace(situation.Trim(), @"\s+", " "))).Append("\n\n");
        sb.Append(Questions[kind]).Append('\n');
        for (int i = 0; i < options.Count; i++)
            sb.Append(Letter(i)).Append(" - ").Append(ToThirdPerson(options[i].Trim())).Append('\n');
        sb.Append("\nAnswer with the letter and nothing else.");
        return sb.ToString();
    }

    /// <summary>
    /// The willingness scale as the judge reads it, each degree defined, keyed by its bare adjective
    /// ("eager"). Listed in this order, never shuffled: shuffled, the same judge was right 83% of the
    /// time against 93%, and half as stable.
    /// </summary>
    public static string StanceLabel(string bareStance) => bareStance switch
    {
        "eager"     => "eager - the act is the very thing this faculty exists to do",
        "willing"   => "willing - the act is within its reach and not against its nature",
        "reluctant" => "reluctant - the act is foreign to it; other faculties would do it better",
        "unwilling" => "unwilling - the act contradicts what this faculty is",
        _           => bareStance,
    };

    private static readonly Dictionary<ChoiceKind, string> Questions = new()
    {
        [ChoiceKind.Focus]       = "Which would it attend to?",
        [ChoiceKind.KeepFocus]   = "Does it keep its attention there?",
        [ChoiceKind.Goal]        = "Which would it be most drawn to?",
        [ChoiceKind.Means]       = "Which practical faculty will it use?",
        [ChoiceKind.Willingness] = "How far does this act belong to this faculty?",
        [ChoiceKind.Reply]       = "Which would it say?",
    };

    /// <summary>
    /// Second person to third ("You are" → "They are", "your" → "their"). Text inside double quotes
    /// is left verbatim — a "you" in quoted speech addresses someone real.
    /// </summary>
    private static string ToThirdPerson(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var parts = text.Split('"');
        for (int i = 0; i < parts.Length; i += 2)
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

    private static string BuildLetterGrammar(int count)
        => "root ::= " + string.Join(" | ", Enumerable.Range(0, count).Select(i => $"\"{Letter(i)}\"")) + "\n";

    private static char Letter(int i) => (char)('A' + i);

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

    // Each judge says "neutral … no morals of your own" for one reason: asked as itself, the model
    // brings its own caution, and a faculty made for theft comes out reluctant to steal.
    private static readonly Dictionary<ChoiceKind, string> SystemPrompts = new()
    {
        [ChoiceKind.Focus] = @"You are the ATTENTION JUDGE. You are neutral and analytical, with no personality and no morals of your own.

A character's mind is made of faculties, each with its own nature, concerns and skills. You are given the description of ONE faculty, the place, and a lettered list of things in view. Decide which one this faculty would turn its attention to.

Guidelines:
- Judge by the faculty's own nature and concerns alone.
- If the list ends with an option to let the attention go, choose it when nothing listed is anything this faculty would care about.
- Answer with the letter of your choice.",

        [ChoiceKind.KeepFocus] = @"You are the ATTENTION JUDGE. You are neutral and analytical, with no personality and no morals of your own.

A character's mind is made of faculties, each with its own nature, concerns and skills. You are given the description of ONE faculty and a thing the character's attention has just been handed to. Decide whether this faculty would keep its attention on that thing or turn it elsewhere.

Guidelines:
- Keep it when the thing touches what this faculty knows, does or cares about; turn elsewhere when it is nothing to this faculty.
- Answer with the letter of your choice.",

        [ChoiceKind.Goal] = @"You are the GOAL CHOOSER. You are neutral and analytical, with no personality of your own.

A character's mind is made of faculties, each with its own temperament and concerns. You are given the description of ONE faculty, the situation, and a lettered list of things the character could do. Decide which one this faculty would be most drawn to.

Guidelines:
- Judge by the faculty's own temperament and concerns alone - not by what would be wise, safe or moral in general.
- If none of the options is anything this faculty would care about, choose the option ""do something else entirely"".
- Answer with the letter of your choice.",

        [ChoiceKind.Means] = @"You are the MEANS MATCHER. You are neutral and analytical, with no personality of your own.

A character's mind is made of faculties. A thinking faculty has settled on a goal and must now pick which of the character's practical faculties will carry it out. You are given the thinking faculty's description, the goal, and a lettered list of practical faculties, each described by what it does.

Guidelines:
- First, the practical faculty must be able to achieve the goal. Never pick one that cannot.
- Among those that could, prefer the one whose way of working best suits the thinking faculty's temperament.
- Answer with the letter of your choice.",

        // Asked "how willingly", the judge brought its own caution and answered "reluctant" for a
        // faculty made for the act whenever the act was a crime (a cutpurse picking a pocket, a forger
        // forging): 20% right on those against 92% for this wording, which asks whether the act
        // belongs to the faculty and says outright that many faculties exist for disreputable work.
        [ChoiceKind.Willingness] = @"You are the WILLINGNESS JUDGE. You are neutral and analytical, with no personality and no morals of your own.

A character's mind is made of faculties, each with its own nature, concerns and skills. You are given the description of ONE faculty and an act it is asked to carry out. Decide how far the act belongs to this faculty.

Guidelines:
- The world is harsh, and many faculties exist for disreputable, criminal or violent work. A faculty made for theft is eager to steal; one made for killing is eager to kill; one made for digging graves is eager to dig them.
- Never apply your own morals, caution or sense of propriety. Only the faculty's own nature counts.
- Answer with the letter of your judgement.",

        [ChoiceKind.Reply] = @"You are the REPLY CHOOSER. You are neutral and analytical, with no personality and no morals of your own.

A character's mind is made of faculties, each with its own nature, concerns and manner of speech. You are given the description of ONE speaking faculty, the conversation so far, and a lettered list of things the character could say next. Decide which one this faculty would say.

Guidelines:
- Judge by the faculty's own nature and manner alone - not by what would be wise, kind or polite in general.
- Answer with the letter of your choice.",
    };
}
