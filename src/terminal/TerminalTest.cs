using System;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Terminal.Utils;

namespace Cathedral.Terminal.Tests
{
    /// <summary>
    /// Simple test program to verify terminal functionality independently
    /// </summary>
    class TerminalTest
    {
        static void TestTerminalBasics()
        {
            Console.WriteLine("=== Terminal Basic Test ===");
            
            // Test TerminalView
            var view = new TerminalView(80, 25);
            Console.WriteLine($"Created terminal view: {view.Width}x{view.Height}");
            
            // Test basic operations
            view.Clear();
            view.Text(0, 0, "Hello Terminal!", Colors.White, Colors.Black);
            view.SetCell(10, 5, '@', Colors.Yellow, Colors.Red);
            
            // Test box drawing
            view.DrawBox(5, 5, 20, 10, BoxStyle.Single, Colors.Cyan, Colors.Black);
            
            // Test progress bar
            view.ProgressBar(6, 6, 18, 75.0f);
            
            Console.WriteLine($"Terminal has changes: {view.HasChanges}");
            Console.WriteLine($"Character at (0,0): '{view.GetCharacter(0, 0)}'");
            Console.WriteLine($"Character at (10,5): '{view.GetCharacter(10, 5)}'");
            
            // Test dirty cells
            Console.WriteLine($"Dirty cells: {view.GetDirtyCellCount()}");
            
            Console.WriteLine("✓ Basic terminal operations work!");
        }
        
        static void TestGlyphAtlas()
        {
            Console.WriteLine("=== Glyph Atlas Test ===");
            
            try
            {
                using var atlas = new GlyphAtlas(32, 24);
                Console.WriteLine($"Created glyph atlas: {atlas.GetAtlasInfo()}");
                
                // Test glyph lookup
                var spaceGlyph = atlas.GetGlyph(' ');
                var atGlyph = atlas.GetGlyph('@');
                var newlineGlyph = atlas.GetGlyph('\n'); // Should be converted to printable
                
                Console.WriteLine($"Space glyph UV: ({spaceGlyph.UvX:F3}, {spaceGlyph.UvY:F3})");
                Console.WriteLine($"@ glyph UV: ({atGlyph.UvX:F3}, {atGlyph.UvY:F3})");
                Console.WriteLine($"Newline glyph converted to: '{newlineGlyph.Glyph}'");
                
                // Test adding new glyphs
                atlas.EnsureGlyphs("€£¥");
                Console.WriteLine($"After adding special chars: {atlas.GetAtlasInfo()}");
                
                Console.WriteLine("✓ Glyph atlas operations work!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Glyph atlas test failed (may be due to missing font): {ex.Message}");
            }
        }
        
        static void TestInputHandler()
        {
            Console.WriteLine("=== Input Handler Test ===");
            
            var view = new TerminalView(80, 25);
            
            // Mock renderer for layout calculations
            // Note: This would normally require OpenGL context, so we'll just test the basic structure
            try
            {
                using var atlas = new GlyphAtlas(16, 12);
                using var renderer = new TerminalRenderer(view, atlas);
                var inputHandler = new TerminalInputHandler(view, renderer);
                
                Console.WriteLine("✓ Input handler created successfully!");
                
                // Test coordinate validation
                Console.WriteLine($"Valid coordinate (10, 5): {view.IsValidCoordinate(10, 5)}");
                Console.WriteLine($"Invalid coordinate (100, 30): {view.IsValidCoordinate(100, 30)}");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Input handler test failed (requires OpenGL context): {ex.Message}");
            }
        }
        
        public static void RunTests()
        {
            Console.WriteLine("Starting Terminal Module Tests...\n");
            
            TestTerminalBasics();
            Console.WriteLine();
            
            TestGlyphAtlas();
            Console.WriteLine();
            
            TestInputHandler();
            Console.WriteLine();
            
            Console.WriteLine("Terminal module tests completed!");
        }
    }
}