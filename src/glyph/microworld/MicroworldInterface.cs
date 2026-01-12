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
        
        private const float MOVE_SPEED = 5.0f; // Moves per second (debugging to understand timing)
        private const char AVATAR_CHAR = '☻';
        private const char PATH_WAYPOINT_CHAR = '.';
        private const char PATH_DESTINATION_CHAR = '+';
        
        // Debug counter for timing
        private int _debugFrameCount = 0;
        
        // Flag to disable world interactions (used when UI is in focus)
        private bool _worldInteractionsEnabled = true;

        // Events for location travel mode
        public event Action<AvatarArrivalInfo>? AvatarArrivedAtLocation;

        /// <summary>
        /// Detailed information about avatar arrival at a vertex
        /// </summary>
        public record AvatarArrivalInfo(
            int VertexIndex,
            LocationType? Location,
            BiomeType Biome,
            float NoiseValue,
            char Glyph,
            Vector3 Position,
            List<int> NeighboringVertices
        );

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
            VertexClickEvent += (vertexIndex, glyph, color, noiseValue) => {
                Console.WriteLine($"VertexClickEvent triggered for vertex {vertexIndex}");
                HandleVertexClicked(vertexIndex);
            };
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
                float size;
                if (location.HasValue)
                {
                    glyphChar = location.Value.Glyph;
                    var locColor = location.Value.Color;
                    color = new System.Numerics.Vector3(locColor.X, locColor.Y, locColor.Z);
                    size = location.Value.Size;
                }
                else
                {
                    glyphChar = biome.Glyph;
                    var biomeColor = biome.Color;
                    color = new System.Numerics.Vector3(biomeColor.X, biomeColor.Y, biomeColor.Z);
                    size = biome.Size;
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
                
                // Set the vertex properties using the interface with size factor
                var vec4Color = new Vector4(color.X / 255.0f, color.Y / 255.0f, color.Z / 255.0f, 1.0f);
                SetVertexGlyph(i, glyphChar, vec4Color, size);
                
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
            // Debug: Show deltaTime every 60 frames (about once per second)
            _debugFrameCount++;
            if (_debugFrameCount % 60 == 0)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Update called: deltaTime={deltaTime:F6}s (frame #{_debugFrameCount})");
            }

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
                    if (animationRandom.NextDouble() < 0.8) continue;

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
                    
                    // Update the visual representation with original biome size
                    var vec4Color = new Vector4(data.Color.X / 255.0f, data.Color.Y / 255.0f, data.Color.Z / 255.0f, 1.0f);
                    SetVertexGlyph(vertexIndex, newGlyph, vec4Color, data.Biome.Size);
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
            
            if (centerCamera)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Avatar placed at vertex {vertexIndex}");
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Avatar moved at vertex {vertexIndex}");
            }
            
            // Only center camera if explicitly requested - do this AFTER avatar is fully set
            if (centerCamera)
            {
                Console.WriteLine($"Centering camera on avatar at vertex {vertexIndex}...");
                core.CenterCameraOnGlyph(vertexIndex);
            }
        }

        private void RestoreVertexData(int vertexIndex, VertexWorldData data)
        {
            // Determine size based on location first, then biome
            float size = data.Location?.Size ?? data.Biome.Size;
            var vec4Color = new Vector4(data.Color.X / 255.0f, data.Color.Y / 255.0f, data.Color.Z / 255.0f, 1.0f);
            SetVertexGlyph(vertexIndex, data.GlyphChar, vec4Color, size);
        }

        public void HandleVertexHovered(int vertexIndex)
        {
            // Ignore hover when interactions are disabled
            if (!_worldInteractionsEnabled)
                return;
                
            if (_avatarVertex == -1 || vertexIndex == _avatarVertex) return;

            // Don't show hover paths while avatar is moving
            if (IsAvatarMoving())
            {
                return;
            }

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
            
            // Ignore clicks when interactions are disabled
            if (!_worldInteractionsEnabled)
            {
                Console.WriteLine("World interactions are disabled");
                return;
            }
            
            if (_avatarVertex == -1)
            {
                Console.WriteLine("Cannot handle click: avatar not initialized");
                return;
            }
            
            // Allow clicking on avatar vertex - let GameController handle it
            // (GameController can enter location interaction mode)
            if (vertexIndex == _avatarVertex)
            {
                Console.WriteLine("HandleVertexClicked: Clicked on avatar vertex (allowing passthrough to GameController)");
                return; // Don't block - let event propagate to GameController
            }

            // Don't allow new movement while avatar is already moving
            if (IsAvatarMoving())
            {
                Console.WriteLine("Cannot handle click: avatar is already moving");
                return;
            }

            // Start movement to clicked vertex
            var pathfindingService = core.GetPathfindingService();
            var graph = core.GetGraph();
            
            Console.WriteLine($"Pathfinding service available: {pathfindingService != null}");
            Console.WriteLine($"Graph available: {graph != null}");
            
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
                        // Determine size based on location first, then biome
                        float size = data.Location?.Size ?? data.Biome.Size;
                        var vec4Color = new Vector4(data.Color.X / 255.0f, data.Color.Y / 255.0f, data.Color.Z / 255.0f, 1.0f);
                        SetVertexGlyph(nodeId, data.GlyphChar, vec4Color, size);
                    }
                }
            }
            _hoveredPath = null;
        }

        private void StartMovement(Cathedral.Pathfinding.Path path)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] StartMovement: Beginning {path.Length}-step path");
            _currentPath = path;
            _pathIndex = 0; // Start at avatar position
            _moveTimer = 0.0f;
            ClearHoveredPath(); // Clear any hover visualization
            _hoveredVertex = -1; // Clear hover state
            
            // Highlight the travel path
            DrawTravelPath();
        }
        
        private void DrawTravelPath()
        {
            if (_currentPath == null || _currentPath.Length <= 1) return;
            
            // Highlight path with yellow/gold color
            var pathColor = new System.Numerics.Vector3(255, 215, 0); // Gold
            var destColor = new System.Numerics.Vector3(255, 255, 128); // Bright yellow for destination
            
            // Draw waypoints (skip avatar position)
            for (int i = 1; i < _currentPath.Length - 1; i++)
            {
                int nodeId = _currentPath.GetNode(i);
                SetVertexGlyph(nodeId, PATH_WAYPOINT_CHAR, pathColor);
            }
            
            // Highlight destination
            if (_currentPath.Length > 1)
            {
                int destNode = _currentPath.GetNode(_currentPath.Length - 1);
                SetVertexGlyph(destNode, PATH_DESTINATION_CHAR, destColor);
            }
        }
        
        private void ClearTravelPath()
        {
            if (_currentPath == null || _currentPath.Length <= 1) return;
            
            // Restore original glyphs for the path
            for (int i = 1; i < _currentPath.Length; i++) // Skip avatar position
            {
                int nodeId = _currentPath.GetNode(i);
                if (nodeId != _avatarVertex && vertexData.TryGetValue(nodeId, out var data))
                {
                    float size = data.Location?.Size ?? data.Biome.Size;
                    var vec4Color = new Vector4(data.Color.X / 255.0f, data.Color.Y / 255.0f, data.Color.Z / 255.0f, 1.0f);
                    SetVertexGlyph(nodeId, data.GlyphChar, vec4Color, size);
                }
            }
        }

        private void UpdateMovement(float deltaTime)
        {
            if (_currentPath == null || _pathIndex >= _currentPath.Length - 1) return;

            // Calculate threshold for this frame
            float threshold = 1.0f / MOVE_SPEED;
            
            // Log detailed timing every frame when moving
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] UpdateMovement: deltaTime={deltaTime:F6}s, moveTimer={_moveTimer:F6}s, threshold={threshold:F6}s, MOVE_SPEED={MOVE_SPEED}");

            _moveTimer += deltaTime;
            
            if (_moveTimer >= threshold)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MOVE TRIGGERED: moveTimer={_moveTimer:F6}s >= threshold={threshold:F6}s");
                _moveTimer = 0.0f;
                _pathIndex++;
                
                if (_pathIndex < _currentPath.Length)
                {
                    int nextVertex = _currentPath.GetNode(_pathIndex);
                    
                    // Restore the previous vertex to its original appearance (no longer on path ahead)
                    if (_pathIndex > 0 && vertexData.TryGetValue(_currentPath.GetNode(_pathIndex - 1), out var prevData))
                    {
                        if (_currentPath.GetNode(_pathIndex - 1) != _avatarVertex)
                        {
                            float size = prevData.Location?.Size ?? prevData.Biome.Size;
                            var vec4Color = new Vector4(prevData.Color.X / 255.0f, prevData.Color.Y / 255.0f, prevData.Color.Z / 255.0f, 1.0f);
                            SetVertexGlyph(_currentPath.GetNode(_pathIndex - 1), prevData.GlyphChar, vec4Color, size);
                        }
                    }
                    
                    PlaceAvatar(nextVertex, centerCamera: true); // Focus camera on avatar with each step
                    
                    if (_pathIndex >= _currentPath.Length - 1)
                    {
                        // Movement complete - clear travel path visualization
                        ClearTravelPath();
                        _currentPath = null;
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Avatar arrived at vertex {_avatarVertex}");
                        
                        // Fire arrival event with detailed location info
                        if (vertexData.TryGetValue(_avatarVertex, out var data))
                        {
                            var neighbors = GetNeighboringVertices(_avatarVertex);
                            var position = GetVertexPosition(_avatarVertex);
                            var arrivalInfo = new AvatarArrivalInfo(
                                _avatarVertex,
                                data.Location,
                                data.Biome,
                                data.NoiseValue,
                                data.GlyphChar,
                                position,
                                neighbors
                            );
                            AvatarArrivedAtLocation?.Invoke(arrivalInfo);
                        }
                    }
                }
            }
        }



        /// <summary>
        /// Enables or disables world map interactions (pathfinding, avatar movement, hover paths)
        /// </summary>
        public void SetWorldInteractionsEnabled(bool enabled)
        {
            _worldInteractionsEnabled = enabled;
            
            // Clear any active hover paths when disabling
            if (!enabled)
            {
                ClearHoveredPath();
                _hoveredVertex = -1;
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

        /// <summary>
        /// Gets location and biome info for the current avatar position
        /// </summary>
        public (LocationType? location, BiomeType biome) GetCurrentLocationInfo()
        {
            if (_avatarVertex >= 0 && vertexData.TryGetValue(_avatarVertex, out var data))
            {
                return (data.Location, data.Biome);
            }
            return (null, Biomes["plain"]); // Default fallback
        }

        /// <summary>
        /// Checks if the avatar is currently at a location (not just any vertex)
        /// </summary>
        public bool IsAtLocation()
        {
            return _avatarVertex >= 0 && 
                   vertexData.TryGetValue(_avatarVertex, out var data) && 
                   data.Location.HasValue;
        }

        /// <summary>
        /// Gets the neighboring vertices for a given vertex
        /// </summary>
        public List<int> GetNeighboringVertices(int vertexIndex)
        {
            var neighbors = new List<int>();
            var graph = core.GetGraph();
            if (graph != null && graph.ContainsNode(vertexIndex))
            {
                neighbors.AddRange(graph.GetConnectedNodes(vertexIndex));
            }
            return neighbors;
        }

        /// <summary>
        /// Gets detailed information about a specific vertex
        /// </summary>
        public (BiomeType biome, LocationType? location, float noiseValue, char glyph)? GetVertexInfo(int vertexIndex)
        {
            if (vertexData.TryGetValue(vertexIndex, out var data))
            {
                return (data.Biome, data.Location, data.NoiseValue, data.GlyphChar);
            }
            return null;
        }

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