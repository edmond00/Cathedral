using System;
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
        public static void Launch(string imagePath, int maxImageWidth = 0, int maxImageHeight = 0)
        {
            Console.WriteLine("=== Image to ASCII Art Converter ===\n");
            Console.WriteLine($"Converting: {imagePath}");
            Console.WriteLine($"Terminal size: {Config.Terminal.MainWidth}x{Config.Terminal.MainHeight} characters");
            if (maxImageWidth > 0 || maxImageHeight > 0)
                Console.WriteLine($"Max image size: {maxImageWidth}x{maxImageHeight} characters\n");
            else
                Console.WriteLine($"Image will fit terminal size\n");

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

            using var window = new ImageToTextWindow(GameWindowSettings.Default, native, imagePath, maxImageWidth, maxImageHeight);
            window.Run();
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
            private bool _conversionDone = false;
            private string? _outputPath;

            public ImageToTextWindow(GameWindowSettings gameSettings, NativeWindowSettings nativeSettings, 
                string imagePath, int maxImageWidth, int maxImageHeight) 
                : base(gameSettings, nativeSettings)
            {
                _imagePath = imagePath;
                _maxImageWidth = maxImageWidth;
                _maxImageHeight = maxImageHeight;
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
                    converter.ConvertImageToTerminal(_imagePath, _terminal, _maxImageWidth, _maxImageHeight);

                    // Save to logs folder
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    string filename = Path.GetFileNameWithoutExtension(_imagePath);
                    _outputPath = Path.Combine("logs", $"ascii_art_{filename}_{timestamp}.txt");
                    
                    converter.ExportToTextFile(_terminal, _outputPath);
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
