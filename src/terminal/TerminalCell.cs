using OpenTK.Mathematics;

namespace Cathedral.Terminal
{
    /// <summary>
    /// Represents a single cell in the terminal grid.
    /// Tracks character, colors, and dirty state for efficient rendering.
    /// </summary>
    public class TerminalCell
    {
        private char _character;
        private Vector4 _textColor;
        private Vector4 _backgroundColor;
        private bool _isDirty;

        // Previous values for change detection
        private char _lastCharacter;
        private Vector4 _lastTextColor;
        private Vector4 _lastBackgroundColor;

        public TerminalCell()
        {
            _character = ' ';
            _textColor = Utils.Colors.White;
            _backgroundColor = Utils.Colors.Black;
            _isDirty = true;

            _lastCharacter = '\0'; // Force initial update
            _lastTextColor = Vector4.Zero;
            _lastBackgroundColor = Vector4.Zero;
        }

        /// <summary>
        /// The character displayed in this cell
        /// </summary>
        public char Character
        {
            get => _character;
            set
            {
                if (_character != value)
                {
                    _character = value;
                    _isDirty = true;
                }
            }
        }

        /// <summary>
        /// The foreground (text) color
        /// </summary>
        public Vector4 TextColor
        {
            get => _textColor;
            set
            {
                if (_textColor != value)
                {
                    _textColor = value;
                    _isDirty = true;
                }
            }
        }

        /// <summary>
        /// The background color
        /// </summary>
        public Vector4 BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                if (_backgroundColor != value)
                {
                    _backgroundColor = value;
                    _isDirty = true;
                }
            }
        }

        /// <summary>
        /// Whether this cell has changed since the last render
        /// </summary>
        public bool IsDirty
        {
            get => _isDirty;
            set => _isDirty = value;
        }

        /// <summary>
        /// Sets the cell character and colors in one operation
        /// </summary>
        public void Set(char character, Vector4 textColor, Vector4 backgroundColor)
        {
            bool changed = false;
            
            if (_character != character)
            {
                _character = character;
                changed = true;
            }
            
            if (_textColor != textColor)
            {
                _textColor = textColor;
                changed = true;
            }
            
            if (_backgroundColor != backgroundColor)
            {
                _backgroundColor = backgroundColor;
                changed = true;
            }
            
            if (changed)
            {
                _isDirty = true;
            }
        }

        /// <summary>
        /// Sets only the character, keeping current colors
        /// </summary>
        public void SetCharacter(char character)
        {
            Character = character;
        }

        /// <summary>
        /// Sets both foreground and background colors
        /// </summary>
        public void SetColors(Vector4 textColor, Vector4 backgroundColor)
        {
            bool changed = false;
            
            if (_textColor != textColor)
            {
                _textColor = textColor;
                changed = true;
            }
            
            if (_backgroundColor != backgroundColor)
            {
                _backgroundColor = backgroundColor;
                changed = true;
            }
            
            if (changed)
            {
                _isDirty = true;
            }
        }

        /// <summary>
        /// Clears the cell to default state (space character, default colors)
        /// </summary>
        public void Clear()
        {
            Set(' ', Utils.Colors.White, Utils.Colors.Black);
        }

        /// <summary>
        /// Marks the cell as clean (up-to-date with GPU state)
        /// </summary>
        internal void MarkClean()
        {
            _isDirty = false;
            _lastCharacter = _character;
            _lastTextColor = _textColor;
            _lastBackgroundColor = _backgroundColor;
        }

        /// <summary>
        /// Forces the cell to be marked as dirty
        /// </summary>
        public void MarkDirty()
        {
            _isDirty = true;
        }

        /// <summary>
        /// Checks if the cell content has actually changed since last clean state
        /// </summary>
        internal bool HasChanged()
        {
            return _character != _lastCharacter ||
                   _textColor != _lastTextColor ||
                   _backgroundColor != _lastBackgroundColor;
        }

        public override string ToString()
        {
            return $"TerminalCell('{_character}', Text:{_textColor}, BG:{_backgroundColor}, Dirty:{_isDirty})";
        }
    }
}