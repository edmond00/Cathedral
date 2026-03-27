using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Cathedral.Game.Narrative;

/// <summary>
/// A keyword enriched with surrounding context words.
/// The raw string format is "a little red &lt;flower&gt;" where the word inside &lt;...&gt;
/// is the actual keyword used for UI display and text matching, while the full phrase
/// (without angle brackets) is used in LLM prompts for richer context.
/// </summary>
public sealed record KeywordInContext
{
    private static readonly Regex TagPattern = new(@"<(\w+)>", RegexOptions.Compiled);

    private static readonly HashSet<string> ValidArticles = new(StringComparer.OrdinalIgnoreCase)
        { "a", "an", "the", "some", "this", "that", "these", "those" };

    /// <summary>Full context phrase without angle brackets — used in LLM prompts.</summary>
    public string Context { get; }

    /// <summary>The bare keyword word — used for UI display and text matching.</summary>
    public string Keyword { get; }

    private KeywordInContext(string context, string keyword)
    {
        Context = context;
        Keyword = keyword;
    }

    /// <summary>
    /// Parses a raw keyword-in-context string of the form "a little red &lt;flower&gt;".
    /// Throws <see cref="ArgumentException"/> if:
    /// <list type="bullet">
    ///   <item>The string does not contain exactly one &lt;word&gt; tag.</item>
    ///   <item>Stripping the tag leaves only one word (no context beyond the keyword itself).</item>
    ///   <item>The first word is not a recognised article.</item>
    /// </list>
    /// </summary>
    public static KeywordInContext Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("KeywordInContext cannot be null or empty.", nameof(input));

        var matches = TagPattern.Matches(input);
        if (matches.Count != 1)
            throw new ArgumentException(
                $"KeywordInContext must contain exactly one <word> tag, found {matches.Count}: \"{input}\"");

        string keyword = matches[0].Groups[1].Value;
        string context = TagPattern.Replace(input, keyword).Trim();

        var contextWords = context.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (contextWords.Length <= 1)
            throw new ArgumentException(
                $"KeywordInContext must have context words beyond just the keyword: \"{input}\"");

        if (!ValidArticles.Contains(contextWords[0]))
            throw new ArgumentException(
                $"KeywordInContext must start with an article (a, an, the, some, this, that, these, those), " +
                $"got \"{contextWords[0]}\": \"{input}\"");

        return new KeywordInContext(context, keyword);
    }

    public override string ToString() => Context;
}
