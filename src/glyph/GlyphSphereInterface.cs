// GlyphSphereInterface.cs - User-friendly interface for the glyph sphere
// Provides easy-to-use methods to modify glyphs, handle biome generation, and manage mouse events
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using static Cathedral.Glyph.BiomeDatabase;

using Vector3 = OpenTK.Mathematics.Vector3;
using Vector4 = OpenTK.Mathematics.Vector4;

namespace Cathedral.Glyph
{
    public class GlyphSphereInterface
    {
        private GlyphSphereCore core;
        
        // Events for user interaction
        public event Action<int, char, Vector4>? VertexHoverEvent;
        public event Action<int, char, Vector4, float>? VertexClickEvent;

        public GlyphSphereInterface(GlyphSphereCore glyphSphereCore)
        {
            core = glyphSphereCore;
            
            // Subscribe to core events
            core.VertexHovered += OnVertexHovered;
            core.VertexClicked += OnVertexClicked;
        }

        // Public interface methods for vertex manipulation
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

        // Generate biomes for all vertices using Perlin noise
        public void GenerateBiomes()
        {
            Console.WriteLine("Generating biomes using Perlin noise...");
            
            var noiseValues = new List<float>();
            var glyphCounts = new Dictionary<char, int>();

            // Apply noise and biome generation to all vertices
            for (int i = 0; i < VertexCount; i++)
            {
                Vector3 pos = GetVertexPosition(i);
                
                // Multi-scale Perlin noise like the original Unity code
                Vector3 off1 = new Vector3(1337.0f, 2468.0f, 9876.0f);
                Vector3 off2 = new Vector3(5432.0f, 8765.0f, 1234.0f);
                Vector3 off3 = new Vector3(9999.0f, 3333.0f, 7777.0f);
                
                Vector3 p1 = (off1 + pos) / 12f;
                Vector3 p2 = (off2 + pos) / 3f;
                Vector3 p3 = (off3 + pos) / 8f;
                
                float perlinNoise1 = Perlin.Noise(p1.X, p1.Y, p1.Z);
                float perlinNoise2 = Perlin.Noise(p2.X, p2.Y, p2.Z);
                float perlinNoise3 = Perlin.Noise(p3.X, p3.Y, p3.Z);
                
                // Determine biome based on the three noise layers (matching Unity logic)
                BiomeType biome = DetermineBiome(perlinNoise1, perlinNoise2, perlinNoise3);
                
                // Calculate location spawn chance and determine if a location should spawn
                LocationType? location = DetermineLocation(biome, pos);
                
                // Get glyph and color based on location first, then biome
                char glyphChar;
                System.Numerics.Vector3 color;
                if (location.HasValue)
                {
                    glyphChar = location.Value.Glyph;
                    var locColor = location.Value.Color;
                    color = new System.Numerics.Vector3(locColor.X, locColor.Y, locColor.Z);
                }
                else
                {
                    glyphChar = biome.Glyph;
                    var biomeColor = biome.Color;
                    color = new System.Numerics.Vector3(biomeColor.X, biomeColor.Y, biomeColor.Z);
                }
                
                // Set the vertex properties using the interface
                SetVertexGlyph(i, glyphChar, color);
                
                // Collect statistics
                float avgNoise = (perlinNoise1 + perlinNoise2 + perlinNoise3) / 3.0f;
                noiseValues.Add(avgNoise);
                
                if (glyphCounts.ContainsKey(glyphChar))
                    glyphCounts[glyphChar]++;
                else
                    glyphCounts[glyphChar] = 1;
            }

            // Print statistics like the original code
            if (noiseValues.Count > 0)
            {
                noiseValues.Sort();
                float min = noiseValues[0];
                float max = noiseValues[noiseValues.Count - 1];
                float mean = noiseValues.Average();
                float median = noiseValues[noiseValues.Count / 2];
                
                double sumSquaredDiffs = noiseValues.Sum(x => Math.Pow(x - mean, 2));
                float stdDev = (float)Math.Sqrt(sumSquaredDiffs / noiseValues.Count);

                float p10 = noiseValues[(int)(noiseValues.Count * 0.1)];
                float p25 = noiseValues[(int)(noiseValues.Count * 0.25)];
                float p75 = noiseValues[(int)(noiseValues.Count * 0.75)];
                float p90 = noiseValues[(int)(noiseValues.Count * 0.9)];

                Console.WriteLine($"\nNoise Distribution Statistics ({noiseValues.Count} vertices):");
                Console.WriteLine($"  Min: {min:F3}, Max: {max:F3}");
                Console.WriteLine($"  Mean: {mean:F3}, Median: {median:F3}, StdDev: {stdDev:F3}");
                Console.WriteLine($"  Percentiles - P10: {p10:F3}, P25: {p25:F3}, P75: {p75:F3}, P90: {p90:F3}");

                Console.WriteLine($"\nBiome-Based Glyph Distribution:");
                foreach (var kvp in glyphCounts.OrderByDescending(x => x.Value))
                {
                    float percentage = (kvp.Value / (float)noiseValues.Count) * 100f;
                    Console.WriteLine($"  '{kvp.Key}': {kvp.Value} vertices ({percentage:F1}%)");
                }
            }
        }

