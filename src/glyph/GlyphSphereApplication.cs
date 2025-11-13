// GlyphSphereApplication.cs - Integrated application that combines Core and Interface
using System;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Cathedral.Glyph.Microworld;
using Cathedral.Engine;

namespace Cathedral.Glyph
{
    public static class GlyphSphereLauncher
    {
        // Public method you call from your terminal-based app.
        public static void LaunchGlyphSphere(int windowWidth = 900, int windowHeight = 900)
        {
            GlyphSphereApplication.Launch(windowWidth, windowHeight);
        }
        
        // Public method that accepts a custom camera
        public static void LaunchGlyphSphere(Camera camera, int windowWidth = 900, int windowHeight = 900)
        {
            GlyphSphereApplication.Launch(camera, windowWidth, windowHeight);
        }
    }

    public class GlyphSphereApplication
    {
        private GlyphSphereCore? core;
        private MicroworldInterface? interfaces;

        public static void Launch(int windowWidth = 900, int windowHeight = 900)
        {
            // Create camera with default settings for glyph sphere
            var camera = new Camera();
            Launch(camera, windowWidth, windowHeight);
        }
        
        public static void Launch(Camera camera, int windowWidth = 900, int windowHeight = 900)
        {
            var native = new NativeWindowSettings()
            {
                ClientSize = new Vector2i(windowWidth, windowHeight),
                Title = "Glyph Sphere Prototype",
                Flags = ContextFlags.Default,
                API = ContextAPI.OpenGL,
                APIVersion = new Version(3, 3),
                WindowBorder = WindowBorder.Resizable
            };
            
            using var core = new GlyphSphereCore(GameWindowSettings.Default, native, camera);
            var interfaces = new MicroworldInterface(core);
            
            // Set up event handlers for enhanced mouse interaction
            interfaces.VertexHoverEvent += (index, glyph, color) =>
            {
                // Enhanced hover feedback could go here
            };
            
            interfaces.VertexClickEvent += (index, glyph, color, noise) =>
            {
                // Enhanced click feedback could go here
                var (biome, location, avgNoise) = interfaces.GetDetailedBiomeInfoAt(index);
                Console.WriteLine($"Clicked vertex {index}: biome='{biome.Name}' location='{location?.Name ?? "none"}' noise={avgNoise:F3}");
            };
            
            // Generate world once the core is loaded
            core.CoreLoaded += () =>
            {
                Console.WriteLine("Generating microworld...");
                interfaces.GenerateWorld();
            };

            core.Run();
        }
    }
}