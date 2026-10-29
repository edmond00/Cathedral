using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cathedral.Game.Narrative;

/// <summary>The voice a journal paragraph is written in.</summary>
public enum ChronicleVoice
{
    /// <summary>A neutral chronicler, third person, past tense.</summary>
    Chronicle,
    /// <summary>The protagonist's own diary, first person, past tense.</summary>
    Diary,
    /// <summary>The diary, coloured by what the visit made the protagonist feel.</summary>
    DiaryTinted,
    /// <summary>A terse campaign log.</summary>
    CampaignLog,
}

/// <summary>What of a visit the chronicler is shown.</summary>
public enum ChronicleInputs { MilestonesOnly, FactsAndMilestones, NarrationOnly, Everything }

/// <summary>What the chronicler is told of what came before.</summary>
public enum ChronicleMemory { None, PreviousParagraph, RollingSummary }

/// <summary>One way of writing the journal: the axes <c>tools/journal/</c> measures, one at a time.</summary>
public sealed record ChronicleConfig(ChronicleVoice Voice, ChronicleInputs Inputs, ChronicleMemory Memory, int MaxTokens);

/// <summary>
/// The chronicler's prompts and the way a visit is put to it. Everything the chronicler sends is built
/// here, and <c>tools/journal/</c> reads this file (the system prompts) and calls
/// <c>--journal-prompts</c> (the user messages) rather than keeping copies — so the harness measures
/// what the game sends.
///
/// <para><b>Short on purpose.</b> A background request is preempted by closing its stream, and
/// llama-server only notices between batches: the longer the prompt, the longer a preempted job can
/// hold the server while the player waits. <see cref="MaxItemChars"/> and <see cref="MaxInputChars"/>
/// keep a job's prompt to a few hundred tokens.</para>
/// </summary>
public static class ChroniclerPrompt
{
    /// <summary>The configuration the game ships, chosen by the <c>tools/journal/</c> comparison.</summary>
    ///
    /// <para>Measured on twelve visits from eight real-model runs (design/BELLIC_JOURNAL_REPORT.md): the
    /// third-person chronicle read best and held its voice; the diary kept slipping into "X wrote in his
    /// diary:" and third person. Milestones or facts alone were worst of all — with little to go on the
    /// model invented reeves, axes and barley fields. The previous paragraph as memory made it copy that
    /// paragraph forward (a quarter of its 4-grams, whole sentences at times); no memory repeated least.</para>
    public static readonly ChronicleConfig Shipped =
        new(ChronicleVoice.Chronicle, ChronicleInputs.Everything, ChronicleMemory.None, MaxTokens: 140);

    /// <summary>Longest a single narration or line may be in the prompt; past it, the start is kept.</summary>
    public const int MaxItemChars = 320;

    /// <summary>Longest the whole list of what happened may be; past it, the oldest items go first.</summary>
    public const int MaxInputChars = 2400;

    private const string Common = @"
Rules:
- Use only what the notes say. Never invent a person, a name, a place, an object or an event.
- Call people what the notes call them (""the brewer"", ""the reeve""). Never give anyone a name the notes do not use.
- Write one paragraph of plain prose, three to five sentences. No title, no list, no date line.
- Do not repeat the previous entry, and do not copy the notes word for word.";

    public static readonly IReadOnlyDictionary<ChronicleVoice, string> System = new Dictionary<ChronicleVoice, string>
    {
        [ChronicleVoice.Chronicle] = @"You are the chronicler of a traveller's journey. You are given notes on one stretch of it — what was seen, said and done, and what changed — and you write that stretch up as a short passage of a chronicle: third person, past tense, sober and clear, naming the traveller as the notes do." + Common,
        [ChronicleVoice.Diary] = @"You keep the diary of a traveller. You are given notes on one stretch of the journey — what was seen, said and done, and what changed — and you write that stretch up as the traveller's own diary entry: first person, past tense, plain and personal, the way someone writes at the end of a day." + Common,
        [ChronicleVoice.DiaryTinted] = @"You keep the diary of a traveller. You are given notes on one stretch of the journey — what was seen, said and done, what changed, and what it made the traveller feel — and you write that stretch up as the traveller's own diary entry: first person, past tense, plain and personal. Let the feelings in the notes colour how the entry is told, without naming them as a list." + Common,
        [ChronicleVoice.CampaignLog] = @"You keep a terse campaign log of a traveller's journey. You are given notes on one stretch of it and you write a log entry: past tense, short factual sentences, what was done and what came of it, nothing else." + Common,
    };

    /// <summary>The instruction that updates the rolling one-line summary after each paragraph.</summary>
    public const string SummarySystem = @"You keep a one-line summary of a traveller's journey so far. You are given the summary and the newest diary entry, and you answer with the new summary: one sentence, at most forty words, covering the whole journey, with the newest events in it and the oldest compressed. Use only what you are given; invent nothing.";

