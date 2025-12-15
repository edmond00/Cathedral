using System;
using OpenTK.Mathematics;
using Cathedral.Terminal.Utils;

namespace Cathedral.Terminal
{
    /// <summary>
    /// Mouse-following popup HUD for displaying context-sensitive information.
    /// Appears at the mouse cursor position and shows small popup info.
    /// </summary>
    public class PopupTerminalHUD : IDisposable
    {
        private readonly TerminalView _view;
        private readonly PopupRenderer _renderer;
        
        private bool _disposed;
        private Vector2 _mousePosition;

        /// <summary>
        /// Creates a new popup terminal HUD
        /// </summary>
        /// <param name="width">Width in characters</param>
        /// <param name="height">Height in characters</param>
        /// <param name="cellSize">Cell size in pixels (should match main terminal)</param>
        /// <param name="atlas">Shared glyph atlas from main terminal</param>
        public PopupTerminalHUD(int width, int height, int cellSize, GlyphAtlas atlas)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Popup terminal dimensions must be positive");
            if (atlas == null)
                throw new ArgumentNullException(nameof(atlas));

            // Initialize components
            _view = new TerminalView(width, height);
            _renderer = new PopupRenderer(_view, atlas, cellSize);
            
            // Initialize with transparent cells
            Clear();
            
            Console.WriteLine($"Popup Terminal: HUD initialized - {width}x{height} grid");
        }

        #region Properties

        /// <summary>
        /// Width of the popup in characters
        /// </summary>
        public int Width => _view.Width;

        /// <summary>
        /// Height of the popup in characters
        /// </summary>
        public int Height => _view.Height;

        /// <summary>
        /// Access to the underlying view for drawing operations
        /// </summary>
        public TerminalView View => _view;

        #endregion

        #region Drawing Operations

        /// <summary>
        /// Clears the popup with transparent cells
        /// </summary>
        public void Clear()
        {
            _view.Fill(' ', Colors.White, Colors.Transparent);
        }

        /// <summary>
        /// Draws text at the specified position
        /// </summary>
        public void DrawText(int x, int y, string text, Vector4? textColor = null, Vector4? backgroundColor = null)
        {
            _view.Text(x, y, text, textColor ?? Colors.White, backgroundColor ?? Colors.Transparent);
        }

        /// <summary>
        /// Draws centered text at the specified y position
        /// </summary>
        public void DrawCenteredText(int y, string text, Vector4? textColor = null, Vector4? backgroundColor = null)
        {
            int x = (_view.Width - text.Length) / 2;
            if (x < 0) x = 0;
            DrawText(x, y, text, textColor, backgroundColor);
        }

        /// <summary>
        /// Draws a box with the specified dimensions
        /// </summary>
        public void DrawBox(int x, int y, int width, int height, Vector4? borderColor = null, Vector4? backgroundColor = null)
        {
            _view.DrawBox(x, y, width, height, BoxStyle.Single, borderColor ?? Colors.White, backgroundColor ?? Colors.Transparent);
        }

        /// <summary>
        /// Fills a region with a character and colors
        /// </summary>
        public void Fill(int x, int y, int width, int height, char character = ' ', Vector4? textColor = null, Vector4? backgroundColor = null)
        {
            _view.FillRect(x, y, width, height, character, textColor ?? Colors.White, backgroundColor ?? Colors.Transparent);
        }

        #endregion

        #region Position Management

        /// <summary>
        /// Updates the mouse position that the popup should follow
        /// </summary>
        public void SetMousePosition(Vector2 position)
        {
            _mousePosition = position;
            _renderer.SetMousePosition(position);
        }

        /// <summary>
        /// Gets the current mouse position being tracked
        /// </summary>
        public Vector2 GetMousePosition()
        {
            return _mousePosition;
        }
        
        /// <summary>
        /// Calculates the visual bounds (bounding box of non-transparent cells)
        /// Returns (minX, minY, maxX, maxY) in cell coordinates, or null if all cells are transparent
        /// </summary>
        private (int minX, int minY, int maxX, int maxY)? CalculateVisualBounds()
        {
            int minX = _view.Width;
            int minY = _view.Height;
            int maxX = -1;
            int maxY = -1;
            bool foundNonTransparent = false;
            
            foreach (var (x, y, cell) in _view.EnumerateCells())
            {
                // Check if cell has non-transparent background or is not a space character
                bool isVisible = cell.BackgroundColor.W > 0.01f || (cell.Character != ' ' && cell.TextColor.W > 0.01f);
                
                if (isVisible)
                {
                    foundNonTransparent = true;
                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
            }
            
            if (!foundNonTransparent)
                return null;
                
            return (minX, minY, maxX, maxY);
        }

        #endregion

        #region Rendering

        /// <summary>
        /// Renders the popup terminal at the current mouse position
        /// </summary>
        public void Render(Vector2i windowSize)
        {
            if (_disposed)
                return;

            // Calculate visual bounds and update renderer
            var visualBounds = CalculateVisualBounds();
            _renderer.SetVisualBounds(visualBounds);

            // Create orthographic projection (top-left origin)
            Matrix4 projection = Matrix4.CreateOrthographicOffCenter(
                0, windowSize.X,
                windowSize.Y, 0,
                -1.0f, 1.0f
            );

            _renderer.Render(projection, windowSize);
        }

        #endregion

        #region Disposal

        public void Dispose()
        {
            if (!_disposed)
            {
                _renderer?.Dispose();
                _disposed = true;
            }
        }

        #endregion
    }
}
