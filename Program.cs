using Cathedral.LLM;
using Cathedral.LLM.JsonConstraints;
using Cathedral.Glyph;
using Cathedral.Engine;
using Cathedral.Glyph.Microworld.LocationSystem;
using Cathedral.Glyph.Microworld.LocationSystem.Generators;
using System.Text;
using System.Text.Json;

// GBNF Validation Function
static async Task ValidateGbnfFile(string filePath)
{
    try
    {
        Console.WriteLine($"Validating GBNF file: {filePath}");
        Console.WriteLine("=" + new string('=', 50));

        // Read the GBNF content
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"❌ ERROR: File not found: {filePath}");
            return;
        }

        var gbnfContent = await File.ReadAllTextAsync(filePath);
        Console.WriteLine($"✅ File loaded successfully ({gbnfContent.Length} characters)");

        // Try to use the GBNF with the LLM manager
        using var llmManager = new LlamaServerManager();
        
        Console.WriteLine("🔄 Starting LLM server...");
        bool serverStarted = false;
        await llmManager.StartServerAsync(started => serverStarted = started);
        
        if (!serverStarted)
        {
            Console.WriteLine("❌ Failed to start LLM server");
            return;
        }

        var slotId = await llmManager.CreateInstanceAsync("You are a helpful assistant.");
        Console.WriteLine($"✅ Created LLM instance: {slotId}");

        // Attempt to make a request with the GBNF grammar
        try
        {
            Console.WriteLine("🔄 Testing GBNF syntax with LLM...");
            Console.WriteLine("📝 LLM Response:");
            Console.WriteLine(new string('-', 40));
            
            bool completed = false;
            var fullResponse = new StringBuilder();
            
            await llmManager.ContinueRequestAsync(
                slotId: slotId,
                userMessage: "Generate a simple valid response:",
                onTokenStreamed: (token, id) => {
                    Console.Write(token);  // Print tokens as they arrive
                    fullResponse.Append(token);
                },
                onCompleted: (id, response, cancelled) => {
                    completed = true;
                    Console.WriteLine();
                    Console.WriteLine(new string('-', 40));
                    if (cancelled)
                    {
                        Console.WriteLine("⚠️ Request was cancelled");
                    }
                },
                gbnfGrammar: gbnfContent  // This is where GBNF syntax errors would be caught
            );

            // Wait for completion with timeout
            var timeout = DateTime.Now.AddSeconds(30);
            while (!completed && DateTime.Now < timeout)
            {
                await Task.Delay(100);
            }

            if (!completed)
            {
                Console.WriteLine("❌ Request timed out");
                return;
            }

            Console.WriteLine("✅ GBNF syntax validation PASSED!");
            Console.WriteLine($"✅ Response length: {fullResponse.Length} characters");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ GBNF syntax validation FAILED!");
            Console.WriteLine($"❌ Error: {ex.Message}");
            
            // Try to extract more specific error information
            if (ex.Message.Contains("grammar") || ex.Message.Contains("syntax") || ex.Message.Contains("parse"))
            {
                Console.WriteLine("🔍 This appears to be a GBNF syntax error.");
                Console.WriteLine("💡 Common issues:");
                Console.WriteLine("   - Missing or extra quotes");
                Console.WriteLine("   - Invalid character class syntax");
                Console.WriteLine("   - Undefined rule references");
                Console.WriteLine("   - Malformed regex patterns");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ FATAL ERROR: {ex.Message}");
    }
}

// Check for special GBNF validation mode
Console.WriteLine($"Debug: args.Length = {args.Length}");
if (args.Length > 0) Console.WriteLine($"Debug: args[0] = '{args[0]}'");
if (args.Length > 1) Console.WriteLine($"Debug: args[1] = '{args[1]}'");

if (args.Length >= 2 && args[0] == "validate-gbnf")
{
    await ValidateGbnfFile(args[1]);
    return;
}

Console.WriteLine("=== Cathedral Application ===\n");
Console.WriteLine("Choose an option:");
Console.WriteLine("1. Run LLM integration tests (JSON constraints)");
Console.WriteLine("2. Launch GlyphSphere with Terminal HUD");
Console.WriteLine("3. Test Terminal Module (standalone)");
Console.WriteLine("4. Test Forest Location System Demo");
Console.WriteLine("5. Exit");

Console.Write("\nEnter your choice (1-5): ");
var choice = Console.ReadLine();

