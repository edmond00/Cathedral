// MicroworldInterface.cs - Concrete implementation for microworld biome generation
// Implements the specific biome and location logic for the microworld system
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenTK.Mathematics;
using static Cathedral.Glyph.Microworld.BiomeDatabase;
using Cathedral.Pathfinding;

using Vector3 = OpenTK.Mathematics.Vector3;

namespace Cathedral.Glyph.Microworld
{
    /// <summary>
    /// Concrete implementation of GlyphSphereInterface for microworld biome generation.
    /// Uses Perlin noise to generate realistic terrain with biomes and locations.
    /// </summary>
    public class MicroworldInterface : GlyphSphereInterface
    {
        // Store world data for each vertex
        private readonly Dictionary<int, VertexWorldData> vertexData = new Dictionary<int, VertexWorldData>();
        
        // Track vertices that need water animation (sea and ocean biomes without locations)
        private readonly HashSet<int> waterVertices = new HashSet<int>();
        
        // Random generator for water animation
        private readonly Random animationRandom = new Random();

        // Avatar system
        private int _avatarVertex = -1;
        private VertexWorldData? _originalAvatarData;
        private Cathedral.Pathfinding.Path? _currentPath;
        private Cathedral.Pathfinding.Path? _hoveredPath;
        private int _hoveredVertex = -1;
        private int _pathIndex = 0;
        private float _moveTimer = 0.0f;
        
        // Threading support for hover paths
        private Cathedral.Pathfinding.Path? _pendingHoverPath;
        private int _pendingHoverVertex = -1;
        
        // Threading support for movement paths
        private Cathedral.Pathfinding.Path? _pendingMovementPath;
        
        private const float MOVE_SPEED = 2.0f; // Moves per second
        private const char AVATAR_CHAR = '☻';
        private const char PATH_WAYPOINT_CHAR = '.';
        private const char PATH_DESTINATION_CHAR = '+';

        public MicroworldInterface(GlyphSphereCore glyphSphereCore) : base(glyphSphereCore)
        {
            // Subscribe to our own events to handle avatar interactions
            VertexHoverEvent += (vertexIndex, glyph, color) => 
            {
                if (vertexIndex >= 0)
                    HandleVertexHovered(vertexIndex);
                else
                    HandleVertexUnhovered();
            };
            VertexClickEvent += (vertexIndex, glyph, color, noiseValue) => HandleVertexClicked(vertexIndex);
        }

        public override void GenerateWorld()
        {
            Console.WriteLine("Generating microworld biomes using Perlin noise...");
            
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
                
                // Store world data for this vertex
                float avgNoise = (perlinNoise1 + perlinNoise2 + perlinNoise3) / 3.0f;
                vertexData[i] = new VertexWorldData
                {
                    Biome = biome,
                    Location = location,
                    NoiseValue = avgNoise,
                    GlyphChar = glyphChar,
                    Color = color
                };
                
                // Track water vertices for animation (sea/ocean biomes without locations)
                if ((biome.Name == "sea" || biome.Name == "ocean") && !location.HasValue)
                {
                    waterVertices.Add(i);
                }
                
                // Set the vertex properties using the interface
                SetVertexGlyph(i, glyphChar, color);
                
                // Collect statistics
                noiseValues.Add(avgNoise);
                
                if (glyphCounts.ContainsKey(glyphChar))
                    glyphCounts[glyphChar]++;
                else
                    glyphCounts[glyphChar] = 1;
            }

            // Print statistics using the base class utilities
            PrintNoiseStatistics(noiseValues, "Microworld Noise Distribution Statistics");
            PrintGlyphStatistics(glyphCounts, VertexCount, "Microworld Biome-Based Glyph Distribution");
            
            // Initialize avatar at a random suitable location
            InitializeAvatar();
        }

