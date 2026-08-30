using System.Runtime.InteropServices;
using OpenTK.Mathematics;

namespace Cathedral.Terminal.Utils
{
    /// <summary>
    /// GPU instance data structure for terminal rendering.
    /// Each instance represents one terminal cell with its position, size, glyph, and colors.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TerminalInstance
    {
        /// <summary>
        /// Screen space position of the cell (in pixels from bottom-left)
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// Size of the cell in pixels (width, height)
        /// </summary>
        public Vector2 Size;

        /// <summary>
        /// UV rectangle for the glyph in the atlas texture (x, y, width, height)
        /// </summary>
        public Vector4 UvRect;

        /// <summary>
        /// Text color with alpha channel
        /// </summary>
        public Vector4 TextColor;

        /// <summary>
        /// Background color with alpha channel
        /// </summary>
        public Vector4 BackgroundColor;

        /// <summary>
        /// Per-glyph multiplier on the glyph quad, on top of the global
        /// <c>Config.Terminal.GlyphScale</c> uniform — 1.0 for every ordinary character, larger
        /// for the few glyphs that are meant to be read as pictures rather than as text.
        /// See <c>Config.GlyphSizeFactors.QuadScales</c>. Affects the glyph pass only; the
        /// background quad is always exactly one cell.
        /// </summary>
        public float GlyphScale;

        public TerminalInstance(Vector3 position, Vector2 size, Vector4 uvRect, Vector4 textColor, Vector4 backgroundColor, float glyphScale)
        {
            Position = position;
            Size = size;
            UvRect = uvRect;
            TextColor = textColor;
            BackgroundColor = backgroundColor;
            GlyphScale = glyphScale;
        }

        /// <summary>
        /// Size of the structure in bytes for GPU buffer allocation
        /// </summary>
        public static int SizeInBytes => Marshal.SizeOf<TerminalInstance>();
    }
}