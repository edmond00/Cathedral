using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        var matches = new List<(int Start, int Length, string Keyword)>();
        
        foreach (var keyword in keywords)
        {
            var index = narrationText.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
            while (index >= 0)
            {
                matches.Add((index, keyword.Length, keyword));
                index = narrationText.IndexOf(keyword, index + keyword.Length, StringComparison.OrdinalIgnoreCase);
            }
        }
        
        // Sort matches by position
        matches = matches.OrderBy(m => m.Start).ToList();
        
        // Remove overlapping matches (keep first occurrence)
        var nonOverlapping = new List<(int Start, int Length, string Keyword)>();
        int lastEnd = -1;
        
        foreach (var match in matches)
        {
            if (match.Start >= lastEnd)
            {
                nonOverlapping.Add(match);
                lastEnd = match.Start + match.Length;
            }
        }
        
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
            
            // Add keyword
            segments.Add(new TextSegment
            {
                Text = narrationText.Substring(match.Start, match.Length),
                IsKeyword = true,
                KeywordValue = match.Keyword
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
    /// Checks if a character position in the narration text is within a keyword.
    /// Returns the keyword if found, null otherwise.
    /// </summary>
    public string? GetKeywordAtPosition(string narrationText, int charPosition, List<string> keywords)
    {
        if (keywords == null || keywords.Count == 0 || charPosition < 0 || charPosition >= narrationText.Length)
            return null;
        
        foreach (var keyword in keywords)
        {
            var index = narrationText.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
            while (index >= 0)
            {
                if (charPosition >= index && charPosition < index + keyword.Length)
                {
                    return keyword;
                }
                index = narrationText.IndexOf(keyword, index + keyword.Length, StringComparison.OrdinalIgnoreCase);
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
