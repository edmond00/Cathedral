using System;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Cathedral.Terminal.Utils;

namespace Cathedral.Terminal
{
    /// <summary>
    /// Main terminal HUD class that coordinates all terminal components.
    /// Provides a high-level interface for terminal operations and handles rendering/input.
    /// </summary>
    public class TerminalHUD : IDisposable
    {
        private readonly TerminalView _view;
        private readonly GlyphAtlas _atlas;
        private readonly TerminalRenderer _renderer;
        private readonly TerminalInputHandler _inputHandler;
        
        private bool _visible = true;
        private float _opacity = 1.0f;
        private bool _disposed;

        // Events for external interaction
        public event Action<int, int>? CellClicked;
        public event Action<int, int>? CellRightClicked;
        public event Action<int, int>? CellHovered;
        public event Action? MouseLeft;

        public TerminalHUD(int width, int height, int cellSize = 32, int fontPixelSize = 24)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Terminal dimensions must be positive");

            // Initialize components
            _view = new TerminalView(width, height);
            _atlas = new GlyphAtlas(cellSize, fontPixelSize);
            _renderer = new TerminalRenderer(_view, _atlas);
            _inputHandler = new TerminalInputHandler(_view, _renderer);
            
            // Wire up events
            _inputHandler.CellClicked += OnCellClicked;
            _inputHandler.CellRightClicked += OnCellRightClicked;
            _inputHandler.CellHovered += OnCellHovered;
            _inputHandler.MouseLeft += OnMouseLeft;
            
            Console.WriteLine($"Terminal: HUD initialized - {width}x{height} grid");
        }

        #region Properties

        /// <summary>
        /// Width of the terminal in characters
        /// </summary>
        public int Width => _view.Width;

        /// <summary>
        /// Height of the terminal in characters
        /// </summary>
        public int Height => _view.Height;

        /// <summary>
        /// Whether the terminal is visible and should be rendered
        /// </summary>
        public bool Visible
        {
            get => _visible;
            set => _visible = value;
        }

        /// <summary>
        /// Overall opacity of the terminal (0.0 = transparent, 1.0 = opaque)
        /// </summary>
        public float Opacity
        {
            get => _opacity;
            set => _opacity = Math.Clamp(value, 0.0f, 1.0f);
        }

        /// <summary>
        /// Access to the underlying view for advanced operations
        /// </summary>
        public TerminalView View => _view;

        /// <summary>
        /// Access to the glyph atlas for debugging or advanced use
        /// </summary>
        public GlyphAtlas Atlas => _atlas;

        /// <summary>
        /// Currently hovered cell coordinates
        /// </summary>
        public Vector2i? HoveredCell => _inputHandler.HoveredCell;

        #endregion

        #region Cell Operations

        /// <summary>
        /// Sets a single cell with character and colors
        /// </summary>
        public void SetCell(int x, int y, char character, Vector4 textColor, Vector4 backgroundColor)
        {
            _view.SetCell(x, y, character, textColor, backgroundColor);
        }

        /// <summary>
        /// Sets a cell with default colors
        /// </summary>
        public void SetCell(int x, int y, char character)
        {
            SetCell(x, y, character, Colors.White, Colors.Black);
        }

        /// <summary>
        /// Gets the character at the specified position
        /// </summary>
        public char GetCharacter(int x, int y)
        {
            return _view.GetCharacter(x, y);
        }

        /// <summary>
        /// Access to individual cells
        /// </summary>
        public TerminalCell this[int x, int y] => _view[x, y];

        #endregion

        #region Text Operations

        /// <summary>
        /// Renders text at the specified position
        /// </summary>
        public void Text(int x, int y, string text, Vector4 textColor, Vector4 backgroundColor, TextAlignment alignment = TextAlignment.Left)
        {
            _view.Text(x, y, text, textColor, backgroundColor, alignment);
        }

        /// <summary>
        /// Renders text with default colors
        /// </summary>
        public void Text(int x, int y, string text, TextAlignment alignment = TextAlignment.Left)
        {
            Text(x, y, text, Colors.White, Colors.Black, alignment);
        }

        /// <summary>
        /// Renders multiline text
        /// </summary>
        public void TextMultiline(int x, int y, string[] lines, Vector4 textColor, Vector4 backgroundColor, TextAlignment alignment = TextAlignment.Left)
        {
            _view.TextMultiline(x, y, lines, textColor, backgroundColor, alignment);
        }

        /// <summary>
        /// Renders multiline text with default colors
        /// </summary>
        public void TextMultiline(int x, int y, string[] lines, TextAlignment alignment = TextAlignment.Left)
        {
            TextMultiline(x, y, lines, Colors.White, Colors.Black, alignment);
        }

