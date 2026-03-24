using System;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Demo program to test the TextTruncationUtils functionality.
/// Run this to verify truncated text is cleaned properly.
/// </summary>
public static class TextTruncationDemo
{
    public static void RunDemo()
    {
        Console.WriteLine("=== Text Truncation Demo ===\n");

        // Test case 1: Text with incomplete sentence (should remove incomplete sentence)
        string test1 = "The forest is dark and mysterious. Ancient trees tower above. You notice something str";
        Console.WriteLine("Test 1 - Incomplete sentence:");
        Console.WriteLine($"  Input:  \"{test1}\"");
        Console.WriteLine($"  Output: \"{TextTruncationUtils.CleanTruncatedText(test1)}\"");
        Console.WriteLine();

        // Test case 2: Text that already ends with period (no change)
        string test2 = "The forest is dark and mysterious. Ancient trees tower above.";
        Console.WriteLine("Test 2 - Complete text:");
        Console.WriteLine($"  Input:  \"{test2}\"");
        Console.WriteLine($"  Output: \"{TextTruncationUtils.CleanTruncatedText(test2)}\"");
        Console.WriteLine();

        // Test case 3: Very short text with incomplete word (should remove incomplete word)
        string test3 = "You see a bi";
        Console.WriteLine("Test 3 - Short text with incomplete word:");
        Console.WriteLine($"  Input:  \"{test3}\"");
        Console.WriteLine($"  Output: \"{TextTruncationUtils.CleanTruncatedText(test3)}\"");
        Console.WriteLine();

        // Test case 4: Multiple sentences with incomplete last one
        string test4 = "The path winds through dense undergrowth. Sunlight filters through the canopy above. Your footsteps echo softly. Something catches your att";
        Console.WriteLine("Test 4 - Multiple complete + one incomplete:");
        Console.WriteLine($"  Input:  \"{test4}\"");
        Console.WriteLine($"  Output: \"{TextTruncationUtils.CleanTruncatedText(test4)}\"");
        Console.WriteLine();

        // Test case 5: No period at all, but long enough text
        string test5 = "You walk forward and notice various interesting details about the environm";
        Console.WriteLine("Test 5 - No period (remove last word):");
        Console.WriteLine($"  Input:  \"{test5}\"");
        Console.WriteLine($"  Output: \"{TextTruncationUtils.CleanTruncatedText(test5)}\"");
        Console.WriteLine();

        // Test case 6: Text with exclamation mark
        string test6 = "The forest is alive! Birds chirp merrily. You feel a sense of peace flooding over";
        Console.WriteLine("Test 6 - With exclamation mark:");
        Console.WriteLine($"  Input:  \"{test6}\"");
        Console.WriteLine($"  Output: \"{TextTruncationUtils.CleanTruncatedText(test6)}\"");
        Console.WriteLine();

        // Test case 7: Text with question mark
        string test7 = "Where are you going? The path splits ahead. Which direction should";
        Console.WriteLine("Test 7 - With question mark:");
        Console.WriteLine($"  Input:  \"{test7}\"");
        Console.WriteLine($"  Output: \"{TextTruncationUtils.CleanTruncatedText(test7)}\"");
        Console.WriteLine();

        // Test case 8: Very short text (< 50 chars after removing incomplete sentence)
        string test8 = "The dark forest looms. You feel uns";
        Console.WriteLine("Test 8 - Would be too short after sentence removal:");
        Console.WriteLine($"  Input:  \"{test8}\"");
        Console.WriteLine($"  Output: \"{TextTruncationUtils.CleanTruncatedText(test8)}\"");
        Console.WriteLine();

        // Test case 9: Single word
        string test9 = "Hello";
        Console.WriteLine("Test 9 - Single word:");
        Console.WriteLine($"  Input:  \"{test9}\"");
        Console.WriteLine($"  Output: \"{TextTruncationUtils.CleanTruncatedText(test9)}\"");
        Console.WriteLine();

        Console.WriteLine("=== Demo Complete ===");
    }
}
