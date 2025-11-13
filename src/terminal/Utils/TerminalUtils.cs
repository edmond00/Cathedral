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

    public static class Colors
    {
        public static readonly Vector4 Black = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
        public static readonly Vector4 White = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        public static readonly Vector4 Red = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
        public static readonly Vector4 Green = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);
        public static readonly Vector4 Blue = new Vector4(0.0f, 0.0f, 1.0f, 1.0f);
        public static readonly Vector4 Yellow = new Vector4(1.0f, 1.0f, 0.0f, 1.0f);
        public static readonly Vector4 Magenta = new Vector4(1.0f, 0.0f, 1.0f, 1.0f);
        public static readonly Vector4 Cyan = new Vector4(0.0f, 1.0f, 1.0f, 1.0f);
        public static readonly Vector4 Gray = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);
        public static readonly Vector4 DarkGray = new Vector4(0.3f, 0.3f, 0.3f, 1.0f);
        public static readonly Vector4 LightGray = new Vector4(0.7f, 0.7f, 0.7f, 1.0f);
        public static readonly Vector4 Transparent = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
        
        // Terminal colors (16-color palette)
        public static readonly Vector4[] Terminal = new Vector4[]
        {
            Black,      // 0: Black
            Red,        // 1: Red
            Green,      // 2: Green
            Yellow,     // 3: Yellow
            Blue,       // 4: Blue
            Magenta,    // 5: Magenta
            Cyan,       // 6: Cyan
            White,      // 7: White
            DarkGray,   // 8: Bright Black (Dark Gray)
            new Vector4(1.0f, 0.5f, 0.5f, 1.0f),  // 9: Bright Red
            new Vector4(0.5f, 1.0f, 0.5f, 1.0f),  // 10: Bright Green
            new Vector4(1.0f, 1.0f, 0.5f, 1.0f),  // 11: Bright Yellow
            new Vector4(0.5f, 0.5f, 1.0f, 1.0f),  // 12: Bright Blue
            new Vector4(1.0f, 0.5f, 1.0f, 1.0f),  // 13: Bright Magenta
            new Vector4(0.5f, 1.0f, 1.0f, 1.0f),  // 14: Bright Cyan
            new Vector4(0.9f, 0.9f, 0.9f, 1.0f),  // 15: Bright White
        };
    }
}