using Cathedral.LLM;
using Cathedral.LLM.JsonConstraints;
using Cathedral.Glyph;
using Cathedral.Engine;
using Cathedral.Glyph.Microworld.LocationSystem;
using Cathedral.Glyph.Microworld.LocationSystem.Generators;

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
    Console.WriteLine("Starting Forest Location System demonstration...\n");
    
    // Create forest generator
    var forestGenerator = new ForestFeatureGenerator();
    
    // Generate different forest instances
    var forestIds = new[] { "forest_001", "forest_002", "forest_003" };
    
    foreach (var forestId in forestIds)
    {
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
        var constraints = Blueprint2Constraint.GenerateActionConstraints(blueprint, "forest_edge", new Dictionary<string, string>());
        
        Console.WriteLine($"\nGenerated JSON constraint field for LLM action generation");
        Console.WriteLine($"Constraint type: {constraints.GetType().Name}");
        
        Console.WriteLine("\n" + new string('-', 60) + "\n");
        
        // Add a small delay for readability
        await Task.Delay(1000);
    }
    
    Console.WriteLine("Forest Location System demo completed!");
    Console.WriteLine("This system demonstrates:");
    Console.WriteLine("✓ Procedural forest generation with environmental variation");
    Console.WriteLine("✓ Hierarchical sublocation systems with conditional access");
    Console.WriteLine("✓ State-dependent content and action generation");
    Console.WriteLine("✓ JSON constraint generation for LLM integration");
    Console.WriteLine("✓ Deterministic seeding for consistent locations");
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