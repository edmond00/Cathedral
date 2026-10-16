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
    /// <summary>How willingly the action modus mentis carries the goal out. An ordinal scale, read
    /// through <see cref="ChoiceJudge.JudgeWillingnessAsync"/> rather than picked from.</summary>
    Willingness,
    /// <summary>Which reply to say (speaking).</summary>
    Reply,
}

/// <summary>The four degrees of the willingness scale, strongest assent first.</summary>
public enum Willingness { Eager, Willing, Reluctant, Unwilling }

/// <summary>
/// The CHOICE JUDGE: one neutral LLM slot per <see cref="ChoiceKind"/>, which reads a modus mentis's
/// description, the situation and a lettered list, and scores every letter — the option that
/// modus mentis would take is read from those scores. It replaced a two-stage pass in which the modus mentis first answered in
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
/// <para><b>The decision is read from the letters' scores, not sampled.</b> One forward pass returns
/// the log-probability of every offered letter, and a rule per kind turns them into a choice — most
/// often simply the highest, but a threshold where the highest misleads. Measured on 135 labelled
/// cases (CPU, shipped model), the model is near-certain on ~90% of its answers, wrong ones included,
/// so no threshold rescues a confident mistake: those are fixed in the descriptions and the prompts
/// (see "Writing for the choice judge" in src/game/CLAUDE.md). Where a threshold does help is where
/// the model hedges:
/// <list type="bullet">
/// <item><b>The goal's decline.</b> A real "do something else" scores +11 to +22 in log-odds over
///       every real goal; a mistaken one, about +4. Declining only above
///       <see cref="GoalDeclineLogOdds"/> took the goal judge from 89% to 92%.</item>
/// <item><b>Willingness.</b> See <see cref="JudgeWillingnessAsync"/>.</item>
/// </list>
/// Averaging over several option orders, or dividing out a per-letter prior, was measured too and
/// made things worse: the position bias they correct is already small. Every decision logs its
/// log-odds, so the thresholds can be re-tuned from real play.</para>
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

    /// <summary>How many first-token candidates to ask the server for. Letters other than the offered
    /// ones, lower-case and space-prefixed variants all take places, so this is generous.</summary>
    private const int TopLogprobs = 40;

    /// <summary>A letter absent from the returned candidates is at least this unlikely.</summary>
    private const double AbsentLogProb = -40.0;

    /// <summary>
    /// The goal judge declines ("do something else entirely") only when the decline's log-odds over
    /// all the real goals together exceed this. On the bench, every right decline scored +11 or more
    /// and every wrong one +4.4 or less.
    /// </summary>
    public const double GoalDeclineLogOdds = 10.0;

    /// <summary>Willingness: assent (eager or willing) when log P(eager or willing) − log P(reluctant or
    /// unwilling) exceeds this. Right on 39 of 40 bench cases, against 38 for the likeliest letter.</summary>
    public const double AssentLogOdds = -1.8;

    /// <summary>Willingness, once assenting: eager when log P(eager) − log P(willing) is at least this.
    /// The bench median, so eager and willing split assent about evenly. The judge almost never answers
    /// "eager" outright (1 case in 40), so its lean, not its answer, is what is read.</summary>
    public const double EagerLogOdds = -6.4;

    /// <summary>Willingness, once dissenting: unwilling (a refusal) when log P(unwilling) − log P(reluctant)
    /// is at least this. The bench median, so the two split dissent about evenly.</summary>
    public const double UnwillingLogOdds = -0.6;

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
    /// <paramref name="question"/> replaces the kind's own question, for one that names something.
    /// <paramref name="declineIndex"/> is the decline option's index, or −1 when there is none; the
    /// goal judge reads it through <see cref="GoalDeclineLogOdds"/>, every other kind takes the
    /// likeliest letter.
    /// </summary>
    public static async Task<int> PickAsync(
        ChoiceKind kind, ModusMentis faculty, string situation, IReadOnlyList<string> options,
        string? question = null, CancellationToken ct = default, int declineIndex = -1)
    {
        if (kind == ChoiceKind.Willingness)
            throw new ArgumentException("ChoiceJudge.PickAsync: willingness is a scale - use JudgeWillingnessAsync.");
        if (options == null || options.Count == 0) return -1;
        if (options.Count > MaxOptions)
            throw new ArgumentException($"ChoiceJudge.PickAsync: {options.Count} options exceeds {MaxOptions}; sample before calling.");

        var lp = await LetterLogProbsAsync(kind, BuildPrompt(kind, faculty, situation, options, question), options.Count);

        int best = ArgMax(lp, except: -1);
        string how;
        if (kind == ChoiceKind.Goal && declineIndex >= 0 && declineIndex < options.Count)
        {
            double declineOdds = lp[declineIndex] - LogSumExp(lp.Where((_, i) => i != declineIndex));
            best = declineOdds > GoalDeclineLogOdds ? declineIndex : ArgMax(lp, except: declineIndex);
            how = $"decline log-odds {F(declineOdds)} vs threshold {F(GoalDeclineLogOdds)}";
        }
        else
        {
            how = $"log-odds {F(lp[best] - LogSumExp(lp.Where((_, i) => i != best)))} over the rest";
        }
        Console.WriteLine($"ChoiceJudge: {kind} for {faculty.DisplayName} -> {options[best]} ({how})");
        return best;
    }

    /// <summary>
    /// How willingly <paramref name="faculty"/> would carry out the act <paramref name="situation"/>
    /// describes. The judge always reads the whole scale, in order (eager to unwilling), and the degree
    /// is read from its scores in two steps:
    /// <list type="number">
    /// <item>assent or dissent: <see cref="AssentLogOdds"/> on (eager or willing) against (reluctant or unwilling);</item>
    /// <item>how strongly, within that side: <see cref="EagerLogOdds"/> or <see cref="UnwillingLogOdds"/>.</item>
    /// </list>
    /// Taking the likeliest letter instead gave 1 eager in 40 bench cases (17 willing, 13 reluctant,
    /// 9 unwilling), so the −1 difficulty of an eager act almost never happened; read this way the four
    /// degrees came out 10/9/10/11 and the assent was right one case more often. The caller narrows the
    /// answer to what the rules leave on offer: a mind with no refusal left answers Reluctant.
    /// </summary>
    public static async Task<Willingness> JudgeWillingnessAsync(
        ModusMentis faculty, string situation, CancellationToken ct = default)
    {
        var scale = new[] { "eager", "willing", "reluctant", "unwilling" }.Select(StanceLabel).ToList();
        var lp = await LetterLogProbsAsync(ChoiceKind.Willingness,
                                           BuildPrompt(ChoiceKind.Willingness, faculty, situation, scale, null), scale.Count);
        double assent = LogSumExp(new[] { lp[0], lp[1] }) - LogSumExp(new[] { lp[2], lp[3] });
        Willingness w = assent > AssentLogOdds
            ? (lp[0] - lp[1] >= EagerLogOdds ? Willingness.Eager : Willingness.Willing)
            : (lp[3] - lp[2] >= UnwillingLogOdds ? Willingness.Unwilling : Willingness.Reluctant);
        Console.WriteLine($"ChoiceJudge: Willingness for {faculty.DisplayName} -> {w} " +
                          $"(assent {F(assent)} vs {F(AssentLogOdds)}; eager-willing {F(lp[0] - lp[1])} vs {F(EagerLogOdds)}; " +
                          $"unwilling-reluctant {F(lp[3] - lp[2])} vs {F(UnwillingLogOdds)})");
        return w;
    }

    /// <summary>
    /// One forward pass; the log-probability of each of the first <paramref name="count"/> letters as
    /// the reply's first token. A letter may come back bare or space-prefixed, and the likelier spelling
    /// stands for it. No grammar: on the bench the offered letters held at least 99.99% of the mass
    /// without one, and a grammar would only hide what the model would otherwise have said.
    /// </summary>
    private static async Task<double[]> LetterLogProbsAsync(ChoiceKind kind, string prompt, int count)
    {
        if (!IsReady || _llm == null || !_llm.IsServerReady)
            throw new InvalidOperationException(
                $"ChoiceJudge is not ready (initialized={_initialized}, " +
                $"serverReady={_llm?.IsServerReady.ToString() ?? "no server"}).");

        var top = await _llm.GetFirstTokenLogProbsAsync(_slots[kind], prompt, TopLogprobs);
        var lp = Enumerable.Repeat(double.NegativeInfinity, count).ToArray();
        foreach (var (token, logProb) in top)
        {
            string t = token.Trim();
            if (t.Length != 1) continue;
            int i = t[0] - 'A';
            if (i >= 0 && i < count && logProb > lp[i]) lp[i] = logProb;
        }
        if (lp.All(double.IsNegativeInfinity))
            throw new InvalidOperationException(
                $"ChoiceJudge: none of the {count} offered letters is among the reply's {top.Count} likeliest " +
                $"first tokens ({string.Join(", ", top.Take(5).Select(t => $"'{t.Token}'"))}). The prompt no " +
                "longer asks for a letter, or the server returned no log-probabilities.");
        for (int i = 0; i < count; i++)
            if (double.IsNegativeInfinity(lp[i])) lp[i] = AbsentLogProb;
        return lp;
    }

    private static int ArgMax(double[] v, int except)
    {
        int best = -1;
        for (int i = 0; i < v.Length; i++)
            if (i != except && (best < 0 || v[i] > v[best])) best = i;
        return best;
    }

    private static double LogSumExp(IEnumerable<double> xs)
    {
        var a = xs.ToArray();
        if (a.Length == 0) return double.NegativeInfinity;
        double m = a.Max();
        return m + Math.Log(a.Sum(x => Math.Exp(x - m)));
    }

    private static string F(double x) => x.ToString("+0.0;-0.0", System.Globalization.CultureInfo.InvariantCulture);

    // ── Prompt / grammar ─────────────────────────────────────────────────────────

    /// <summary>"SNEAK ART - Holds part of the attention… Its manner: a cautious shadow…."</summary>
    public static string Describe(ModusMentis mm)
    {
        var sb = new StringBuilder(mm.DisplayName.ToUpperInvariant()).Append(" - ").Append(mm.MenuDescription.Trim());
        if (!string.IsNullOrWhiteSpace(mm.PersonaTone)) sb.Append(" Its manner: ").Append(mm.PersonaTone.Trim()).Append('.');
        return sb.ToString();
    }

    private static string BuildPrompt(ChoiceKind kind, ModusMentis faculty, string situation, IReadOnlyList<string> options, string? question)
    {
        var sb = new StringBuilder();
        sb.Append(kind == ChoiceKind.Means ? "The thinking faculty: " : "The faculty: ").Append(Describe(faculty)).Append("\n\n");
        if (!string.IsNullOrWhiteSpace(situation))
            sb.Append("The situation: ").Append(ToThirdPerson(Regex.Replace(situation.Trim(), @"\s+", " "))).Append("\n\n");
        sb.Append(question ?? Questions[kind]).Append('\n');
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
        [ChoiceKind.KeepFocus]   = "Does it hold anything for this faculty?",   // callers name the thing
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

    private static char Letter(int i) => (char)('A' + i);

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

        // Asked "does it keep its attention there?" against "turn elsewhere", the judge turned away
        // from almost everything — 28% kept, Exploration dropping a slip face in a desert. Asked
        // whether the thing holds anything for the faculty, it kept 51% and judged every labelled
        // case right.
        [ChoiceKind.KeepFocus] = @"You are the ATTENTION JUDGE. You are neutral and analytical, with no personality and no morals of your own.

A character's mind is made of faculties, each with its own nature, concerns and skills. You are given the description of ONE faculty and a thing in front of the character. Decide whether the thing holds anything for this faculty.

Guidelines:
- Judge by what this faculty knows, does, notices or cares about.
- Answer with the letter of your choice.",

        [ChoiceKind.Goal] = @"You are the GOAL CHOOSER. You are neutral and analytical, with no personality of your own.

A character's mind is made of faculties, each with its own temperament and concerns. You are given the description of ONE faculty, the situation, and a lettered list of things the character could do. Decide which one this faculty would be most drawn to.

Guidelines:
- Judge by the faculty's own temperament and concerns alone - not by what would be wise, safe or moral in general.
- If none of the options is anything this faculty would care about, choose the option ""do something else entirely"".
- Answer with the letter of your choice.",

        // "Never because a description shares words with the goal": the means judge matches words
        // before temperaments. Asked to cross a river, it took Fording ("the crossing of water on foot")
        // at near certainty for Caution and Temerity alike. The line won one bench case back (87.7% ->
        // 89.2%) and did not move that one; prefer a SkillMeans that does not echo a goal's verb.
        [ChoiceKind.Means] = @"You are the MEANS MATCHER. You are neutral and analytical, with no personality of your own.

A character's mind is made of faculties. A thinking faculty has settled on a goal and must now pick which of the character's practical faculties will carry it out. You are given the thinking faculty's description, the goal, and a lettered list of practical faculties, each described by what it does.

Guidelines:
- First, the practical faculty must be able to achieve the goal. Never pick one that cannot.
- Usually several could. Choose among them by the thinking faculty's temperament - what it would want done, and how - never because a description shares words with the goal.
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
