using System.Text;
using Cathedral.LLM.JsonConstraints;

namespace Cathedral.LLM;

/// <summary>
/// Comprehensive tests for JSON constraints using the LLM server
/// </summary>
public static class JsonConstraintLLMTests
{
    public class TestResult
    {
        public string TestName { get; set; } = "";
        public JsonField Schema { get; set; } = null!;
        public string Prompt { get; set; } = "";
        public string LLMResponse { get; set; } = "";
        public bool IsValid { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public TimeSpan ResponseTime { get; set; }
        public string GbnfUsed { get; set; } = "";
        public string TemplateUsed { get; set; } = "";
    }

    /// <summary>
    /// Run comprehensive JSON constraint tests with the LLM
    /// </summary>
    public static async Task<List<TestResult>> RunAllTests(LlamaServerManager llmManager, string systemPrompt = "")
    {
        Console.WriteLine("=== JSON Constraint LLM Integration Tests ===\n");

        var results = new List<TestResult>();
        
        // Test scenarios
        var testScenarios = CreateTestScenarios();

        // Create a specialized LLM instance for JSON generation
        var jsonSystemPrompt = string.IsNullOrEmpty(systemPrompt) 
            ? "You are a JSON data generator. You must follow the exact format specified and generate realistic data that matches the constraints. Always respond with valid JSON only, no additional text."
            : systemPrompt;

        var llmSlotId = await llmManager.CreateInstanceAsync(jsonSystemPrompt);
        Console.WriteLine($"✓ Created JSON generator LLM instance with slot ID: {llmSlotId}\n");

        int testNumber = 1;
        foreach (var scenario in testScenarios)
        {
            Console.WriteLine($"Running Test {testNumber}: {scenario.TestName}");
            var result = await RunSingleTest(llmManager, llmSlotId, scenario);
            results.Add(result);
            
            PrintTestResult(result);
            Console.WriteLine();
            testNumber++;

            // Reset the LLM instance between tests to avoid context pollution
            llmManager.ResetInstance(llmSlotId);
            await Task.Delay(500); // Small delay between tests
        }

        PrintSummary(results);
        return results;
    }

