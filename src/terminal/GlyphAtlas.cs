using System;
using System.Collections.Generic;
using System.IO;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Cathedral.Terminal
{
    /// <summary>
    /// Information about a glyph in the texture atlas
    /// </summary>
    public struct GlyphInfo
    {
        public char Glyph;
        public float UvX, UvY, UvW, UvH;
        
        public Vector4 UvRect => new Vector4(UvX, UvY, UvW, UvH);
        
        public GlyphInfo(char glyph, float uvX, float uvY, float uvW, float uvH)
        {
            Glyph = glyph;
            UvX = uvX;
            UvY = uvY;
            UvW = uvW;
            UvH = uvH;
        }
    }

    /// <summary>
    /// Manages font texture atlas generation for terminal rendering.
    /// Reuses patterns from GlyphSphereCore but optimized for terminal use.
    /// </summary>
    public class GlyphAtlas : IDisposable
    {
        private readonly Dictionary<char, GlyphInfo> _glyphMap;
        private readonly int _cellSize;
        private readonly int _fontPixelSize;
        private string _currentGlyphSet;
        private int _textureId;
        private Font? _font;
        private bool _disposed;

        // Default terminal glyph set (ASCII printable characters)
        private const string DEFAULT_GLYPH_SET = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";
        
        // Padding in pixels between glyphs to prevent texture bleeding with linear filtering
        private const int GLYPH_PADDING = 2;

        public GlyphAtlas(int cellSize = 64, int fontPixelSize = 48)
        {
            _cellSize = cellSize;
            _fontPixelSize = fontPixelSize;
            _glyphMap = new Dictionary<char, GlyphInfo>();
            _currentGlyphSet = "";
            _textureId = 0;
            
            LoadFont();
            BuildAtlas(DEFAULT_GLYPH_SET);
        }

        #region Properties

        public int TextureId => _textureId;
        public int CellSize => _cellSize;
        public int FontPixelSize => _fontPixelSize;
        public string CurrentGlyphSet => _currentGlyphSet;
        public int GlyphCount => _glyphMap.Count;

        #endregion

        #region Font Management

        private void LoadFont()
        {
            try
            {
                var fontCollection = new FontCollection();
                string fontPath = "assets/fonts/FreeMono.ttf";
                
                if (File.Exists(fontPath))
                {
                    var fontFamily = fontCollection.Add(fontPath);
                    _font = fontFamily.CreateFont(_fontPixelSize, FontStyle.Regular);
                    Console.WriteLine($"Terminal: Loaded font from {fontPath}");
                }
                else
                {
                    throw new FileNotFoundException($"Font not found: {fontPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Terminal: Failed to load custom font, falling back to system font: {ex.Message}");
                try
                {
                    _font = SystemFonts.CreateFont("Consolas", _fontPixelSize, FontStyle.Regular);
                }
                catch
                {
                    _font = SystemFonts.CreateFont("Courier New", _fontPixelSize, FontStyle.Regular);
                }
            }
        }

        #endregion

        #region Atlas Building

        /// <summary>
        /// Builds or rebuilds the texture atlas with the specified glyph set
        /// </summary>
        public void BuildAtlas(string glyphSet)
        {
            if (string.IsNullOrEmpty(glyphSet))
                glyphSet = DEFAULT_GLYPH_SET;

            // Remove duplicates while preserving order
            var uniqueGlyphs = new HashSet<char>();
            var cleanGlyphSet = "";
            foreach (char c in glyphSet)
            {
                if (uniqueGlyphs.Add(c))
                {
                    cleanGlyphSet += c;
                }
            }

            if (cleanGlyphSet == _currentGlyphSet)
                return; // No change needed

            Console.WriteLine($"Terminal: Building atlas with {cleanGlyphSet.Length} glyphs");

            // Dispose old texture
            if (_textureId != 0)
            {
                GL.DeleteTexture(_textureId);
                _textureId = 0;
            }

            // Clear old glyph mapping
            _glyphMap.Clear();

            // Calculate atlas dimensions (add padding between cells)
            int glyphCount = cleanGlyphSet.Length;
            int cols = (int)Math.Ceiling(Math.Sqrt(glyphCount));
            int rows = (int)Math.Ceiling((float)glyphCount / cols);
            int cellWithPadding = _cellSize + GLYPH_PADDING;
            int atlasWidth = cols * cellWithPadding;
            int atlasHeight = rows * cellWithPadding;

            // Create atlas image
            using var atlasImage = new Image<Rgba32>(atlasWidth, atlasHeight, Color.Transparent);

            // Render each glyph
            for (int i = 0; i < glyphCount; i++)
            {
                char glyph = cleanGlyphSet[i];
                int col = i % cols;
                int row = i / cols;
                int x = col * cellWithPadding + GLYPH_PADDING / 2;
                int y = row * cellWithPadding + GLYPH_PADDING / 2;

                // Render glyph to atlas
                RenderGlyphToAtlas(atlasImage, glyph, x, y);

                // Store glyph info with UV inset to avoid sampling padding
                float uvInset = 0.5f; // Half pixel inset in texture space
                var glyphInfo = new GlyphInfo(
                    glyph,
                    (float)(x + uvInset) / atlasWidth,
                    (float)(y + uvInset) / atlasHeight,
                    (float)(_cellSize - uvInset * 2) / atlasWidth,
                    (float)(_cellSize - uvInset * 2) / atlasHeight
                );
                
                _glyphMap[glyph] = glyphInfo;
            }

            // Upload to GPU
            _textureId = CreateTexture(atlasImage);
            _currentGlyphSet = cleanGlyphSet;

            Console.WriteLine($"Terminal: Atlas built successfully - {cols}x{rows} grid, {atlasWidth}x{atlasHeight} pixels");
        }

        private void RenderGlyphToAtlas(Image<Rgba32> atlas, char glyph, int x, int y)
        {
            if (_font == null)
                return;

            // Get glyph-specific size factor from config
            float sizeFactor = Cathedral.Config.GlyphSizeFactors.GetFactor(glyph);
            
            // Create font with adjusted size if needed
            Font fontToUse = _font;
            if (sizeFactor != 1.0f)
            {
                int adjustedSize = (int)(_fontPixelSize * sizeFactor);
                fontToUse = _font.Family.CreateFont(adjustedSize, FontStyle.Regular);
            }

            atlas.Mutate(ctx =>
            {
                var textOptions = new RichTextOptions(fontToUse)
                {
                    Origin = new PointF(x + _cellSize / 2f, y + _cellSize / 2f),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                // Use white color for the glyph (we'll tint it with fragment shader)
                ctx.DrawText(textOptions, glyph.ToString(), Color.White);
            });
        }

        private int CreateTexture(Image<Rgba32> image)
        {
            // Convert image to byte array
            var pixels = new byte[image.Width * image.Height * 4];
            image.CopyPixelDataTo(pixels);

            // Create OpenGL texture
            int textureId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, textureId);

            // Upload texture data
            GL.TexImage2D(
                TextureTarget.Texture2D,
                0,
                PixelInternalFormat.Rgba,
                image.Width,
                image.Height,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                pixels
            );

            // Set texture parameters
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            GL.BindTexture(TextureTarget.Texture2D, 0);

            return textureId;
        }

        #endregion

        #region Glyph Lookup

        /// <summary>
        /// Gets glyph information for the specified character.
        /// If the character is not in the current atlas, it will be added and the atlas rebuilt.
        /// </summary>
        public GlyphInfo GetGlyph(char c)
        {
            // Check if glyph is already in atlas
            if (_glyphMap.TryGetValue(c, out var existingGlyph))
            {
                return existingGlyph;
            }

            // Add new glyph and rebuild atlas
            Console.WriteLine($"Terminal: Adding new glyph '{c}' to atlas");
            string newGlyphSet = _currentGlyphSet + c;
            BuildAtlas(newGlyphSet);

            // Try again (should succeed now)
            if (_glyphMap.TryGetValue(c, out var newGlyph))
            {
                return newGlyph;
            }

            // Fallback to space character if something went wrong
            Console.WriteLine($"Terminal: Warning - failed to add glyph '{c}', using space");
            return _glyphMap.TryGetValue(' ', out var spaceGlyph) ? spaceGlyph : new GlyphInfo();
        }

        /// <summary>
        /// Checks if a character is available in the current atlas
        /// </summary>
        public bool HasGlyph(char c)
        {
            return _glyphMap.ContainsKey(c);
        }

        /// <summary>
        /// Adds a set of characters to the atlas if not already present
        /// </summary>
        public void EnsureGlyphs(string characters)
        {
            var newChars = "";
            foreach (char c in characters)
            {
                if (!_glyphMap.ContainsKey(c))
                {
                    newChars += c;
                }
            }

            if (!string.IsNullOrEmpty(newChars))
            {
                BuildAtlas(_currentGlyphSet + newChars);
            }
        }

        #endregion

        #region Utility

        /// <summary>
        /// Gets all glyphs currently in the atlas
        /// </summary>
        public IEnumerable<char> GetAllGlyphs()
        {
            return _glyphMap.Keys;
        }

        /// <summary>
        /// Gets detailed information about the atlas for debugging
        /// </summary>
        public string GetAtlasInfo()
        {
            if (_textureId == 0)
                return "No atlas loaded";

            GL.BindTexture(TextureTarget.Texture2D, _textureId);
            GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureWidth, out int width);
            GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureHeight, out int height);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            return $"Atlas: {width}x{height}px, {_glyphMap.Count} glyphs, Cell: {_cellSize}px, Font: {_fontPixelSize}px";
        }

        #endregion

        #region Disposal

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_textureId != 0)
                {
                    GL.DeleteTexture(_textureId);
                    _textureId = 0;
                }

                _glyphMap.Clear();
                _font = null;
                _disposed = true;
            }
        }

        #endregion
    }
}