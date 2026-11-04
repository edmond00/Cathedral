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
/// <para>Every modus mentis's persona texts are checked too: the model reads them on every request, so
/// a listed word in one is an invitation to write it.</para>
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

        // The persona texts reach the model on every request a modus mentis makes; a listed word in
        // one is an invitation to write it, and the narration then disagrees with its own prompt.
        foreach (var mm in ModusMentisRegistry.Instance.GetAllModiMentis())
            foreach (var (field, text) in new[]
            {
                ("PersonaPrompt", mm.PersonaPrompt), ("PersonaTone", mm.PersonaTone),
                ("PersonaReminder", mm.PersonaReminder), ("PersonaReminder2", mm.PersonaReminder2),
                ("StyleInstruction", mm.StyleInstruction), ("SkillMeans", mm.SkillMeans),
                ("MenuDescription", mm.MenuDescription), ("DisplayName", mm.DisplayName),
            })
                foreach (var form in ForbiddenWordsDictionary.FindForms(text ?? "").Distinct())
                    failures.Add($"{mm.ModusMentisId}.{field} holds the listed form '{form}'");

        RegisterChecks(placeholders, failures);

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

    // The persona registers (PersonaLexicons): the same structural rules, a known-rewrite table per
    // register at a share of 100, the streaming promise, and the keyword a flavoured noun keeps.
    private static void RegisterChecks(HashSet<string> placeholders, List<string> failures)
    {
        foreach (var register in Enum.GetValues<Register>())
        {
            var entries = PersonaLexicons.For(register);
            foreach (var e in entries)
            {
                if (placeholders.Contains(e.Form) || placeholders.Contains(e.Replacement))
                    failures.Add($"{register}: '{e.Form}' → '{e.Replacement}' is a NameFaking placeholder");
                if (ForbiddenWordsDictionary.FindForms(e.Replacement).FirstOrDefault() is { } bad)
                    failures.Add($"{register}: '{e.Form}' → '{e.Replacement}' writes the forbidden '{bad}'");
            }

            var flavour = PersonaFlavour.Of(100, register);
            int ok = 0, cases = RegisterSentences[register].Length;
            foreach (var (input, expected) in RegisterSentences[register])
            {
                string got = flavour.Apply(input);
                if (got == expected) ok++;
                else failures.Add($"{register}: \"{input}\"\n      → \"{got}\"\n      expected \"{expected}\"");
            }
            Console.WriteLine($"  {register}: {entries.Count} forms, {ok}/{cases} sentences rewritten as expected");
        }

        // A word two registers replace differently goes to the first a mind declares; listed, not failed.
        var clashes = PersonaLexicons.For(Register.Archaic)
            .Join(PersonaLexicons.For(Register.Rustic), a => a.Form.ToLowerInvariant(), r => r.Form.ToLowerInvariant(), (a, r) => (a, r))
            .Where(p => p.a.Replacement != p.r.Replacement)
            .Select(p => $"{p.a.Form} → {p.a.Replacement} / {p.r.Replacement}").ToList();
        if (clashes.Count > 0) Console.WriteLine($"  registers disagree on: {string.Join(", ", clashes)}");

        // The preview rewrites the growing text on every streamed word, so a prefix must flavour the
        // way the whole text does: every word but the one still arriving must already be final. And a
        // line is voiced whole or not at all, never one word in two.
        var streamed = PersonaFlavour.Of(PersonaFlavour.SharePercent, Register.Rustic, Register.Archaic);
        int voicedLines = 0;
        var lines = new[]
        {
            "Yes, I think the child has nothing, yes, nothing at all, and the boy has nothing either, yes.",
            "Open your purse and tell me what you saw, my friend.",
            "The girls and the children ran until evening, almost to the river.",
            "Perhaps it is nothing; perhaps you will find something in your pack.",
        };
        foreach (var stream in lines)
        {
            var swaps = new List<Lexicon.Swap>();
            string whole = streamed.Apply(stream, swaps: swaps);
            if (swaps.Count > 0) voicedLines++;
            if (swaps.Count > 0 && PersonaFlavour.Of(100, Register.Rustic, Register.Archaic).Apply(stream) != whole)
                failures.Add($"a voiced line was voiced only in part: \"{whole}\"");
            for (int k = 1; k < stream.Length; k++)
            {
                if (stream[k] != ' ') continue;
                string prefix = streamed.Apply(stream[..k]);
                if (!whole.StartsWith(prefix[..Math.Max(0, prefix.LastIndexOf(' '))], StringComparison.Ordinal))
                {
                    failures.Add($"streamed prefix \"{prefix}\" disagrees with the whole \"{whole}\"");
                    break;
                }
            }
            Console.WriteLine($"  {(swaps.Count > 0 ? "voiced" : "plain ")}: \"{whole}\"");
        }

        // A flavoured noun keeps the keyword of the noun it replaced.
        var archaic = PersonaFlavour.Of(100, Register.Archaic);
        // "the", not "a": with no tagger loaded (as here) the rule-based noun finder skips a word after "a".
        // "gatehouse" outranks "steed" on every ground but naming the horse, so only the lemma map picks it.
        string text = archaic.Apply("I spy the horse tethered by the gatehouse, the rope at its neck.");
        var keywords = KeywordExtractor.ExtractKeywords(text, "horse", 1, ownName: "Horse",
                                                        flavourLemmas: archaic.KeywordLemmas);
        var unmapped = KeywordExtractor.ExtractKeywords(text, "horse", 1, ownName: "Horse");
        if (unmapped.FirstOrDefault() == "steed")
            failures.Add($"keyword test no longer discriminates: \"{text}\" picks 'steed' without the lemma map");
        if (keywords.FirstOrDefault() != "steed")
            failures.Add($"keyword of \"{text}\" for a horse is '{keywords.FirstOrDefault()}', expected 'steed'");
        else
            Console.WriteLine($"  keyword kept: \"{text}\" → 'steed' names the horse");

        // A kenning keeps it too, whether the tagger reads "bite-iron" as one word or as two: the
        // lemma map holds the whole and its last part. Read with the tagger the game loads.
        NounExtractor.InitializeAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "catalyst-models"))
                     .GetAwaiter().GetResult();
        var feral = PersonaFlavour.Of(100, Register.Feral);
        string kenning = feral.Apply("I smell the knife left by the gatehouse, under the rain.");
        var kennKeys = KeywordExtractor.ExtractKeywords(kenning, "knife", 1, flavourLemmas: feral.KeywordLemmas);
        if (kennKeys.FirstOrDefault() is not ("bite-iron" or "iron"))
            failures.Add($"keyword of \"{kenning}\" for a knife is '{kennKeys.FirstOrDefault()}', expected the kenning");
        else
            Console.WriteLine($"  keyword kept: \"{kenning}\" → '{kennKeys[0]}' names the knife");

        // People speak in their standing's register (NamedNpcArchetype.Registers).
        var speakers = typeof(Npc.NamedNpcArchetype).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(Npc.NamedNpcArchetype)) && t.GetConstructor(Type.EmptyTypes) != null)
            .Select(t => (Npc.NamedNpcArchetype)Activator.CreateInstance(t)!)
            .Where(a => a.CanSpeak).ToList();
        var byRegister = speakers.GroupBy(a => a.Registers.Count == 0 ? "plain" : string.Join("+", a.Registers))
                                 .OrderBy(g => g.Key)
                                 .Select(g => $"{g.Key} {g.Count()} ({string.Join(", ", g.Select(a => a.ArchetypeId).Take(4))}{(g.Count() > 4 ? ", …" : "")})");
        Console.WriteLine($"  people: {string.Join("; ", byRegister)}");
        foreach (var a in speakers.Where(a => a.Social == Npc.SocialCategory.Peasant && !a.Registers.Contains(Register.Rustic)))
            failures.Add($"{a.ArchetypeId}: a peasant who does not speak Rustic");
        Console.WriteLine();
    }

    private static readonly Dictionary<Register, (string, string)[]> RegisterSentences = new()
    {
        [Register.Archaic] = new[]
        {
            ("Yes, there is nothing in the barrel.", "Aye, there is naught in the barrel."),
            ("It is a fine horse.", "'Tis a fine steed."),
            ("I fear it is late.", "I fear 'tis late."),
            ("Perhaps the horses will come soon.", "Mayhap the steeds will come anon."),
            ("I stand between the stones until morning.", "I stand betwixt the stones till morn."),
            ("an evening sky", "an eventide firmament"),
            ("a horse's mane", "a steed's mane"),
            ("The girl looks over there.", "The maiden looks yonder."),
            // Left alone: other senses of the period's own words.
            ("I stand before the gate.", "I stand before the gate."),
            ("After a while, I go.", "After a while, I go."),
            ("Father Ansel blesses the bread.", "Father Ansel blesses the bread."),
            ("I see a cart; I saw the gate.", "I behold a cart; I beheld the gate."),
            ("Farewell, and peace be with you. Rest in peace.", "Vale, and pax tecum. Requiescat in pace."),
            // Left alone: "I see" that understands, and a farewell bidden.
            ("I see that you lied. I see. I bid him farewell.", "I see that you lied. I see. I bid him farewell."),
        },
        [Register.Rustic] = new[]
        {
            ("Yes, I think the child is hungry.", "Aye, I reckon the bairn is hungry."),
            ("There is nothing in my pack.", "There is nowt in me pack."),
            ("The children and the girls laugh.", "The bairns and the lasses laugh."),
            ("a little boy", "a wee lad"),
            ("Did you see your money?", "Did ye see yer brass?"),
            ("It is nearly done, I suppose so.", "It is nigh on done, I s'pose so."),
            // Left alone: "think of" is not an opinion.
            ("I think of home.", "I think of home."),
            ("I thought nothing of it.", "I thought nowt of it."),
            ("I see a lass; I saw nothing.", "I spy a lass; I spied nowt."),
        },
        [Register.Cant] = new[]
        {
            ("My friend stole the man's purse.", "My mate nicked the cove's poke."),
            ("The men have money and food.", "The coves have chink and grub."),
            ("They threw him in prison, a very old man.", "They threw him in clink, a proper old cove."),
            ("Keep your eyes open.", "Keep your peepers open."),
            // Left alone: "very" meaning "itself", and "very much".
            ("At the very end I thank you very much.", "At the very end I thank you very much."),
            ("I read it in your very eyes.", "I read it in your very peepers."),
        },
        [Register.Childish] = new[]
        {
            ("My mother says the rabbit is very big.", "My mam says the bunny is ever so big."),
            ("I was frightened of the huge dog.", "I was scared of the great big dog."),
            ("An enormous barn and a tiny stomach.", "A great big barn and a teeny tummy."),
            ("Mother laughs; father is angry.", "Mam laughs; da is cross."),
            ("I see a rabbit. I saw a cat.", "I can see a bunny. I saw a cat."),
            ("It is a very old and difficult road.", "It is an ever so old and hard road."),
            // Left alone: a priest, a prioress, "by and large", "very well".
            ("Father Ansel speaks to Mother Superior.", "Father Ansel speaks to Mother Superior."),
            ("By and large, very well.", "By and large, very well."),
        },
        [Register.Learned] = new[]
        {
            ("I begin to think about the idea.", "I commence to consider the notion."),
            ("She bought a tool and tried to sell it.", "She purchased an instrument and endeavoured to sell it."),
            ("It seems he talked to the reeve so that we could buy bread.", "It appears he conversed with the reeve in order that we could purchase bread."),
            ("In the middle of the square, because of the rain, I looked at the sky.", "In the midst of the square, owing to the rain, I regarded the sky."),
            ("I see the seal; I copy it word for word.", "I observe the seal; I copy it verbatim."),
            ("The law, that is to say the writ, binds him in good faith, namely the oath.", "The law, id est the writ, binds him bona fide, videlicet the oath."),
            ("I see what you mean, and I see why.", "I see what you mean, and I see why."),
            // Left alone: "so that's", and a look taken.
            ("So that's the way, and I take a look at it.", "So that's the way, and I take a look at it."),
        },
        [Register.Terse] = new[]
        {
            ("Very well, it is very cold.", "Well, it is cold."),
            ("It is a very old house, quite empty.", "It is an old house, empty."),
            ("I really must go in order to be able to rest.", "I must go to be able to rest."),
            ("In spite of the cold, I am able to walk.", "Despite the cold, I can walk."),
            ("Each and every man made a decision.", "Every man decided."),
            ("Actually, it is truly a fine day.", "It is a fine day."),
            ("It is, really, a fine day.", "It is a fine day."),
            // Left alone: what is not an intensifier.
            ("Not quite; quite a few, not really. Really?", "Not quite; quite a few, not really. Really?"),
            ("At the very end, I stop.", "At the very end, I stop."),
        },
        [Register.Feral] = new[]
        {
            ("The man draws a knife by the door.", "The two-leg draws a bite-iron by the den-mouth."),
            ("People sit by the fire with their children.", "Two-legs sit by the hot-light with their cubs."),
            ("A lantern hangs from a rope in the house.", "A caged-light hangs from a long-vine in the stone-den."),
            ("Then I see a man; I saw the door.", "Then my eyes find a two-leg; my eyes found the den-mouth."),
            ("I see a man.", "My eyes find a two-leg."),
            ("The men's swords and arrows.", "The two-legs' long-claws and flying-thorns."),
        },
    };

    /// <summary>
    /// Reads lines of <c>ModusMentisClassName&lt;TAB&gt;text</c> (or <c>Register&lt;TAB&gt;text</c>), runs each text through Layer 1 and then
    /// the flavour of that modus mentis, as the rewriter does, and prints every line the flavour changed
    /// with the words it swapped, then the share of lines changed and the words most swapped.
    /// </summary>
    public static void ScanFlavour(string path)
    {
        var byClass = ModusMentisRegistry.Instance.GetAllModiMentis().ToDictionary(m => m.GetType().Name);
        int lines = 0, flavoured = 0, swapsTotal = 0;
        var counts = new Dictionary<string, int>();
        foreach (var line in File.ReadLines(path))
        {
            var tab = line.IndexOf('\t');
            if (tab < 0) continue;
            // The first column names a modus mentis class, or a register outright (a person's lines).
            string who = line[..tab];
            PersonaFlavour? flavour = byClass.TryGetValue(who, out var mm) ? PersonaFlavour.For(mm)
                                    : Enum.TryParse<Register>(who, out var register) ? PersonaFlavour.Of(PersonaFlavour.SharePercent, register)
                                    : null;
            if (flavour == null) continue;
            lines++;
            string text = ForbiddenWordsDictionary.Apply(line[(tab + 1)..]);
            var swaps = new List<Lexicon.Swap>();
            string voiced = flavour.Apply(text, swaps: swaps);
            if (swaps.Count == 0) continue;
            flavoured++; swapsTotal += swaps.Count;
            foreach (var sw in swaps) { string k = $"{sw.Original.ToLowerInvariant()} → {sw.Replacement}"; counts[k] = counts.GetValueOrDefault(k) + 1; }
            Console.WriteLine($"[{mm?.ModusMentisId ?? "person"}: {string.Join(", ", flavour.Registers)}]\n- {text}\n+ {voiced}\n");
        }
        Console.WriteLine($"{flavoured} of {lines} lines took the flavour, {swapsTotal} words in all");
        foreach (var (k, n) in counts.OrderByDescending(kv => kv.Value)) Console.WriteLine($"  {n,4}  {k}");
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
        ("Heaven knows why.", "Heaven knows why."),
        ("Inclines toward the game over the errand", "Inclines toward the game over the errand"),
        ("the souls in Heaven", "the souls in the Garden"),
        ("two electric chairs", "two gallows"),
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
