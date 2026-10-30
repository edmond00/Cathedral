using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cathedral.Game.Narrative.Sanitizer;

/// <summary>
/// <c>--forbidden-words-audit</c>: the Layer 1 word list checked against itself and against sentences
/// whose correct rewrite is known. Headless, no model.
///
/// <para>Three structural rules, each a silent fault otherwise:</para>
/// <list type="bullet">
/// <item>no form is a <see cref="NameFaking"/> placeholder — those stand for characters' real names
///   and are swapped back after Layer 1, so replacing one would rename a character;</item>
/// <item>every replacement survives a second pass unchanged — a replacement holding a listed form
///   ("throwing star") would be fixed on the next text and not this one;</item>
/// <item>no replacement is empty — removing a word leaves a hole in the sentence.</item>
/// </list>
/// <para>Then the sentences: each pairs an LLM-shaped line with the line it must become, covering the
/// casing, article and plural rules, and — as important — words that must be left alone because they
/// are ordinary in the period (<i>may</i>, <i>march</i>, <i>temple</i>, <i>second</i>, <i>a pen of
/// sheep</i>).</para>
///
/// <para><c>--forbidden-words-scan &lt;file&gt;</c> applies the list to every line of a text file and
/// prints what it changed: the loop that grows the list is to generate narration with the shipped
/// model, scan it, and read what is left.</para>
/// </summary>
public static class ForbiddenWordsAudit
{
    public static void Run()
    {
        Console.WriteLine("=== FORBIDDEN WORDS AUDIT ===\n");
        var failures = new List<string>();
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var entries = ForbiddenWordsDictionary.Entries;   // the first touch builds the table and the pattern
        double buildMs = clock.Elapsed.TotalMilliseconds;
        const string sample = "Three weeks ago, under the full moon, the professor checked his phone and walked a mile to the church.";
        clock.Restart();
        for (int i = 0; i < 1000; i++) ForbiddenWordsDictionary.Apply(sample);
        Console.WriteLine($"  built in {buildMs:F0} ms; {clock.Elapsed.TotalMilliseconds / 1000 * 1000:F0} µs per sentence");
        Console.WriteLine($"  \"{sample}\"\n  → \"{ForbiddenWordsDictionary.Apply(sample)}\"\n");

        Console.WriteLine($"  {entries.Count} forms, {entries.Count(e => e.Proper)} of them proper nouns, " +
                          $"{entries.Select(e => e.Replacement).Distinct().Count()} distinct replacements\n");

        var placeholders = NameFaking.MaleNames.Concat(NameFaking.FemaleNames)
                                     .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var e in entries.Where(e => placeholders.Contains(e.Form)))
            failures.Add($"'{e.Form}' is a NameFaking placeholder");

        foreach (var e in entries.Where(e => e.Replacement != e.Form))   // a kept phrase is its own form
        {
            if (string.IsNullOrWhiteSpace(e.Replacement))
                failures.Add($"'{e.Form}' is replaced by nothing");
            else if (ForbiddenWordsDictionary.FindForms(e.Replacement).FirstOrDefault() is { } inner)
                failures.Add($"'{e.Form}' → '{e.Replacement}' holds the listed form '{inner}'");
        }

        int passed = 0;
        foreach (var (input, expected) in Sentences)
        {
            string got = ForbiddenWordsDictionary.Apply(input);
            if (got == expected) { passed++; continue; }
            failures.Add($"\"{input}\"\n      → \"{got}\"\n      expected \"{expected}\"");
        }
        // A name the request supplied is spared mid-sentence; the same word in lower case is not.
        var supplied = SourceVocabulary.From("The brothers of the Ashen Star keep the moon-vigil.");
        foreach (var (input, expected) in new[]
        {
            ("I met a brother of the Ashen Star.", "I met a brother of the Ashen Star."),
            ("A star fell over the Ashen Star.",    "An ember fell over the Ashen Star."),
        })
        {
            string got = ForbiddenWordsDictionary.Apply(input, supplied);
            if (got == expected) passed++;
            else failures.Add($"(supplied name) \"{input}\"\n      → \"{got}\"\n      expected \"{expected}\"");
        }
        Console.WriteLine($"  {passed}/{Sentences.Length + 2} sentences rewritten as expected\n");

