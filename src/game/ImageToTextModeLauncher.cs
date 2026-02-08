using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL;
using Cathedral.Terminal;

namespace Cathedral.Game
{
    /// <summary>
    /// Minimal launcher for image-to-text conversion mode.
    /// Only initializes terminal rendering without 3D sphere or game logic.
    /// Uses fixed terminal size from Config.Terminal (100x30).
    /// </summary>
    public class ImageToTextModeLauncher
    {
        public static void Launch(string imagePath, int maxImageWidth = 0, int maxImageHeight = 0, bool useNegative = false, bool autoContrast = false, float manualContrast = 1.0f)
        {
            Console.WriteLine("=== Image to ASCII Art Converter ===\n");
            Console.WriteLine($"Converting: {imagePath}");
            Console.WriteLine($"Terminal size: {Config.Terminal.MainWidth}x{Config.Terminal.MainHeight} characters");
            if (maxImageWidth > 0 || maxImageHeight > 0)
                Console.WriteLine($"Max image size: {maxImageWidth}x{maxImageHeight} characters");
            else
                Console.WriteLine($"Image will fit terminal size");
            if (useNegative)
                Console.WriteLine($"Using negative (inverted) image");
            if (autoContrast)
                Console.WriteLine($"Auto-adjusting contrast");
            if (manualContrast != 1.0f)
                Console.WriteLine($"Manual contrast adjustment: {manualContrast:F2}x");
            Console.WriteLine();

            var native = new NativeWindowSettings()
            {
                ClientSize = new Vector2i(Config.Terminal.MainWidth * Config.Terminal.MainCellSize, 
                                         Config.Terminal.MainHeight * Config.Terminal.MainCellSize),
                Title = "Cathedral - Image to ASCII Converter",
                Flags = ContextFlags.Default,
                API = ContextAPI.OpenGL,
                APIVersion = new Version(3, 3),
                WindowBorder = WindowBorder.Resizable
            };

            using var window = new ImageToTextWindow(GameWindowSettings.Default, native, imagePath, maxImageWidth, maxImageHeight, useNegative, autoContrast, manualContrast);
            window.Run();
        }

        /// <summary>
        /// Launch draw mode to display a previously saved layered ASCII art.
        /// </summary>
        public static void LaunchDrawMode(string folderPath)
        {
            Console.WriteLine("=== ASCII Art Viewer ===\n");
            Console.WriteLine($"Loading from: {folderPath}");

            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine($"❌ Error: Folder not found: {folderPath}");
                return;
            }

            // Check for required files
            string asciiPath = Path.Combine(folderPath, "ascii_art.txt");
            string layerMapPath = Path.Combine(folderPath, "layer_map.txt");
            string colorsPath = Path.Combine(folderPath, "layer_colors.csv");

            if (!File.Exists(asciiPath) || !File.Exists(layerMapPath) || !File.Exists(colorsPath))
            {
                Console.WriteLine("❌ Error: Missing required files (ascii_art.txt, layer_map.txt, layer_colors.csv)");
                return;
            }

            Console.WriteLine($"Terminal size: {Config.Terminal.MainWidth}x{Config.Terminal.MainHeight} characters\n");

            var native = new NativeWindowSettings()
            {
                ClientSize = new Vector2i(Config.Terminal.MainWidth * Config.Terminal.MainCellSize,
                                         Config.Terminal.MainHeight * Config.Terminal.MainCellSize),
                Title = "Cathedral - ASCII Art Viewer",
                Flags = ContextFlags.Default,
                API = ContextAPI.OpenGL,
                APIVersion = new Version(3, 3),
                WindowBorder = WindowBorder.Resizable
            };

            using var window = new DrawModeWindow(GameWindowSettings.Default, native, asciiPath, layerMapPath, colorsPath);
            window.Run();
        }

        /// <summary>
        /// Window for displaying previously saved layered ASCII art
        /// </summary>
        private class DrawModeWindow : GameWindow
        {
            private TerminalHUD? _terminal;
            private readonly string _asciiPath;
            private readonly string _layerMapPath;
            private readonly string _colorsPath;

            public DrawModeWindow(GameWindowSettings gameSettings, NativeWindowSettings nativeSettings,
                string asciiPath, string layerMapPath, string colorsPath)
                : base(gameSettings, nativeSettings)
            {
                _asciiPath = asciiPath;
                _layerMapPath = layerMapPath;
                _colorsPath = colorsPath;
            }

