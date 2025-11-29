using System;

namespace Cathedral.Game;

/// <summary>
/// Simple test to verify ActionInfo formatting logic
/// </summary>
public static class ActionInfoTest
{
    public static void RunTests()
    {
        Console.WriteLine("=== ActionInfo Format Tests ===\n");
        
        // Test 1: Action with "try to " prefix - should be removed
        var action1 = new ActionInfo("try to infiltrate the castle at night", "stealth");
        Console.WriteLine($"Test 1 - Original: '{action1.ActionText}'");
        Console.WriteLine($"Test 1 - Display: '{action1.GetDisplayText()}'");
        Console.WriteLine($"Test 1 - Formatted: '{action1.GetFormattedDisplayText()}'");
        Console.WriteLine($"Expected: '[stealth] infiltrate the castle at night'");
        Console.WriteLine();
        
        // Test 2: Action without "try to " prefix
        var action2 = new ActionInfo("climb the mountain path", "athletics");
        Console.WriteLine($"Test 2 - Original: '{action2.ActionText}'");
        Console.WriteLine($"Test 2 - Display: '{action2.GetDisplayText()}'");
        Console.WriteLine($"Test 2 - Formatted: '{action2.GetFormattedDisplayText()}'");
        Console.WriteLine($"Expected: '[athletics] climb the mountain path'");
        Console.WriteLine();
        
        // Test 3: Action with "Try to " (capital T) prefix - should be removed (case-insensitive)
        var action3 = new ActionInfo("Try to convince the guard", "charisma");
        Console.WriteLine($"Test 3 - Original: '{action3.ActionText}'");
        Console.WriteLine($"Test 3 - Display: '{action3.GetDisplayText()}'");
        Console.WriteLine($"Test 3 - Formatted: '{action3.GetFormattedDisplayText()}'");
        Console.WriteLine($"Expected: '[charisma] convince the guard'");
        Console.WriteLine();
        
        // Test 4: Action with no skill
        var action4 = new ActionInfo("examine the surroundings", "");
        Console.WriteLine($"Test 4 - Original: '{action4.ActionText}'");
        Console.WriteLine($"Test 4 - Display: '{action4.GetDisplayText()}'");
        Console.WriteLine($"Test 4 - Formatted: '{action4.GetFormattedDisplayText()}'");
        Console.WriteLine($"Expected: 'examine the surroundings' (no skill bracket)");
        Console.WriteLine();
        
        // Test 5: Edge case - just "try to " with nothing after
        var action5 = new ActionInfo("try to ", "perception");
        Console.WriteLine($"Test 5 - Original: '{action5.ActionText}'");
        Console.WriteLine($"Test 5 - Display: '{action5.GetDisplayText()}'");
        Console.WriteLine($"Test 5 - Formatted: '{action5.GetFormattedDisplayText()}'");
        Console.WriteLine($"Expected: '[perception] ' (empty action text after removal)");
        Console.WriteLine();
        
        Console.WriteLine("=== Tests Complete ===");
    }
}
