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

        // Protagonist system
        private int _protagonistVertex = -1;
        private VertexWorldData? _originalProtagonistData;
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

        // Travel constraint (forbids sea/ocean for default on-foot travel) and the
        // constraint-aware graph view used for hover-path pathfinding.
        private ITravelConstraint? _travelConstraint;
        private ConstrainedPathGraph? _constrainedGraph;

        // Externally-controlled travel mode: when true, clicks do not auto-start
        // movement (the controller handles waypoint queuing and triggers movement
        // explicitly via BeginTravelAlongPath).
        private bool _externalTravelControl;

        // Committed (post-waypoint) path drawn on top of the world while the player is
        // still planning. Stored as vertex ids; rendering uses these to know which
        // tiles to restore when the plan is cleared or the hover preview overlaps.
        private readonly List<int> _plannedPathVertices = new();
        private readonly List<int> _plannedWaypointVertices = new();
        // Origin used as the start of the hover-path preview (typically the last
        // waypoint when one is set, otherwise the protagonist vertex).
        private int _hoverPathOrigin = -1;
        
        private const float MOVE_SPEED = 5.0f; // Moves per second (debugging to understand timing)
        
        // Debug counter for timing
        private int _debugFrameCount = 0;
        
        // Flag to disable world interactions (used when UI is in focus)
        private bool _worldInteractionsEnabled = true;

        // Events for location travel mode
        public event Action<ProtagonistArrivalInfo>? ProtagonistArrivedAtLocation;

        /// <summary>
        /// Detailed information about protagonist arrival at a vertex
        /// </summary>
        public record ProtagonistArrivalInfo(
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
            // Subscribe to our own events to handle protagonist interactions
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

        // ── Travel constraint plumbing ──────────────────────────────────────────────

        /// <summary>
        /// Installs the travel constraint used for hover pathfinding and traversability
        /// checks. Pass <c>null</c> to clear the constraint.
        /// </summary>
        public void SetTravelConstraint(ITravelConstraint? constraint)
        {
            _travelConstraint = constraint;
            var baseGraph = core.GetGraph();
            _constrainedGraph = (constraint != null && baseGraph != null)
                ? new ConstrainedPathGraph(baseGraph, constraint)
                : null;
        }

        /// <summary>The pathfinding graph currently used for travel (constraint-aware if one is set).</summary>
        public IPathGraph? GetTravelGraph() => (IPathGraph?)_constrainedGraph ?? core.GetGraph();

        /// <summary>Returns false if the given vertex is forbidden by the active constraint.</summary>
        public bool IsVertexTraversable(int vertexIndex)
            => _travelConstraint == null || _travelConstraint.IsTraversable(vertexIndex);

        /// <summary>Looks up the biome name at a vertex, or null if unknown.</summary>
        public string? GetBiomeNameAt(int vertexIndex)
            => vertexData.TryGetValue(vertexIndex, out var data) ? data.Biome.Name : null;

        // ── External travel control ─────────────────────────────────────────────────

        /// <summary>
        /// When enabled, the interface stops auto-starting movement on world clicks.
        /// The owning controller becomes responsible for queuing waypoints and calling
        /// <see cref="BeginTravelAlongPath"/> once the player commits to a route.
        /// </summary>
        public void SetExternalTravelControl(bool enabled) => _externalTravelControl = enabled;

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
                
                SetVertexGlyph(i, glyphChar, TileColor(vertexData[i]), size);
                
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

            PostProcessWorld();

            // Initialize protagonist at a random suitable location
            InitializeProtagonist();
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
            // Protagonist takes priority over biome data
            if (vertexIndex == _protagonistVertex)
            {
                return Config.GlyphSphere.ProtagonistChar;
            }

            if (vertexData.TryGetValue(vertexIndex, out var data))
            {
                return data.GlyphChar;
            }
            return '.'; // Default fallback
        }

        protected override System.Numerics.Vector3 GetColorAt(int vertexIndex)
        {
            // Protagonist takes priority over biome data
            if (vertexIndex == _protagonistVertex)
            {
                return Config.GlyphSphere.ProtagonistColor;
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

            // CITY (perlinNoise2) — tightened from -0.4 to -0.58
            // if (perlinNoise2 < -0.58f)
            //     return Biomes["city"];
            if (perlinNoise2 < -0.58f)
                return Biomes["field"]; // TODO: restore city biome

            // COAST (perlinNoise1)
            if (perlinNoise1 <= 0.065f)
                return Biomes["coast"];

            // FOREST (perlinNoise2)
            if (perlinNoise2 > 0.25f)
                return Biomes["forest"];

            // FIELD (perlinNoise2) — tightened from -0.15 to -0.38
            if (perlinNoise2 < -0.38f)
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
            
            // Update protagonist movement
            UpdateMovement(deltaTime);
            
            // Animate water vertices (sea and ocean biomes)
            foreach (int vertexIndex in waterVertices)
            {
                if (vertexData.TryGetValue(vertexIndex, out var data))
                {
                    // Skip water animation if this vertex has the protagonist
                    if (vertexIndex == _protagonistVertex) continue;
                    
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
                    SetVertexGlyph(vertexIndex, newGlyph, TileColor(data), data.Biome.Size);
                }
            }
        }

        // Protagonist Management Methods
        private void InitializeProtagonist()
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
                _protagonistVertex = suitableVertices[animationRandom.Next(suitableVertices.Count)];
                PlaceProtagonist(_protagonistVertex, centerCamera: true); // Center camera only during initialization
                
                Console.WriteLine($"Protagonist initialized at vertex {_protagonistVertex} ({vertexData[_protagonistVertex].Biome.Name})");
            }
        }

        private void PlaceProtagonist(int vertexIndex, bool centerCamera = false)
        {
            // Store the original data if we're moving to a new vertex
            if (_protagonistVertex != -1 && _protagonistVertex != vertexIndex && _originalProtagonistData.HasValue)
            {
                RestoreVertexData(_protagonistVertex, _originalProtagonistData.Value);
            }

            // Store the new vertex data
            if (vertexData.TryGetValue(vertexIndex, out var data))
            {
                _originalProtagonistData = data;
            }

            // Set protagonist character and color
            _protagonistVertex = vertexIndex;
            SetVertexGlyph(vertexIndex, Config.GlyphSphere.ProtagonistChar, Config.GlyphSphere.ProtagonistColor, true); // Mark as UI element
            
            if (centerCamera)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Protagonist placed at vertex {vertexIndex}");
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Protagonist moved at vertex {vertexIndex}");
            }
            
            // Only center camera if explicitly requested - do this AFTER protagonist is fully set
            if (centerCamera)
            {
                Console.WriteLine($"Centering camera on protagonist at vertex {vertexIndex}...");
                core.CenterCameraOnGlyph(vertexIndex);
            }
        }

        /// <summary>
        /// Returns the shader category alpha for a tile:
        ///   1.0 = nature (grayscale), 2.0 = water (dark purple),
        ///   3.0 = human construction (dark yellow), 4.0 = field (intermediate)
        /// </summary>
        private static float GetTileCategory(VertexWorldData data)
        {
            if (data.Location.HasValue)
            {
                string n = data.Location.Value.Name;
                if (BiomeDatabase.WaterLocations.Contains(n))  return 2.0f;
                if (BiomeDatabase.HumanLocations.Contains(n))  return 3.0f;
            }
            else
            {
                string n = data.Biome.Name;
                if (BiomeDatabase.WaterBiomes.Contains(n))     return 2.0f;
                if (BiomeDatabase.HumanBiomes.Contains(n))     return 3.0f;
                if (n == "field")                              return 4.0f;
            }
            return 1.0f;
        }

        /// <summary>
        /// Post-processes the generated world to fix coherence issues.
        /// Currently ensures every field tile is adjacent to at least one farm or village.
        /// </summary>
        private void PostProcessWorld()
        {
            int placed = 0;
            LocationType farm    = Locations["farm"];
            LocationType village = Locations["village"];

            for (int i = 0; i < VertexCount; i++)
            {
                if (!vertexData.TryGetValue(i, out var data) || data.Biome.Name != "field")
                    continue;

                var neighbors = GetNeighboringVertices(i);

                // Already satisfied if any neighbor has a farm or village
                bool satisfied = neighbors.Any(n =>
                    vertexData.TryGetValue(n, out var nd) &&
                    nd.Location.HasValue &&
                    (nd.Location.Value.Name == "farm" || nd.Location.Value.Name == "village"));

                if (satisfied)
                    continue;

                // Pick placement candidate: self first (if empty), then an empty field neighbor, then force self
                int candidate = -1;
                if (!data.Location.HasValue)
                {
                    candidate = i;
                }
                else
                {
                    foreach (int n in neighbors)
                    {
                        if (vertexData.TryGetValue(n, out var nd) &&
                            nd.Biome.Name == "field" && !nd.Location.HasValue)
                        {
                            candidate = n;
                            break;
                        }
                    }
                    if (candidate == -1)
                        candidate = i; // force-overwrite self as last resort
                }

                // Randomly pick farm or village (seeded on vertex index for determinism)
                LocationType chosen = new Random(i).Next(2) == 0 ? farm : village;

                // Place chosen location on candidate and refresh its visual
                var cd = vertexData[candidate];
                cd.Location = chosen;
                cd.GlyphChar = chosen.Glyph;
                cd.Color = new System.Numerics.Vector3(chosen.Color.X, chosen.Color.Y, chosen.Color.Z);
                vertexData[candidate] = cd;
                SetVertexGlyph(candidate, chosen.Glyph, TileColor(vertexData[candidate]), chosen.Size);
                placed++;
            }

            if (placed > 0)
                Console.WriteLine($"[PostProcess] Placed {placed} farm(s)/village(s) to satisfy field adjacency.");
        }

        private static Vector4 TileColor(VertexWorldData data) =>
            new Vector4(data.Color.X / 255.0f, data.Color.Y / 255.0f, data.Color.Z / 255.0f, GetTileCategory(data));

        private void RestoreVertexData(int vertexIndex, VertexWorldData data)
        {
            float size = data.Location?.Size ?? data.Biome.Size;
            SetVertexGlyph(vertexIndex, data.GlyphChar, TileColor(data), size);
        }

        public void HandleVertexHovered(int vertexIndex)
        {
            // Ignore hover when interactions are disabled
            if (!_worldInteractionsEnabled)
                return;

            if (_protagonistVertex == -1 || vertexIndex == _protagonistVertex) return;

            // Don't show hover paths while protagonist is moving
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

            // Skip hover-path computation for impassable destinations under the active
            // constraint. The popup terminal still shows the biome name so the player
            // gets feedback that the cell is reachable as a piece of geography just not
            // as a destination.
            if (!IsVertexTraversable(vertexIndex))
                return;

            // Request path to hovered vertex from the configured hover-path origin
            // (defaults to the protagonist, but can be set to the last waypoint by the
            // travel planner so the preview shows the *next* segment).
            int origin = _hoverPathOrigin >= 0 ? _hoverPathOrigin : _protagonistVertex;
            if (origin == vertexIndex) return;

            var pathfindingService = core.GetPathfindingService();
            var graph = GetTravelGraph();

            if (pathfindingService != null && graph != null)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        var path = await pathfindingService.FindPathAsync(graph, origin, vertexIndex);

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
            Console.WriteLine($"HandleVertexClicked: vertex {vertexIndex}, protagonist at {_protagonistVertex}");

            // Ignore clicks when interactions are disabled
            if (!_worldInteractionsEnabled)
            {
                Console.WriteLine("World interactions are disabled");
                return;
            }

            if (_protagonistVertex == -1)
            {
                Console.WriteLine("Cannot handle click: protagonist not initialized");
                return;
            }

            // Allow clicking on protagonist vertex - let GameController handle it
            // (GameController can enter location interaction mode)
            if (vertexIndex == _protagonistVertex)
            {
                Console.WriteLine("HandleVertexClicked: Clicked on protagonist vertex (allowing passthrough to GameController)");
                return; // Don't block - let event propagate to GameController
            }

            // Don't allow new movement while protagonist is already moving
            if (IsAvatarMoving())
            {
                Console.WriteLine("Cannot handle click: protagonist is already moving");
                return;
            }

            // When external travel control is enabled (waypoint mode), the controller
            // queues waypoints and calls BeginTravelAlongPath() once committed; the
            // interface no longer auto-starts movement on click.
            if (_externalTravelControl)
                return;

            // Legacy direct-movement-on-click behaviour. Still useful when no waypoint
            // planner is wired up (e.g. tests). Uses the constrained graph so sea/ocean
            // travel is forbidden even on this path.
            var pathfindingService = core.GetPathfindingService();
            var graph = GetTravelGraph();

            if (pathfindingService != null && graph != null)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        var path = await pathfindingService.FindPathAsync(graph, _protagonistVertex, vertexIndex);

                        if (path != null && path.Length > 1)
                        {
                            _pendingMovementPath = path;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Movement pathfinding error: {ex.Message}");
                    }
                });
            }
        }

        // ── Externally-driven travel & planned-path rendering ──────────────────────

        /// <summary>
        /// Sets the start vertex used for the hover-path preview. Passing -1 reverts to
        /// the protagonist's current position (default).
        /// </summary>
        public void SetHoverPathOrigin(int vertex)
        {
            _hoverPathOrigin = vertex;
            // Force a redraw of the hover preview against the new origin.
            ClearHoveredPath();
            _hoveredVertex = -1;
        }

        /// <summary>
        /// Draws the committed travel plan: the concatenated path through the queue of
        /// waypoints, with each waypoint cell shown as a numbered marker.
        /// </summary>
        public void ShowPlannedPath(IReadOnlyList<int> pathVertices,
                                    IReadOnlyList<int> waypointVertices)
        {
            ClearPlannedPath();
            if (pathVertices == null || pathVertices.Count <= 1) return;

            _plannedPathVertices.AddRange(pathVertices);
            if (waypointVertices != null) _plannedWaypointVertices.AddRange(waypointVertices);

            // Intermediate path cells (skip start = protagonist; skip waypoints themselves)
            var wpSet = new HashSet<int>(_plannedWaypointVertices);
            for (int i = 1; i < pathVertices.Count; i++)
            {
                int nodeId = pathVertices[i];
                if (nodeId == _protagonistVertex) continue;
                if (wpSet.Contains(nodeId)) continue;
                SetVertexGlyph(nodeId, Config.GlyphSphere.PathWaypointChar,
                    Config.GlyphSphere.PathWaypointActiveColor, true);
            }

            // Numbered waypoint markers in click order.
            for (int i = 0; i < _plannedWaypointVertices.Count; i++)
            {
                int wp = _plannedWaypointVertices[i];
                char marker = i < Config.GlyphSphere.WaypointNumberChars.Length
                    ? Config.GlyphSphere.WaypointNumberChars[i]
                    : Config.GlyphSphere.PathDestinationChar;
                var color = (i == _plannedWaypointVertices.Count - 1)
                    ? Config.GlyphSphere.PathDestinationActiveColor
                    : Config.GlyphSphere.PathWaypointActiveColor;
                SetVertexGlyph(wp, marker, color, true);
            }
        }

        /// <summary>Restores the original glyphs over the committed path cells.</summary>
        public void ClearPlannedPath()
        {
            // Restore intermediate path cells.
            for (int i = 1; i < _plannedPathVertices.Count; i++)
            {
                int nodeId = _plannedPathVertices[i];
                if (nodeId == _protagonistVertex) continue;
                if (vertexData.TryGetValue(nodeId, out var d))
                {
                    SetVertexGlyph(nodeId, d.GlyphChar, TileColor(d),
                        d.Location?.Size ?? d.Biome.Size);
                }
            }
            _plannedPathVertices.Clear();
            _plannedWaypointVertices.Clear();
        }

        /// <summary>
        /// Briefly overlays the "forbidden" glyph on a single cell to tell the player
        /// the click was rejected. The cell is drawn as a world tile (not a UI element)
        /// with the water category alpha so the world shader colors it purple, matching
        /// the danger tone used elsewhere in the UI.
        /// </summary>
        public void FlashForbiddenCell(int vertexIndex)
        {
            if (vertexIndex < 0 || !vertexData.ContainsKey(vertexIndex)) return;
            // alpha = 2.0 routes through the "water" branch of the world fragment
            // shader, producing a dark-purple rendering of the glyph. RGB is kept
            // bright so the shader's colorLuminance-modulated purple comes out vivid.
            var purple = new Vector4(1.0f, 1.0f, 1.0f, 2.0f);
            SetVertexGlyph(vertexIndex, Config.GlyphSphere.ForbiddenDestinationChar,
                purple, 1.5f);
        }

        /// <summary>Restores a cell previously decorated by <see cref="FlashForbiddenCell"/>.</summary>
        public void RestoreCellGlyph(int vertexIndex)
        {
            if (vertexIndex < 0) return;
            RestoreCellAppearance(vertexIndex);
        }

        /// <summary>
        /// Starts movement animation along an externally-resolved path. The path's
        /// first node must be the protagonist's current vertex.
        /// </summary>
        public void BeginTravelAlongPath(Cathedral.Pathfinding.Path path)
        {
            if (path == null || path.Length < 2) return;
            ClearPlannedPath();
            _pendingMovementPath = path;
        }

        private void UpdateHoveredPath(Cathedral.Pathfinding.Path? path)
        {
            ClearHoveredPath();
            _hoveredPath = path;
            
            if (path != null && path.Length > 1)
            {
                // Show path visualization
                for (int i = 1; i < path.Length - 1; i++) // Skip start (protagonist) and end (destination)
                {
                    int nodeId = path.GetNode(i);
                    SetVertexGlyph(nodeId, Config.GlyphSphere.PathWaypointChar, Config.GlyphSphere.PathWaypointPreviewColor, true); // Mark as UI element
                }

                // Mark destination
                if (path.Length > 1)
                {
                    int destNode = path.GetNode(path.Length - 1);
                    SetVertexGlyph(destNode, Config.GlyphSphere.PathDestinationChar, Config.GlyphSphere.PathDestinationPreviewColor, true); // Mark as UI element
                }
            }
        }

        private void ClearHoveredPath()
        {
            if (_hoveredPath != null && _hoveredPath.Length > 1)
            {
                // Restore each cell either to its planned-path appearance (when the
                // hover overlapped a committed waypoint/path) or to its original
                // biome/location glyph.
                for (int i = 1; i < _hoveredPath.Length; i++) // Skip start (protagonist)
                {
                    int nodeId = _hoveredPath.GetNode(i);
                    if (nodeId == _protagonistVertex) continue;
                    RestoreCellAppearance(nodeId);
                }
            }
            _hoveredPath = null;
        }

        /// <summary>
        /// Restores a cell to whichever overlay should currently own it: protagonist
        /// glyph → planned waypoint marker → planned intermediate path → underlying
        /// biome/location glyph.
        /// </summary>
        private void RestoreCellAppearance(int nodeId)
        {
            if (nodeId == _protagonistVertex) return; // owned by movement system

            // Planned waypoint marker takes priority.
            int wpIndex = _plannedWaypointVertices.IndexOf(nodeId);
            if (wpIndex >= 0)
            {
                char marker = wpIndex < Config.GlyphSphere.WaypointNumberChars.Length
                    ? Config.GlyphSphere.WaypointNumberChars[wpIndex]
                    : Config.GlyphSphere.PathDestinationChar;
                var color = (wpIndex == _plannedWaypointVertices.Count - 1)
                    ? Config.GlyphSphere.PathDestinationActiveColor
                    : Config.GlyphSphere.PathWaypointActiveColor;
                SetVertexGlyph(nodeId, marker, color, true);
                return;
            }

            // Planned intermediate cell.
            if (_plannedPathVertices.Count > 1 && _plannedPathVertices.Contains(nodeId))
            {
                SetVertexGlyph(nodeId, Config.GlyphSphere.PathWaypointChar,
                    Config.GlyphSphere.PathWaypointActiveColor, true);
                return;
            }

            // Otherwise restore original biome/location appearance.
            if (vertexData.TryGetValue(nodeId, out var data))
            {
                SetVertexGlyph(nodeId, data.GlyphChar, TileColor(data),
                    data.Location?.Size ?? data.Biome.Size);
            }
        }

        private void StartMovement(Cathedral.Pathfinding.Path path)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] StartMovement: Beginning {path.Length}-step path");
            _currentPath = path;
            _pathIndex = 0; // Start at protagonist position
            _moveTimer = 0.0f;
            ClearHoveredPath(); // Clear any hover visualization
            _hoveredVertex = -1; // Clear hover state
            
            // Highlight the travel path
            DrawTravelPath();
        }
        
        private void DrawTravelPath()
        {
            if (_currentPath == null || _currentPath.Length <= 1) return;
            
            // Draw waypoints (skip protagonist position)
            for (int i = 1; i < _currentPath.Length - 1; i++)
            {
                int nodeId = _currentPath.GetNode(i);
                SetVertexGlyph(nodeId, Config.GlyphSphere.PathWaypointChar, Config.GlyphSphere.PathWaypointActiveColor, true); // Mark as UI element
            }
            
            // Highlight destination
            if (_currentPath.Length > 1)
            {
                int destNode = _currentPath.GetNode(_currentPath.Length - 1);
                SetVertexGlyph(destNode, Config.GlyphSphere.PathDestinationChar, Config.GlyphSphere.PathDestinationActiveColor, true); // Mark as UI element
            }
        }
        
        private void ClearTravelPath()
        {
            if (_currentPath == null || _currentPath.Length <= 1) return;
            
            // Restore original glyphs for the path
            for (int i = 1; i < _currentPath.Length; i++) // Skip protagonist position
            {
                int nodeId = _currentPath.GetNode(i);
                if (nodeId != _protagonistVertex && vertexData.TryGetValue(nodeId, out var data))
                {
                    SetVertexGlyph(nodeId, data.GlyphChar, TileColor(data), data.Location?.Size ?? data.Biome.Size);
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
                        if (_currentPath.GetNode(_pathIndex - 1) != _protagonistVertex)
                        {
                            SetVertexGlyph(_currentPath.GetNode(_pathIndex - 1), prevData.GlyphChar, TileColor(prevData), prevData.Location?.Size ?? prevData.Biome.Size);
                        }
                    }
                    
                    PlaceProtagonist(nextVertex, centerCamera: true); // Focus camera on protagonist with each step
                    
                    if (_pathIndex >= _currentPath.Length - 1)
                    {
                        // Movement complete - clear travel path visualization
                        ClearTravelPath();
                        _currentPath = null;
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Protagonist arrived at vertex {_protagonistVertex}");
                        
                        // Fire arrival event with detailed location info
                        if (vertexData.TryGetValue(_protagonistVertex, out var data))
                        {
                            var neighbors = GetNeighboringVertices(_protagonistVertex);
                            var position = GetVertexPosition(_protagonistVertex);
                            var arrivalInfo = new ProtagonistArrivalInfo(
                                _protagonistVertex,
                                data.Location,
                                data.Biome,
                                data.NoiseValue,
                                data.GlyphChar,
                                position,
                                neighbors
                            );
                            ProtagonistArrivedAtLocation?.Invoke(arrivalInfo);
                        }
                    }
                }
            }
        }



        /// <summary>
        /// Enables or disables world map interactions (pathfinding, protagonist movement, hover paths)
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
        /// Gets the current protagonist vertex index
        /// </summary>
        public int GetAvatarVertex() => _protagonistVertex;

        /// <summary>
        /// Resets the protagonist to a new random starting position.
        /// Used when starting a new game from the main menu.
        /// </summary>
        public void ResetProtagonistPosition()
        {
            // Cancel any in-progress movement
            _currentPath = null;
            _hoveredPath = null;
            _pendingHoverPath = null;
            _pendingMovementPath = null;
            _pathIndex = 0;
            _moveTimer = 0.0f;
            _hoveredVertex = -1;
            _pendingHoverVertex = -1;
            
            // Re-initialize protagonist at a new random position
            InitializeProtagonist();
            Console.WriteLine($"MicroworldInterface: Protagonist reset to vertex {_protagonistVertex}");
        }

        /// <summary>
        /// Checks if the protagonist is currently moving
        /// </summary>
        public bool IsAvatarMoving() => _currentPath != null;

        /// <summary>
        /// Gets location and biome info for the current protagonist position
        /// </summary>
        public (LocationType? location, BiomeType biome) GetCurrentLocationInfo()
        {
            if (_protagonistVertex >= 0 && vertexData.TryGetValue(_protagonistVertex, out var data))
            {
                return (data.Location, data.Biome);
            }
            return (null, Biomes["plain"]); // Default fallback
        }

        /// <summary>
        /// Checks if the protagonist is currently at a location (not just any vertex)
        /// </summary>
        public bool IsAtLocation()
        {
            return _protagonistVertex >= 0 && 
                   vertexData.TryGetValue(_protagonistVertex, out var data) && 
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