        #endregion

        #region Fill Operations

        /// <summary>
        /// Fills the entire terminal with the specified character and colors
        /// </summary>
        public void Fill(char character, Vector4 textColor, Vector4 backgroundColor)
        {
            _view.Fill(character, textColor, backgroundColor);
        }

        /// <summary>
        /// Fills a rectangular region
        /// </summary>
        public void FillRect(int x, int y, int width, int height, char character, Vector4 textColor, Vector4 backgroundColor)
        {
            _view.FillRect(x, y, width, height, character, textColor, backgroundColor);
        }

        /// <summary>
        /// Clears the entire terminal (fills with spaces)
        /// </summary>
        public void Clear()
        {
            _view.Clear();
        }

        /// <summary>
        /// Clears a rectangular region
        /// </summary>
        public void ClearRect(int x, int y, int width, int height)
        {
            _view.ClearRect(x, y, width, height);
        }

        #endregion

        #region Drawing Operations

        /// <summary>
        /// Draws a box outline
        /// </summary>
        public void DrawBox(int x, int y, int width, int height, BoxStyle style, Vector4 textColor, Vector4 backgroundColor)
        {
            _view.DrawBox(x, y, width, height, style, textColor, backgroundColor);
        }

        /// <summary>
        /// Draws a box with default colors
        /// </summary>
        public void DrawBox(int x, int y, int width, int height, BoxStyle style = BoxStyle.Single)
        {
            DrawBox(x, y, width, height, style, Colors.White, Colors.Black);
        }

        /// <summary>
        /// Draws a progress bar
        /// </summary>
        public void ProgressBar(int x, int y, int width, float percent, Vector4 fillColor, Vector4 emptyColor, Vector4 backgroundColor)
        {
            _view.ProgressBar(x, y, width, percent, fillColor, emptyColor, backgroundColor);
        }

        /// <summary>
        /// Draws a progress bar with default styling
        /// </summary>
        public void ProgressBar(int x, int y, int width, float percent)
        {
            _view.ProgressBar(x, y, width, percent);
        }

        #endregion

        #region Convenience Methods

        /// <summary>
        /// Writes text centered in the terminal
        /// </summary>
        public void CenteredText(int y, string text, Vector4 textColor, Vector4 backgroundColor)
        {
            Text(Width / 2, y, text, textColor, backgroundColor, TextAlignment.Center);
        }

        /// <summary>
        /// Writes text centered with default colors
        /// </summary>
        public void CenteredText(int y, string text)
        {
            CenteredText(y, text, Colors.White, Colors.Black);
        }

        /// <summary>
        /// Creates a simple message box
        /// </summary>
        public void MessageBox(string title, string[] content, BoxStyle style = BoxStyle.Single)
        {
            int maxWidth = Math.Max(title.Length + 4, content.Length > 0 ? content.Max(line => line?.Length ?? 0) + 4 : 6);
            int height = content.Length + 4;
            
            int x = (Width - maxWidth) / 2;
            int y = (Height - height) / 2;
            
            // Clear the area
            FillRect(x, y, maxWidth, height, ' ', Colors.White, Colors.Black);
            
            // Draw box
            DrawBox(x, y, maxWidth, height, style);
            
            // Draw title
            if (!string.IsNullOrEmpty(title))
            {
                Text(x + maxWidth / 2, y, $" {title} ", Colors.Yellow, Colors.Black, TextAlignment.Center);
            }
            
            // Draw content
            for (int i = 0; i < content.Length; i++)
            {
                Text(x + 2, y + 2 + i, content[i] ?? "", Colors.White, Colors.Black);
            }
        }

        /// <summary>
        /// Creates a simple input field display
        /// </summary>
        public void InputField(int x, int y, int width, string label, string value, bool focused = false)
        {
            // Draw label
            Text(x, y, label + ":", Colors.White, Colors.Black);
            
            // Draw input box
            Vector4 bgColor = focused ? Colors.DarkGray : Colors.Black;
            Vector4 textColor = focused ? Colors.White : Colors.LightGray;
            
            FillRect(x, y + 1, width, 1, ' ', textColor, bgColor);
            DrawBox(x, y + 1, width, 1, BoxStyle.Single, Colors.Gray, Colors.Black);
            
            // Draw value
            string displayValue = value ?? "";
            if (displayValue.Length > width - 2)
            {
                displayValue = displayValue.Substring(0, width - 2);
            }
            
            Text(x + 1, y + 1, displayValue, textColor, bgColor);
            
            // Draw cursor if focused
            if (focused && displayValue.Length < width - 2)
            {
                SetCell(x + 1 + displayValue.Length, y + 1, '_', Colors.White, bgColor);
            }
        }

