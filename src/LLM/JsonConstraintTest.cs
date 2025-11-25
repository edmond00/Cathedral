using Cathedral.LLM.JsonConstraints;
using Cathedral;

namespace Cathedral.LLM;

/// <summary>
/// Simple test program to verify the JsonConstraintGenerator implementation
/// </summary>
public class JsonConstraintTest
{
    public static void TestJsonConstraintGeneration()
    {
        Console.WriteLine("Testing JSON Constraint Generation System...\n");

        try
        {
            // Create a simple schema for testing
            var testSchema = new CompositeField("TestEvent", new JsonField[]
            {
                new StringField("name", 3, 20),
                new DigitField("level", 2),  // 2-digit level (00-99)
                new ChoiceField<string>("type", "fire", "water", "earth", "air"),
                new BooleanField("isActive")
            });

            // Generate GBNF
            var gbnf = JsonConstraintGenerator.GenerateGBNF(testSchema);
            Console.WriteLine("Generated GBNF:");
            Console.WriteLine(gbnf);
            Console.WriteLine();

            // Generate Template
            var template = JsonConstraintGenerator.GenerateTemplate(testSchema);
            Console.WriteLine("Generated Template:");
            Console.WriteLine(template);
            Console.WriteLine();

            // Generate Hints
            var hints = JsonConstraintGenerator.GenerateHints(testSchema);
            Console.WriteLine("Generated Hints:");
            Console.WriteLine(hints);
            Console.WriteLine();

            // Test the validator
            TestJsonValidator();

            // Test the demo examples
            Console.WriteLine("Running full demonstration...");
            JsonConstraintDemo.RunDemonstration();

            Console.WriteLine("JSON Constraint Generation test completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during testing: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    public static async Task TestWithLLM()
    {
        Console.WriteLine("=== JSON Constraint LLM Integration Tests ===\n");

        // Create the LLM server manager
        using var llmManager = new LlamaServerManager();

        // Start the server
        Console.WriteLine("Starting LLM Server...");
        var serverStarted = false;

        await llmManager.StartServerAsync(
            isReady =>
            {
                serverStarted = isReady;
                if (isReady)
                {
                    Console.WriteLine("✓ Server started successfully!");
                }
                else
                    Console.WriteLine("✗ Failed to start server!");
            },
            modelAlias: "tiny"); // Use the small model for testing

        if (!serverStarted)
        {
            Console.WriteLine("Cannot run LLM tests without server. Skipping LLM integration tests.");
            return;
        }

        try
        {
            // Run the comprehensive LLM tests
            var results = await JsonConstraintLLMTests.RunAllTests(llmManager);

            // Save detailed results to file
            await SaveTestResults(results);
        }
        finally
        {
            Console.WriteLine("\nStopping LLM server...");
            // Server will be stopped automatically when llmManager is disposed
        }
    }

    private static void TestJsonValidator()
    {
        Console.WriteLine("Testing JSON Validator...\n");

        // Test schema
        var schema = new CompositeField("person", new JsonField[]
        {
            new StringField("name", 2, 20),
            new DigitField("age", 3),    // 3-digit age (000-999)
            new BooleanField("active")
        });

        // Test cases
        var testCases = new[]
        {
            // Valid JSON
            new { 
                name = "Valid JSON", 
                json = """{"name": "John", "age": 25, "active": true}""",
                shouldPass = true 
            },
            
            // Invalid: name too short
            new { 
                name = "Name too short", 
                json = """{"name": "J", "age": 25, "active": true}""",
                shouldPass = false 
            },
            
            // Invalid: age out of range
            new { 
                name = "Age out of range", 
                json = """{"name": "John", "age": 150, "active": true}""",
                shouldPass = false 
            },
            
            // Invalid: wrong type for active
            new { 
                name = "Wrong boolean type", 
                json = """{"name": "John", "age": 25, "active": "yes"}""",
                shouldPass = false 
            },
            
            // Invalid: missing field
            new { 
                name = "Missing field", 
                json = """{"name": "John", "age": 25}""",
                shouldPass = false 
            },
            
            // Invalid: malformed JSON
            new { 
                name = "Malformed JSON", 
                json = """{"name": "John", "age": 25, "active": true""",
                shouldPass = false 
            }
        };

        int passed = 0;
        int total = testCases.Length;

        foreach (var testCase in testCases)
        {
            var isValid = JsonValidator.ValidateJson(testCase.json, schema, out var errors);
            var testPassed = isValid == testCase.shouldPass;
            
            Console.WriteLine($"{(testPassed ? "✓" : "✗")} {testCase.name}: {(isValid ? "VALID" : "INVALID")}");
            
            if (!testPassed)
            {
                Console.WriteLine($"  Expected: {(testCase.shouldPass ? "VALID" : "INVALID")}");
            }
            
            if (!isValid && errors.Any())
            {
                foreach (var error in errors.Take(2))
                {
                    Console.WriteLine($"    Error: {error}");
                }
            }
            
            if (testPassed) passed++;
            Console.WriteLine();
        }

        Console.WriteLine($"Validator Tests: {passed}/{total} passed ({passed * 100.0 / total:F1}%)\n");
    }

    private static async Task SaveTestResults(List<JsonConstraintLLMTests.TestResult> results)
    {
        try
        {
            var resultsDir = Path.Combine(Environment.CurrentDirectory, "TestResults");
            Directory.CreateDirectory(resultsDir);

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var resultsFile = Path.Combine(resultsDir, $"JsonConstraintTests_{timestamp}.json");

            var detailedResults = results.Select(r => new
            {
                r.TestName,
                r.IsValid,
                ResponseTimeMs = r.ResponseTime.TotalMilliseconds,
                r.LLMResponse,
                r.ValidationErrors,
                SchemaType = r.Schema.GetType().Name,
                PromptLength = r.Prompt.Length,
                GbnfRuleCount = r.GbnfUsed.Split('\n').Length
            });

            var json = System.Text.Json.JsonSerializer.Serialize(detailedResults, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });

            await File.WriteAllTextAsync(resultsFile, json);
            Console.WriteLine($"\nDetailed test results saved to: {resultsFile}");

            // Also save a summary
            var summaryFile = Path.Combine(resultsDir, $"TestSummary_{timestamp}.txt");
            var summary = $"""
JSON Constraint LLM Integration Test Summary
Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

Total Tests: {results.Count}
Passed: {results.Count(r => r.IsValid)} ({results.Count(r => r.IsValid) * 100.0 / results.Count:F1}%)
Failed: {results.Count(r => !r.IsValid)} ({results.Count(r => !r.IsValid) * 100.0 / results.Count:F1}%)

Average Response Time: {results.Average(r => r.ResponseTime.TotalMilliseconds):F0}ms

Test Results:
{string.Join("\n", results.Select(r => $"  {(r.IsValid ? "✓" : "✗")} {r.TestName} ({r.ResponseTime.TotalMilliseconds:F0}ms)"))}

Failed Tests Details:
{string.Join("\n\n", results.Where(r => !r.IsValid).Select(r => 
    $"❌ {r.TestName}:\n  Errors: {string.Join(", ", r.ValidationErrors.Take(3))}"))}
""";

            await File.WriteAllTextAsync(summaryFile, summary);
            Console.WriteLine($"Test summary saved to: {summaryFile}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving test results: {ex.Message}");
        }
    }
}