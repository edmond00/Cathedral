using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Cathedral.Game.Narrative;

/// <summary>
/// <c>--journal-prompts &lt;journals-dir&gt; &lt;out.jsonl&gt;</c>: renders the chronicler's prompts for every
/// group of every journal dumped by the CLI's <c>journal dump</c>, under every variant
/// <c>tools/journal/</c> compares — so the harness sends exactly what the game would, and holds no
/// copy of the prompts or of the way a group is put to the model.
///
/// <para>Each line is one (grouping, group, variant): the system prompt (with the setting rule, as
/// <c>CreateInstanceAsync</c> appends it), the user message with <see cref="ChroniclerPrompt.MemoryPlaceholder"/>
/// standing for the memory the harness fills in as it writes, and the group's entries themselves for
/// the fidelity checks. Variants differ from <see cref="ChroniclerPrompt.Shipped"/> on one axis at a time.</para>
/// </summary>
public static class ChroniclerBench
{
    public static void Run(string dir, string outPath)
    {
        var files = Directory.GetFiles(dir, "*.json").OrderBy(f => f, StringComparer.Ordinal).ToList();
        var shipped = ChroniclerPrompt.Shipped;
        var variants = new List<(string Axis, string Name, ChronicleConfig Config)> { ("shipped", "shipped", shipped) };
        foreach (var v in Enum.GetValues<ChronicleVoice>().Where(v => v != shipped.Voice))
            variants.Add(("voice", v.ToString(), shipped with { Voice = v }));
        foreach (var i in Enum.GetValues<ChronicleInputs>().Where(i => i != shipped.Inputs))
            variants.Add(("inputs", i.ToString(), shipped with { Inputs = i }));
        foreach (var m in Enum.GetValues<ChronicleMemory>().Where(m => m != shipped.Memory))
            variants.Add(("memory", m.ToString(), shipped with { Memory = m }));
        foreach (var t in new[] { 90, 260 })
            variants.Add(("length", $"max{t}", shipped with { MaxTokens = t }));

        using var w = new StreamWriter(outPath);
        int lines = 0;
        foreach (var f in files)
        {
            var journal = JsonSerializer.Deserialize<Journal>(File.ReadAllText(f)) ?? new Journal();
            string source = Path.GetFileNameWithoutExtension(f);
            foreach (var (grouping, groups) in Groupings(journal))
                foreach (var (gid, entries) in groups)
                {
                    if (!entries.Any(e => e.Kind != JournalEntryKind.Milestone && e.Source.Length > 0)) continue;
                    // Grouping is its own axis: only the shipped variant is rendered for the others.
                    foreach (var (axis, name, config) in grouping == "visit" ? variants : variants.Take(1).ToList())
                    {
                        var row = new Dictionary<string, object?>
                        {
                            ["source"] = source,
                            ["grouping"] = grouping,
                            ["group"] = gid,
                            ["axis"] = grouping == "visit" ? axis : "grouping",
                            ["variant"] = grouping == "visit" ? name : grouping,
                            ["voice"] = config.Voice.ToString(),
                            ["memory"] = config.Memory.ToString(),
                            ["max_tokens"] = config.MaxTokens,
                            ["system"] = $"{ChroniclerPrompt.System[config.Voice].TrimEnd()}\n\n{SceneSetting.Rule}",
                            ["summary_system"] = $"{ChroniclerPrompt.SummarySystem.TrimEnd()}\n\n{SceneSetting.Rule}",
                            ["user"] = ChroniclerPrompt.UserMessage(entries, config, ChroniclerPrompt.MemoryPlaceholder),
                            ["entries"] = entries.Select(e => new { kind = e.Kind.ToString(), block = e.Block?.ToString(),
                                                                    actor = e.Actor, companion = e.ByCompanion,
                                                                    place = e.Place, area = e.Area, day = e.Day, text = e.Source }),
                        };
                        w.WriteLine(JsonSerializer.Serialize(row));
                        lines++;
                    }
                }
        }
        Console.WriteLine($"journal-prompts: {files.Count} journal(s) → {lines} prompt(s) in {outPath}");
    }

    /// <summary>
    /// The three groupings: one visit (as the game groups), one event (a visit cut after each outcome —
    /// one act and what came of it), and one day (every visit of a calendar day together).
    /// </summary>
    private static IEnumerable<(string, List<(int, List<JournalEntry>)>)> Groupings(Journal j)
    {
        var visits = j.Entries.GroupBy(e => e.Group).Select(g => (g.Key, g.ToList())).ToList();
        yield return ("visit", visits);

        var events = new List<(int, List<JournalEntry>)>();
        int n = 0;
        foreach (var (_, entries) in visits)
        {
            var cur = new List<JournalEntry>();
            foreach (var e in entries)
            {
                cur.Add(e);
                if (e.Kind == JournalEntryKind.Narration && e.Block == NarrationBlockType.Outcome)
                {
                    events.Add((++n, cur));
                    cur = new List<JournalEntry>();
                }
            }
            if (cur.Count > 0) events.Add((++n, cur));
        }
        yield return ("event", events);

        yield return ("day", j.Entries.GroupBy(e => e.Day).Select(g => (g.Key, g.ToList())).ToList());
    }
}
