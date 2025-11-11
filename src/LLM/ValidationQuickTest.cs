using Cathedral.LLM.JsonConstraints;

namespace Cathedral.LLM;

/// <summary>
/// Quick test to verify JSON validation works correctly
/// </summary>
public static class ValidationQuickTest
{
    public static void TestValidation()
    {
        Console.WriteLine("=== Quick Validation Test ===\n");

        // Test 1: Simple character name (the failing case)
        var nameSchema = new CompositeField("character", new JsonField[]
        {
            new StringField("name", 3, 15)
        });

        var nameJson = """{"name":"Narada"}""";
        var nameValid = JsonValidator.ValidateJson(nameJson, nameSchema, out var nameErrors);
        
        Console.WriteLine($"Test 1 - Character Name:");
        Console.WriteLine($"  JSON: {nameJson}");
        Console.WriteLine($"  Valid: {nameValid}");
        if (!nameValid)
        {
            Console.WriteLine($"  Errors: {string.Join(", ", nameErrors)}");
        }
        Console.WriteLine();

        // Test 2: Character level
        var levelSchema = new CompositeField("character", new JsonField[]
        {
            new IntField("level", 1, 20)
        });

        var levelJson = """{"level":5}""";
        var levelValid = JsonValidator.ValidateJson(levelJson, levelSchema, out var levelErrors);
        
        Console.WriteLine($"Test 2 - Character Level:");
        Console.WriteLine($"  JSON: {levelJson}");
        Console.WriteLine($"  Valid: {levelValid}");
        if (!levelValid)
        {
            Console.WriteLine($"  Errors: {string.Join(", ", levelErrors)}");
        }
        Console.WriteLine();

        // Test 3: Character class choice
        var classSchema = new CompositeField("character", new JsonField[]
        {
            new ChoiceField<string>("class", "warrior", "mage", "rogue", "archer")
        });

        var classJson = """{"class":"warrior"}""";
        var classValid = JsonValidator.ValidateJson(classJson, classSchema, out var classErrors);
        
        Console.WriteLine($"Test 3 - Character Class:");
        Console.WriteLine($"  JSON: {classJson}");
        Console.WriteLine($"  Valid: {classValid}");
        if (!classValid)
        {
            Console.WriteLine($"  Errors: {string.Join(", ", classErrors)}");
        }
        Console.WriteLine();

        // Test 4: Invalid case
        var invalidJson = """{"class":"invalid-class"}""";
        var invalidValid = JsonValidator.ValidateJson(invalidJson, classSchema, out var invalidErrors);
        
        Console.WriteLine($"Test 4 - Invalid Class:");
        Console.WriteLine($"  JSON: {invalidJson}");
        Console.WriteLine($"  Valid: {invalidValid}");
        if (!invalidValid)
        {
            Console.WriteLine($"  Errors: {string.Join(", ", invalidErrors)}");
        }

        var passedTests = new[] { nameValid, levelValid, classValid, !invalidValid }.Count(x => x);
        Console.WriteLine($"\nValidation Quick Test Results: {passedTests}/4 passed");
    }
}