using System;
using System.Globalization;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using OpenTK.Mathematics;

namespace Cathedral.Terminal
{
    /// <summary>
    /// Converts images to ASCII-art-like text representation using layered brightness mapping.
    /// Each brightness layer has its own glyph gradient and color for engraving-style rendering.
    /// </summary>
    public class ImageToTextConverter
    {
        // Layer data for each cell (matches terminal dimensions)
        private int[,]? _layerMap;
        private int _lastWidth;
        private int _lastHeight;
        private string _lastImagePath = "";
        
        /// <summary>
        /// Convert an image to ASCII art and populate a terminal grid using layered rendering.
        /// Image will be centered in the terminal with black background in empty areas.
        /// </summary>
        /// <param name="imagePath">Path to the image file</param>
        /// <param name="terminal">Terminal HUD to populate</param>
        /// <param name="maxWidth">Maximum width for image in characters (0 = use terminal width)</param>
        /// <param name="maxHeight">Maximum height for image in characters (0 = use terminal height)</param>
        /// <param name="useNegative">If true, inverts the image (negative)</param>
        /// <param name="autoContrast">If true, automatically adjusts contrast to spread brightness evenly</param>
        /// <param name="manualContrast">Manual contrast multiplier (1.0 = no change, >1.0 = increase, <1.0 = decrease)</param>
        public void ConvertImageToTerminal(string imagePath, TerminalHUD terminal, int maxWidth = 0, int maxHeight = 0, bool useNegative = false, bool autoContrast = false, float manualContrast = 1.0f, bool stretchToFit = false)
        {
            if (!File.Exists(imagePath))
                throw new FileNotFoundException($"Image file not found: {imagePath}");

            // Use terminal dimensions as max if not specified
            if (maxWidth <= 0) maxWidth = terminal.Width;
            if (maxHeight <= 0) maxHeight = terminal.Height;
            
            // Constrain to terminal dimensions
            maxWidth = Math.Min(maxWidth, terminal.Width);
            maxHeight = Math.Min(maxHeight, terminal.Height);

            using var image = Image.Load<Rgba32>(imagePath);
            
            // Apply image preprocessing
            image.Mutate(ctx =>
            {
                // Apply manual contrast adjustment
                if (manualContrast != 1.0f)
                {
                    ctx.Contrast(manualContrast);
                    Console.WriteLine($"Applied manual contrast: {manualContrast:F2}x");
                }
                
                // Apply auto-contrast (histogram equalization)
                if (autoContrast)
                {
                    ctx.HistogramEqualization();
                    Console.WriteLine("Applied auto-contrast");
                }
                
                // Apply negative (invert colors)
                if (useNegative)
                {
                    ctx.Invert();
                    Console.WriteLine("Applied negative (inverted colors)");
                }
            });
            
            int resizeWidth = maxWidth;
            int resizeHeight = maxHeight;

            if (!stretchToFit)
            {
                // Preserve aspect ratio: fit image within maxWidth x maxHeight
                float imageAspect = (float)image.Width / image.Height;
                float targetAspect = (float)maxWidth / maxHeight;
                float adjustedImageAspect = imageAspect; // character cells treated as 1:1

                if (adjustedImageAspect > targetAspect)
                {
                    // Image is wider - fit to width
                    resizeHeight = (int)(maxWidth / adjustedImageAspect);
                }
                else
                {
                    // Image is taller - fit to height
                    resizeWidth = (int)(maxHeight * adjustedImageAspect);
                }
            }

            // Ensure dimensions are at least 1
            resizeWidth = Math.Max(1, Math.Min(resizeWidth, maxWidth));
            resizeHeight = Math.Max(1, Math.Min(resizeHeight, maxHeight));

            // Resize image to target dimensions
            image.Mutate(x => x.Resize(resizeWidth, resizeHeight, KnownResamplers.Lanczos3));

            // Initialize layer map
            _layerMap = new int[terminal.Width, terminal.Height];
            _lastWidth = terminal.Width;
            _lastHeight = terminal.Height;
            _lastImagePath = imagePath;
            
            // Clear terminal and layer map (fills with black background and layer -1)
            terminal.Clear();
            for (int y = 0; y < terminal.Height; y++)
                for (int x = 0; x < terminal.Width; x++)
                    _layerMap[x, y] = -1;

            // Calculate centering offsets (center image within terminal)
            int offsetX = (terminal.Width - resizeWidth) / 2;
            int offsetY = (terminal.Height - resizeHeight) / 2;

            // Convert each pixel to a character using layer system
            for (int y = 0; y < resizeHeight; y++)
            {
                for (int x = 0; x < resizeWidth; x++)
                {
                    var pixel = image[x, y];
                    float brightness = CalculateBrightness(pixel);
                    
                    // Find appropriate layer for this brightness
                    int layerIndex = FindLayerForBrightness(brightness);
                    
                    if (layerIndex >= 0)
                    {
                        var layer = Config.ImageToText.Layers[layerIndex];
                        char character = BrightnessToCharacter(brightness, layer);
                        
                        int cellX = offsetX + x;
                        int cellY = offsetY + y;
                        
                        terminal.SetCell(cellX, cellY, character, layer.Color, Config.Colors.Black);
                        _layerMap[cellX, cellY] = layerIndex;
                    }
                }
            }

            Console.WriteLine($"Image converted to {resizeWidth}x{resizeHeight} ASCII art using {Config.ImageToText.Layers.Count} layers");
            Console.WriteLine($"Centered in {terminal.Width}x{terminal.Height} terminal");
        }

