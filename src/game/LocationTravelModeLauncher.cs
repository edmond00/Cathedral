using System;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Cathedral.Glyph;
using Cathedral.Glyph.Microworld;
using Cathedral.Game;
using Cathedral.Engine;

namespace Cathedral.Game;

/// <summary>
/// Launcher for the Location Travel Mode game.
/// Sets up the GlyphSphere, MicroworldInterface, and GameController.
/// </summary>
public static class LocationTravelModeLauncher
{
    public static void Launch(int windowWidth = 1200, int windowHeight = 900)
    {
        var camera = new Camera();
        Launch(camera, windowWidth, windowHeight);
    }

    public static void Launch(Camera camera, int windowWidth = 1200, int windowHeight = 900)
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
            
            // Wire up game controller events
            gameController.ModeChanged += (oldMode, newMode) =>
            {
                Console.WriteLine($"\n*** GAME MODE CHANGED: {oldMode} → {newMode} ***\n");
            };
            
            gameController.LocationEntered += (locationState) =>
            {
                Console.WriteLine($"\n*** ENTERED LOCATION: {locationState} ***");
                Console.WriteLine("Press ESC to leave location and return to world view\n");
            };
            
            gameController.LocationExited += (locationState) =>
            {
                Console.WriteLine($"\n*** EXITED LOCATION: {locationState} ***");
                Console.WriteLine("You can now travel to another location\n");
            };
            
            gameController.TravelStarted += () =>
            {
                Console.WriteLine("\n*** TRAVEL STARTED ***");
                Console.WriteLine("Avatar is moving to destination...\n");
            };
            
            gameController.TravelCompleted += () =>
            {
                Console.WriteLine("\n*** TRAVEL COMPLETED ***\n");
            };
            
            // Wire up arrival notification from microworld interface
            microworldInterface.AvatarArrivedAtLocation += (arrivalInfo) =>
            {
                Console.WriteLine($"MicroworldInterface: Avatar arrived at vertex {arrivalInfo.VertexIndex}, location: {arrivalInfo.Location?.Name ?? "none"}");
                Console.WriteLine($"  Biome: {arrivalInfo.Biome.Name}, Neighbors: {arrivalInfo.NeighboringVertices.Count}");
                gameController?.OnAvatarArrived(arrivalInfo.VertexIndex);
            };
            
            Console.WriteLine("\n=== Location Travel Mode Ready ===");
            Console.WriteLine("Controls:");
            Console.WriteLine("  - Click on locations to travel");
            Console.WriteLine("  - Click on avatar to interact with current location");
            Console.WriteLine("  - ESC to leave location interaction");
            Console.WriteLine("  - Arrow keys to rotate camera");
            Console.WriteLine("  - W/S to zoom in/out");
            Console.WriteLine("  - C to toggle debug camera");
            Console.WriteLine("  - D to dump game state\n");
            
            // Show initial state
            Console.WriteLine(gameController.GetDebugInfo());
        };
        
        // Handle ESC key to exit location
        core.KeyDown += (args) =>
        {
            if (args.Key == OpenTK.Windowing.GraphicsLibraryFramework.Keys.Escape)
            {
                if (gameController?.CurrentMode == GameMode.LocationInteraction)
                {
                    Console.WriteLine("ESC pressed - exiting location");
                    gameController.EndLocationInteraction();
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
        };

        core.Run();
        
        // Cleanup
        gameController?.Dispose();
    }
}