        if (failures.Count == 0)
        {
            Console.WriteLine("  ✓ no faults");
            return;
        }
        foreach (var f in failures) Console.WriteLine($"  ✗ {f}");
        Console.WriteLine($"\n--- {failures.Count} FAILURE(S) ---");
    }

    /// <summary>Writes the whole list as tab-separated rows: category, form, replacement, proper.</summary>
    public static void Dump(string path)
    {
        using var w = new StreamWriter(path);
        w.WriteLine("category\tform\treplacement\tcase");
        foreach (var e in ForbiddenWordsDictionary.Entries.Where(e => e.Replacement != e.Form)
                     .OrderBy(e => e.Category).ThenBy(e => e.Form, StringComparer.OrdinalIgnoreCase))
            w.WriteLine($"{e.Category}\t{e.Form}\t{e.Replacement}\t{(e.Proper ? (e.MidSentenceOnly ? "name, mid-sentence" : "name") : "any")}{(e.Counted != null ? ", counted" : "")}");
        Console.WriteLine($"{ForbiddenWordsDictionary.Entries.Count} forms written to {path}");
    }

    /// <summary>Applies the list to each line of <paramref name="path"/> and prints every change.</summary>
    public static void Scan(string path)
    {
        int lines = 0, changed = 0;
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadLines(path))
        {
            lines++;
            var forms = ForbiddenWordsDictionary.FindForms(line).ToList();
            if (forms.Count == 0) continue;
            changed++;
            foreach (var f in forms) counts[f] = counts.GetValueOrDefault(f) + 1;
            Console.WriteLine($"- {line}\n+ {ForbiddenWordsDictionary.Apply(line)}\n");
        }
        Console.WriteLine($"{changed} of {lines} lines changed");
        foreach (var (form, n) in counts.OrderByDescending(kv => kv.Value))
            Console.WriteLine($"  {n,4}  {form}");
    }

    // Input, and exactly what it must become.
    private static readonly (string, string)[] Sentences =
    {
        // Plurals and verb forms are paired, never stemmed.
        ("The computers hummed.", "The reckoning engines hummed."),
        ("Three miles to the north.", "Three leagues to the north."),
        ("We walked for a mile.", "We walked for a league."),
        ("Five years had passed.", "Five rounds had passed."),
        ("a ten-year-old boy", "a ten-round-old boy"),
        ("It took centuries.", "It took hundreds of rounds."),
        ("He reigned six centuries.", "He reigned six hundred rounds."),
        ("a few centuries later", "a few hundred rounds later"),
        ("A century ago", "A hundred rounds ago"),
        ("in 3 centuries", "in 300 rounds"),
        ("two millennia", "two thousand rounds"),
        ("three decades", "thirty rounds"),
        ("for decades", "for tens of rounds"),
        ("Two months later", "Sixty days later"),
        ("in a month", "in thirty days"),
        ("for 6 months", "for 180 days"),
        ("for months", "for seasons"),
        ("this month", "this season"),
        ("eleven months", "three hundred and thirty days"),
        ("a fortnight", "fourteen days"),
        ("twice a month", "twice in thirty days"),
        ("at a tenth a month", "at a tenth in thirty days"),
        ("Two or three a month go", "Two or three in thirty days go"),
        ("once a century", "once in a hundred rounds"),
        ("a month's wages", "thirty days' wages"),
        ("by the month's end", "by the season's end"),
        ("the Bible's verses", "the Ten Chapters' verses"),
        ("one August", "one Drought"),
        ("last Christmas", "last Remnant Days"),
        ("Minute and devoted", "Minute and devoted"),
        ("wait a minute", "wait a moment"),
        ("twenty minutes", "twenty moments"),
        ("You experiment with crops.", "You experiment with crops."),
        ("Discounts the risk", "Discounts the risk"),
        ("bleached pale by sun", "bleached pale by sun"),
        ("The military order", "The military order"),
        ("Electric blue", "Vivid blue"),
        ("Weeks went by.", "Eleven-days went by."),
        ("The barrel exploded.", "The barrel burst."),

        // Articles are re-agreed, and an article the replacement carries yields to the text's.
        ("I waited a week.", "I waited an eleven-day."),
        ("A star fell.", "An ember fell."),
        ("an hour and a minute", "an hour and a moment"),
        ("The Earth trembled.", "The world trembled."),
        ("Nothing on Earth moves so.", "Nothing on the world moves so."),
        ("of Earth's oldest stones", "of the world's oldest stones"),
        ("They read the Bible aloud.", "They read the Ten Chapters aloud."),

        // Casing: shouted stays shouted, an acronym does not shout, a proper noun's replacement keeps
        // its own case mid-sentence, and anything at a sentence start is capitalised.
        ("OKAY, fine.", "VERY WELL, fine."),
        ("OK, then.", "Very well, then."),
        ("He said \"okay\" and left.", "He said \"very well\" and left."),
        ("\"Okay,\" he said.", "\"Very well,\" he said."),
        ("The old TV flickered.", "The old vision glass flickered."),
        ("a Roman road", "an imperial road"),
        ("Roman roads run straight.", "Imperial roads run straight."),
        ("It smelled of French wine.", "It smelled of foreign wine."),
        ("Napoleon would have smiled.", "Aqilon would have smiled."),
        ("as strong as Hercules", "as strong as Vhomanor"),
        ("By Zeus!", "By Qoth!"),
        ("God knows.", "Qoth knows."),
        ("The church bells rang for Christmas.", "The church bells rang for the Remnant Days."),

        // Possessives keep their apostrophe.
        ("the computer's lid", "the reckoning engine's lid"),
        ("the moon's light", "the eggmoon's light"),

        // Months and days: only the sense that is a name.
        ("It was the first of March.", "It was the first of the Weeping."),
        ("March onward!", "March onward!"),
        ("You may enter.", "You may enter."),
        ("May I enter?", "May I enter?"),
        ("an august presence", "an august presence"),
        ("in August", "in the Drought"),
        ("Every Sunday he rested.", "Every Silday he rested."),
        ("I'll come back on Monday.", "I'll come back on Hookday."),

        // Words that must be left alone: ordinary in the period, or a second sense.
        ("The second door was locked.", "The second door was locked."),
        ("He stood in the temple.", "He stood in the temple."),
        ("a pen of sheep", "a pen of sheep"),
        ("The cell was cold.", "The cell was cold."),
        ("She will train the hound.", "She will train the hound."),
        ("He planed the plank smooth.", "He planed the plank smooth."),
        ("an ancient queen", "an ancient queen"),
        ("a day's walk", "a day's walk"),
        ("He poured from the jet-black jug.", "He poured from the jet-black jug."),
        ("The sun-baked clay cracked.", "The sun-baked clay cracked."),
        ("a half-moon", "a half-eggmoon"),
        ("The tank of water", "The tank of water"),
        ("They crossed the moor.", "They crossed the moor."),
        ("Moonlight fell on the eggmoon.", "Eggmoon-light fell on the eggmoon."),
        ("starting over", "starting over"),
        ("a cathedral of stone", "a cathedral of stone"),
        ("the start of the march", "the start of the march"),
        ("For God's sake!", "For Qoth's sake!"),
        ("the priests of the Fallen God", "the priests of the Fallen God"),
        ("the salt magazine", "the salt magazine"),
        ("the Imperial Senate", "the Imperial Senate"),

        // Lore replacements.
        ("Airplanes and rockets crossed the sky.", "Submarines and submarines crossed the sky."),
        ("the vast universe", "the vast cosmic sea"),
        ("under the stars", "under the embers"),
        ("a dollar and ten cents", "a scute and ten pellets"),
        ("a pizza and a coffee", "a flatbread and a roasted brew"),
        ("the medieval village", "the old village"),
        ("a gun at his belt", "an arquebus at his belt"),
        ("a haiku about rain", "a short verse about rain"),
        ("the president of the guild", "the high ruler of the guild"),
        ("the mosque", "the temple"),
    };
}
