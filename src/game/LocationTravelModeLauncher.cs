using System;
using System.Threading.Tasks;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Cathedral.Audio;
using Cathedral.Glyph;
using Cathedral.Glyph.Microworld;
using Cathedral.Game;
using Cathedral.Game.Scene.Farm;
using Cathedral.Game.Scene.Plain;
using Cathedral.Game.Scene.Field;
using Cathedral.Game.Scene.Forest;
using Cathedral.Game.Scene.Village;
using Cathedral.Game.Scene.Cave;
using Cathedral.Game.Scene.Mountain;
using Cathedral.Game.Scene.Peak;
using Cathedral.Game.Scene.Coast;
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

        // Create and start procedural ambient music engine (gracefully disabled if no MIDI device)
        var ambianceEngine = new AmbianceEngine();
        if (!ambianceEngine.Start())
        {
            Console.WriteLine("No MIDI output device found — ambient music disabled.");
            ambianceEngine.Dispose();
            ambianceEngine = null!;
        }
        else
        {
            Console.WriteLine($"✓ Ambient music engine started ({ambianceEngine.DeviceName})");
            ambianceEngine.SetMood(MusicMoodState.Neutral);
            ambianceEngine.SetActiveTrackCount(1); // Drone only until a mode sets the full track count
        }

        // Create game controller AFTER core is set up
        LocationTravelGameController? gameController = null;

        // Command driver for --cli: created once the controller exists (see CoreLoaded below).
        Cathedral.Game.Cli.CliDriver? cliDriver = null;
        
        // LLM server (optional)
        LlamaServerManager? llamaServer = null;
        
        // Initialize LLM if requested
        if (useLLM)
        {
            if (PlaygroundMode.IsActive)
            {
                // Playground: create the server object so components can be constructed,
                // but do NOT start it — all actual LLM calls are intercepted before reaching it.
                Console.WriteLine("=== Playground Mode: LLM server not started ===");
                llamaServer = new LlamaServerManager();
            }
            else
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
                    
                    // Start server with the single configured model (see Config.LLM.ModelFileName).
                    Console.WriteLine("Starting LLM server...");
                    var startTask = llamaServer.StartServerAsync(
                        onServerReady: (ready) =>
                        {
                            if (ready)
                            {
                                Console.WriteLine("✓ LLM server started successfully");
                            }
                        }
                    );
                    
                    // Don't block - server will start in background
                    Console.WriteLine("LLM server starting (this may take 30-60 seconds)...");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Failed to initialize LLM: {ex.Message}");
                    Console.WriteLine("  Continuing without LLM (fallback narration)");
                    llamaServer = null;
                }
            }
        }
        else
        {
            Console.WriteLine("=== LLM Disabled - Using fallback narration ===");
        }
        
        // Set up event handlers for enhanced interaction
        int lastHoveredVertexTick = -1;
        microworldInterface.VertexHoverEvent += (index, glyph, color) =>
        {
            // Play tick when entering a new vertex that would show a path/location preview.
            // Mirror the guard conditions in HandleVertexHovered: skip protagonist's own cell and
            // skip while moving (path previews aren't shown in either case).
            if (index >= 0
                && index != lastHoveredVertexTick
                && index != microworldInterface.GetAvatarVertex()
                && !microworldInterface.IsAvatarMoving()
                && !microworldInterface.IsOutOfTravelRange(index))
            {
                ambianceEngine?.TriggerGameEvent(GameEventType.SmallInteraction);
            }
            lastHoveredVertexTick = index;
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
            gameController = new LocationTravelGameController(core, microworldInterface, ambianceEngine);

            // Register scene factories for specific biome types
            gameController.RegisterSceneFactory("farm",     new FarmSceneFactory());
            gameController.RegisterSceneFactory("plain",    new PlainSceneFactory());
            gameController.RegisterSceneFactory("field",    new FieldSceneFactory());
            gameController.RegisterSceneFactory("forest",   new ForestSceneFactory());
            gameController.RegisterSceneFactory("village",  new VillageSceneFactory());
            gameController.RegisterSceneFactory("cave",     new CaveSceneFactory());
            gameController.RegisterSceneFactory("mountain", new MountainSceneFactory());
            gameController.RegisterSceneFactory("peak",     new PeakSceneFactory());
            gameController.RegisterSceneFactory("coast",    new CoastSceneFactory());
            
            // Attach the LLM server if it is ready
            if (PlaygroundMode.IsActive && llamaServer != null)
            {
                // Playground mode: attach immediately — the server is never actually started
                Console.WriteLine("Attaching playground LLM server (no server process)...");
                gameController?.SetLlamaServer(llamaServer);
                Console.WriteLine("✓ Playground LLM server attached");
            }
            else if (llamaServer != null && llamaServer.IsServerReady)
            {
                Console.WriteLine("Attaching LLM server...");
                gameController.SetLlamaServer(llamaServer);
                Console.WriteLine("✓ LLM server attached");
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
                llamaServer.ServerReady += (sender, e) =>
                {
                    if (e.IsReady && gameController != null)
                    {
                        Console.WriteLine("LLM server became ready - attaching and leaving loading screen...");
                        try
                        {
                            gameController.SetLlamaServer(llamaServer);
                            Console.WriteLine("✓ LLM server attached (delayed)");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"✗ Failed to attach delayed LLM server: {ex.Message}");
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

            // Wire up per-step notification so vital heat can be consumed at each vertex entered
            microworldInterface.ProtagonistSteppedToVertex += (vertexIndex) =>
            {
                gameController?.OnProtagonistSteppedToVertex(vertexIndex);
            };
            
            // Wire up update loop for loading animations
            core.UpdateRequested += (deltaTime) =>
            {
                gameController?.Update((float)deltaTime);

                // Drain queued CLI commands on the game thread, after Update so `dump`/`regions`
                // observe the frame the player would see and hit-regions are freshly populated.
                cliDriver?.Pump();
            };
            
            // Wire up mouse wheel for scrolling
            core.MouseWheelScrolled += (delta) =>
            {
                gameController?.OnMouseWheel(delta);
            };
            
            // Start accepting scripted/stdin commands now that the controller exists.
            if (Cathedral.Game.Cli.CliMode.IsActive && gameController != null)
            {
                cliDriver = new Cathedral.Game.Cli.CliDriver(gameController);
                cliDriver.Start();
            }

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
                    // ESC during dialogue: pause overlay, exactly like narration — the conversation
                    // is NOT interrupted (walking away is the footer INTERRUPT button's job) and
                    // resumes when the menu is dismissed (see MenuReturnMode).
                    Console.WriteLine("ESC pressed - opening pause menu over dialogue");
                    gameController.SetMode(GameMode.MainMenu);
                }
                else if (gameController?.CurrentMode == GameMode.MainMenu)
                {
                    // ESC in main menu: resume where the menu was opened from (pause-overlay return —
                    // narration or world). Do nothing before the game has started.
                    if (gameController is LocationTravelGameController ltgc && ltgc.HasGameStarted)
                    {
                        Console.WriteLine($"ESC pressed - closing main menu, resuming {ltgc.MenuReturnMode}");
                        gameController.SetMode(ltgc.MenuReturnMode);
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

                        // No popup open: open the main menu as a pause overlay WITHOUT tearing down
                        // narration. Leaving the scene is done via the in-narration LEAVE/RUNAWAY button.
                        Console.WriteLine("ESC pressed - opening pause menu over narration");
                        gameController.SetMode(GameMode.MainMenu);
                    }
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
        gameController?.Dispose();
        llamaServer?.Dispose();
        ambianceEngine?.Dispose();
        Console.WriteLine("Cleanup complete");
    }
}
