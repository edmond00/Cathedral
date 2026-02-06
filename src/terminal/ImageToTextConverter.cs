using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using OpenTK.Mathematics;

namespace Cathedral.Terminal
{
    /// <summary>
    /// Converts images to ASCII-art-like text representation using brightness mapping.
    /// </summary>
    public class ImageToTextConverter
    {
        // Character density gradient from darkest to brightest
        // Easy to modify - replace this string to change the character mapping
        private string _densityGradient = " .:-=+*#%@";
        
        /// <summary>
        /// Set custom character density gradient for conversion.
        /// Characters should be ordered from darkest (space) to brightest.
        /// </summary>
        public void SetDensityGradient(string gradient)
        {
            if (string.IsNullOrEmpty(gradient))
                throw new ArgumentException("Gradient cannot be empty");
            _densityGradient = gradient;
        }

        /// <summary>
        /// Convert an image to ASCII art and populate a terminal grid.
        /// Image will be centered in the terminal with black background in empty areas.
        /// </summary>
        /// <param name="imagePath">Path to the image file</param>
        /// <param name="terminal">Terminal HUD to populate</param>
        /// <param name="maxWidth">Maximum width for image in characters (0 = use terminal width)</param>
        /// <param name="maxHeight">Maximum height for image in characters (0 = use terminal height)</param>
        public void ConvertImageToTerminal(string imagePath, TerminalHUD terminal, int maxWidth = 0, int maxHeight = 0)
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
            
            // Calculate aspect ratio preserving dimensions
            float imageAspect = (float)image.Width / image.Height;
            float targetAspect = (float)maxWidth / maxHeight;
            
            int resizeWidth = maxWidth;
            int resizeHeight = maxHeight;
            
            // Character cells are typically taller than wide (adjust for terminal font aspect)
            float charAspect = 1.8f; // Typical character cell is ~1.8x taller than wide
            float adjustedImageAspect = imageAspect * charAspect;
            
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

            // Ensure dimensions are at least 1
            resizeWidth = Math.Max(1, Math.Min(resizeWidth, maxWidth));
            resizeHeight = Math.Max(1, Math.Min(resizeHeight, maxHeight));

            // Resize image to target dimensions
            image.Mutate(x => x.Resize(resizeWidth, resizeHeight, KnownResamplers.Lanczos3));

            // Clear terminal (fills with black background)
            terminal.Clear();

            // Calculate centering offsets (center image within terminal)
            int offsetX = (terminal.Width - resizeWidth) / 2;
            int offsetY = (terminal.Height - resizeHeight) / 2;

            // Convert each pixel to a character
            for (int y = 0; y < resizeHeight; y++)
            {
                for (int x = 0; x < resizeWidth; x++)
                {
                    var pixel = image[x, y];
                    char character = PixelToCharacter(pixel);
                    var color = PixelToColor(pixel);
                    
                    terminal.SetCell(offsetX + x, offsetY + y, character, color, Config.Colors.Black);
                }
            }

            Console.WriteLine($"Image converted to {resizeWidth}x{resizeHeight} ASCII art (centered in {terminal.Width}x{terminal.Height} terminal)");
        }

        /// <summary>
        /// Export the current terminal state to a plain text file.
        /// </summary>
        public void ExportToTextFile(TerminalHUD terminal, string outputPath)
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

            // Trim leading whitespace from all lines
            var trimmedLines = lines.Take(lastNonEmpty + 1).Select(line => line.TrimStart()).ToArray();

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.WriteAllLines(outputPath, trimmedLines);
            
            Console.WriteLine($"Saved ASCII art to: {outputPath}");
        }

        /// <summary>
        /// Convert a pixel to a character based on brightness.
        /// This method is designed to be easily customizable.
        /// </summary>
        private char PixelToCharacter(Rgba32 pixel)
        {
            // Calculate perceived brightness (weighted by human eye sensitivity)
            float brightness = (0.299f * pixel.R + 0.587f * pixel.G + 0.114f * pixel.B) / 255f;
            
            // Map brightness to character index
            int index = (int)(brightness * (_densityGradient.Length - 1));
            index = Math.Clamp(index, 0, _densityGradient.Length - 1);
            
            return _densityGradient[index];
        }

        /// <summary>
        /// Convert a pixel to a terminal color (approximated to available palette).
        /// Can be enhanced later for color preservation.
        /// </summary>
        private Vector4 PixelToColor(Rgba32 pixel)
        {
            // For now, use grayscale based on brightness
            float brightness = (0.299f * pixel.R + 0.587f * pixel.G + 0.114f * pixel.B) / 255f;
            
            // Map to white/gray scale
            if (brightness > 0.8f) return Config.Colors.White;
            if (brightness > 0.6f) return Config.Colors.LightGray;
            if (brightness > 0.4f) return Config.Colors.Gray;
            if (brightness > 0.2f) return Config.Colors.DarkGray;
            return Config.Colors.DarkGray;
        }
    }
}
