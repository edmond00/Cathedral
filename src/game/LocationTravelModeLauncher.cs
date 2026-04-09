using System;
using System.Threading.Tasks;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Cathedral.Glyph;
using Cathedral.Glyph.Microworld;
using Cathedral.Game;
using Cathedral.Game.Scene.Farm;
using Cathedral.Engine;
using Cathedral.LLM;

namespace Cathedral.Game;

/// <summary>
/// Launcher for the Location Travel Mode game.
/// Sets up the GlyphSphere, MicroworldInterface, and GameController.
/// </summary>
public static class LocationTravelModeLauncher
{
    public static void Launch(int windowWidth = 1200, int windowHeight = 900, bool useLLM = true)
    {
        var camera = new Camera();
        Launch(camera, windowWidth, windowHeight, useLLM);
    }

    public static void Launch(Camera camera, int windowWidth = 1200, int windowHeight = 900, bool useLLM = true)
    {
        Console.WriteLine("=== Launching Location Travel Mode ===\n");
        
        var native = new NativeWindowSettings()
        {
            ClientSize = new Vector2i(windowWidth, windowHeight),
            Title = "Cathedral - Location Travel Mode",
            Flags = ContextFlags.Default,
            API = ContextAPI.OpenGL,
            APIVersion = new Version(3, 3),
            WindowBorder = WindowBorder.Resizable
        };

        using var core = new GlyphSphereCore(GameWindowSettings.Default, native, camera);
        var microworldInterface = new MicroworldInterface(core);
        
        // Create game controller AFTER core is set up
        LocationTravelGameController? gameController = null;
        
        // LLM components (optional - Phase 5)
        LlamaServerManager? llamaServer = null;
        LLMActionExecutor? llmExecutor = null;
        
        // Initialize LLM if requested
        if (useLLM)
        {
            Console.WriteLine("=== Initializing LLM System (Phase 5) ===");
            
            // Initialize logging for LLM communications
            LLMLogger.Initialize();
            Console.WriteLine("✓ LLM communication logging enabled");
            
            try
            {
                llamaServer = new LlamaServerManager();
                
                // Set up server ready callback
                bool serverReady = false;
                llamaServer.ServerReady += (sender, e) =>
                {
                    serverReady = e.IsReady;
                    if (e.IsReady)
                    {
                        Console.WriteLine("✓ LLM Server is ready");
                    }
                    else
                    {
                        Console.WriteLine($"✗ LLM Server failed: {e.Message}");
                    }
                };
                
                // Start server with "tiny" model (qwen2-0.5b) - faster but less sophisticated
                // Use "medium" for phi-4 (better quality but slower)
                Console.WriteLine("Starting LLM server...");
                var startTask = llamaServer.StartServerAsync(
                    onServerReady: (ready) =>
                    {
                        if (ready)
                        {
                            Console.WriteLine("✓ LLM server started successfully");
                        }
                    },
                    modelAlias: null // or "medium" for better quality
                );
                
                // Don't block - server will start in background
                Console.WriteLine("LLM server starting (this may take 30-60 seconds)...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Failed to initialize LLM: {ex.Message}");
                Console.WriteLine("  Continuing without LLM (will use fallback executor)");
                llamaServer = null;
            }
        }
        else
        {
            Console.WriteLine("=== LLM Disabled - Using Simple Action Executor ===");
        }
        
        // Set up event handlers for enhanced interaction
        microworldInterface.VertexHoverEvent += (index, glyph, color) =>
        {
            // Hover feedback
        };
        
        microworldInterface.VertexClickEvent += (index, glyph, color, noise) =>
        {
            var (biome, location, avgNoise) = microworldInterface.GetDetailedBiomeInfoAt(index);
            Console.WriteLine($"Clicked vertex {index}: biome='{biome.Name}' location='{location?.Name ?? "none"}' noise={avgNoise:F3}");
        };
        
        // Generate world and set up game controller when core is loaded
        core.CoreLoaded += () =>
        {
            Console.WriteLine("Core loaded - generating microworld...");
            microworldInterface.GenerateWorld();
            
            Console.WriteLine("Creating game controller...");
            gameController = new LocationTravelGameController(core, microworldInterface);

            // Register scene factories for specific biome types
            gameController.RegisterSceneFactory("farm", new FarmSceneFactory());
            
            // Set up LLM action executor if server is ready
            if (llamaServer != null && llamaServer.IsServerReady)
            {
                Console.WriteLine("Setting up LLM action executor...");
                try
                {
                    var simpleExecutor = new SimpleActionExecutor();
                    llmExecutor = new LLMActionExecutor(llamaServer, simpleExecutor);
                    
                    // Initialize async and set immediately when ready
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            // Initialize (Mode 6 doesn't create Director/Narrator slots)
                            await llmExecutor.InitializeAsync();
                            gameController.SetLLMActionExecutor(llmExecutor);
                            Console.WriteLine("✓ LLM action executor ready");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"✗ Failed to initialize LLM executor: {ex.Message}");
                        }
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Failed to create LLM executor: {ex.Message}");
                    llmExecutor = null;
                }
            }
            else if (llamaServer != null)
            {
                Console.WriteLine("LLM server not ready yet — showing loading screen...");

                // Show loading screen while the model loads
                gameController.UpdateLLMProgress(llamaServer.LoadingProgress, llamaServer.LoadingStatusMessage);
                gameController.SetMode(GameMode.LLMLoading);

                // Forward progress events from the background task to the game controller
                llamaServer.LoadingProgressUpdated += (sender, e) =>
                {
                    gameController?.UpdateLLMProgress(e.Progress, e.StatusMessage);
                };

                // Wire the ServerReady callback to transition out of loading screen
                llamaServer.ServerReady += async (sender, e) =>
                {
                    if (e.IsReady && gameController != null)
                    {
                        Console.WriteLine("LLM server became ready - setting up executor and leaving loading screen...");
                        try
                        {
                            var simpleExecutor = new SimpleActionExecutor();
                            var executor = new LLMActionExecutor(llamaServer, simpleExecutor);
                            // Initialize (Mode 6 doesn't create Director/Narrator slots)
                            await executor.InitializeAsync();
                            gameController.SetLLMActionExecutor(executor);
                            Console.WriteLine("✓ LLM action executor ready (delayed initialization)");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"✗ Failed to initialize delayed LLM executor: {ex.Message}");
                        }
                        // Signal the main-thread game loop to leave LLMLoading mode
                        gameController.NotifyLLMReady();
                    }
                    else if (!e.IsReady && gameController != null)
                    {
                        // LLM failed — still let the player into the game (without LLM)
                        Console.WriteLine("✗ LLM failed to start, continuing without AI.");
                        gameController.NotifyLLMReady();
                    }
                };
            }
            
            // Wire up game controller events
            gameController.ModeChanged += (oldMode, newMode) =>
            {
                Console.WriteLine($"\n*** GAME MODE CHANGED: {oldMode} → {newMode} ***\n");
            };
            
            gameController.LocationExited += (locationState) =>
            {
                Console.WriteLine($"\n*** EXITED LOCATION: {locationState} ***");
                Console.WriteLine("You can now travel to another location\n");
            };
            
            gameController.TravelStarted += () =>
            {
                Console.WriteLine("\n*** TRAVEL STARTED ***");
                Console.WriteLine("Protagonist is moving to destination...\n");
            };
            
            gameController.TravelCompleted += () =>
            {
                Console.WriteLine("\n*** TRAVEL COMPLETED ***\n");
            };
            
            // Wire up arrival notification from microworld interface
            microworldInterface.ProtagonistArrivedAtLocation += (arrivalInfo) =>
            {
                Console.WriteLine($"MicroworldInterface: Protagonist arrived at vertex {arrivalInfo.VertexIndex}, location: {arrivalInfo.Location?.Name ?? "none"}");
                Console.WriteLine($"  Biome: {arrivalInfo.Biome.Name}, Neighbors: {arrivalInfo.NeighboringVertices.Count}");
                gameController?.OnProtagonistArrived(arrivalInfo.VertexIndex);
            };
            
            // Wire up update loop for loading animations
            core.UpdateRequested += (deltaTime) =>
            {
                gameController?.Update();
            };
            
            // Wire up mouse wheel for scrolling
            core.MouseWheelScrolled += (delta) =>
            {
                gameController?.OnMouseWheel(delta);
            };
            
            Console.WriteLine("\n=== Location Travel Mode Ready ===");
            Console.WriteLine("Controls:");
            Console.WriteLine("  - Click on locations to travel");
            Console.WriteLine("  - Click on protagonist to interact with current location");
            Console.WriteLine("  - ESC to leave location interaction");
            Console.WriteLine("  - Arrow keys to rotate camera");
            Console.WriteLine("  - W/S to zoom in/out");
            Console.WriteLine("  - C to toggle debug camera");
            Console.WriteLine("  - D to dump game state\n");
            
            // Show initial state
            Console.WriteLine(gameController.GetDebugInfo());
        };
        
        // Handle ESC key for menu and location exit
        core.KeyDown += (args) =>
        {
            if (args.Key == OpenTK.Windowing.GraphicsLibraryFramework.Keys.Escape)
            {
                if (gameController?.CurrentMode == GameMode.Fighting)
                {
                    // ESC during fight: pass to fight adapter (cancels skill mode or does nothing)
                    if (gameController is LocationTravelGameController ltgc)
                    {
                        ltgc.OnKeyDown(args.Key);
                    }
                }
                else if (gameController?.CurrentMode == GameMode.Dialogue)
                {
                    // ESC during dialogue: request exit
                    if (gameController is LocationTravelGameController ltgc)
                    {
                        ltgc.OnKeyDown(args.Key);
                    }
                }
                else if (gameController?.CurrentMode == GameMode.MainMenu)
                {
                    // ESC in main menu: return to world if game has started, otherwise do nothing
                    if (gameController is LocationTravelGameController ltgc && ltgc.HasGameStarted)
                    {
                        Console.WriteLine("ESC pressed - closing main menu");
                        gameController.SetMode(GameMode.WorldView);
                    }
                }
                else if (gameController?.CurrentMode == GameMode.ProtagonistManagement)
                {
                    // ESC in management menu: return to main menu
                    Console.WriteLine("ESC pressed - closing management menu");
                    gameController.SetMode(GameMode.MainMenu);
                }
                else if (gameController?.CurrentMode == GameMode.LocationInteraction)
                {
                    // Check if in Phase 6 mode with popup open
                    if (gameController is LocationTravelGameController ltgc)
                    {
                        // First try to close popup
                        if (ltgc.CloseNarrativePopup())
                        {
                            Console.WriteLine("ESC pressed - closed thinking modusMentis popup");
                            return; // Don't exit location, just close popup
                        }
                        
                        // No popup open, exit Phase 6 mode
                        Console.WriteLine("ESC pressed - exiting location");
                        ltgc.ExitNarrativeMode();
                    }
                    
                    gameController.EndLocationInteraction();
                }
                else if (gameController?.CurrentMode == GameMode.WorldView)
                {
                    // ESC in world view: open main menu
                    Console.WriteLine("ESC pressed - opening main menu");
                    gameController.SetMode(GameMode.MainMenu);
                }
            }
            else if (args.Key == OpenTK.Windowing.GraphicsLibraryFramework.Keys.D)
            {
                // Dump debug info
                if (gameController != null)
                {
                    Console.WriteLine("\n" + gameController.GetDebugInfo());
                }
            }
            else if (args.Key == OpenTK.Windowing.GraphicsLibraryFramework.Keys.G)
            {
                // Dump narration graph structure (only works in narrative mode)
                if (gameController?.CurrentMode == GameMode.LocationInteraction)
                {
                    if (gameController is LocationTravelGameController ltgc)
                    {
                        ltgc.PrintNarrativeGraph();
                    }
                }
                else
                {
                    Console.WriteLine("G key: Graph visualization only available in narrative mode (enter a location first)");
                }
            }
            else
            {
                // Forward other keys to fight/dialogue modes
                if (gameController is LocationTravelGameController ltgc2 &&
                    (ltgc2.CurrentMode == GameMode.Fighting || ltgc2.CurrentMode == GameMode.Dialogue))
                {
                    ltgc2.OnKeyDown(args.Key);
                }
            }
        };

        core.Run();
        
        // Cleanup
        Console.WriteLine("Shutting down...");
        // llmExecutor no longer needs Dispose() - simplified to just provide LlamaServerManager access
        gameController?.Dispose();
        llamaServer?.Dispose();
        Console.WriteLine("Cleanup complete");
    }
}