        #endregion

        #region Rendering

        /// <summary>
        /// Renders the terminal to the screen
        /// </summary>
        public void Render(Vector2i windowSize)
        {
            if (!_visible || _disposed)
                return;
            
            // Create orthographic projection matrix for HUD rendering
            Matrix4 projection = Matrix4.CreateOrthographic(windowSize.X, windowSize.Y, -1.0f, 1.0f);
            
            _renderer.Render(projection, windowSize);
        }

        /// <summary>
        /// Forces a complete refresh of the rendering
        /// </summary>
        public void ForceRefresh()
        {
            _renderer.ForceRefresh();
        }

        #endregion

        #region Input Handling

        /// <summary>
        /// Handles mouse movement events
        /// </summary>
        public void HandleMouseMove(Vector2 mousePosition, Vector2i windowSize)
        {
            if (!_visible) return;
            _inputHandler.HandleMouseMove(mousePosition, windowSize);
        }

        /// <summary>
        /// Handles mouse button down events
        /// Returns true if the terminal handled the event
        /// </summary>
        public bool HandleMouseDown(Vector2 mousePosition, Vector2i windowSize, MouseButton button)
        {
            if (!_visible) return false;
            return _inputHandler.HandleMouseDown(mousePosition, windowSize, button);
        }

        /// <summary>
        /// Handles mouse button up events
        /// </summary>
        public void HandleMouseUp(MouseButton button)
        {
            if (!_visible) return;
            _inputHandler.HandleMouseUp(button);
        }

        /// <summary>
        /// Checks if a screen position is within the terminal area
        /// </summary>
        public bool IsPositionInTerminal(Vector2 screenPosition, Vector2i windowSize)
        {
            return _visible && _inputHandler.IsPositionInTerminal(screenPosition, windowSize);
        }

        #endregion

        #region Event Handlers

        private void OnCellClicked(int x, int y)
        {
            CellClicked?.Invoke(x, y);
        }

        private void OnCellRightClicked(int x, int y)
        {
            CellRightClicked?.Invoke(x, y);
        }

        private void OnCellHovered(int x, int y)
        {
            CellHovered?.Invoke(x, y);
        }

        private void OnMouseLeft()
        {
            MouseLeft?.Invoke();
        }

        #endregion

        #region Debug and Utility

        /// <summary>
        /// Gets layout information for debugging
        /// </summary>
        public TerminalLayoutInfo GetLayoutInfo(Vector2i windowSize)
        {
            return _inputHandler.GetLayoutInfo(windowSize);
        }

        /// <summary>
        /// Gets atlas information for debugging
        /// </summary>
        public string GetAtlasInfo()
        {
            return _atlas.GetAtlasInfo();
        }

        /// <summary>
        /// Gets detailed terminal state information
        /// </summary>
        public string GetDebugInfo(Vector2i windowSize)
        {
            var layout = GetLayoutInfo(windowSize);
            var atlas = GetAtlasInfo();
            var dirtyCells = _view.GetDirtyCellCount();
            
            return $"Terminal Debug Info:\n" +
                   $"Grid: {Width}x{Height}\n" +
                   $"Visible: {_visible}, Opacity: {_opacity:F2}\n" +
                   $"Dirty Cells: {dirtyCells}\n" +
                   $"Hovered: {HoveredCell}\n" +
                   $"{layout}\n" +
                   $"{atlas}";
        }

        #endregion

        #region Disposal

        public void Dispose()
        {
            if (!_disposed)
            {
                // Unwire events
                _inputHandler.CellClicked -= OnCellClicked;
                _inputHandler.CellRightClicked -= OnCellRightClicked;
                _inputHandler.CellHovered -= OnCellHovered;
                _inputHandler.MouseLeft -= OnMouseLeft;
                
                // Dispose components
                _renderer?.Dispose();
                _atlas?.Dispose();
                
                _disposed = true;
                Console.WriteLine("Terminal: HUD disposed");
            }
        }

        #endregion
    }

    // Extension methods for common operations
    public static class TerminalExtensions
    {
        /// <summary>
        /// Extension to find the maximum length in a string array
        /// </summary>
        public static int Max(this string[] strings, Func<string, int> selector)
        {
            int max = 0;
            foreach (var str in strings)
            {
                int value = selector(str);
                if (value > max) max = value;
            }
            return max;
        }
    }
}