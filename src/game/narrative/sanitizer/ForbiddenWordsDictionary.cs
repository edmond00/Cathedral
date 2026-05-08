using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Cathedral.Game.Narrative.Sanitizer;

/// <summary>
/// Layer 1 of the text sanitization pipeline.
/// Direct word replacement with lowercase normalization and basic plural/stem handling.
/// Replaces forbidden modern/informal tokens in-place, preserving surrounding capitalisation.
/// </summary>
public static class ForbiddenWordsDictionary
{
    // ── Replacement map (stem → medieval equivalent) ───────────────────────────
    // Keys are lowercase stems; values are the replacement phrase.
    // Plural and conjugated forms are reduced to their stem before lookup.
    private static readonly Dictionary<string, string> _map = new(StringComparer.OrdinalIgnoreCase)
    {
        // Informalisms
        { "okay",    "very well" },
        { "ok",      "very well" },
        { "alright", "very well" },
        { "hi",      "hail" },
        { "hey",     "hail" },
        { "yeah",    "aye" },
        { "yep",     "aye" },
        { "yup",     "aye" },
        { "nope",    "nay" },
        { "gonna",   "going to" },
        { "wanna",   "wish to" },
        { "kinda",   "somewhat" },
        { "sorta",   "somewhat" },
        { "gotta",   "must" },
        { "dunno",   "know not" },

        // Modern technology
        { "computer",     "arcane device" },
        { "electricity",  "lightning force" },
        { "internet",     "messenger web" },
        { "telephone",    "speaking stone" },
        { "phone",        "speaking stone" },
        { "television",   "vision glass" },
        { "factory",      "manufactory" },
        { "laboratory",   "alchemist workshop" },
        { "lab",          "workshop" },

        // Modern science terms
        { "chemistry",    "alchemy" },
        { "chemist",      "alchemist" },
        { "scientist",    "scholar" },
        { "experiment",   "trial" },

        // Modern political/social
        { "capitalism",   "merchant rule" },
        { "communism",    "commune law" },
        { "fascism",      "iron rule" },
        { "democracy",    "council rule" },
        { "parliament",   "high council" },
        { "president",    "high ruler" },
        { "senator",      "councillor" },
        { "congress",     "council" },

        // Modern education
        { "university",   "academy" },
        { "professor",    "master scholar" },

        // Anachronistic weapons
        { "pistol",       "hand crossbow" },
        { "rifle",        "long crossbow" },
        { "grenade",      "fire flask" },
        { "missile",      "bolt" },
        { "bomb",         "fire cask" },
    };

    // Matches individual word tokens (letters and apostrophes for contractions)
    private static readonly Regex _wordPattern =
        new(@"[a-zA-Z']+", RegexOptions.Compiled);

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Scans <paramref name="text"/> for forbidden tokens and replaces them.
    /// Capitalisation of the first letter is preserved when the original token started with a capital.
    /// </summary>
    public static string Apply(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var sb = new StringBuilder(text.Length);
        int lastIndex = 0;

        foreach (Match match in _wordPattern.Matches(text))
        {
            // Copy everything between the previous match and this one unchanged
            sb.Append(text, lastIndex, match.Index - lastIndex);

            string token = match.Value;
            string stem  = Stem(token.ToLowerInvariant());

            if (_map.TryGetValue(stem, out string? replacement))
            {
                sb.Append(ApplyCasing(replacement, token));
            }
            else
            {
                sb.Append(token);
            }

            lastIndex = match.Index + match.Length;
        }

        sb.Append(text, lastIndex, text.Length - lastIndex);
        return sb.ToString();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Very lightweight stemmer: strips common English plural/verb suffixes
    /// so that "computers", "okay'd", "labs" all map to their dictionary stems.
    /// </summary>
    private static string Stem(string lower)
    {
        // Contractions: strip trailing 's or 't
        if (lower.EndsWith("'s", StringComparison.Ordinal) || lower.EndsWith("'t", StringComparison.Ordinal))
            lower = lower[..^2];

        // ies → y  (factories → factory)
        if (lower.Length > 4 && lower.EndsWith("ies", StringComparison.Ordinal))
            return lower[..^3] + "y";

        // es → (remove)  (factories handled above; boxes → box)
        if (lower.Length > 4 && lower.EndsWith("es", StringComparison.Ordinal))
        {
            string candidate = lower[..^2];
            if (_map.ContainsKey(candidate)) return candidate;
        }

        // s → (remove)  (phones → phone, labs → lab)
        if (lower.Length > 3 && lower.EndsWith("s", StringComparison.Ordinal))
        {
            string candidate = lower[..^1];
            if (_map.ContainsKey(candidate)) return candidate;
        }

        // ed → (remove)  (experimented → experiment)
        if (lower.Length > 4 && lower.EndsWith("ed", StringComparison.Ordinal))
        {
            string candidate = lower[..^2];
            if (_map.ContainsKey(candidate)) return candidate;
        }

        // ing → (remove)  (computing → compute)
        if (lower.Length > 5 && lower.EndsWith("ing", StringComparison.Ordinal))
        {
            string candidate = lower[..^3];
            if (_map.ContainsKey(candidate)) return candidate;
        }

        return lower;
    }

    /// <summary>
    /// Applies the capitalisation pattern of <paramref name="original"/> to <paramref name="replacement"/>.
    /// ALL_CAPS → REPLACEMENT, Title → Replacement, lower → replacement.
    /// </summary>
    private static string ApplyCasing(string replacement, string original)
    {
        if (original.Length == 0) return replacement;

        // All-caps original
        if (original == original.ToUpperInvariant() && original.Length > 1)
            return replacement.ToUpperInvariant();

        // Title-case original (first letter upper)
        if (char.IsUpper(original[0]))
        {
            if (replacement.Length == 0) return replacement;
            return char.ToUpperInvariant(replacement[0]) + replacement[1..];
        }

        return replacement;
    }
}
