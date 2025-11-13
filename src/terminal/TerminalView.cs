using System;
using OpenTK.Mathematics;
using Cathedral.Terminal.Utils;

namespace Cathedral.Terminal
{
    /// <summary>
    /// Manages the terminal grid and provides high-level operations for text rendering.
    /// Similar to the original TerminalView but optimized for OpenGL rendering.
    /// </summary>
    public class TerminalView
    {
        private readonly TerminalCell[,] _cells;
        private readonly int _width;
        private readonly int _height;
        private bool _hasChanges;

        public TerminalView(int width, int height)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Terminal dimensions must be positive");

            _width = width;
            _height = height;
            _cells = new TerminalCell[height, width];
            _hasChanges = false;

            // Initialize all cells
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    _cells[y, x] = new TerminalCell();
                }
            }
        }

        #region Properties

        public int Width => _width;
        public int Height => _height;
        public bool HasChanges => _hasChanges;

        /// <summary>
        /// Indexer to access cells by coordinates
        /// </summary>
        public TerminalCell this[int x, int y]
        {
            get
            {
                if (!IsValidCoordinate(x, y))
                    throw new ArgumentOutOfRangeException($"Coordinate ({x},{y}) is out of bounds for {_width}x{_height} terminal");
                return _cells[y, x];
            }
        }

        #endregion

        #region Coordinate Validation

        public bool IsValidCoordinate(int x, int y)
        {
            return x >= 0 && x < _width && y >= 0 && y < _height;
        }

        private void ValidateCoordinate(int x, int y)
        {
            if (!IsValidCoordinate(x, y))
                throw new ArgumentOutOfRangeException($"Coordinate ({x},{y}) is out of bounds for {_width}x{_height} terminal");
        }

        #endregion

        #region Basic Cell Operations

        /// <summary>
        /// Sets a single cell with character and colors
        /// </summary>
        public void SetCell(int x, int y, char character, Vector4 textColor, Vector4 backgroundColor)
        {
            if (!IsValidCoordinate(x, y)) return;
            
            _cells[y, x].Set(character, textColor, backgroundColor);
            _hasChanges = true;
        }

        /// <summary>
        /// Sets a cell character only, keeping current colors
        /// </summary>
        public void SetCharacter(int x, int y, char character)
        {
            if (!IsValidCoordinate(x, y)) return;
            
            _cells[y, x].Character = character;
            _hasChanges = true;
        }

        /// <summary>
        /// Sets cell colors only, keeping current character
        /// </summary>
        public void SetColors(int x, int y, Vector4 textColor, Vector4 backgroundColor)
        {
            if (!IsValidCoordinate(x, y)) return;
            
            _cells[y, x].SetColors(textColor, backgroundColor);
            _hasChanges = true;
        }

        /// <summary>
        /// Gets the character at the specified position
        /// </summary>
        public char GetCharacter(int x, int y)
        {
            if (!IsValidCoordinate(x, y)) return ' ';
            return _cells[y, x].Character;
        }

        #endregion

        #region Fill Operations

        /// <summary>
        /// Fills the entire terminal with the specified character and colors
        /// </summary>
        public void Fill(char character, Vector4 textColor, Vector4 backgroundColor)
        {
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    _cells[y, x].Set(character, textColor, backgroundColor);
                }
            }
            _hasChanges = true;
        }

        /// <summary>
        /// Fills a rectangular region
        /// </summary>
        public void FillRect(int x, int y, int width, int height, char character, Vector4 textColor, Vector4 backgroundColor)
        {
            for (int dy = 0; dy < height; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    int cellX = x + dx;
                    int cellY = y + dy;
                    if (IsValidCoordinate(cellX, cellY))
                    {
                        _cells[cellY, cellX].Set(character, textColor, backgroundColor);
                    }
                }
            }
            _hasChanges = true;
        }

        /// <summary>
        /// Clears the entire terminal (fills with spaces and default colors)
        /// </summary>
        public void Clear()
        {
            Fill(' ', Colors.White, Colors.Black);
        }

        /// <summary>
        /// Clears a rectangular region
        /// </summary>
        public void ClearRect(int x, int y, int width, int height)
        {
            FillRect(x, y, width, height, ' ', Colors.White, Colors.Black);
        }

        #endregion

        #region Text Operations

        /// <summary>
        /// Renders text at the specified position with alignment
        /// </summary>
        public void Text(int x, int y, string text, Vector4 textColor, Vector4 backgroundColor, TextAlignment align = TextAlignment.Left)
        {
            if (string.IsNullOrEmpty(text)) return;

            // Calculate starting position based on alignment
            int startX = x;
            switch (align)
            {
                case TextAlignment.Center:
                    startX = x - text.Length / 2;
                    break;
                case TextAlignment.Right:
                    startX = x - text.Length;
                    break;
            }

            // Render each character
            for (int i = 0; i < text.Length; i++)
            {
                int cellX = startX + i;
                if (IsValidCoordinate(cellX, y))
                {
                    _cells[y, cellX].Set(text[i], textColor, backgroundColor);
                }
            }
            _hasChanges = true;
        }

        /// <summary>
        /// Renders text with default colors
        /// </summary>
        public void Text(int x, int y, string text, TextAlignment align = TextAlignment.Left)
        {
            Text(x, y, text, Colors.White, Colors.Black, align);
        }

        /// <summary>
        /// Renders multiline text
        /// </summary>
        public void TextMultiline(int x, int y, string[] lines, Vector4 textColor, Vector4 backgroundColor, TextAlignment align = TextAlignment.Left)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                Text(x, y + i, lines[i], textColor, backgroundColor, align);
            }
        }

        #endregion

        #region Box Drawing

        /// <summary>
        /// Draws a box outline using box drawing characters
        /// </summary>
        public void DrawBox(int x, int y, int width, int height, BoxStyle style, Vector4 textColor, Vector4 backgroundColor)
        {
            if (width < 2 || height < 2) return;

            char topLeft, topRight, bottomLeft, bottomRight;
            char horizontal, vertical;

            // Select characters based on style
            switch (style)
            {
                case BoxStyle.Single:
                    topLeft = BoxChars.Single.TopLeft;
                    topRight = BoxChars.Single.TopRight;
                    bottomLeft = BoxChars.Single.BottomLeft;
                    bottomRight = BoxChars.Single.BottomRight;
                    horizontal = BoxChars.Single.Horizontal;
                    vertical = BoxChars.Single.Vertical;
                    break;
                case BoxStyle.Double:
                    topLeft = BoxChars.Double.TopLeft;
                    topRight = BoxChars.Double.TopRight;
                    bottomLeft = BoxChars.Double.BottomLeft;
                    bottomRight = BoxChars.Double.BottomRight;
                    horizontal = BoxChars.Double.Horizontal;
                    vertical = BoxChars.Double.Vertical;
                    break;
                case BoxStyle.Thick:
                    topLeft = BoxChars.Thick.TopLeft;
                    topRight = BoxChars.Thick.TopRight;
                    bottomLeft = BoxChars.Thick.BottomLeft;
                    bottomRight = BoxChars.Thick.BottomRight;
                    horizontal = BoxChars.Thick.Horizontal;
                    vertical = BoxChars.Thick.Vertical;
                    break;
                default:
                    return; // No box for None style
            }

            // Draw corners
            SetCell(x, y, topLeft, textColor, backgroundColor);
            SetCell(x + width - 1, y, topRight, textColor, backgroundColor);
            SetCell(x, y + height - 1, bottomLeft, textColor, backgroundColor);
            SetCell(x + width - 1, y + height - 1, bottomRight, textColor, backgroundColor);

            // Draw horizontal lines
            for (int i = 1; i < width - 1; i++)
            {
                SetCell(x + i, y, horizontal, textColor, backgroundColor);
                SetCell(x + i, y + height - 1, horizontal, textColor, backgroundColor);
            }

            // Draw vertical lines
            for (int i = 1; i < height - 1; i++)
            {
                SetCell(x, y + i, vertical, textColor, backgroundColor);
                SetCell(x + width - 1, y + i, vertical, textColor, backgroundColor);
            }
        }

        /// <summary>
        /// Draws a box with default colors
        /// </summary>
        public void DrawBox(int x, int y, int width, int height, BoxStyle style = BoxStyle.Single)
        {
            DrawBox(x, y, width, height, style, Colors.White, Colors.Black);
        }

        #endregion

        #region Progress Bar

        /// <summary>
        /// Draws a progress bar
        /// </summary>
        public void ProgressBar(int x, int y, int width, float percent, Vector4 fillColor, Vector4 emptyColor, Vector4 backgroundColor, char fillChar = '█', char emptyChar = '░')
        {
            if (width <= 0) return;

            percent = Math.Clamp(percent, 0f, 100f);
            int fillWidth = (int)((percent / 100f) * width);

            for (int i = 0; i < width; i++)
            {
                if (i < fillWidth)
                {
                    SetCell(x + i, y, fillChar, fillColor, backgroundColor);
                }
                else
                {
                    SetCell(x + i, y, emptyChar, emptyColor, backgroundColor);
                }
            }
        }

        /// <summary>
        /// Draws a progress bar with default styling
        /// </summary>
        public void ProgressBar(int x, int y, int width, float percent)
        {
            ProgressBar(x, y, width, percent, Colors.Green, Colors.DarkGray, Colors.Black);
        }

        #endregion

        #region Dirty State Management

        /// <summary>
        /// Marks all cells as clean (called after successful render)
        /// </summary>
        internal void MarkAllClean()
        {
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    _cells[y, x].MarkClean();
                }
            }
            _hasChanges = false;
        }

        /// <summary>
        /// Forces all cells to be marked as dirty
        /// </summary>
        public void MarkAllDirty()
        {
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    _cells[y, x].MarkDirty();
                }
            }
            _hasChanges = true;
        }

        /// <summary>
        /// Gets the number of dirty cells
        /// </summary>
        public int GetDirtyCellCount()
        {
            int count = 0;
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    if (_cells[y, x].IsDirty)
                        count++;
                }
            }
            return count;
        }

        #endregion

        #region Enumeration

        /// <summary>
        /// Enumerates all cells with their coordinates
        /// </summary>
        public System.Collections.Generic.IEnumerable<(int x, int y, TerminalCell cell)> EnumerateCells()
        {
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    yield return (x, y, _cells[y, x]);
                }
            }
        }

        /// <summary>
        /// Enumerates only dirty cells with their coordinates
        /// </summary>
        public System.Collections.Generic.IEnumerable<(int x, int y, TerminalCell cell)> EnumerateDirtyCells()
        {
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    var cell = _cells[y, x];
                    if (cell.IsDirty)
                        yield return (x, y, cell);
                }
            }
        }

        #endregion
    }
}