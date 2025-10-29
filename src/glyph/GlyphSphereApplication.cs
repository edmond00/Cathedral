// GlyphSphereApplication.cs - Integrated application that combines Core and Interface
using System;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace Cathedral.Glyph
{
    public class GlyphSphereApplication
    {
        private GlyphSphereCore core;
        private GlyphSphereInterface interfaces;

        public static void Launch(int windowWidth = 900, int windowHeight = 900)
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

            using var core = new GlyphSphereCore(GameWindowSettings.Default, native);
            var interfaces = new GlyphSphereInterface(core);
            
            // Set up event handlers for enhanced mouse interaction
            interfaces.VertexHoverEvent += (index, glyph, color) =>
            {
                // Enhanced hover feedback could go here
            };
            
            interfaces.VertexClickEvent += (index, glyph, color, noise) =>
            {
                // Enhanced click feedback could go here
                var (biome, location, avgNoise) = interfaces.GetBiomeInfoAt(index);
                Console.WriteLine($"Clicked vertex {index}: biome='{biome.Name}' location='{location?.Name ?? "none"}' noise={avgNoise:F3}");
            };
            
            // Generate biomes once the core is loaded
            core.CoreLoaded += () =>
            {
                Console.WriteLine("Generating biomes...");
                interfaces.GenerateBiomes();
            };

            core.Run();
        }
    }
}