    private static List<TestScenario> CreateTestScenarios()
    {
        return new List<TestScenario>
        {
            // Simple field tests (wrapped in composite for proper JSON structure)
            new TestScenario
            {
                TestName = "Simple Character Name",
                Schema = new CompositeField("character", new JsonField[]
                {
                    new StringField("name", 3, 15)
                }),
                PromptTemplate = "Generate a fantasy character name"
            },
            
            new TestScenario
            {
                TestName = "Character Level",
                Schema = new CompositeField("character", new JsonField[]
                {
                    new IntField("level", 1, 20)
                }),
                PromptTemplate = "Generate a character level"
            },
            
            new TestScenario
            {
                TestName = "Character Class Choice",
                Schema = new CompositeField("character", new JsonField[]
                {
                    new ChoiceField<string>("class", "warrior", "mage", "rogue", "archer")
                }),
                PromptTemplate = "Choose a character class"
            },
            
            // Composite tests
            new TestScenario
            {
                TestName = "Simple Character",
                Schema = new CompositeField("character", new JsonField[]
                {
                    new StringField("name", 3, 20),
                    new IntField("level", 1, 100),
                    new ChoiceField<string>("class", "warrior", "mage", "rogue"),
                    new BooleanField("isAlive")
                }),
                PromptTemplate = "Generate a fantasy game character"
            },
            
            new TestScenario
            {
                TestName = "Character with Stats",
                Schema = new CompositeField("character", new JsonField[]
                {
                    new StringField("name", 3, 25),
                    new CompositeField("stats", new JsonField[]
                    {
                        new IntField("strength", 1, 20),
                        new IntField("dexterity", 1, 20),
                        new IntField("intelligence", 1, 20)
                    }),
                    new FloatField("health", 10.0, 100.0)
                }),
                PromptTemplate = "Generate a character with statistics"
            },
            
            // Array tests
            new TestScenario
            {
                TestName = "Character Skills",
                Schema = new CompositeField("character", new JsonField[]
                {
                    new StringField("name", 3, 20),
                    new ArrayField("skills", new StringField("skill", 3, 15), 1, 4)
                }),
                PromptTemplate = "Generate a character with a list of skills"
            },
            
            // Template string tests
            new TestScenario
            {
                TestName = "Character Dialogue",
                Schema = new CompositeField("dialogue", new JsonField[]
                {
                    new StringField("speaker", 3, 20),
                    new TemplateStringField("text", "says <generated>", 10, 50)
                }),
                PromptTemplate = "Generate character dialogue"
            },
            
            // Variant tests
            new TestScenario
            {
                TestName = "Game Event (Variants)",
                Schema = new CompositeField("event", new JsonField[]
                {
                    new ChoiceField<string>("type", "combat", "dialogue"),
                    new VariantField("data",
                        new CompositeField("combat", new JsonField[]
                        {
                            new StringField("enemy", 3, 20),
                            new IntField("enemyLevel", 1, 50)
                        }),
                        new CompositeField("dialogue", new JsonField[]
                        {
                            new StringField("npc", 3, 20),
                            new StringField("message", 10, 100)
                        })
                    )
                }),
                PromptTemplate = "Generate a game event (either combat or dialogue)"
            },
            
            // Complex nested test
            new TestScenario
            {
                TestName = "Complete Quest",
                Schema = JsonConstraintDemo.CreateQuestSchema(),
                PromptTemplate = "Generate a complete fantasy quest with objectives"
            }
            ,
            // Edge case: Optional field omitted / empty
            new TestScenario
            {
                TestName = "Optional Field Empty",
                Schema = new CompositeField("character", new JsonField[]
                {
                    new StringField("name", 1, 20),
                    new OptionalField("nickname", new StringField("nickname", 1, 10))
                }),
                PromptTemplate = "Generate a character. The optional nickname may be omitted."
            },

            // Edge case: Boundary values (min and max)
            new TestScenario
            {
                TestName = "Boundary Values",
                Schema = new CompositeField("stats", new JsonField[]
                {
                    new ConstantIntField("minVal", 0),
                    new ConstantIntField("maxVal", 9999)
                }),
                PromptTemplate = "Generate numeric boundary values exactly matching the required min/max."
            },

            // Edge case: Empty array allowed and max-length arrays
            new TestScenario
            {
                TestName = "Array Edge Cases",
                Schema = new CompositeField("inventory", new JsonField[]
                {
                    new StringField("owner", 3, 20),
                    new ArrayField("items", new StringField("item", 1, 30), 0, 5)
                }),
                PromptTemplate = "Generate an inventory, arrays may be empty or contain up to 5 items."
            },

            // Edge case: Strings with special characters and escapes
            new TestScenario
            {
                TestName = "Special Characters String",
                Schema = new CompositeField("message", new JsonField[]
                {
                    new StringField("content", 1, 100)
                }),
                PromptTemplate = "Generate a JSON message that may include punctuation and special characters like \" \\ / \n \t and emojis 😊."
            },

            // Edge case: Deeply nested variants
            new TestScenario
            {
                TestName = "Nested Variants",
                Schema = new CompositeField("event", new JsonField[]
                {
                    new VariantField("payload",
                        new CompositeField("typeA", new JsonField[]
                        {
                            new StringField("aName", 1, 20),
                            new ArrayField("aList", new IntField("inner", 0, 5), 0, 3)
                        }),
                        new CompositeField("typeB", new JsonField[]
                        {
                            new VariantField("sub",
                                new CompositeField("subX", new JsonField[] { new StringField("x",1,5) }),
                                new CompositeField("subY", new JsonField[] { new IntField("y",1,3) })
                            )
                        })
                    )
                }),
                PromptTemplate = "Generate an event using nested variants and ensure structure validity."
            },

            // Edge case: Empty optional field
            new TestScenario
            {
                TestName = "Empty Optional Field",
                Schema = new CompositeField("profile", new JsonField[]
                {
                    new StringField("username", 3, 20),
                    new OptionalField("bio", new StringField("bio", 0, 200))
                }),
                PromptTemplate = "Generate a user profile where the bio field might be empty or missing."
            },

            // Edge case: Template with minimal and maximal generation lengths
            new TestScenario
            {
                TestName = "Template Extremes",
                Schema = new CompositeField("response", new JsonField[]
                {
                    new TemplateStringField("minimal", "Hi <generated>", 1, 1),
                    new TemplateStringField("extensive", "Story: <generated>", 50, 100)
                }),
                PromptTemplate = "Generate responses with very short and very long generated parts."
            },

            // Edge case: Mixed boolean and exact value constraints
            new TestScenario
            {
                TestName = "Boolean and Exact Values",
                Schema = new CompositeField("config", new JsonField[]
                {
                    new BooleanField("enabled"),
                    new ConstantIntField("exactPort", 8080), 
                    new ConstantFloatField("exactRatio", 1.0),
                    new BooleanField("debug")
                }),
                PromptTemplate = "Generate configuration with exact values and boolean settings."
            }
        };
    }