        // Event handlers for core mouse interactions
        private void OnVertexHovered(int vertexIndex, OpenTK.Mathematics.Vector2 mousePos)
        {
            // Get vertex information and fire interface event
            Vector3 pos = GetVertexPosition(vertexIndex);
            
            // For now, we'll use placeholder values since we don't store them in Core
            char glyph = '.'; // Will be updated when we have proper glyph tracking
            Vector4 color = new Vector4(0, 1, 0, 1); // Green default
            
            Console.WriteLine($"Mouse: ({mousePos.X:F0}, {mousePos.Y:F0}) -> Vertex {vertexIndex} at {pos}");
            
            VertexHoverEvent?.Invoke(vertexIndex, glyph, color);
        }

        private void OnVertexClicked(int vertexIndex, OpenTK.Mathematics.Vector2 mousePos)
        {
            // Get vertex information and fire interface event
            Vector3 pos = GetVertexPosition(vertexIndex);
            
            // For now, we'll use placeholder values
            char glyph = '.';
            Vector4 color = new Vector4(0, 1, 0, 1);
            float noise = 0.0f; // Placeholder
            
            Console.WriteLine($"Picked vertex {vertexIndex}, glyph '{glyph}', noise {noise:F3}");
            
            VertexClickEvent?.Invoke(vertexIndex, glyph, color, noise);
        }

        // Biome determination logic (from original code)
        private BiomeType DetermineBiome(float perlinNoise1, float perlinNoise2, float perlinNoise3)
        {
            // Based on Unity Microworld.cs biome classification logic (exact match)
            // perlinNoise1: water classification (-1 to 1 range)
            // perlinNoise2: cities/forests/fields classification (-1 to 1 range)  
            // perlinNoise3: mountains classification (-1 to 1 range)

            // WATER (perlinNoise1)
            if (perlinNoise1 <= -0.25f)
                return Biomes["ocean"];
            if (perlinNoise1 <= 0.0f)
                return Biomes["sea"];

            // MOUNTAIN (perlinNoise3)
            if (perlinNoise3 > 0.5f)
                return Biomes["peak"];
            if (perlinNoise3 > 0.3f)
                return Biomes["mountain"];

            // CITY (perlinNoise2)
            if (perlinNoise2 < -0.4f)
                return Biomes["city"];

            // COAST (perlinNoise1)
            if (perlinNoise1 <= 0.065f)
                return Biomes["coast"];

            // FOREST (perlinNoise2)
            if (perlinNoise2 > 0.25f)
                return Biomes["forest"];

            // FIELD (perlinNoise2)
            if (perlinNoise2 < -0.15f)
                return Biomes["field"];

            // PLAIN (default fallback)
            return Biomes["plain"];
        }

        private LocationType? DetermineLocation(BiomeType biome, Vector3 position)
        {
            // Generate a pseudo-random value based on position for consistency
            int seed = (int)(position.X * 1000 + position.Y * 2000 + position.Z * 3000);
            var random = new Random(Math.Abs(seed));
            
            // Check if a location should spawn based on biome density
            if (random.NextDouble() > biome.Density)
                return null;

            // Get locations that can spawn in this biome
            var compatibleLocations = new List<LocationType>();
            foreach (var locationPair in Locations)
            {
                var location = locationPair.Value;
                if (location.AllowedBiomes.Contains(biome.Name))
                {
                    compatibleLocations.Add(location);
                }
            }

            // If no compatible locations, return null
            if (compatibleLocations.Count == 0)
                return null;

            // Randomly select a compatible location
            int locationIndex = random.Next(compatibleLocations.Count);
            return compatibleLocations[locationIndex];
        }

        // Optional convenience methods for bulk operations
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

        // Method to get biome info at a specific vertex (useful for debugging)
        public (BiomeType biome, LocationType? location, float noise) GetBiomeInfoAt(int vertexIndex)
        {
            Vector3 pos = GetVertexPosition(vertexIndex);
            
            // Recalculate noise values for this position
            Vector3 off1 = new Vector3(1337.0f, 2468.0f, 9876.0f);
            Vector3 off2 = new Vector3(5432.0f, 8765.0f, 1234.0f);
            Vector3 off3 = new Vector3(9999.0f, 3333.0f, 7777.0f);
            
            Vector3 p1 = (off1 + pos) / 12f;
            Vector3 p2 = (off2 + pos) / 3f;
            Vector3 p3 = (off3 + pos) / 8f;
            
            float perlinNoise1 = Perlin.Noise(p1.X, p1.Y, p1.Z);
            float perlinNoise2 = Perlin.Noise(p2.X, p2.Y, p2.Z);
            float perlinNoise3 = Perlin.Noise(p3.X, p3.Y, p3.Z);
            
            BiomeType biome = DetermineBiome(perlinNoise1, perlinNoise2, perlinNoise3);
            LocationType? location = DetermineLocation(biome, pos);
            float avgNoise = (perlinNoise1 + perlinNoise2 + perlinNoise3) / 3.0f;
            
            return (biome, location, avgNoise);
        }
    }
}