switch (choice)
{
    case "1":
        Console.WriteLine("\n=== JSON Constraint LLM Integration Tests ===");
        Console.WriteLine("This will start the LLM server and run comprehensive tests...");
        Console.WriteLine("Press Enter to continue or Ctrl+C to cancel.");
        Console.ReadLine();
        await JsonConstraintTest.TestWithLLM();
        break;

    case "2":
        Console.WriteLine("\n=== Launching GlyphSphere with Terminal HUD ===");
        
        // Example: Create and configure camera externally
        Console.WriteLine("Camera setup options:");
        Console.WriteLine("1. Default camera settings");
        Console.WriteLine("2. Custom positioned camera (side view)");
        Console.WriteLine("3. Debug camera (top-down view)");
        Console.Write("Choose camera setup (1-3): ");
        
        var cameraChoice = Console.ReadLine();
        var camera = new Camera();
        
        switch (cameraChoice)
        {
            case "1":
                // Default camera - no changes needed
                Console.WriteLine("Using default camera settings");
                break;
                
            case "2":
                // Custom positioned camera
                camera.SetCameraParameters(yaw: 45f, pitch: -20f, distance: 100f);
                Console.WriteLine("Camera positioned at 45° yaw, -20° pitch, 100 units distance");
                break;
                
            case "3":
                // Debug camera in top-down view
                camera.SetDebugCameraParameters(debugMode: true, angle: 1, distance: 150f);
                Console.WriteLine("Debug camera enabled - top-down view");
                break;
                
            default:
                Console.WriteLine("Invalid choice, using default camera settings");
                break;
        }
        
        // Launch with the configured camera
        GlyphSphereLauncher.LaunchGlyphSphere(camera);
        break;

    case "3":
        Console.WriteLine("\n=== Testing Terminal Module ===");
        try 
        {
            Cathedral.Terminal.Tests.TerminalTest.RunTests();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Terminal test failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        break;

    case "4":
        Console.WriteLine("\n=== Forest Location System Demo ===");
        await TestForestLocationSystem();
        break;

    case "5":
        Console.WriteLine("Goodbye!");
        Environment.Exit(0);
        break;

    default:
        Console.WriteLine("Invalid choice. Exiting.");
        break;
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();

static async Task TestForestLocationSystem()
{
    Console.WriteLine("=== Forest Location System with LLM Integration Demo ===\n");
    
    // Create forest generator
    var forestGenerator = new ForestFeatureGenerator();
    
    // Generate one random forest for LLM testing
    var forestId = "forest_001";
    Console.WriteLine($"=== {forestId.ToUpper()} ===");
    
    // Generate context (natural language description)
    var context = forestGenerator.GenerateContext(forestId);
    Console.WriteLine($"Context: {context}");
    Console.WriteLine();
    
    // Generate blueprint (structured data)
    var blueprint = forestGenerator.GenerateBlueprint(forestId);
    
    Console.WriteLine($"Forest Type: {blueprint.LocationType}");
    Console.WriteLine($"Sublocations: {blueprint.Sublocations.Count}");
    
    // Show some interesting sublocations
    Console.WriteLine("Notable sublocations:");
    foreach (var (id, sublocation) in blueprint.Sublocations.Take(5))
    {
        Console.WriteLine($"  - {sublocation.Name}: {sublocation.Description}");
    }
    
    // Show state categories
    Console.WriteLine("\nEnvironmental states:");
    foreach (var (categoryId, category) in blueprint.StateCategories.Take(3))
    {
        Console.WriteLine($"  - {category.Name}: {string.Join(", ", category.PossibleStates.Keys.Take(3))}");
    }
    
    // Generate constraints for LLM
    var currentStates = new Dictionary<string, string>
    {
        ["time_of_day"] = "morning",
        ["weather"] = "clear",
        ["wildlife_state"] = "calm"
    };
    
    var constraints = Blueprint2Constraint.GenerateActionConstraints(blueprint, "forest_edge", currentStates, 7);
    
    Console.WriteLine($"\nGenerated JSON constraint field for LLM action generation");
    Console.WriteLine($"Constraint type: {constraints.GetType().Name}");
    
    // Generate GBNF grammar and template
    var gbnf = JsonConstraintGenerator.GenerateGBNF(constraints);
    var template = JsonConstraintGenerator.GenerateTemplate(constraints);
    
    Console.WriteLine("\n" + new string('=', 60));
    Console.WriteLine("GENERATED JSON TEMPLATE:");
    Console.WriteLine(template);
    Console.WriteLine("\n" + new string('=', 60));
    Console.WriteLine("GENERATED GBNF GRAMMAR:");
    Console.WriteLine(gbnf);
    Console.WriteLine(new string('=', 60) + "\n");
    
    // Start LLM integration
    Console.WriteLine("Starting LLM Server for action generation...");
    
    using var llmManager = new LlamaServerManager();
    var serverStarted = false;

    await llmManager.StartServerAsync(
        isReady =>
        {
            serverStarted = isReady;
            if (isReady)
            {
                Console.WriteLine("✓ LLM Server started successfully!");
            }
            else
                Console.WriteLine("✗ Failed to start LLM server!");
        },
        modelAlias: "tiny"); // Use the small model for testing

    if (!serverStarted)
    {
        Console.WriteLine("Cannot run LLM integration without server. Showing system structure only.");
        return;
    }

    try
    {
        // Create specialized DM system prompt
        var systemPrompt = @"You are a skilled Dungeon Master for a fantasy RPG. You are currently managing a forest exploration scenario.
Your role is to suggest appropriate player actions based on the current game state and environment.
Always respond with valid JSON in the exact format specified. Be creative but realistic within the fantasy forest setting.";

        var llmSlotId = await llmManager.CreateInstanceAsync(systemPrompt);
        Console.WriteLine($"✓ Created DM LLM instance with slot ID: {llmSlotId}\n");

        // Create the DM prompt for generating 7 actions in one call
        var dmPrompt = $@"The player is currently in a {blueprint.LocationType} at the {blueprint.Sublocations["forest_edge"].Name}.

Current situation:
- Location: {blueprint.Sublocations["forest_edge"].Description}
- Time: {currentStates["time_of_day"]}  
- Weather: {currentStates["weather"]}
- Wildlife: {currentStates["wildlife_state"]}
- Environment: {context}

As the Dungeon Master, generate 7 different action options the player could take in this situation. Provide variety - consider different approaches like exploration, interaction, combat preparation, skill use, environmental manipulation, etc.

Generate a JSON response with 7 action choices that exactly matches this template format:
{template}

Respond with valid JSON only, no additional text or explanations.";

        Console.WriteLine("SENDING PROMPT TO LLM FOR 7 ACTION GENERATION:");
        Console.WriteLine(new string('-', 50));
        Console.WriteLine(dmPrompt);
        Console.WriteLine(new string('-', 50) + "\n");

        var responseBuilder = new StringBuilder();
        var completed = false;
        var startTime = DateTime.UtcNow;

        Console.WriteLine("Waiting for LLM response (generating 7 actions)...\n");

        // Make the LLM request with GBNF grammar constraints
        await llmManager.ContinueRequestAsync(
            llmSlotId,
            dmPrompt,
            onTokenStreamed: (token, _) =>
            {
                Console.Write(token); // Stream output in real-time
                responseBuilder.Append(token);
            },
            onCompleted: (_, response, wasCancelled) =>
            {
                completed = true;
            },
            gbnfGrammar: gbnf
        );

        // Wait for completion
        while (!completed)
        {
            await Task.Delay(100);
        }

        var llmResponse = responseBuilder.ToString().Trim();
        var responseTime = DateTime.UtcNow - startTime;

        Console.WriteLine($"\n\n{new string('=', 60)}");
        Console.WriteLine("LLM RESPONSE ANALYSIS:");
        Console.WriteLine($"Response time: {responseTime.TotalMilliseconds:F2}ms");
        Console.WriteLine($"Response length: {llmResponse.Length} characters");
        
        // Validate the response against our JSON schema
        var isValid = JsonValidator.ValidateJson(llmResponse, constraints, out var validationErrors);
        
        Console.WriteLine($"JSON Validation: {(isValid ? "✓ VALID" : "✗ INVALID")}");
        if (!isValid)
        {
            Console.WriteLine("Validation Errors:");
            foreach (var error in validationErrors.Take(5))
            {
                Console.WriteLine($"  - {error}");
            }
        }

        // Parse and display the 7 actions
        if (isValid)
        {
            try
            {
                using var doc = JsonDocument.Parse(llmResponse);
                if (doc.RootElement.TryGetProperty("actions", out var actionsArray))
                {
                    Console.WriteLine($"\n{new string('=', 60)}");
                    Console.WriteLine("7 GENERATED ACTION OPTIONS:");
                    Console.WriteLine($"{new string('=', 60)}");
                    
                    var actionIndex = 1;
                    foreach (var actionElement in actionsArray.EnumerateArray())
                    {
                        Console.WriteLine($"\n{actionIndex}. ");
                        
                        if (actionElement.TryGetProperty("action_text", out var actionTextElement))
                        {
                            Console.WriteLine($"   Action: {actionTextElement.GetString()}");
                        }
                        
                        if (actionElement.TryGetProperty("related_skill", out var skillElement))
                        {
                            Console.WriteLine($"   Skill: {skillElement.GetString()}");
                        }
                        
                        if (actionElement.TryGetProperty("difficulty", out var diffElement))
                        {
                            Console.WriteLine($"   Difficulty: {diffElement.GetInt32()}/5");
                        }
                        
                        if (actionElement.TryGetProperty("failure_consequences", out var failElement) &&
                            failElement.TryGetProperty("type", out var failTypeElement))
                        {
                            Console.WriteLine($"   Risk: {failTypeElement.GetString()}");
                        }
                        
                        actionIndex++;
                    }
                    
                    Console.WriteLine($"\n{new string('-', 40)}");
                    Console.WriteLine($"Successfully generated all 7 actions in {responseTime.TotalMilliseconds:F0}ms");
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON parsing error: {ex.Message}");
            }
        }

        Console.WriteLine($"{new string('=', 60)}\n");

    }
    finally
    {
        Console.WriteLine("Stopping LLM server...");
        // Server will be stopped automatically when llmManager is disposed
    }
    
    Console.WriteLine("Forest Location System with LLM integration demo completed!");
    Console.WriteLine("This system demonstrates:");
    Console.WriteLine("✓ Procedural forest generation with environmental variation");
    Console.WriteLine("✓ Hierarchical sublocation systems with conditional access");
    Console.WriteLine("✓ State-dependent content and action generation");
    Console.WriteLine("✓ JSON constraint generation for LLM integration");
    Console.WriteLine("✓ GBNF grammar generation for structured LLM output");
    Console.WriteLine("✓ Array-based action generation (7 actions in single call)");
    Console.WriteLine("✓ Real-time LLM streaming with structured validation");
    Console.WriteLine("✓ Complete DM pipeline from game state to action options");
}

/*

// Test the new LLM server interface
const string SYSTEM_PROMPT = @"You are a masterful fantasy RPG dungeon master and storyteller. Your role is to create immersive, engaging adventures in a rich fantasy world.";

Console.WriteLine("=== Cathedral LLM Server Interface Test ===\n");

// Create the LLM server manager
using var llmManager = new LlamaServerManager();

// Display available models
Console.WriteLine("Available models:");
var availableModels = llmManager.GetAvailableModels();
foreach (var model in availableModels)
{
    Console.WriteLine($"  • {model.Key}: {model.Value}");
}
Console.WriteLine();

// Determine which model to use
string modelToUse = args.Length > 0 && availableModels.ContainsKey(args[0]) ? args[0] : "tiny";
Console.WriteLine($"Using model: {modelToUse} ({availableModels[modelToUse]})");

// Test 1: Start the server
Console.WriteLine("\nTest 1: Starting LLM Server...");
var serverStarted = false;

await llmManager.StartServerAsync(
    isReady =>
    {
        serverStarted = isReady;
        if (isReady)
        {
            Console.WriteLine("✓ Server started successfully!");
            Console.WriteLine($"✓ Current model: {llmManager.GetCurrentModelAlias()} ({llmManager.GetCurrentModelFileName()})");
        }
        else
            Console.WriteLine("✗ Failed to start server!");
    },
    modelAlias: modelToUse);

if (!serverStarted)
{
    Console.WriteLine("Cannot continue tests without server. Exiting.");
    Environment.Exit(1);
}

// Test 2: Create LLM instances
Console.WriteLine("\nTest 2: Creating LLM instances...");
var dmSlotId = await llmManager.CreateInstanceAsync(SYSTEM_PROMPT);
var assistantSlotId = await llmManager.CreateInstanceAsync("You are a helpful assistant.");

Console.WriteLine($"✓ Created DM instance with slot ID: {dmSlotId}");
Console.WriteLine($"✓ Created Assistant instance with slot ID: {assistantSlotId}");

// Test 3: Simple conversation
Console.WriteLine("\nTest 3: Simple conversation with DM...");

var conversationCompleted = false;
var fullResponse = "";

await llmManager.ContinueRequestAsync(
    dmSlotId,
    "Create a brief fantasy adventure hook for a party of 4 level 3 adventurers.",
    onTokenStreamed: (token, slotId) =>
    {
        Console.Write(token);
        fullResponse += token;
    },
    onCompleted: (slotId, response, wasCancelled) =>
    {
        conversationCompleted = true;
        Console.WriteLine($"\n✓ Conversation completed. Response length: {response.Length} characters");
    },
    gbnfGrammar: null
);

// Wait for completion
while (!conversationCompleted)
{
    await Task.Delay(100);
}

// Test 4: Multiple instances conversation
Console.WriteLine("\nTest 4: Testing multiple instances...");

var assistantCompleted = false;
var assistantResponse = "";

await llmManager.ContinueRequestAsync(
    assistantSlotId,
    "What's 2 + 2?",
    onTokenStreamed: (token, slotId) =>
    {
        Console.Write($"[Assistant]: {token}");
        assistantResponse += token;
    },
    onCompleted: (slotId, response, wasCancelled) =>
    {
        assistantCompleted = true;
        Console.WriteLine($"\n✓ Assistant response completed.");
    },
    gbnfGrammar: null
);

// Wait for assistant completion
while (!assistantCompleted)
{
    await Task.Delay(100);
}

// Test 5: Continue conversation with context
Console.WriteLine("\nTest 5: Continue conversation with context...");

var contextCompleted = false;

await llmManager.ContinueRequestAsync(
    dmSlotId,
    "Make the adventure hook you just created more mysterious.",
    onTokenStreamed: (token, slotId) =>
    {
        Console.Write(token);
    },
    onCompleted: (slotId, response, wasCancelled) =>
    {
        contextCompleted = true;
        Console.WriteLine($"\n✓ Context-aware response completed.");
    },
    gbnfGrammar: null
);

// Wait for completion
while (!contextCompleted)
{
    await Task.Delay(100);
}

// Test 6: Reset instance
Console.WriteLine("\nTest 6: Testing instance reset...");
llmManager.ResetInstance(dmSlotId);

var resetCompleted = false;

await llmManager.ContinueRequestAsync(
    dmSlotId,
    "Do you remember the adventure hook you created earlier?",
    onTokenStreamed: (token, slotId) =>
    {
        Console.Write(token);
    },
    onCompleted: (slotId, response, wasCancelled) =>
    {
        resetCompleted = true;
        Console.WriteLine($"\n✓ Reset test completed. (Should not remember previous conversation)");
    },
    gbnfGrammar: null
);

// Wait for completion
while (!resetCompleted)
{
    await Task.Delay(100);
}

// Test 7: Instance information
Console.WriteLine("\nTest 7: Instance information...");
var dmInstance = llmManager.GetInstance(dmSlotId);
var assistantInstance = llmManager.GetInstance(assistantSlotId);

if (dmInstance != null)
{
    Console.WriteLine($"DM Instance - Slot: {dmInstance.SlotId}, Messages: {dmInstance.ConversationHistory.Count}, Created: {dmInstance.CreatedAt:HH:mm:ss}");
}

if (assistantInstance != null)
{
    Console.WriteLine($"Assistant Instance - Slot: {assistantInstance.SlotId}, Messages: {assistantInstance.ConversationHistory.Count}, Created: {assistantInstance.CreatedAt:HH:mm:ss}");
}

var allInstances = llmManager.GetAllInstances();
Console.WriteLine($"✓ Total active instances: {allInstances.Count}");

// Test 8: Cancellation test (optional, only if user wants to test it)
if (args.Length > 0 && args[0] == "--test-cancel")
{
    Console.WriteLine("\nTest 8: Testing request cancellation...");
    
    // Start a long request
    var cancelTestCompleted = false;
    _ = Task.Run(async () =>
    {
        await llmManager.ContinueRequestAsync(
            dmSlotId,
            "Write a very long and detailed fantasy story with at least 1000 words.",
            onTokenStreamed: (token, slotId) =>
            {
                Console.Write(token);
            },
            onCompleted: (slotId, response, wasCancelled) =>
            {
                cancelTestCompleted = true;
                Console.WriteLine($"\n✓ Cancellation test - WasCancelled: {wasCancelled}");
            },
            gbnfGrammar: null
        );
    });
    
    // Wait a bit, then cancel
    await Task.Delay(2000);
    await llmManager.CancelRequestAsync(dmSlotId, slotId =>
    {
        Console.WriteLine($"\n✓ Cancelled request for slot {slotId}");
    });
    
    // Wait for completion
    while (!cancelTestCompleted)
    {
        await Task.Delay(100);
    }
}

Console.WriteLine("\n=== All Tests Completed Successfully! ===");
Console.WriteLine($"\nModel used: {llmManager.GetCurrentModelAlias()} ({llmManager.GetCurrentModelFileName()})");
Console.WriteLine("\nUsage: dotnet run [model-alias]");
Console.WriteLine("  Available models: " + string.Join(", ", availableModels.Keys));
Console.WriteLine("  Default: tiny");
Console.WriteLine("\nPress any key to stop the server and exit...");
Console.ReadKey();

Console.WriteLine("\nStopping server...");
// The server will be stopped automatically when the LlamaServerManager is disposed

*/