            protected override void OnLoad()
            {
                base.OnLoad();

                GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

                // Initialize terminal with fixed size from Config
                _terminal = new TerminalHUD(
                    Config.Terminal.MainWidth,
                    Config.Terminal.MainHeight,
                    Config.Terminal.MainCellSize,
                    Config.Terminal.MainFontSize);
                Console.WriteLine("Terminal renderer initialized");

                // Load and display saved ASCII art
                try
                {
                    LoadLayeredAsciiArt();
                    Console.WriteLine("\n✓ ASCII art loaded successfully!");
                    Console.WriteLine("Press ESC to close.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n❌ Failed to load ASCII art: {ex.Message}");
                    _terminal.Clear();
                    _terminal.Text(2, 2, "ERROR: Failed to load ASCII art", Config.Colors.Red, Config.Colors.Black);
                    _terminal.Text(2, 3, ex.Message, Config.Colors.Red, Config.Colors.Black);
                    _terminal.Text(2, 5, "Press ESC to exit", Config.Colors.Yellow, Config.Colors.Black);
                }
            }

            private void LoadLayeredAsciiArt()
            {
                if (_terminal == null) return;

                // Parse layer colors CSV
                var layerColors = new Dictionary<int, Vector4>();
                var colorLines = File.ReadAllLines(_colorsPath);
                for (int i = 1; i < colorLines.Length; i++) // Skip header
                {
                    var parts = ParseCsvLine(colorLines[i]);
                    if (parts.Count >= 6)
                    {
                        int layerIndex = int.Parse(parts[0], CultureInfo.InvariantCulture);
                        float r = float.Parse(parts[2], CultureInfo.InvariantCulture);
                        float g = float.Parse(parts[3], CultureInfo.InvariantCulture);
                        float b = float.Parse(parts[4], CultureInfo.InvariantCulture);
                        float a = float.Parse(parts[5], CultureInfo.InvariantCulture);
                        layerColors[layerIndex] = new Vector4(r, g, b, a);
                    }
                }

                Console.WriteLine($"Loaded {layerColors.Count} layer color definitions");

                // Read ASCII art and layer map
                var asciiLines = File.ReadAllLines(_asciiPath);
                var layerMapLines = File.ReadAllLines(_layerMapPath);

                // Clear terminal
                _terminal.Clear();

                // Render each character with its layer color
                for (int y = 0; y < asciiLines.Length && y < _terminal.Height; y++)
                {
                    string asciiLine = asciiLines[y];
                    string layerLine = y < layerMapLines.Length ? layerMapLines[y] : "";

                    for (int x = 0; x < asciiLine.Length && x < _terminal.Width; x++)
                    {
                        char character = asciiLine[x];
                        
                        // Parse layer index from layer map
                        int layerIndex = -1;
                        if (x < layerLine.Length)
                        {
                            char layerChar = layerLine[x];
                            if (layerChar >= '0' && layerChar <= '9')
                                layerIndex = layerChar - '0';
                            else if (layerChar >= 'A' && layerChar <= 'Z')
                                layerIndex = 10 + (layerChar - 'A');
                        }

                        // Get color for this layer
                        Vector4 color = layerColors.TryGetValue(layerIndex, out var c) ? c : Config.Colors.White;

                        _terminal.SetCell(x, y, character, color, Config.Colors.Black);
                    }
                }

                Console.WriteLine($"Rendered {asciiLines.Length} lines of ASCII art");
            }

            /// <summary>
            /// Parse a CSV line respecting quoted fields
            /// </summary>
            private List<string> ParseCsvLine(string line)
            {
                var fields = new List<string>();
                var currentField = new System.Text.StringBuilder();
                bool inQuotes = false;

                for (int i = 0; i < line.Length; i++)
                {
                    char c = line[i];

                    if (c == '"')
                    {
                        inQuotes = !inQuotes;
                    }
                    else if (c == ',' && !inQuotes)
                    {
                        fields.Add(currentField.ToString());
                        currentField.Clear();
                    }
                    else
                    {
                        currentField.Append(c);
                    }
                }

                fields.Add(currentField.ToString());
                return fields;
            }

