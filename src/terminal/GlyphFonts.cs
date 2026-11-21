using System;
using System.Collections.Generic;
using System.Globalization;
using SixLabors.Fonts;
using SixLabors.Fonts.Unicode;
using Font = SixLabors.Fonts.Font;

namespace Cathedral.Terminal
{
    /// <summary>
    /// Which of the two fonts a character is rasterised from, for the terminal's atlas and the
    /// world sphere's alike.
    ///
    /// <para><b>The font that has the glyph draws it.</b> <see cref="Config.Terminal.FallbackGlyphs"/>
    /// is a preference, for a character both fonts have and the fallback draws better; it is not
    /// what makes a character drawable. It used to be: a character went to the fallback font if it
    /// was listed and to the main font if it was not, whatever either font held, and a character
    /// sent to the font that lacked it was drawn as that font's "missing" box with no error and no
    /// log line. Four characters were in that state when this was written — the tree root listed
    /// for a fallback font that lacks it, and the journal's source mark and two humor symbols
    /// unlisted while only the fallback font has them.</para>
    ///
    /// <para>A character neither font has still draws as the box, and now says so once, on stderr.
    /// <c>python3 tools/glyph_audit.py</c> finds them before a player does.</para>
    /// </summary>
    internal static class GlyphFonts
    {
        private static readonly HashSet<char> Reported = new();

        /// <summary>The font to rasterise <paramref name="glyph"/> from.</summary>
        public static Font Choose(char glyph, Font main, Font? fallback)
        {
            bool preferred = Config.Terminal.FallbackGlyphs.Contains(glyph);
            if (preferred && fallback != null && Has(fallback, glyph)) return fallback;
            if (Has(main, glyph)) return main;
            if (fallback != null && Has(fallback, glyph)) return fallback;

            lock (Reported)
                if (Reported.Add(glyph))
                    Console.Error.WriteLine($"[glyphs] U+{(int)glyph:X4} '{glyph}' has no glyph in either font: it draws as the "
                                          + "missing box. Replace the character, or see tools/glyph_audit.py.");
            return main;
        }

        /// <summary>
        /// Whether <paramref name="glyph"/> has anything to draw at all. Half of a surrogate pair (an
        /// emoji out of a model's text is two of them) and the invisible format characters (a
        /// zero-width space, a variation selector, a byte-order mark) have no glyph of their own in
        /// any font: their cell is left empty rather than given one or two boxes.
        /// </summary>
        public static bool IsDrawable(char glyph)
            => !char.IsSurrogate(glyph) && char.GetUnicodeCategory(glyph) != UnicodeCategory.Format;

        // What each face can draw, read once from its character map.
        private static readonly Dictionary<string, HashSet<int>> Coverage = new();

        /// <summary>
        /// Whether the face maps the character to a glyph of its own, by its character map.
        /// <c>Font.TryGetGlyphs</c> cannot be asked: it answers yes for a character the face lacks
        /// and hands back the face's "missing" glyph, which is the box this class exists to avoid.
        /// </summary>
        private static bool Has(Font font, char glyph)
        {
            HashSet<int>? covered;
            lock (Coverage)
                if (!Coverage.TryGetValue(font.Family.Name, out covered))
                {
                    covered = new HashSet<int>();
                    foreach (var point in font.FontMetrics.GetAvailableCodePoints()) covered.Add(point.Value);
                    Coverage[font.Family.Name] = covered;
                }
            return covered.Contains(glyph);
        }
    }
}
