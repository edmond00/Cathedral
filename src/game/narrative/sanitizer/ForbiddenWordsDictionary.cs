using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Sanitizer;

/// <summary>
/// Layer 1 of the text sanitization pipeline: direct word replacement, with no request made.
/// Every word or phrase in <see cref="ForbiddenWords"/> is replaced in place by a word of the world —
/// "a mile" → "a league", "last week" → "last eleven-day", "the Moon" → "the eggmoon". The rewrite
/// itself, and what keeps it grammatical, is <see cref="Lexicon"/>.
/// </summary>
public static class ForbiddenWordsDictionary
{
    private static readonly Lexicon _lexicon = new(new ForbiddenWords().Build());

    /// <summary>Every form the list replaces, for audits and tests.</summary>
    public static IReadOnlyCollection<Lexicon.Entry> Entries => _lexicon.Entries;

    /// <summary>
    /// Replaces every forbidden form in <paramref name="text"/>; a name the request supplied
    /// (<paramref name="sourceVocabulary"/>) is kept. See <see cref="Lexicon.Rewrite"/>.
    /// </summary>
    public static string Apply(string text, IReadOnlySet<string>? sourceVocabulary = null)
        => _lexicon.Rewrite(text, sourceVocabulary);

    /// <summary>The forms of <paramref name="text"/> that <see cref="Apply"/> would replace.</summary>
    public static IReadOnlyList<string> FindForms(string text, IReadOnlySet<string>? sourceVocabulary = null)
    {
        var swaps = new List<Lexicon.Swap>();
        _lexicon.Rewrite(text, sourceVocabulary, swaps);
        return swaps.ConvertAll(s => s.Original);
    }
}
