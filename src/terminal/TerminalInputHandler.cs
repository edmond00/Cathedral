using System;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Cathedral.Terminal
{
    /// <summary>
    /// Handles mouse and keyboard input for the terminal.
    /// Maps screen coordinates to terminal grid coordinates and manages input events.
    /// </summary>
    public class TerminalInputHandler
    {
        private readonly TerminalView _view;
        private readonly TerminalRenderer _renderer;
        private Func<float>? _getBorderHeight;
        
        // Mouse state tracking
        private Vector2i? _hoveredCell;
        private Vector2i? _lastHoveredCell;
        private bool _leftMouseDown;
        private bool _rightMouseDown;
        private Vector2 _lastRawMousePosition;
        
        // Events
        public event Action<int, int>? CellClicked;
        public event Action<int, int>? CellRightClicked;
        public event Action<int, int>? CellHovered;
        public event Action? MouseLeft;

        public TerminalInputHandler(TerminalView view, TerminalRenderer renderer)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        }

        #region Properties

        /// <summary>
        /// The currently hovered cell coordinates, or null if no cell is hovered
        /// </summary>
        public Vector2i? HoveredCell => _hoveredCell;

        /// <summary>
        /// Whether the left mouse button is currently down
        /// </summary>
        public bool IsLeftMouseDown => _leftMouseDown;

        /// <summary>
        /// Whether the right mouse button is currently down
        /// </summary>
        public bool IsRightMouseDown => _rightMouseDown;

        /// <summary>
        /// The last raw screen mouse position (in screen pixel coordinates)
        /// </summary>
        public Vector2 LastRawMousePosition => _lastRawMousePosition;

        #endregion

        #region Border Height
        
        /// <summary>
        /// Sets the border height function delegate
        /// </summary>
        public void SetBorderHeightFunction(Func<float> getBorderHeight)
        {
            _getBorderHeight = getBorderHeight;
        }
        
        /// <summary>
        /// Gets the window border height for mouse position correction
        /// </summary>
        private float GetWindowBorderHeight()
        {
            return _getBorderHeight?.Invoke() ?? 0f;
        }
        
        /// <summary>
        /// Gets the last raw mouse position with border height correction applied.
        /// This is the actual corrected screen position used for coordinate conversion.
        /// </summary>
        public Vector2 GetCorrectedMousePosition()
        {
            float borderHeight = GetWindowBorderHeight();
            return new Vector2(_lastRawMousePosition.X, _lastRawMousePosition.Y + borderHeight);
        }
        
        #endregion

        #region Mouse Input

        /// <summary>
        /// Handles mouse movement events
        /// </summary>
        public void HandleMouseMove(Vector2 mousePosition, Vector2i windowSize)
        {
            _lastRawMousePosition = mousePosition;
            
            var cellPos = ScreenToCell(mousePosition, windowSize);
            
            if (cellPos != _hoveredCell)
            {
                _lastHoveredCell = _hoveredCell;
                _hoveredCell = cellPos;
                
                if (_hoveredCell.HasValue)
                {
                    CellHovered?.Invoke(_hoveredCell.Value.X, _hoveredCell.Value.Y);
                }
                else
                {
                    MouseLeft?.Invoke();
                }
            }
        }

        /// <summary>
        /// Handles mouse button down events
        /// </summary>
        public bool HandleMouseDown(Vector2 mousePosition, Vector2i windowSize, MouseButton button)
        {
            _lastRawMousePosition = mousePosition;
            
            var cellPos = ScreenToCell(mousePosition, windowSize);
            
            if (!cellPos.HasValue)
                return false; // Mouse is outside terminal area
            
            switch (button)
            {
                case MouseButton.Left:
                    _leftMouseDown = true;
                    CellClicked?.Invoke(cellPos.Value.X, cellPos.Value.Y);
                    return true;
                    
                case MouseButton.Right:
                    _rightMouseDown = true;
                    CellRightClicked?.Invoke(cellPos.Value.X, cellPos.Value.Y);
                    return true;
                    
                default:
                    return false;
            }
        }

        /// <summary>
        /// Handles mouse button up events
        /// </summary>
        public void HandleMouseUp(MouseButton button)
        {
            switch (button)
            {
                case MouseButton.Left:
                    _leftMouseDown = false;
                    break;
                    
                case MouseButton.Right:
                    _rightMouseDown = false;
                    break;
            }
        }

        #endregion

        #region Coordinate Conversion

        /// <summary>
        /// Converts screen coordinates to terminal cell coordinates
        /// </summary>
        /// <param name="screenPos">Mouse position in screen coordinates (top-left origin)</param>
        /// <param name="windowSize">Current window size</param>
        /// <returns>Cell coordinates (0-based), or null if outside terminal area</returns>
        public Vector2i? ScreenToCell(Vector2 screenPos, Vector2i windowSize)
        {
            // Apply border height correction to mouse position
            float borderHeight = GetWindowBorderHeight();
            Vector2 correctedScreenPos = new Vector2(screenPos.X, screenPos.Y + borderHeight);
            
            // Get terminal layout information
            var (terminalSize, cellSize, offset) = _renderer.GetLayoutInfo(windowSize);
            
            // Convert screen position to terminal-local coordinates
            Vector2 localPos = correctedScreenPos - offset;
            
            // Check if position is within terminal bounds
            if (localPos.X < 0 || localPos.X >= terminalSize.X ||
                localPos.Y < 0 || localPos.Y >= terminalSize.Y)
            {
                return null;
            }
            
            // Convert to cell coordinates
            // Note: Renderer positions cells by their centers, so we need to adjust
            float cellXFloat = (localPos.X + cellSize.X * 0.5f) / cellSize.X;
            float cellYFloat = (localPos.Y + cellSize.Y * 0.5f) / cellSize.Y;
            
            // Use floor for proper cell boundary detection
            int cellX = (int)Math.Floor(cellXFloat);
            int cellY = (int)Math.Floor(cellYFloat);
            
            // Clamp to valid range (defensive programming)
            cellX = Math.Clamp(cellX, 0, _view.Width - 1);
            cellY = Math.Clamp(cellY, 0, _view.Height - 1);
            
            return new Vector2i(cellX, cellY);
        }

        /// <summary>
        /// Converts terminal cell coordinates to screen coordinates (center of cell)
        /// </summary>
        /// <param name="cellX">Cell X coordinate</param>
        /// <param name="cellY">Cell Y coordinate</param>
        /// <param name="windowSize">Current window size</param>
        /// <returns>Screen position at the center of the specified cell</returns>
        public Vector2 CellToScreen(int cellX, int cellY, Vector2i windowSize)
        {
            if (!_view.IsValidCoordinate(cellX, cellY))
                throw new ArgumentOutOfRangeException($"Cell coordinates ({cellX}, {cellY}) are invalid");
            
            var (terminalSize, cellSize, offset) = _renderer.GetLayoutInfo(windowSize);
            
            // Calculate center of cell
            Vector2 cellCenter = new Vector2(
                offset.X + (cellX + 0.5f) * cellSize.X,
                offset.Y + (cellY + 0.5f) * cellSize.Y
            );
            
            return cellCenter;
        }

        /// <summary>
        /// Gets the bounding rectangle of a cell in screen coordinates
        /// </summary>
        public (Vector2 topLeft, Vector2 bottomRight) GetCellBounds(int cellX, int cellY, Vector2i windowSize)
        {
            if (!_view.IsValidCoordinate(cellX, cellY))
                throw new ArgumentOutOfRangeException($"Cell coordinates ({cellX}, {cellY}) are invalid");
            
            var (terminalSize, cellSize, offset) = _renderer.GetLayoutInfo(windowSize);
            
            Vector2 topLeft = new Vector2(
                offset.X + cellX * cellSize.X,
                offset.Y + cellY * cellSize.Y
            );
            
            Vector2 bottomRight = new Vector2(
                offset.X + (cellX + 1) * cellSize.X,
                offset.Y + (cellY + 1) * cellSize.Y
            );
            
            return (topLeft, bottomRight);
        }

        #endregion

        #region Terminal Area Queries

        /// <summary>
        /// Checks if a screen position is within the terminal area
        /// </summary>
        public bool IsPositionInTerminal(Vector2 screenPos, Vector2i windowSize)
        {
            return ScreenToCell(screenPos, windowSize).HasValue;
        }

        /// <summary>
        /// Gets the terminal bounds in screen coordinates
        /// </summary>
        public (Vector2 topLeft, Vector2 bottomRight) GetTerminalBounds(Vector2i windowSize)
        {
            var (terminalSize, cellSize, offset) = _renderer.GetLayoutInfo(windowSize);
            
            return (offset, offset + terminalSize);
        }

        /// <summary>
        /// Gets detailed layout information for debugging or advanced use
        /// </summary>
        public TerminalLayoutInfo GetLayoutInfo(Vector2i windowSize)
        {
            var (terminalSize, cellSize, offset) = _renderer.GetLayoutInfo(windowSize);
            
            return new TerminalLayoutInfo
            {
                WindowSize = windowSize,
                TerminalSize = terminalSize,
                CellSize = cellSize,
                Offset = offset,
                GridSize = new Vector2i(_view.Width, _view.Height)
            };
        }

        #endregion

        #region State Reset

        /// <summary>
        /// Resets all input state (call when terminal loses focus)
        /// </summary>
        public void ResetState()
        {
            _hoveredCell = null;
            _lastHoveredCell = null;
            _leftMouseDown = false;
            _rightMouseDown = false;
        }

        #endregion
    }

    /// <summary>
    /// Contains detailed layout information for the terminal
    /// </summary>
    public struct TerminalLayoutInfo
    {
        public Vector2i WindowSize;
        public Vector2 TerminalSize;
        public Vector2 CellSize;
        public Vector2 Offset;
        public Vector2i GridSize;

        public override string ToString()
        {
            return $"Terminal Layout: Window={WindowSize}, Terminal={TerminalSize}, Cell={CellSize}, Offset={Offset}, Grid={GridSize}";
        }
    }
}