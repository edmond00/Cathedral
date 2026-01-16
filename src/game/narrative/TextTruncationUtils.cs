using System;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Utilities for processing and truncating text, especially LLM-generated text
/// that may be incomplete due to token limits.
/// </summary>
public static class TextTruncationUtils
{
    /// <summary>
    /// Processes potentially truncated LLM text to ensure it ends gracefully.
    /// Removes incomplete sentences or words and appends "..." to indicate truncation.
    /// </summary>
    /// <param name="text">The text to process (may be truncated)</param>
    /// <param name="minLengthForSentence">Minimum length to remove incomplete sentences (default 50)</param>
    /// <returns>Cleaned text ending with "..." if truncated</returns>
    public static string CleanTruncatedText(string text, int minLengthForSentence = 50)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        
        // Trim whitespace
        text = text.Trim();
        
        if (string.IsNullOrEmpty(text))
            return text;
        
        // Check if text already ends properly with sentence-ending punctuation
        if (text.EndsWith('.') || text.EndsWith('!') || text.EndsWith('?'))
            return text; // Text is complete, no need to truncate
        
        // Try to find the last complete sentence
        int lastPeriod = text.LastIndexOf('.');
        int lastExclamation = text.LastIndexOf('!');
        int lastQuestion = text.LastIndexOf('?');
        
        // Find the position of the last sentence-ending punctuation
        int lastSentenceEnd = Math.Max(lastPeriod, Math.Max(lastExclamation, lastQuestion));
        
        // If we found a sentence ending and the remaining text is long enough
        if (lastSentenceEnd >= 0)
        {
            string beforeLastSentence = text.Substring(0, lastSentenceEnd + 1);
            string afterLastSentence = text.Substring(lastSentenceEnd + 1).Trim();
            
            // Check if there's significant text after the last sentence
            // and if the text before is long enough
            if (afterLastSentence.Length > 0 && beforeLastSentence.Length >= minLengthForSentence)
            {
                // Remove the last period and replace with "..."
                return beforeLastSentence.Substring(0, beforeLastSentence.Length - 1) + "...";
            }
        }
        
        // Either no period found or removing the incomplete sentence would make text too short
        // Remove the last incomplete word instead
        int lastSpace = text.LastIndexOf(' ');
        
        if (lastSpace > 0)
        {
            // Remove last word and replace space with "..."
            return text.Substring(0, lastSpace) + "...";
        }
        
        // Edge case: single word or no spaces - just append "..."
        return text + "...";
    }
}