            protected override void OnRenderFrame(FrameEventArgs args)
            {
                base.OnRenderFrame(args);

                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                // Render terminal
                if (_terminal != null)
                {
                    var windowSize = new Vector2i(ClientSize.X, ClientSize.Y);
                    _terminal.Render(windowSize);
                }

                SwapBuffers();
            }

            protected override void OnUpdateFrame(FrameEventArgs args)
            {
                base.OnUpdateFrame(args);

                // Handle ESC key to close
                if (KeyboardState.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Escape))
                {
                    Close();
                }
            }

            protected override void OnResize(ResizeEventArgs e)
            {
                base.OnResize(e);
                GL.Viewport(0, 0, e.Width, e.Height);

                // Force terminal to recalculate its layout immediately
                if (_terminal != null)
                {
                    _terminal.ForceRefresh();
                }
            }

            protected override void OnUnload()
            {
                _terminal?.Dispose();
                base.OnUnload();
            }
        }

        /// <summary>
        /// Minimal window for displaying converted image.
        /// Terminal is fixed size from Config.Terminal.
        /// </summary>
        private class ImageToTextWindow : GameWindow
        {
            private TerminalHUD? _terminal;
            private readonly string _imagePath;
            private readonly int _maxImageWidth;
            private readonly int _maxImageHeight;
            private readonly bool _useNegative;
            private readonly bool _autoContrast;
            private readonly float _manualContrast;
            private bool _conversionDone = false;
            private string? _outputPath;

            public ImageToTextWindow(GameWindowSettings gameSettings, NativeWindowSettings nativeSettings, 
                string imagePath, int maxImageWidth, int maxImageHeight, bool useNegative, bool autoContrast, float manualContrast) 
                : base(gameSettings, nativeSettings)
            {
                _imagePath = imagePath;
                _maxImageWidth = maxImageWidth;
                _maxImageHeight = maxImageHeight;
                _useNegative = useNegative;
                _autoContrast = autoContrast;
                _manualContrast = manualContrast;
            }

            protected override void OnLoad()
            {
                base.OnLoad();

                GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

                // Initialize terminal with fixed size from Config
                _terminal = new TerminalHUD(
                    Config.Terminal.MainWidth, 
                    Config.Terminal.MainHeight, 
                    Config.Terminal.MainCellSize, 
                    Config.Terminal.MainFontSize);
                Console.WriteLine("Terminal renderer initialized");

                // Perform conversion
                try
                {
                    var converter = new ImageToTextConverter();
                    converter.ConvertImageToTerminal(_imagePath, _terminal, _maxImageWidth, _maxImageHeight, _useNegative, _autoContrast, _manualContrast);

                    // Export to layered files (ASCII art + layer map + CSV)
                    converter.ExportToLayeredFiles(_terminal);
                    _conversionDone = true;

                    Console.WriteLine("\n✓ Conversion complete!");
                    Console.WriteLine("Press ESC to close, or keep window open to view result.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n❌ Conversion failed: {ex.Message}");
                    _terminal.Clear();
                    _terminal.Text(2, 2, "ERROR: Failed to convert image", Config.Colors.Red, Config.Colors.Black);
                    _terminal.Text(2, 3, ex.Message, Config.Colors.Red, Config.Colors.Black);
                    _terminal.Text(2, 5, "Press ESC to exit", Config.Colors.Yellow, Config.Colors.Black);
                }
            }

            protected override void OnRenderFrame(FrameEventArgs args)
            {
                base.OnRenderFrame(args);

                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                // Render terminal
                if (_terminal != null)
                {
                    var windowSize = new Vector2i(ClientSize.X, ClientSize.Y);
                    _terminal.Render(windowSize);
                }

                SwapBuffers();
            }

            protected override void OnUpdateFrame(FrameEventArgs args)
            {
                base.OnUpdateFrame(args);

                // Handle ESC key to close
                if (KeyboardState.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Escape))
                {
                    Close();
                }
            }

            protected override void OnResize(ResizeEventArgs e)
            {
                base.OnResize(e);
                GL.Viewport(0, 0, e.Width, e.Height);
                
                // Force terminal to recalculate its layout immediately
                if (_terminal != null)
                {
                    _terminal.ForceRefresh();
                }
            }

            protected override void OnUnload()
            {
                _terminal?.Dispose();
                base.OnUnload();
            }
        }
    }
}
