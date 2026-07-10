using System;
using System.Collections.Generic;
using System.IO;

namespace Cathedral.Game.Narrative.Sanitizer;

/// <summary>
/// Common-word guard for Layer 2 (WikiNER) of the text sanitization pipeline.
///
/// WikiNER flags any capitalised token as a possible real-world named entity, which
/// produces false positives for ordinary common words that merely happen to be
/// capitalised (e.g. "Nostalgia" at the start of a sentence). This lexicon lets the
/// pipeline drop those: a flagged token is treated as a genuine name only if it does
/// NOT exist as a lowercase entry in a standard English dictionary.
///
/// Backed by a SCOWL common-word list (<c>en_scowl_40.txt</c>) placed in <c>models/</c>.
/// Size 40 is chosen deliberately: it is small enough to exclude obscure lowercase
/// homographs of famous names ("napoleon" the pastry, "japan" the lacquer) so those
/// names still get rewritten, yet large enough to keep ordinary common words
/// ("nostalgia", "march", "nice"). See notes in the pipeline for the size trade-off.
///
/// Case handling is deliberate and case-SENSITIVE:
///   • "nostalgia" has a lowercase entry  → common word → dropped (false positive).
///   • "Rome" exists only capitalised      → not a common word → kept (real name).
/// Storing the dictionary lowercased would wrongly spare "Rome", so entries are kept
/// in their original case and membership is tested against the token's lowercase form.
/// </summary>
public static class CommonWordLexicon
{
    /// <summary>Word list expected in <c>models/</c>.</summary>
    private const string DictionaryFileName = "en_scowl_40.txt";

    // Original-case entries (ordinal, case-sensitive). A lowercase query only matches
    // if the dictionary actually contains that word in lowercase.
    private static HashSet<string>? _words;

    public static bool IsReady => _words is { Count: > 0 };

    // ── Initialisation ──────────────────────────────────────────────────────────

    /// <summary>
    /// Loads the dictionary from <c>models/en_US.dic</c>. Idempotent and non-fatal:
    /// if the file is missing or unreadable the guard simply stays disabled and the
    /// pipeline behaves as if no word is ever a common word (no filtering).
    /// </summary>
    public static void Initialize()
    {
        if (_words != null) return;

        try
        {
            var path = ResolveDictionaryPath();
            if (path == null)
            {
                Console.Error.WriteLine(
                    $"CommonWordLexicon: '{DictionaryFileName}' not found in models/; Layer 2 guard disabled.");
                _words = new HashSet<string>(StringComparer.Ordinal); // mark initialised-but-empty
                return;
            }

            var words = new HashSet<string>(StringComparer.Ordinal);
            using var reader = new StreamReader(path);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                // Format-agnostic: works for both a SCOWL list (multi-line text header,
                // URLs, then one word per line) and a Hunspell .dic ("count" line, then
                // "word/AFFIXFLAGS"). Strip any affix flags, then accept only tokens that
                // look like a word — this rejects headers, blank lines, URLs and counts.
                var word = line.Trim();
                int slash = word.IndexOf('/');
                if (slash >= 0) word = word[..slash];

                if (IsWordToken(word)) words.Add(word);
            }

            _words = words;
            Console.WriteLine($"CommonWordLexicon: loaded {_words.Count} words from {Path.GetFileName(path)}.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CommonWordLexicon: failed to load dictionary, guard disabled. {ex.Message}");
            _words = new HashSet<string>(StringComparer.Ordinal);
        }
    }

    /// <summary>
    /// True if <paramref name="s"/> looks like a dictionary word: starts with a letter
    /// and contains only letters, apostrophes or hyphens. Rejects blank lines, header
    /// prose (has whitespace), URLs (contain ':' '.' '/') and count lines (digits).
    /// </summary>
    private static bool IsWordToken(string s)
    {
        if (s.Length == 0 || !char.IsLetter(s[0]))
            return false;

        foreach (char c in s)
            if (!char.IsLetter(c) && c != '\'' && c != '-')
                return false;

        return true;
    }

    // ── Guard ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// True if <paramref name="token"/> is an ordinary English common word — i.e. its
    /// lowercase form appears as a lowercase dictionary entry. Proper nouns that only
    /// exist capitalised in the dictionary (Rome, Versailles) return false and are
    /// therefore kept by the caller. Returns false when the guard is unavailable.
    /// </summary>
    public static bool IsCommonWord(string token)
    {
        if (_words == null || _words.Count == 0 || string.IsNullOrWhiteSpace(token))
            return false;

        return _words.Contains(token.ToLowerInvariant());
    }

    // ── Path resolution (mirrors WordEmbedding.ResolveVectorsPath) ───────────────

    private static string? ResolveDictionaryPath()
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir != null && !Directory.Exists(Path.Combine(dir, "models")))
            dir = Directory.GetParent(dir)?.FullName;
        if (dir == null) return null;

        var candidate = Path.Combine(dir, "models", DictionaryFileName);
        return File.Exists(candidate) ? candidate : null;
    }
}