        /// <summary>
        /// Export the current terminal state to multiple files in a timestamped folder:
        /// - ASCII art text file
        /// - Layer map file (showing layer index for each character)
        /// - Layer colors CSV (mapping layers to their colors)
        /// </summary>
        public void ExportToLayeredFiles(TerminalHUD terminal)
        {
            if (_layerMap == null)
            {
                Console.WriteLine("Warning: No layer data available. Call ConvertImageToTerminal first.");
                return;
            }

            // Create timestamped folder
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string imageName = Path.GetFileNameWithoutExtension(_lastImagePath);
            string folderName = $"{Config.ImageToText.OutputFolderPrefix}{imageName}_{timestamp}";
            string folderPath = Path.Combine(Config.ImageToText.OutputBaseDirectory, folderName);
            
            Directory.CreateDirectory(folderPath);
            
            // Export ASCII art
            string asciiPath = Path.Combine(folderPath, "ascii_art.txt");
            ExportAsciiArt(terminal, asciiPath);
            
            // Export layer map
            string layerMapPath = Path.Combine(folderPath, "layer_map.txt");
            ExportLayerMap(layerMapPath);
            
            // Export layer colors CSV
            string colorsPath = Path.Combine(folderPath, "layer_colors.csv");
            ExportLayerColors(colorsPath);
            
            Console.WriteLine($"\n✓ Exported layered files to: {folderPath}");
            Console.WriteLine($"  - ascii_art.txt: Visual ASCII art");
            Console.WriteLine($"  - layer_map.txt: Layer indices for each character");
            Console.WriteLine($"  - layer_colors.csv: Layer definitions with colors");
        }

        /// <summary>
        /// Export ASCII art to text file (legacy single-file method)
        /// </summary>
        public void ExportToTextFile(TerminalHUD terminal, string outputPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            ExportAsciiArt(terminal, outputPath);
        }

        #region Export Helpers

        private void ExportAsciiArt(TerminalHUD terminal, string outputPath)
        {
            var lines = new string[terminal.Height];
            
            for (int y = 0; y < terminal.Height; y++)
            {
                var chars = new char[terminal.Width];
                for (int x = 0; x < terminal.Width; x++)
                {
                    chars[x] = terminal.View[x, y].Character;
                }
                lines[y] = new string(chars).TrimEnd(); // Remove trailing spaces
            }

            // Remove trailing empty lines
            int lastNonEmpty = lines.Length - 1;
            while (lastNonEmpty >= 0 && string.IsNullOrWhiteSpace(lines[lastNonEmpty]))
                lastNonEmpty--;

            var trimmedLines = lines.Take(lastNonEmpty + 1).ToArray();
            File.WriteAllLines(outputPath, trimmedLines);
        }

