using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Represents a text segment with optional keyword information.
/// Used for rendering narration with highlighted keywords.
/// </summary>
public class TextSegment
{
    public string Text { get; set; } = "";
    public bool IsKeyword { get; set; }
    public string? KeywordValue { get; set; }  // The actual keyword if IsKeyword=true
}

/// <summary>
/// Parses narration text to identify and highlight keywords.
/// Converts text into segments that can be rendered with different colors.
/// </summary>
public class KeywordRenderer
{
    /// <summary>
    /// Generates morphological variations of a keyword (plurals, verb forms).
    /// </summary>
    private List<string> GenerateKeywordVariations(string keyword)
    {
        var variations = new List<string> { keyword };
        var lower = keyword.ToLowerInvariant();
        
        // Plural forms
        if (lower.EndsWith("s"))
        {
            // If plural, add singular
            if (lower.EndsWith("es"))
            {
                variations.Add(keyword.Substring(0, keyword.Length - 2)); // "bushes" -> "bush"
            }
            else if (lower.EndsWith("ies") && keyword.Length > 3)
            {
                variations.Add(keyword.Substring(0, keyword.Length - 3) + "y"); // "berries" -> "berry"
            }
            else
            {
                variations.Add(keyword.Substring(0, keyword.Length - 1)); // "scales" -> "scale"
            }
        }
        else
        {
            // If singular, add plural
            if (lower.EndsWith("s") || lower.EndsWith("x") || lower.EndsWith("z") || 
                lower.EndsWith("ch") || lower.EndsWith("sh"))
            {
                variations.Add(keyword + "es"); // "bush" -> "bushes"
            }
            else if (lower.EndsWith("y") && keyword.Length > 1 && !IsVowel(lower[lower.Length - 2]))
            {
                variations.Add(keyword.Substring(0, keyword.Length - 1) + "ies"); // "berry" -> "berries"
            }
            else
            {
                variations.Add(keyword + "s"); // "scale" -> "scales"
            }
        }
        
        // Verb forms (-ing, -ed)
        if (!lower.EndsWith("ing") && !lower.EndsWith("ed"))
        {
            // Add -ing form
            if (lower.EndsWith("e") && keyword.Length > 2)
            {
                variations.Add(keyword.Substring(0, keyword.Length - 1) + "ing"); // "sprawl" -> "sprawling" (but "free" -> "freeing")
            }
            else if (keyword.Length > 2 && !IsVowel(lower[lower.Length - 1]) && 
                     IsVowel(lower[lower.Length - 2]) && !IsVowel(lower[lower.Length - 3]))
            {
                // Double final consonant for CVC pattern
                variations.Add(keyword + keyword[keyword.Length - 1] + "ing"); // "swim" -> "swimming"
            }
            else
            {
                variations.Add(keyword + "ing"); // "cluster" -> "clustering"
            }
            
            // Add -ed form
            if (lower.EndsWith("e"))
            {
                variations.Add(keyword + "d"); // "sprawl" -> "sprawled" (but if ends in e: "pile" -> "piled")
            }
            else if (keyword.Length > 2 && !IsVowel(lower[lower.Length - 1]) && 
                     IsVowel(lower[lower.Length - 2]) && !IsVowel(lower[lower.Length - 3]))
            {
                // Double final consonant for CVC pattern
                variations.Add(keyword + keyword[keyword.Length - 1] + "ed"); // "swim" -> "swimmed" (not perfect but covers pattern)
            }
            else
            {
                variations.Add(keyword + "ed"); // "cluster" -> "clustered"
            }
        }
        else if (lower.EndsWith("ing"))
        {
            // Reverse -ing form
            string stem = keyword.Substring(0, keyword.Length - 3);
            variations.Add(stem); // "sprawling" -> "sprawl"
            
            // Handle doubled consonant
            if (stem.Length > 1 && stem[stem.Length - 1] == stem[stem.Length - 2])
            {
                variations.Add(stem.Substring(0, stem.Length - 1)); // "swimming" -> "swim"
            }
            
            // Add -e variant
            variations.Add(stem + "e"); // "clustering" might be from "cluster" or theoretically "clustere"
        }
        else if (lower.EndsWith("ed"))
        {
            // Reverse -ed form
            string stem = keyword.Substring(0, keyword.Length - 2);
            variations.Add(stem); // "clustered" -> "cluster"
            
            // Handle doubled consonant
            if (stem.Length > 1 && stem[stem.Length - 1] == stem[stem.Length - 2])
            {
                variations.Add(stem.Substring(0, stem.Length - 1)); // "spotted" -> "spot"
            }
        }
        
        return variations.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
    
    private bool IsVowel(char c)
    {
        c = char.ToLowerInvariant(c);
        return c == 'a' || c == 'e' || c == 'i' || c == 'o' || c == 'u';
    }
    
    /// <summary>
    /// Parses narration text and identifies keyword locations.
    /// Returns list of text segments (normal text and keywords).
    /// </summary>
    public List<TextSegment> ParseNarrationWithKeywords(string narrationText, List<string> keywords)
    {
        var segments = new List<TextSegment>();
        
        if (string.IsNullOrEmpty(narrationText) || keywords == null || keywords.Count == 0)
        {
            segments.Add(new TextSegment { Text = narrationText, IsKeyword = false });
            return segments;
        }
        
        // Create list of keyword matches with their positions
        var matches = new List<(int Start, int Length, string Keyword, string MatchedText)>();
        
        foreach (var keyword in keywords)
        {
            // Generate variations for this keyword
            var variations = GenerateKeywordVariations(keyword);
            
            foreach (var variation in variations)
            {
                // \b is a zero-width word boundary (between \w and \W).
                // Because punctuation characters (-  ,  .  "  (  )  …) are all \W,
                // this correctly matches "leaf" inside "leaf-litter", "leaf," or "(leaf)".
                string pattern = @"\b" + Regex.Escape(variation) + @"\b";
                var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                
                foreach (Match match in regex.Matches(narrationText))
                {
                    // Store the original keyword (not the variation) for tracking
                    matches.Add((match.Index, match.Length, keyword, match.Value));
                }
            }
        }
        
        // Sort matches by position, then by length (longest first)
        matches = matches.OrderBy(m => m.Start).ThenByDescending(m => m.Length).ToList();
        
        // Remove overlapping matches (keep longest match for each position)
        var nonOverlapping = new List<(int Start, int Length, string Keyword, string MatchedText)>();
        
        foreach (var match in matches)
        {
            // Check if this match overlaps with any already selected match
            bool overlaps = nonOverlapping.Any(existing =>
                match.Start < existing.Start + existing.Length && 
                match.Start + match.Length > existing.Start);
            
            if (!overlaps)
            {
                nonOverlapping.Add(match);
            }
        }
        
        // Sort again by position for segment building
        nonOverlapping = nonOverlapping.OrderBy(m => m.Start).ToList();
        
        // Build segments
        int currentPos = 0;
        
        foreach (var match in nonOverlapping)
        {
            // Add text before keyword
            if (match.Start > currentPos)
            {
                segments.Add(new TextSegment
                {
                    Text = narrationText.Substring(currentPos, match.Start - currentPos),
                    IsKeyword = false
                });
            }
            
            // Add keyword (use the matched text from narration, not the keyword itself)
            segments.Add(new TextSegment
            {
                Text = match.MatchedText,
                IsKeyword = true,
                KeywordValue = match.Keyword // Store the original keyword for tracking
            });
            
            currentPos = match.Start + match.Length;
        }
        
        // Add remaining text
        if (currentPos < narrationText.Length)
        {
            segments.Add(new TextSegment
            {
                Text = narrationText.Substring(currentPos),
                IsKeyword = false
            });
        }
        
        return segments;
    }
    
    /// <summary>
    /// Like <see cref="ParseNarrationWithKeywords"/> but highlights only the specific occurrence
    /// (0-based index within this line) of each keyword rather than every occurrence.
    /// <paramref name="keywords"/> and <paramref name="occurrenceIndices"/> are parallel lists.
    /// </summary>
    public List<TextSegment> ParseNarrationWithKeywordsAtOccurrences(
        string narrationText, List<string> keywords, List<int> occurrenceIndices)
    {
        var segments = new List<TextSegment>();

        if (string.IsNullOrEmpty(narrationText) || keywords == null || keywords.Count == 0)
        {
            segments.Add(new TextSegment { Text = narrationText, IsKeyword = false });
            return segments;
        }

        var matches = new List<(int Start, int Length, string Keyword, string MatchedText)>();

        for (int ki = 0; ki < keywords.Count; ki++)
        {
            string keyword = keywords[ki];
            int targetOccurrence = ki < occurrenceIndices.Count ? occurrenceIndices[ki] : 0;

            var variations = GenerateKeywordVariations(keyword);
            // Collect all matches across all variations, sorted by position
            var allMatches = new List<(int Start, int Length, string MatchedText)>();
            foreach (var variation in variations)
            {
                string pattern = @"\b" + Regex.Escape(variation) + @"\b";
                var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                foreach (Match m in regex.Matches(narrationText))
                    allMatches.Add((m.Index, m.Length, m.Value));
            }
            // Deduplicate overlapping, sort by position
            allMatches = allMatches
                .OrderBy(m => m.Start)
                .ThenByDescending(m => m.Length)
                .ToList();
            var deduped = new List<(int Start, int Length, string MatchedText)>();
            foreach (var m in allMatches)
            {
                if (!deduped.Any(e => m.Start < e.Start + e.Length && m.Start + m.Length > e.Start))
                    deduped.Add(m);
            }
            deduped = deduped.OrderBy(m => m.Start).ToList();

            if (targetOccurrence < deduped.Count)
            {
                var chosen = deduped[targetOccurrence];
                matches.Add((chosen.Start, chosen.Length, keyword, chosen.MatchedText));
            }
        }

        // Remove overlapping matches (keep first encountered)
        matches = matches.OrderBy(m => m.Start).ThenByDescending(m => m.Length).ToList();
        var nonOverlapping = new List<(int Start, int Length, string Keyword, string MatchedText)>();
        foreach (var match in matches)
        {
            if (!nonOverlapping.Any(e => match.Start < e.Start + e.Length && match.Start + match.Length > e.Start))
                nonOverlapping.Add(match);
        }
        nonOverlapping = nonOverlapping.OrderBy(m => m.Start).ToList();

        int currentPos = 0;
        foreach (var match in nonOverlapping)
        {
            if (match.Start > currentPos)
                segments.Add(new TextSegment { Text = narrationText.Substring(currentPos, match.Start - currentPos) });
            segments.Add(new TextSegment { Text = match.MatchedText, IsKeyword = true, KeywordValue = match.Keyword });
            currentPos = match.Start + match.Length;
        }
        if (currentPos < narrationText.Length)
            segments.Add(new TextSegment { Text = narrationText.Substring(currentPos) });

        return segments;
    }

    /// <summary>
    /// Checks if a character position in the narration text is within a keyword.
    /// Returns the keyword if found, null otherwise.
    /// </summary>
    public string? GetKeywordAtPosition(string narrationText, int charPosition, List<string> keywords)
    {
        if (keywords == null || keywords.Count == 0 || charPosition < 0 || charPosition >= narrationText.Length)
            return null;
        
        foreach (var keyword in keywords)
        {
            var variations = GenerateKeywordVariations(keyword);
            
            foreach (var variation in variations)
            {
                string pattern = @"\b" + Regex.Escape(variation) + @"\b";
                var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                
                foreach (Match match in regex.Matches(narrationText))
                {
                    if (charPosition >= match.Index && charPosition < match.Index + match.Length)
                    {
                        return keyword; // Return the original keyword
                    }
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Formats narration text for terminal display with color codes.
    /// Keywords are enclosed in special markers that the terminal can render differently.
    /// </summary>
    public string FormatForTerminal(string narrationText, List<string> keywords, bool keywordsEnabled = true)
    {
        if (!keywordsEnabled || keywords == null || keywords.Count == 0)
        {
            return narrationText;
        }
        
        var segments = ParseNarrationWithKeywords(narrationText, keywords);
        var result = new StringBuilder();
        
        foreach (var segment in segments)
        {
            if (segment.IsKeyword)
            {
                // Use special markers for terminal rendering
                // Format: <KEYWORD:value>text</KEYWORD>
                result.Append($"<KEYWORD:{segment.KeywordValue}>{segment.Text}</KEYWORD>");
            }
            else
            {
                result.Append(segment.Text);
            }
        }
        
        return result.ToString();
    }
}
