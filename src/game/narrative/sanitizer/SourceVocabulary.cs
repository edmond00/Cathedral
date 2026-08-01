using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Sanitizer;

/// <summary>
/// The words a rewrite request already handed to the model — the neutral line it was asked to
/// re-express, and the situation text around it. Everything in here is game-authored (scene names,
/// point-of-interest labels, item names, the dialogue subject), so by construction it is in-world:
/// when the sanitizer flags one of these it is a false positive, and "repairing" it makes the
/// rewrite say something the neutral line did not.
///
/// <para>
/// That was the "Square-Mill" case: the scene named a lane <c>square-mill lane</c>, the rewrite
/// faithfully kept the name, and WikiNER read it as a real-world place. The rewriter then demoted it
/// ("Square-Mill path"), which is both wrong and unfixable — the next rewrite of that lane hands the
/// model the same name again.
/// </para>
///
/// <para>
/// Matching is per <i>token</i>, on the token's word parts: a detected <c>Square-Mill</c> splits into
/// <c>square</c> + <c>mill</c> and is exempt only if the source contained <b>both</b>. Splitting on
/// everything that is not a letter or digit is what makes the comparison survive the punctuation the
/// two sides disagree on — the source wrote an en-dash, the model a hyphen, and a plain string
/// compare would have missed it.
/// </para>
///
/// <para>
/// This covers Layers 2 and 3 (the detection that triggers the LLM rewrite). Layer 1
/// (<see cref="ForbiddenWordsDictionary"/>) is a flat deterministic replacement applied before any
/// detection and is deliberately left alone: it cannot truncate or reword a sentence, so it has none
/// of the failure mode this guard exists for.
/// </para>
/// </summary>
public static class SourceVocabulary
{
    /// <summary>
    /// Builds the exemption set from the texts a request was given. Nulls and blanks are skipped, so
    /// callers can pass their optional prompt pieces straight through.
    /// <para>
    /// Pass only game-authored text. Model-authored text (the persona's own free reasoning, say) must
    /// stay out: exempting it would let one hallucinated name license itself in every later sentence.
    /// </para>
    /// </summary>
    public static IReadOnlySet<string> From(params string?[] sources)
    {
        var vocabulary = new HashSet<string>(StringComparer.Ordinal);
        if (sources == null) return vocabulary;

        foreach (var source in sources)
            AddWords(source, vocabulary);

        return vocabulary;
    }

    /// <summary>Same, for a sequence of texts (e.g. the option labels a choice was offered over).</summary>
    public static IReadOnlySet<string> From(IEnumerable<string?> sources)
    {
        var vocabulary = new HashSet<string>(StringComparer.Ordinal);
        if (sources == null) return vocabulary;

        foreach (var source in sources)
            AddWords(source, vocabulary);

        return vocabulary;
    }

    /// <summary>
    /// True when <paramref name="token"/> is made entirely of words the source text already used, and
    /// so must not be treated as a detection hit. A token with no word characters at all is never
    /// exempt, nor is anything at all when the vocabulary is null or empty.
    /// </summary>
    public static bool Contains(IReadOnlySet<string>? vocabulary, string? token)
    {
        if (vocabulary == null || vocabulary.Count == 0 || string.IsNullOrEmpty(token))
            return false;

        bool sawWord = false;
        foreach (var word in Words(token))
        {
            sawWord = true;
            if (!vocabulary.Contains(word)) return false;
        }

        return sawWord;
    }

    private static void AddWords(string? text, HashSet<string> into)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        foreach (var word in Words(text))
            into.Add(word);
    }

    /// <summary>
    /// Lowercased runs of letters/digits. Every other character — hyphen, en-dash, apostrophe,
    /// quote, comma — is a separator, so the same name written two ways yields the same words.
    /// </summary>
    private static IEnumerable<string> Words(string text)
    {
        int start = -1;
        for (int i = 0; i <= text.Length; i++)
        {
            bool isWordChar = i < text.Length && char.IsLetterOrDigit(text[i]);
            if (isWordChar)
            {
                if (start < 0) start = i;
            }
            else if (start >= 0)
            {
                yield return text.Substring(start, i - start).ToLowerInvariant();
                start = -1;
            }
        }
    }
}