        private void ExportLayerMap(string outputPath)
        {
            if (_layerMap == null) return;
            
            var lines = new string[_lastHeight];
            
            for (int y = 0; y < _lastHeight; y++)
            {
                var sb = new StringBuilder();
                for (int x = 0; x < _lastWidth; x++)
                {
                    int layer = _layerMap[x, y];
                    // Use space for empty cells, digit for layer 0-9, letter for 10+
                    char layerChar = layer < 0 ? ' ' : 
                                    layer < 10 ? (char)('0' + layer) :
                                    (char)('A' + (layer - 10));
                    sb.Append(layerChar);
                }
                lines[y] = sb.ToString().TrimEnd();
            }

            // Remove trailing empty lines
            int lastNonEmpty = lines.Length - 1;
            while (lastNonEmpty >= 0 && string.IsNullOrWhiteSpace(lines[lastNonEmpty]))
                lastNonEmpty--;

            var trimmedLines = lines.Take(lastNonEmpty + 1).ToArray();
            File.WriteAllLines(outputPath, trimmedLines);
        }

        private void ExportLayerColors(string outputPath)
        {
            var lines = new List<string>
            {
                "Layer,Name,ColorR,ColorG,ColorB,ColorA"
            };

            for (int i = 0; i < Config.ImageToText.Layers.Count; i++)
            {
                var layer = Config.ImageToText.Layers[i];
                // Use invariant culture to ensure periods as decimal separators
                lines.Add($"{i}," +
                         $"\"{layer.Name}\"," +
                         $"{layer.Color.X.ToString("F3", CultureInfo.InvariantCulture)}," +
                         $"{layer.Color.Y.ToString("F3", CultureInfo.InvariantCulture)}," +
                         $"{layer.Color.Z.ToString("F3", CultureInfo.InvariantCulture)}," +
                         $"{layer.Color.W.ToString("F3", CultureInfo.InvariantCulture)}");
            }

            File.WriteAllLines(outputPath, lines);
        }

        /// <summary>
        /// Escape special characters in CSV field
        /// </summary>
        private string EscapeCsvField(string field)
        {
            return field.Replace("\"", "\"\""); // Escape quotes by doubling them
        }

        #endregion

        #region Layer System

        /// <summary>
        /// Calculate perceived brightness using human eye sensitivity weights
        /// </summary>
        private float CalculateBrightness(Rgba32 pixel)
        {
            return (0.299f * pixel.R + 0.587f * pixel.G + 0.114f * pixel.B) / 255f;
        }

        /// <summary>
        /// Find which layer a given brightness value belongs to
        /// </summary>
        private int FindLayerForBrightness(float brightness)
        {
            for (int i = 0; i < Config.ImageToText.Layers.Count; i++)
            {
                var layer = Config.ImageToText.Layers[i];
                if (brightness >= layer.MinBrightness && brightness <= layer.MaxBrightness)
                {
                    return i;
                }
            }
            
            // Fallback to closest layer if not found (handles edge cases)
            return Config.ImageToText.Layers.Count - 1;
        }

        /// <summary>
        /// Convert brightness to character within a layer's glyph gradient
        /// </summary>
        private char BrightnessToCharacter(float brightness, Config.ImageToText.BrightnessLayer layer)
        {
            if (string.IsNullOrEmpty(layer.GlyphGradient))
                return ' ';
            
            // Normalize brightness within layer's range
            float layerRange = layer.MaxBrightness - layer.MinBrightness;
            if (layerRange <= 0) return layer.GlyphGradient[0];
            
            float normalizedInLayer = (brightness - layer.MinBrightness) / layerRange;
            normalizedInLayer = Math.Clamp(normalizedInLayer, 0f, 1f);
            
            // Map to character index
            int index = (int)(normalizedInLayer * (layer.GlyphGradient.Length - 1));
            index = Math.Clamp(index, 0, layer.GlyphGradient.Length - 1);
            
            return layer.GlyphGradient[index];
        }

        #endregion
    }
}