    private static async Task<TestResult> RunSingleTest(LlamaServerManager llmManager, int slotId, TestScenario scenario)
    {
        var result = new TestResult
        {
            TestName = scenario.TestName,
            Schema = scenario.Schema
        };

        try
        {
            // Generate GBNF and template
            var gbnf = JsonConstraintGenerator.GenerateGBNF(scenario.Schema);
            var template = JsonConstraintGenerator.GenerateTemplate(scenario.Schema);
            
            result.GbnfUsed = gbnf;
            result.TemplateUsed = template;

            // Debug output for failing constant field tests
            if (scenario.TestName == "Boundary Values" || scenario.TestName == "Boolean and Exact Values")
            {
                Console.WriteLine($"\n--- DEBUG: GBNF for {scenario.TestName} ---");
                Console.WriteLine(gbnf);
                Console.WriteLine("--- END DEBUG ---\n");
            }

            // Create the prompt
            var prompt = $@"{scenario.PromptTemplate}

Please generate JSON data that exactly matches this template format:
{template}

Respond with valid JSON only, no additional text or explanations.";

            result.Prompt = prompt;

            var responseBuilder = new StringBuilder();
            var startTime = DateTime.UtcNow;
            var completed = false;

            // Make the LLM request with GBNF grammar constraints
            await llmManager.ContinueRequestAsync(
                slotId,
                prompt,
                onTokenStreamed: (token, _) =>
                {
                    responseBuilder.Append(token);
                },
                onCompleted: (_, response, wasCancelled) =>
                {
                    completed = true;
                    result.LLMResponse = response.Trim();
                    result.ResponseTime = DateTime.UtcNow - startTime;
                },
                gbnfGrammar: gbnf  // Pass GBNF grammar directly
            );

            // Wait for completion
            while (!completed)
            {
                await Task.Delay(100);
            }

            // Validate the response
            List<string> validationErrors;
            result.IsValid = JsonValidator.ValidateJson(result.LLMResponse, scenario.Schema, out validationErrors);
            result.ValidationErrors = validationErrors;
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.ValidationErrors.Add($"Test execution error: {ex.Message}");
        }

        return result;
    }

    private static void PrintTestResult(TestResult result)
    {
        var status = result.IsValid ? "✓ PASS" : "✗ FAIL";
        Console.WriteLine($"  {status} - {result.TestName}");
        Console.WriteLine($"    Response Time: {result.ResponseTime.TotalMilliseconds:F0}ms");
        
        if (!string.IsNullOrEmpty(result.LLMResponse))
        {
            var preview = result.LLMResponse.Length > 100 
                ? result.LLMResponse.Substring(0, 100) + "..."
                : result.LLMResponse;
            Console.WriteLine($"    LLM Output: {preview.Replace("\n", "\\n")}");
        }

        if (!result.IsValid && result.ValidationErrors.Any())
        {
            Console.WriteLine("    Validation Errors:");
            foreach (var error in result.ValidationErrors.Take(3))
            {
                Console.WriteLine($"      • {error}");
            }
            if (result.ValidationErrors.Count > 3)
            {
                Console.WriteLine($"      • ... and {result.ValidationErrors.Count - 3} more errors");
            }
        }
    }

    private static void PrintSummary(List<TestResult> results)
    {
        Console.WriteLine("=== Test Summary ===");
        var totalTests = results.Count;
        var passedTests = results.Count(r => r.IsValid);
        var failedTests = totalTests - passedTests;
        
        Console.WriteLine($"Total Tests: {totalTests}");
        Console.WriteLine($"Passed: {passedTests} ({(passedTests * 100.0 / totalTests):F1}%)");
        Console.WriteLine($"Failed: {failedTests} ({(failedTests * 100.0 / totalTests):F1}%)");
        
        var avgResponseTime = results.Where(r => r.ResponseTime.TotalMilliseconds > 0)
                                    .Average(r => r.ResponseTime.TotalMilliseconds);
        Console.WriteLine($"Average Response Time: {avgResponseTime:F0}ms");

        if (failedTests > 0)
        {
            Console.WriteLine("\nFailed Tests:");
            foreach (var failure in results.Where(r => !r.IsValid))
            {
                Console.WriteLine($"  • {failure.TestName}");
            }
        }

        Console.WriteLine("\nTest Categories:");
        var categoryStats = results.GroupBy(r => GetTestCategory(r.TestName))
                                  .Select(g => new 
                                  {
                                      Category = g.Key,
                                      Total = g.Count(),
                                      Passed = g.Count(r => r.IsValid)
                                  });
        
        foreach (var category in categoryStats)
        {
            var passRate = category.Passed * 100.0 / category.Total;
            Console.WriteLine($"  {category.Category}: {category.Passed}/{category.Total} ({passRate:F1}%)");
        }
    }

    private static string GetTestCategory(string testName)
    {
        return testName.ToLower() switch
        {
            var name when name.Contains("simple") => "Simple Fields",
            var name when name.Contains("array") => "Arrays",
            var name when name.Contains("variant") => "Variants",
            var name when name.Contains("dialogue") || name.Contains("template") => "Templates",
            var name when name.Contains("quest") || name.Contains("complete") => "Complex",
            _ => "Composite"
        };
    }

    private class TestScenario
    {
        public string TestName { get; set; } = "";
        public JsonField Schema { get; set; } = null!;
        public string PromptTemplate { get; set; } = "";
    }
}