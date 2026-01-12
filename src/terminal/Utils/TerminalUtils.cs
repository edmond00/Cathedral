using OpenTK.Mathematics;

namespace Cathedral.Terminal.Utils
{
    public enum TextAlignment
    {
        Left,
        Center,
        Right
    }

    public enum BoxStyle
    {
        None,
        Single,
        Double,
        Rounded,
        Thick
    }

    public static class BoxChars
    {
        public static class Single
        {
            public const char TopLeft = '┌';
            public const char TopRight = '┐';
            public const char BottomLeft = '└';
            public const char BottomRight = '┘';
            public const char Horizontal = '─';
            public const char Vertical = '│';
            public const char Cross = '┼';
            public const char TopTee = '┬';
            public const char BottomTee = '┴';
            public const char LeftTee = '├';
            public const char RightTee = '┤';
        }

        public static class Double
        {
            public const char TopLeft = '╔';
            public const char TopRight = '╗';
            public const char BottomLeft = '╚';
            public const char BottomRight = '╝';
            public const char Horizontal = '═';
            public const char Vertical = '║';
            public const char Cross = '╬';
            public const char TopTee = '╦';
            public const char BottomTee = '╩';
            public const char LeftTee = '╠';
            public const char RightTee = '╣';
        }

        public static class Thick
        {
            public const char TopLeft = '┏';
            public const char TopRight = '┓';
            public const char BottomLeft = '┗';
            public const char BottomRight = '┛';
            public const char Horizontal = '━';
            public const char Vertical = '┃';
            public const char Cross = '╋';
            public const char TopTee = '┳';
            public const char BottomTee = '┻';
            public const char LeftTee = '┣';
            public const char RightTee = '┫';
        }
    }

    /// <summary>
    /// Color constants referencing the centralized Config class.
    /// Maintained for backward compatibility with existing code.
    /// </summary>
    public static class Colors
    {
        public static readonly Vector4 Black = Config.Colors.Black;
        public static readonly Vector4 White = Config.Colors.White;
        public static readonly Vector4 Red = Config.Colors.Red;
        public static readonly Vector4 Green = Config.Colors.Green;
        public static readonly Vector4 Blue = Config.Colors.Blue;
        public static readonly Vector4 Yellow = Config.Colors.Yellow;
        public static readonly Vector4 Magenta = Config.Colors.Magenta;
        public static readonly Vector4 Cyan = Config.Colors.Cyan;
        public static readonly Vector4 Gray = Config.Colors.Gray;
        public static readonly Vector4 DarkGray = Config.Colors.DarkGray;
        public static readonly Vector4 LightGray = Config.Colors.LightGray;
        public static readonly Vector4 Transparent = Config.Colors.Transparent;
        
        // Terminal colors (16-color palette)
        public static readonly Vector4[] Terminal = Config.Colors.Terminal;
    }
}