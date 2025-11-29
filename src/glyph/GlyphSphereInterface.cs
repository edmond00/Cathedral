// IGlyphSphereInterface.cs - Abstract base class for glyph sphere interfaces
// Defines the contract that any interface implementation must follow
using System;
using OpenTK.Mathematics;

using Vector3 = OpenTK.Mathematics.Vector3;
using Vector4 = OpenTK.Mathematics.Vector4;

namespace Cathedral.Glyph
{
    /// <summary>
    /// Abstract base class for glyph sphere interfaces that provides common functionality
    /// and defines the contract for world generation implementations.
    /// </summary>
    public abstract class GlyphSphereInterface
    {
        protected GlyphSphereCore core;
        
        // Events for user interaction
        public event Action<int, char, Vector4>? VertexHoverEvent;
        public event Action<int, char, Vector4, float>? VertexClickEvent;

        protected GlyphSphereInterface(GlyphSphereCore glyphSphereCore)
        {
            core = glyphSphereCore;
            
            // Subscribe to core events
            core.VertexHovered += OnVertexHovered;
            core.VertexClicked += OnVertexClicked;
            core.UpdateRequested += OnUpdateRequested;
        }

        // Public interface properties and methods for vertex manipulation
        public int VertexCount => core.VertexCount;
        
        public Vector3 GetVertexPosition(int index)
        {
            return core.GetVertexPosition(index);
        }

        public void SetVertexGlyph(int index, char glyph, Vector4 color)
        {
            core.SetVertexGlyph(index, glyph, color);
        }

        public void SetVertexGlyph(int index, char glyph, System.Numerics.Vector3 color)
        {
            var vec4Color = new Vector4(color.X / 255.0f, color.Y / 255.0f, color.Z / 255.0f, 1.0f);
            core.SetVertexGlyph(index, glyph, vec4Color);
        }
        
        public void SetVertexGlyph(int index, char glyph, Vector4 color, float size)
        {
            core.SetVertexGlyph(index, glyph, color, size);
        }
        
        public void SetVertexBiome(int index, string biomeName, Vector4? colorOverride = null)
        {
            core.SetVertexBiome(index, biomeName, colorOverride);
        }
        
        public void SetVertexLocation(int index, string locationName, Vector4? colorOverride = null)
        {
            core.SetVertexLocation(index, locationName, colorOverride);
        }

        // Abstract methods that concrete implementations must provide
        /// <summary>
        /// Generate the world content (biomes, terrain, etc.) for all vertices using the implementation's specific logic.
        /// </summary>
        public abstract void GenerateWorld();

        /// <summary>
        /// Get detailed information about the world content at a specific vertex (biome, location, noise, etc.).
        /// Returns a tuple with relevant information for the specific world generation system.
        /// </summary>
        /// <param name="vertexIndex">Index of the vertex to query</param>
        /// <returns>Tuple containing world information specific to the implementation</returns>
        public abstract (string primaryType, string secondaryType, float noiseValue) GetWorldInfoAt(int vertexIndex);

        /// <summary>
        /// Get the glyph character that should be displayed at a specific vertex.
        /// </summary>
        /// <param name="vertexIndex">Index of the vertex</param>
        /// <returns>Character to display</returns>
        protected abstract char GetGlyphAt(int vertexIndex);

        /// <summary>
        /// Get the color that should be used for a specific vertex.
        /// </summary>
        /// <param name="vertexIndex">Index of the vertex</param>
        /// <returns>Color as System.Numerics.Vector3 (RGB 0-255 range)</returns>
        protected abstract System.Numerics.Vector3 GetColorAt(int vertexIndex);

        /// <summary>
        /// Update method called at regular intervals to animate world elements.
        /// Implementations can use this to animate water, clouds, or other dynamic elements.
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update in seconds</param>
        public abstract void Update(float deltaTime);

        // Common utility methods available to all implementations
        public void ClearAllGlyphs()
        {
            var greenColor = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);
            for (int i = 0; i < VertexCount; i++)
            {
                SetVertexGlyph(i, '.', greenColor);
            }
        }

        public void SetAllGlyphsTo(char glyph, Vector4 color)
        {
            for (int i = 0; i < VertexCount; i++)
            {
                SetVertexGlyph(i, glyph, color);
            }
        }