    /// <summary>The placeholder a memory variant's text replaces (the harness fills it as it goes).</summary>
    public const string MemoryPlaceholder = "{MEMORY}";

    /// <summary>
    /// The user message for one group. <paramref name="memory"/> is the previous paragraph or the
    /// rolling summary (or null for none); pass <see cref="MemoryPlaceholder"/> to render a template.
    /// </summary>
    public static string UserMessage(IReadOnlyList<JournalEntry> group, ChronicleConfig config, string? memory)
    {
        var sb = new StringBuilder();
        var first = group.FirstOrDefault(e => e.Kind != JournalEntryKind.Milestone) ?? group.FirstOrDefault();
        if (first != null)
        {
            string where = first.Place.Length > 0 ? $"at the {first.Place.ToLowerInvariant()}" : "on the road";
            var areas = group.Select(e => e.Area).Where(a => a.Length > 0).Distinct().Take(4).ToList();
            sb.Append($"Day {first.Day + 1}, {where}");
            if (areas.Count > 0) sb.Append($" ({string.Join(", ", areas.Select(a => a.ToLowerInvariant()))})");
            sb.Append(".\n");
            if (first.Actor.Length > 0) sb.Append($"The traveller is {first.Actor}.\n");
        }

        if (memory != null && config.Memory != ChronicleMemory.None)
            sb.Append(config.Memory == ChronicleMemory.PreviousParagraph
                ? $"Previous entry: {memory}\n"
                : $"The journey so far: {memory}\n");

        sb.Append("\nNotes, in order:\n");
        var items = group.Select(e => Item(e, config)).Where(s => s != null).Select(s => s!).ToList();
        // Oldest first out when over budget: the end of a visit is where it lands.
        while (items.Count > 1 && items.Sum(i => i.Length + 1) > MaxInputChars) items.RemoveAt(0);
        foreach (var i in items) sb.Append(i).Append('\n');

        sb.Append(config.Voice switch
        {
            ChronicleVoice.Chronicle   => "\nWrite the chronicle passage for these notes.",
            ChronicleVoice.CampaignLog => "\nWrite the log entry for these notes.",
            _                          => "\nWrite the diary entry for these notes.",
        });
        return sb.ToString();
    }

    private static string? Item(JournalEntry e, ChronicleConfig config)
    {
        bool narration = config.Inputs is ChronicleInputs.NarrationOnly or ChronicleInputs.Everything;
        bool facts     = config.Inputs is ChronicleInputs.FactsAndMilestones or ChronicleInputs.Everything;
        bool marks     = config.Inputs != ChronicleInputs.NarrationOnly;
        string who = e.ByCompanion ? $" ({e.Actor}, a companion)" : "";
        return e.Kind switch
        {
            JournalEntryKind.Milestone when marks => $"- MILESTONE: {e.Source}",
            JournalEntryKind.Fact when facts      => $"- CHANGED: {e.Source}",
            JournalEntryKind.Dialogue when narration => $"- SAID by {e.Actor}: \"{Clip(e.Source.Trim('"'))}\"",
            JournalEntryKind.Narration when narration && e.Block == NarrationBlockType.Emotion
                                                    => $"- FELT{who}: {Clip(e.Source)}",
            JournalEntryKind.Narration when narration => $"- {Label(e.Block)}{who}: {Clip(e.Source)}",
            _ => null,
        };
    }

    private static string Label(NarrationBlockType? block) => block switch
    {
        NarrationBlockType.Observation => "SAW",
        NarrationBlockType.Thinking    => "THOUGHT",
        NarrationBlockType.Action      => "DID",
        NarrationBlockType.Outcome     => "DID",
        NarrationBlockType.Speaking    => "SAID",
        _                              => "NOTE",
    };

    private static string Clip(string s)
    {
        s = s.Replace('\n', ' ').Trim();
        return s.Length <= MaxItemChars ? s : s[..MaxItemChars].TrimEnd() + "…";
    }

    /// <summary>Trims a reply to its first paragraph and strips a heading or quote the model added.</summary>
    public static string Clean(string reply)
    {
        var text = reply.Replace("\r", "").Trim().Trim('"');
        var para = text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        var lines = para.Split('\n').Where(l => !l.TrimStart().StartsWith('#') && !l.TrimEnd().EndsWith(':')).ToList();
        var joined = string.Join(" ", lines).Trim();
        // A reply cut off by the token cap ends mid-sentence: keep it to its last full stop.
        int end = joined.LastIndexOfAny(new[] { '.', '!', '?' });
        return end > joined.Length / 2 ? joined[..(end + 1)] : joined;
    }
}
