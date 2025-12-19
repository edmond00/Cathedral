using Cathedral.LLM;
using Cathedral.LLM.JsonConstraints;
using Cathedral.Glyph;
using Cathedral.Engine;
using Cathedral.Game;
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
Console.WriteLine("1. Launch Narrative RPG System (Phase 6 - Chain-of-Thought)");
Console.WriteLine("2. Run LLM integration tests (JSON constraints)");
Console.WriteLine("3. Launch GlyphSphere with Terminal HUD");
Console.WriteLine("4. Test Terminal Module (standalone)");
Console.WriteLine("5. Test Forest Location System Demo");
Console.WriteLine("6. Launch Location Travel Mode (Phase 1)");
Console.WriteLine("7. Test Critic Evaluator (token probabilities)");
Console.WriteLine("8. Exit");

Console.Write("\nEnter your choice (1-8): ");
var choice = Console.ReadLine();

switch (choice)
{
    case "1":
        Console.WriteLine("\n=== Narrative RPG System (Chain-of-Thought) ===");
        Console.WriteLine("This launches the Phase 6 narrative system with observation, thinking, and action phases.");
        Console.WriteLine("Press Enter to continue or Ctrl+C to cancel.");
        Console.ReadLine();
        await Cathedral.Game.Narrative.NarrativeSystemDemo.RunDemo();
        break;

    case "2":
        Console.WriteLine("\n=== JSON Constraint LLM Integration Tests ===");
        Console.WriteLine("This will start the LLM server and run comprehensive tests...");
        Console.WriteLine("Press Enter to continue or Ctrl+C to cancel.");
        Console.ReadLine();
        await JsonConstraintTest.TestWithLLM();
        break;

    case "3":
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

    case "4":
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

    case "5":
        Console.WriteLine("\n=== Forest Location System Demo ===");
        await TestForestLocationSystem();
        break;

    case "6":
        Console.WriteLine("\n=== Location Travel Mode (Phase 1) ===");
        Console.WriteLine("This is the new integrated mode combining GlyphSphere + Terminal + Location System");
        Console.WriteLine("Phase 1: Core framework with mode transitions");
        Console.WriteLine("Press Enter to continue or Ctrl+C to cancel.");
        Console.ReadLine();
        Cathedral.Game.LocationTravelModeLauncher.Launch();
        break;

    case "7":
        Console.WriteLine("\n=== Testing Critic Evaluator ===");
        Console.WriteLine("This will test the Critic's ability to evaluate actions using token probabilities...");
        Console.WriteLine("Press Enter to continue or Ctrl+C to cancel.");
        Console.ReadLine();
        await TestCriticEvaluator();
        break;

    case "8":
        Console.WriteLine("Goodbye!");
        Environment.Exit(0);
        break;

    default:
        Console.WriteLine("Invalid choice. Exiting.");
        break;
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();

static async Task TestCriticEvaluator()
{
    Console.WriteLine("\n=== Critic Evaluator Test Suite ===\n");
    
    // Start LLM Server
    Console.WriteLine("Starting LLM Server...");
    using var llmManager = new LlamaServerManager();
    var serverStarted = false;

    await llmManager.StartServerAsync(
        isReady => serverStarted = isReady,
        modelAlias: "tiny");

    if (!serverStarted)
    {
        Console.WriteLine("✗ Cannot run Critic tests without LLM server.");
        return;
    }

    Console.WriteLine("✓ LLM Server started successfully!\n");

    // Initialize Critic Evaluator
    using var critic = new CriticEvaluator(llmManager);
    await critic.InitializeAsync();
    Console.WriteLine("✓ Critic Evaluator initialized\n");

    Console.WriteLine(new string('=', 80));
    
    // Test 1: Action-Skill Coherence
    Console.WriteLine("\n--- TEST 1: Action-Skill Coherence ---\n");
    
    var testCases = new[]
    {
        ("Climb a tall tree to scout the area", "Athletics"),
        ("Pick the lock on an old chest", "Lockpicking"),
        ("Swim across a raging river", "Cooking"),  // Intentionally incoherent
        ("Persuade the guard to let you pass", "Persuasion"),
        ("Cast a fireball at the enemies", "Swimming")  // Intentionally incoherent
    };

    foreach (var (action, skill) in testCases)
    {
        Console.WriteLine($"Action: {action}");
        Console.WriteLine($"Skill:  {skill}");
        
        var coherenceScore = await critic.EvaluateActionSkillCoherence(action, skill);
        
        Console.WriteLine($"Coherence Score: {coherenceScore:F4} ({coherenceScore * 100:F1}%)");
        Console.WriteLine($"Assessment: {GetCoherenceAssessment(coherenceScore)}");
        Console.WriteLine();
    }

    Console.WriteLine(new string('-', 80));

    // Test 2: Action-Consequence Plausibility
    Console.WriteLine("\n--- TEST 2: Action-Consequence Plausibility ---\n");
    
    var consequenceTests = new[]
    {
        ("Light a campfire in the clearing", "The area becomes warm and illuminated"),
        ("Attack the dragon with a wooden sword", "You slay the ancient dragon effortlessly"),  // Implausible
        ("Drink from the poisoned well", "You feel refreshed and energized"),  // Implausible
        ("Study the ancient tome for hours", "You gain knowledge of forgotten spells"),
        ("Jump off a cliff without protection", "You land safely and continue walking")  // Implausible
    };

    foreach (var (action, consequence) in consequenceTests)
    {
        Console.WriteLine($"Action:      {action}");
        Console.WriteLine($"Consequence: {consequence}");
        
        var plausibilityScore = await critic.EvaluateActionConsequencePlausibility(action, consequence);
        
        Console.WriteLine($"Plausibility Score: {plausibilityScore:F4} ({plausibilityScore * 100:F1}%)");
        Console.WriteLine($"Assessment: {GetPlausibilityAssessment(plausibilityScore)}");
        Console.WriteLine();
    }

    Console.WriteLine(new string('-', 80));

    // Test 3: Narrative Quality
    Console.WriteLine("\n--- TEST 3: Narrative Quality ---\n");
    
    var narrativeTests = new[]
    {
        ("You walk into the forest. It is dark. There are trees.", "engaging and vivid"),
        ("As twilight descends, the ancient forest beckons with whispered secrets and dancing shadows.", "engaging and vivid"),
        ("Thing happen. You do stuff. End.", "engaging and vivid"),  // Poor quality
        ("The warrior raised his blade, determination burning in his eyes as thunder rumbled overhead.", "dramatic and exciting")
    };

    foreach (var (narrative, criterion) in narrativeTests)
    {
        Console.WriteLine($"Narrative: {narrative}");
        Console.WriteLine($"Criterion: {criterion}");
        
        var qualityScore = await critic.EvaluateNarrativeQuality(narrative, criterion);
        
        Console.WriteLine($"Quality Score: {qualityScore:F4} ({qualityScore * 100:F1}%)");
        Console.WriteLine($"Assessment: {GetQualityAssessment(qualityScore)}");
        Console.WriteLine();
    }

    Console.WriteLine(new string('=', 80));
    
    // Display statistics
    Console.WriteLine("\n--- CRITIC STATISTICS ---\n");
    var stats = critic.GetStatistics();
    Console.WriteLine($"Total Evaluations: {stats.totalEvaluations}");
    Console.WriteLine($"Total Duration:    {stats.totalDurationMs:F0}ms");
    if (stats.totalEvaluations > 0)
    {
        Console.WriteLine($"Average Duration:  {stats.totalDurationMs / stats.totalEvaluations:F0}ms per evaluation");
    }
    
    Console.WriteLine("\n✓ All Critic tests completed!");
}

static string GetCoherenceAssessment(double score)
{
    return score switch
    {
        >= 0.8 => "✓ Highly coherent",
        >= 0.6 => "○ Moderately coherent",
        >= 0.4 => "△ Somewhat coherent",
        _ => "✗ Incoherent"
    };
}

static string GetPlausibilityAssessment(double score)
{
    return score switch
    {
        >= 0.8 => "✓ Highly plausible",
        >= 0.6 => "○ Moderately plausible",
        >= 0.4 => "△ Somewhat plausible",
        _ => "✗ Implausible"
    };
}

static string GetQualityAssessment(double score)
{
    return score switch
    {
        >= 0.8 => "✓ High quality",
        >= 0.6 => "○ Good quality",
        >= 0.4 => "△ Acceptable quality",
        _ => "✗ Poor quality"
    };
}

static async Task TestForestLocationSystem()
{
    Console.WriteLine("=== Interactive Forest Adventure Demo ===\n");
    
    // Create forest generator
    var forestGenerator = new ForestFeatureGenerator();
    var forestId = "forest_001";
    
    // Generate blueprint (structured data)
    var blueprint = forestGenerator.GenerateBlueprint(forestId);
    
    // Initial game state
    var currentSublocation = "forest_edge";
    var currentStates = new Dictionary<string, string>
    {
        ["time_of_day"] = "morning",
        ["weather"] = "clear", 
        ["wildlife_state"] = "calm",
    };

    Console.WriteLine($"=== ENTERING {forestId.ToUpper()} ===\n");
    
    // Start LLM Server
    Console.WriteLine("Starting LLM Server...");
    using var llmManager = new LlamaServerManager();
    var serverStarted = false;

    await llmManager.StartServerAsync(
        isReady => serverStarted = isReady,
        modelAlias: "medium");

    if (!serverStarted)
    {
        Console.WriteLine("✗ Cannot run interactive demo without LLM server.");
        return;
    }

    Console.WriteLine("✓ LLM Server started successfully!\n");

    // Initialize prompt constructors
    var director = new DirectorPromptConstructor(blueprint, currentSublocation, currentStates, 5);
    var narrator = new NarratorPromptConstructor(blueprint, currentSublocation, currentStates);

    // Initialize gameplay logger
    var logger = new GameplayLogger(forestId);
    Console.WriteLine($"✓ Logging to: {logger.GetLogFilePath()}");

    // Create LLM instances with different roles
    var directorSlotId = await llmManager.CreateInstanceAsync(director.GetSystemPrompt());
    var narratorSlotId = await llmManager.CreateInstanceAsync(narrator.GetSystemPrompt());

    Console.WriteLine("✓ Created Director LLM instance (action generation)");
    Console.WriteLine("✓ Created Narrator LLM instance (storytelling)\n");

    // Game loop variables
    PlayerAction? previousAction = null;
    var turnNumber = 1;
    var gameRunning = true;

    Console.WriteLine("Welcome to the Interactive Forest Adventure!");
    Console.WriteLine("Type 'quit' at any time to exit the game.\n");
    Console.WriteLine(new string('=', 80));

    while (gameRunning)
    {
        Console.WriteLine($"\n--- TURN {turnNumber} ---\n");

        // Log turn start
        logger.LogTurnStart(turnNumber, currentSublocation, currentStates);

        // Step 1: Director generates action choices
        Console.WriteLine("🎲 Director is generating action choices...");
        
        var directorPrompt = director.ConstructPrompt(previousAction);
        var directorGbnf = director.GetGbnf();
        
        // Log director request
        logger.LogDirectorRequest(directorPrompt, directorGbnf);
        
        var (actionResponse, directorResponseTime) = await GetLLMResponseWithTiming(llmManager, directorSlotId, directorPrompt, directorGbnf);
        
        if (string.IsNullOrEmpty(actionResponse))
        {
            Console.WriteLine("✗ Failed to generate actions. Ending game.");
            logger.LogGameEnd("Failed to generate actions");
            break;
        }

        // Validate and parse actions
        var isValid = JsonValidator.ValidateJson(actionResponse, director.GetConstraints(), out var validationErrors);
        
        // Log director response
        logger.LogDirectorResponse(actionResponse, directorResponseTime, isValid, validationErrors.ToList());
        
        if (!isValid)
        {
            Console.WriteLine("✗ Invalid action response generated. Ending game.");
            foreach (var error in validationErrors.Take(3))
            {
                Console.WriteLine($"  Error: {error}");
            }
            logger.LogGameEnd("Invalid action response generated");
            break;
        }

        var actionChoices = NarratorPromptConstructor.ParseActionChoices(actionResponse);
        var actionTexts = NarratorPromptConstructor.ExtractActionChoices(actionResponse);

        if (actionChoices.Count == 0)
        {
            Console.WriteLine("✗ No valid actions generated. Ending game.");
            logger.LogGameEnd("No valid actions generated");
            break;
        }

        // Step 2: Narrator presents the situation
        Console.WriteLine("📖 Narrator is crafting the scene...\n");
        
        // Convert action texts to ActionInfo (with empty skill for now since we're not parsing it)
        var actionInfos = actionTexts.Select(text => new ActionInfo(text, "")).ToList();
        var narratorPrompt = narrator.ConstructPrompt(previousAction, actionInfos);
        var narratorGbnf = narrator.GetGbnf(previousAction?.WasSuccessful);
        
        // Log narrator request
        logger.LogNarratorRequest(narratorPrompt, narratorGbnf);
        
        var (narrativeResponse, narratorResponseTime) = await GetLLMResponseWithTiming(llmManager, narratorSlotId, narratorPrompt, narratorGbnf);
        
        // Log narrator response
        logger.LogNarratorResponse(narrativeResponse, narratorResponseTime);
        
        if (!string.IsNullOrEmpty(narrativeResponse))
        {
            // Display the narrative
            Console.WriteLine(new string('─', 80));
            Console.WriteLine(narrativeResponse);
            Console.WriteLine(new string('─', 80));
        }

        // Step 3: Present action choices to player
        Console.WriteLine("\n🎯 AVAILABLE ACTIONS:");
        Console.WriteLine();
        for (int i = 0; i < actionChoices.Count; i++)
        {
            var choice = actionChoices[i];
            Console.WriteLine($"[{i + 1}] {choice.ActionText}");
            Console.WriteLine($"     Skill: {choice.Skill} | Difficulty: {choice.Difficulty}/5 | Risk: {choice.Risk}");
            Console.WriteLine();
        }

        // Step 4: Get player choice
        var playerChoice = GetPlayerChoice(actionChoices.Count);
        
        if (playerChoice == -1)
        {
            Console.WriteLine("\nThanks for playing! Farewell, adventurer.");
            logger.LogGameEnd("Player quit");
            gameRunning = false;
            continue;
        }

        var selectedAction = actionChoices[playerChoice - 1];
        Console.WriteLine($"\n➤ You chose: {selectedAction.ActionText}");
        
        // Log player action
        logger.LogPlayerAction(playerChoice, selectedAction.ActionText);

        // Step 5: Simulate action outcome (simplified for demo)
        var outcome = SimulateActionOutcome(selectedAction, actionResponse, playerChoice - 1);
        
        // Log action outcome
        logger.LogActionOutcome(outcome);
        
        // Update game state based on outcome
        ApplyActionOutcome(outcome, ref currentSublocation, currentStates, director, narrator);
        
        previousAction = outcome;
        turnNumber++;

        // Add a pause between turns
        Console.WriteLine("\nPress Enter to continue to the next turn...");
        Console.ReadLine();
        Console.Clear();
    }

    Console.WriteLine("\n" + new string('=', 80));
    Console.WriteLine("ADVENTURE COMPLETED");
    logger.LogGameEnd("Demo completed successfully");
    Console.WriteLine($"📝 Complete session log saved to: {logger.GetLogFilePath()}");
    Console.WriteLine("This interactive demo showcased:");
    Console.WriteLine("✓ Modular Director/Narrator LLM architecture");
    Console.WriteLine("✓ Dynamic action generation with JSON constraints");
    Console.WriteLine("✓ Immersive narrative storytelling");
    Console.WriteLine("✓ Interactive player choice system");
    Console.WriteLine("✓ Game state management and progression");
    Console.WriteLine("✓ Real-time LLM integration for dual-role gameplay");
    Console.WriteLine("✓ Complete session logging for analysis");
    Console.WriteLine(new string('=', 80));
}

// Helper methods for interactive game loop

/// <summary>
/// Gets LLM response with optional GBNF grammar and logging
/// </summary>
static async Task<(string response, TimeSpan responseTime)> GetLLMResponseWithTiming(LlamaServerManager llmManager, int slotId, string prompt, string? gbnf = null)
{
    var responseBuilder = new StringBuilder();
    var completed = false;
    var startTime = DateTime.UtcNow;

    await llmManager.ContinueRequestAsync(
        slotId,
        prompt,
        onTokenStreamed: (token, _) => responseBuilder.Append(token),
        onCompleted: (_, response, wasCancelled) => completed = true,
        gbnfGrammar: gbnf
    );

    // Wait for completion
    while (!completed)
    {
        await Task.Delay(100);
    }

    var responseTime = DateTime.UtcNow - startTime;
    return (responseBuilder.ToString().Trim(), responseTime);
}

/// <summary>
/// Gets player's action choice from input
/// </summary>
static int GetPlayerChoice(int maxChoices)
{
    while (true)
    {
        Console.Write($"\nChoose an action (1-{maxChoices}) or 'quit' to exit: ");
        var input = Console.ReadLine()?.Trim().ToLower();

        if (input == "quit" || input == "q" || input == "exit")
        {
            return -1; // Signal to quit
        }

        if (int.TryParse(input, out var choice) && choice >= 1 && choice <= maxChoices)
        {
            return choice;
        }

        Console.WriteLine($"Invalid choice. Please enter a number between 1 and {maxChoices}, or 'quit' to exit.");
    }
}

/// <summary>
/// Simulates action outcome (simplified for demo)
/// </summary>
static PlayerAction SimulateActionOutcome(ActionChoice selectedAction, string fullActionResponse, int actionIndex)
{
    var random = new Random();
    var success = random.NextDouble() > 0.3; // 70% success rate for demo
    
    var outcome = new PlayerAction
    {
        ActionText = selectedAction.ActionText,
        WasSuccessful = success
    };

    // Parse the full action data for consequences
    try
    {
        using var doc = JsonDocument.Parse(fullActionResponse);
        if (doc.RootElement.TryGetProperty("actions", out var actionsArray))
        {
            var actions = actionsArray.EnumerateArray().ToList();
            if (actionIndex < actions.Count)
            {
                var actionData = actions[actionIndex];
                
                string consequenceType = success ? "success_consequences" : "failure_consequences";
                
                if (actionData.TryGetProperty(consequenceType, out var consequences))
                {
                    // Parse success consequences
                    if (success && consequences.TryGetProperty("state_changes", out var stateChanges))
                    {
                        if (stateChanges.TryGetProperty("category", out var category) && 
                            stateChanges.TryGetProperty("new_state", out var newState))
                        {
                            var categoryStr = category.GetString();
                            var newStateStr = newState.GetString();
                            
                            if (!string.IsNullOrEmpty(categoryStr) && !string.IsNullOrEmpty(newStateStr) && 
                                categoryStr != "none" && newStateStr != "none")
                            {
                                outcome.StateChanges[categoryStr] = newStateStr;
                            }
                        }
                    }
                    
                    if (success)
                    {
                        if (consequences.TryGetProperty("sublocation_change", out var sublocChange))
                        {
                            var newSubloc = sublocChange.GetString();
                            if (!string.IsNullOrEmpty(newSubloc) && newSubloc != "none")
                            {
                                outcome.NewSublocation = newSubloc;
                            }
                        }
                        
                        if (consequences.TryGetProperty("item_gained", out var itemGained))
                        {
                            var item = itemGained.GetString();
                            if (!string.IsNullOrEmpty(item) && item != "none")
                            {
                                outcome.ItemGained = item;
                            }
                        }
                        
                        if (consequences.TryGetProperty("companion_gained", out var companionGained))
                        {
                            var companion = companionGained.GetString();
                            if (!string.IsNullOrEmpty(companion) && companion != "none")
                            {
                                outcome.CompanionGained = companion;
                            }
                        }
                    }
                    
                    // Set outcome description
                    if (consequences.TryGetProperty("description", out var description))
                    {
                        outcome.Outcome = description.GetString() ?? "";
                    }
                    else if (consequences.TryGetProperty("type", out var failureType))
                    {
                        outcome.Outcome = failureType.GetString() ?? "";
                    }
                }
            }
        }
    }
    catch (JsonException)
    {
        // Fallback outcome if parsing fails
        outcome.Outcome = success ? "Your action succeeded!" : "Your action didn't go as planned.";
    }

    if (string.IsNullOrEmpty(outcome.Outcome))
    {
        outcome.Outcome = success ? "Your action was successful!" : "Your attempt failed.";
    }

    return outcome;
}

/// <summary>
/// Applies action outcome to game state
/// </summary>
static void ApplyActionOutcome(PlayerAction outcome, ref string currentSublocation, 
    Dictionary<string, string> currentStates, DirectorPromptConstructor director, NarratorPromptConstructor narrator)
{
    // Apply state changes
    foreach (var (category, newState) in outcome.StateChanges)
    {
        currentStates[category] = newState;
    }

    // Apply sublocation change
    if (!string.IsNullOrEmpty(outcome.NewSublocation))
    {
        currentSublocation = outcome.NewSublocation;
    }

    // Update both prompt constructors with new state
    director.UpdateGameState(currentSublocation, currentStates);
    narrator.UpdateGameState(currentSublocation, currentStates);
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