        /// <summary>
        /// Utility method to print statistics about glyph distribution.
        /// Can be used by concrete implementations in their GenerateWorld method.
        /// </summary>
        /// <param name="glyphCounts">Dictionary of glyph characters and their counts</param>
        /// <param name="totalVertices">Total number of vertices</param>
        /// <param name="title">Title for the statistics output</param>
        protected void PrintGlyphStatistics(System.Collections.Generic.Dictionary<char, int> glyphCounts, int totalVertices, string title = "Glyph Distribution")
        {
            Console.WriteLine($"\n{title}:");
            foreach (var kvp in glyphCounts.OrderByDescending(x => x.Value))
            {
                float percentage = (kvp.Value / (float)totalVertices) * 100f;
                Console.WriteLine($"  '{kvp.Key}': {kvp.Value} vertices ({percentage:F1}%)");
            }
        }

        /// <summary>
        /// Utility method to print noise distribution statistics.
        /// Can be used by concrete implementations that use noise generation.
        /// </summary>
        /// <param name="noiseValues">List of noise values (should be sorted)</param>
        /// <param name="title">Title for the statistics output</param>
        protected void PrintNoiseStatistics(System.Collections.Generic.List<float> noiseValues, string title = "Noise Distribution Statistics")
        {
            if (noiseValues.Count == 0) return;

            var sortedValues = new System.Collections.Generic.List<float>(noiseValues);
            sortedValues.Sort();
            
            float min = sortedValues[0];
            float max = sortedValues[sortedValues.Count - 1];
            float mean = sortedValues.Average();
            float median = sortedValues[sortedValues.Count / 2];
            
            double sumSquaredDiffs = sortedValues.Sum(x => Math.Pow(x - mean, 2));
            float stdDev = (float)Math.Sqrt(sumSquaredDiffs / sortedValues.Count);

            float p10 = sortedValues[(int)(sortedValues.Count * 0.1)];
            float p25 = sortedValues[(int)(sortedValues.Count * 0.25)];
            float p75 = sortedValues[(int)(sortedValues.Count * 0.75)];
            float p90 = sortedValues[(int)(sortedValues.Count * 0.9)];

            Console.WriteLine($"\n{title} ({sortedValues.Count} vertices):");
            Console.WriteLine($"  Min: {min:F3}, Max: {max:F3}");
            Console.WriteLine($"  Mean: {mean:F3}, Median: {median:F3}, StdDev: {stdDev:F3}");
            Console.WriteLine($"  Percentiles - P10: {p10:F3}, P25: {p25:F3}, P75: {p75:F3}, P90: {p90:F3}");
        }

        // Event handlers for core mouse interactions
        private void OnVertexHovered(int vertexIndex, OpenTK.Mathematics.Vector2 mousePos)
        {
            if (vertexIndex >= 0)
            {
                // Get vertex information and fire interface event
                Vector3 pos = GetVertexPosition(vertexIndex);
                
                // Get actual glyph and color from the implementation
                char glyph = GetGlyphAt(vertexIndex);
                var color3 = GetColorAt(vertexIndex);
                Vector4 color = new Vector4(color3.X / 255.0f, color3.Y / 255.0f, color3.Z / 255.0f, 1.0f);
                VertexHoverEvent?.Invoke(vertexIndex, glyph, color);
            }
            else
            {
                // Mouse is not over any vertex - fire unhover event
                VertexHoverEvent?.Invoke(-1, ' ', new Vector4(0, 0, 0, 0));
            }
        }

        private void OnVertexClicked(int vertexIndex, OpenTK.Mathematics.Vector2 mousePos)
        {
            // Get vertex information and fire interface event
            Vector3 pos = GetVertexPosition(vertexIndex);
            
            // Get actual world info from the implementation
            var (primaryType, secondaryType, noiseValue) = GetWorldInfoAt(vertexIndex);
            char glyph = GetGlyphAt(vertexIndex);
            var color3 = GetColorAt(vertexIndex);
            Vector4 color = new Vector4(color3.X / 255.0f, color3.Y / 255.0f, color3.Z / 255.0f, 1.0f);
            
            Console.WriteLine($"Picked vertex {vertexIndex}, glyph '{glyph}', {primaryType}" +
                             (string.IsNullOrEmpty(secondaryType) ? "" : $"/{secondaryType}") + 
                             $", noise {noiseValue:F3}");
            
            VertexClickEvent?.Invoke(vertexIndex, glyph, color, noiseValue);
        }

        private void OnUpdateRequested(float deltaTime)
        {
            // Call the abstract Update method that concrete implementations must provide
            Update(deltaTime);
        }
    }
}