        public override (string primaryType, string secondaryType, float noiseValue) GetWorldInfoAt(int vertexIndex)
        {
            if (!vertexData.TryGetValue(vertexIndex, out var data))
            {
                return ("unknown", "", 0.0f);
            }

            string primaryType = data.Biome.Name;
            string secondaryType = data.Location?.Name ?? "";
            return (primaryType, secondaryType, data.NoiseValue);
        }

        protected override char GetGlyphAt(int vertexIndex)
        {
            // Avatar takes priority over biome data
            if (vertexIndex == _avatarVertex)
            {
                return AVATAR_CHAR;
            }

            if (vertexData.TryGetValue(vertexIndex, out var data))
            {
                return data.GlyphChar;
            }
            return '.'; // Default fallback
        }

        protected override System.Numerics.Vector3 GetColorAt(int vertexIndex)
        {
            // Avatar takes priority over biome data
            if (vertexIndex == _avatarVertex)
            {
                return new System.Numerics.Vector3(255, 255, 0); // Yellow for avatar
            }

            if (vertexData.TryGetValue(vertexIndex, out var data))
            {
                return data.Color;
            }
            return new System.Numerics.Vector3(0, 255, 0); // Default green
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

        /// <summary>
        /// Get detailed biome information at a specific vertex (microworld-specific method).
        /// </summary>
        /// <param name="vertexIndex">Index of the vertex to query</param>
        /// <returns>Detailed biome and location information</returns>
        public (BiomeType biome, LocationType? location, float noise) GetDetailedBiomeInfoAt(int vertexIndex)
        {
            if (vertexData.TryGetValue(vertexIndex, out var data))
            {
                return (data.Biome, data.Location, data.NoiseValue);
            }

            // Fallback: recalculate if not found
            Vector3 pos = GetVertexPosition(vertexIndex);
            
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

        public override void Update(float deltaTime)
        {
            // Process pending hover path from background thread
            ProcessPendingHoverPath();
            
            // Process pending movement from background thread
            ProcessPendingMovement();
            
            // Update avatar movement
            UpdateMovement(deltaTime);
            
            // Animate water vertices (sea and ocean biomes)
            foreach (int vertexIndex in waterVertices)
            {
                if (vertexData.TryGetValue(vertexIndex, out var data))
                {
                    // Skip water animation if this vertex has the avatar
                    if (vertexIndex == _avatarVertex) continue;
                    
                    // Skip water animation if this vertex is part of the hover path
                    if (IsVertexInHoverPath(vertexIndex)) continue;

                    char newGlyph;
                    
                    // Animate based on biome type:
                    // Sea: alternate between '~' and '≈'
                    // Ocean: alternate between '≈' and '≋'
                    if (data.Biome.Name == "sea")
                    {
                        // Sea animation: '~' and '≈'
                        newGlyph = animationRandom.NextDouble() < 0.5 ? '~' : '≈';
                    }
                    else if (data.Biome.Name == "ocean")
                    {
                        // Ocean animation: '≈' and '≋'
                        newGlyph = animationRandom.NextDouble() < 0.5 ? '≈' : '≋';
                    }
                    else
                    {
                        // Fallback - shouldn't happen
                        newGlyph = data.GlyphChar;
                    }
                    
                    // Update the glyph in the vertex data
                    var updatedData = data;
                    updatedData.GlyphChar = newGlyph;
                    vertexData[vertexIndex] = updatedData;
                    
                    // Update the visual representation
                    SetVertexGlyph(vertexIndex, newGlyph, data.Color);
                }
            }
        }

        // Avatar Management Methods
        private void InitializeAvatar()
        {
            // Find a suitable starting location (preferably plain or field biome)
            var suitableVertices = new List<int>();
            
            foreach (var kvp in vertexData)
            {
                var biome = kvp.Value.Biome;
                if (biome.Name == "plain" || biome.Name == "field" || biome.Name == "coast")
                {
                    suitableVertices.Add(kvp.Key);
                }
            }

            if (suitableVertices.Count == 0)
            {
                // Fallback: use any non-water vertex
                foreach (var kvp in vertexData)
                {
                    var biome = kvp.Value.Biome;
                    if (biome.Name != "sea" && biome.Name != "ocean")
                    {
                        suitableVertices.Add(kvp.Key);
                    }
                }
            }

            if (suitableVertices.Count > 0)
            {
                _avatarVertex = suitableVertices[animationRandom.Next(suitableVertices.Count)];
                PlaceAvatar(_avatarVertex, centerCamera: true); // Center camera only during initialization
                
                Console.WriteLine($"Avatar initialized at vertex {_avatarVertex} ({vertexData[_avatarVertex].Biome.Name})");
            }
        }

        private void PlaceAvatar(int vertexIndex, bool centerCamera = false)
        {
            // Store the original data if we're moving to a new vertex
            if (_avatarVertex != -1 && _avatarVertex != vertexIndex && _originalAvatarData.HasValue)
            {
                RestoreVertexData(_avatarVertex, _originalAvatarData.Value);
            }

            // Store the new vertex data
            if (vertexData.TryGetValue(vertexIndex, out var data))
            {
                _originalAvatarData = data;
            }

            // Set avatar character and color
            _avatarVertex = vertexIndex;
            var avatarColor = new System.Numerics.Vector3(255, 255, 0); // Yellow for avatar
            SetVertexGlyph(vertexIndex, AVATAR_CHAR, avatarColor);
            
            Console.WriteLine($"Avatar {(centerCamera ? "placed" : "moved")} at vertex {vertexIndex}");
            
            // Only center camera if explicitly requested - do this AFTER avatar is fully set
            if (centerCamera)
            {
                Console.WriteLine($"Centering camera on avatar at vertex {vertexIndex}...");
                core.CenterCameraOnGlyph(vertexIndex);
            }
        }

        private void RestoreVertexData(int vertexIndex, VertexWorldData data)
        {
            SetVertexGlyph(vertexIndex, data.GlyphChar, data.Color);
        }

        public void HandleVertexHovered(int vertexIndex)
        {
            if (_avatarVertex == -1 || vertexIndex == _avatarVertex) return;

            // Clear any existing hover path first
            if (_hoveredVertex != vertexIndex)
            {
                ClearHoveredPath();
            }

            _hoveredVertex = vertexIndex;
            
            // Request path to hovered vertex
            var pathfindingService = core.GetPathfindingService();
            var graph = core.GetGraph();
            
            if (pathfindingService != null && graph != null)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        var path = await pathfindingService.FindPathAsync(graph, _avatarVertex, vertexIndex);
                        
                        // Schedule path update on the main thread if still hovering the same vertex
                        if (_hoveredVertex == vertexIndex)
                        {
                            // Store the path for main thread processing
                            _pendingHoverPath = path;
                            _pendingHoverVertex = vertexIndex;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Pathfinding error: {ex.Message}");
                    }
                });
            }
        }

        public void HandleVertexUnhovered()
        {
            _hoveredVertex = -1;
            ClearHoveredPath();
        }

        private void ProcessPendingHoverPath()
        {
            if (_pendingHoverPath != null && _pendingHoverVertex == _hoveredVertex)
            {
                UpdateHoveredPath(_pendingHoverPath);
                _pendingHoverPath = null;
                _pendingHoverVertex = -1;
            }
        }

        private void ProcessPendingMovement()
        {
            if (_pendingMovementPath != null)
            {
                Console.WriteLine("Starting movement from pending path");
                StartMovement(_pendingMovementPath);
                _pendingMovementPath = null;
            }
        }

        private bool IsVertexInHoverPath(int vertexIndex)
        {
            if (_hoveredPath == null) return false;
            
            for (int i = 0; i < _hoveredPath.Length; i++)
            {
                if (_hoveredPath.GetNode(i) == vertexIndex)
                    return true;
            }
            return false;
        }

        public void HandleVertexClicked(int vertexIndex)
        {
            Console.WriteLine($"HandleVertexClicked: vertex {vertexIndex}, avatar at {_avatarVertex}");
            
            if (_avatarVertex == -1 || vertexIndex == _avatarVertex) return;

            // Start movement to clicked vertex
            var pathfindingService = core.GetPathfindingService();
            var graph = core.GetGraph();
            
            if (pathfindingService != null && graph != null)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        var path = await pathfindingService.FindPathAsync(graph, _avatarVertex, vertexIndex);
                        
                        if (path != null && path.Length > 1)
                        {
                            Console.WriteLine($"Path found for movement: {path.Length} nodes");
                            // Schedule movement on main thread
                            _pendingMovementPath = path;
                        }
                        else
                        {
                            Console.WriteLine("No path found for movement or path too short");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Movement pathfinding error: {ex.Message}");
                    }
                });
            }
        }

        private void UpdateHoveredPath(Cathedral.Pathfinding.Path? path)
        {
            ClearHoveredPath();
            _hoveredPath = path;
            
            if (path != null && path.Length > 1)
            {
                // Show path visualization
                for (int i = 1; i < path.Length - 1; i++) // Skip start (avatar) and end (destination)
                {
                    int nodeId = path.GetNode(i);
                    var waypointColor = new System.Numerics.Vector3(128, 128, 255); // Light blue for waypoints
                    SetVertexGlyph(nodeId, PATH_WAYPOINT_CHAR, waypointColor);
                }

                // Mark destination
                if (path.Length > 1)
                {
                    int destNode = path.GetNode(path.Length - 1);
                    var destColor = new System.Numerics.Vector3(255, 128, 128); // Light red for destination
                    SetVertexGlyph(destNode, PATH_DESTINATION_CHAR, destColor);
                }
            }
        }

        private void ClearHoveredPath()
        {
            if (_hoveredPath != null && _hoveredPath.Length > 1)
            {
                // Restore original glyphs for path visualization
                for (int i = 1; i < _hoveredPath.Length; i++) // Skip start (avatar)
                {
                    int nodeId = _hoveredPath.GetNode(i);
                    if (nodeId != _avatarVertex && vertexData.TryGetValue(nodeId, out var data))
                    {
                        SetVertexGlyph(nodeId, data.GlyphChar, data.Color);
                    }
                }
            }
            _hoveredPath = null;
        }

        private void StartMovement(Cathedral.Pathfinding.Path path)
        {
            _currentPath = path;
            _pathIndex = 0; // Start at avatar position
            _moveTimer = 0.0f;
            ClearHoveredPath(); // Clear any hover visualization
        }

        private void UpdateMovement(float deltaTime)
        {
            if (_currentPath == null || _pathIndex >= _currentPath.Length - 1) return;

            _moveTimer += deltaTime;
            
            if (_moveTimer >= 1.0f / MOVE_SPEED)
            {
                _moveTimer = 0.0f;
                _pathIndex++;
                
                if (_pathIndex < _currentPath.Length)
                {
                    int nextVertex = _currentPath.GetNode(_pathIndex);
                    PlaceAvatar(nextVertex, centerCamera: false); // Don't center camera during movement
                    
                    if (_pathIndex >= _currentPath.Length - 1)
                    {
                        // Movement complete
                        _currentPath = null;
                        Console.WriteLine($"Avatar arrived at vertex {_avatarVertex}");
                    }
                }
            }
        }



        /// <summary>
        /// Gets the current avatar vertex index
        /// </summary>
        public int GetAvatarVertex() => _avatarVertex;

        /// <summary>
        /// Checks if the avatar is currently moving
        /// </summary>
        public bool IsAvatarMoving() => _currentPath != null;

        // Data structure to store world information for each vertex
        private struct VertexWorldData
        {
            public BiomeType Biome;
            public LocationType? Location;
            public float NoiseValue;
            public char GlyphChar;
            public System.Numerics.Vector3 Color;
        }
    }
}