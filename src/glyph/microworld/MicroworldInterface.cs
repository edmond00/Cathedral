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
        
        // Random generator for water animation (purely cosmetic — intentionally unseeded)
        private readonly Random animationRandom = new Random();

        // Master-seed-derived offset added to every Perlin sample position, so a
        // different seed shifts the sampled noise field and yields a different world.
        // Computed once in GenerateWorld and reused by the recompute fallback.
        private Vector3 _worldNoiseOffset;

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

        // Travel range — vertices beyond this radius are darkened and blocked as waypoints.
        private readonly HashSet<int> _outOfRangeVertices = new();
        private const float TravelRangeDarkenFactor = 0.35f;

        // Events for location travel mode
        public event Action<ProtagonistArrivalInfo>? ProtagonistArrivedAtLocation;

        /// <summary>
        /// Fires every time the protagonist moves to a new vertex during travel,
        /// including the final step (before <see cref="ProtagonistArrivedAtLocation"/>).
        /// Carries the vertex index that was just entered.
        /// </summary>
        public event Action<int>? ProtagonistSteppedToVertex;

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
            RebuildConstrainedGraph();
        }

        /// <summary>
        /// Rebuilds the constrained path graph from the current biome constraint and the
        /// active travel-range set. Must be called whenever either changes.
        /// </summary>
        private void RebuildConstrainedGraph()
        {
            var baseGraph = core.GetGraph();
            if (baseGraph == null) { _constrainedGraph = null; return; }

            // Build the effective constraint: biome constraint AND range exclusion (if active).
            ITravelConstraint? effective = _travelConstraint;
            if (_outOfRangeVertices.Count > 0)
            {
                var rangeConstraint = new RangeExclusionConstraint(_outOfRangeVertices);
                effective = effective != null
                    ? new CompositeTravelConstraint(effective, rangeConstraint)
                    : rangeConstraint;
            }

            _constrainedGraph = effective != null
                ? new ConstrainedPathGraph(baseGraph, effective)
                : null;
        }

        /// <summary>The pathfinding graph currently used for travel (constraint-aware if one is set).</summary>
        public IPathGraph? GetTravelGraph() => (IPathGraph?)_constrainedGraph ?? core.GetGraph();

        /// <summary>Returns false if the given vertex is forbidden by the active constraint.</summary>
        public bool IsVertexTraversable(int vertexIndex)
            => _travelConstraint == null || _travelConstraint.IsTraversable(vertexIndex);

        /// <summary>Returns true if the vertex lies outside the current travel range radius.</summary>
        public bool IsOutOfTravelRange(int vertexIndex)
            => _outOfRangeVertices.Contains(vertexIndex);

        /// <summary>
        /// Every world vertex the avatar could currently set out for: has world data, is not blocked by
        /// the active travel constraint, and lies inside the stat-derived travel radius.
        ///
        /// <para>Exists for the <c>--cli</c> driver. Travel range covers far more of the map than the
        /// handful of immediate graph neighbours, so without this a test script can only ever reach
        /// whatever happens to border the spawn point — which is how testing a village feature turned
        /// into hunting for a seed that spawns beside one.</para>
        /// </summary>
        public IEnumerable<int> EnumerateReachableVertices()
        {
            for (int i = 0; i < VertexCount; i++)
            {
                if (i == _protagonistVertex) continue;
                if (!vertexData.ContainsKey(i)) continue;
                if (!IsVertexTraversable(i) || IsOutOfTravelRange(i)) continue;
                yield return i;
            }
        }

        /// <summary>
        /// Darkens every vertex beyond <paramref name="radius"/> from <paramref name="protagonistVertex"/>
        /// and records them so they are blocked as waypoint destinations and restored correctly
        /// when paths are cleared. Calls <see cref="ClearTravelRange"/> first.
        /// </summary>
        public void SetTravelRange(int protagonistVertex, float radius)
        {
            ClearTravelRange();
            if (protagonistVertex < 0 || radius <= 0f) return;

            Vector3 origin = GetVertexPosition(protagonistVertex);
            for (int i = 0; i < VertexCount; i++)
            {
                if (i == protagonistVertex || !vertexData.ContainsKey(i)) continue;
                if (Vector3.Distance(origin, GetVertexPosition(i)) > radius)
                {
                    _outOfRangeVertices.Add(i);
                    if (i != _protagonistVertex)
                        ApplyDarkening(i);
                }
            }

            RebuildConstrainedGraph();
        }

        /// <summary>Restores all darkened out-of-range vertices and clears the range set.</summary>
        public void ClearTravelRange()
        {
            foreach (int i in _outOfRangeVertices)
            {
                if (i == _protagonistVertex) continue;
                if (vertexData.TryGetValue(i, out var data))
                    SetVertexGlyph(i, data.GlyphChar, TileColor(i, data), data.Location?.Size ?? data.Biome.Size);
            }
            _outOfRangeVertices.Clear();
            RebuildConstrainedGraph();
        }

        private void ApplyDarkening(int vertexIndex)
        {
            if (!vertexData.TryGetValue(vertexIndex, out var data)) return;
            // Dim whatever the tile would otherwise be, overlay included: out-of-range has to keep
            // reading as out-of-range while the regions are on screen.
            var lit = TileColor(vertexIndex, data);
            var dark = new Vector4(
                lit.X * TravelRangeDarkenFactor,
                lit.Y * TravelRangeDarkenFactor,
                lit.Z * TravelRangeDarkenFactor,
                lit.W);
            SetVertexGlyph(vertexIndex, data.GlyphChar, dark, data.Location?.Size ?? data.Biome.Size);
        }

        /// <summary>Looks up the biome name at a vertex, or null if unknown.</summary>
        public string? GetBiomeNameAt(int vertexIndex)
            => vertexData.TryGetValue(vertexIndex, out var data) ? data.Biome.Name : null;

        // ── External travel control ─────────────────────────────────────────────────

        /// <summary>
        /// When true, <see cref="UpdateMovement"/> is suspended so the controller can
        /// process per-frame humor consumption before allowing the next vertex step.
        /// </summary>
        public bool MovementPaused { get; set; } = false;

        /// <summary>
        /// When enabled, the interface stops auto-starting movement on world clicks.
        /// The owning controller becomes responsible for queuing waypoints and calling
        /// <see cref="BeginTravelAlongPath"/> once the player commits to a route.
        /// </summary>
        public void SetExternalTravelControl(bool enabled) => _externalTravelControl = enabled;

        /// <summary>
        /// True once a world has been generated into this interface.
        ///
        /// <para>Worth a flag rather than a null check because the world is no longer built at
        /// startup: a new run builds it when its moon is confirmed, and a continued run builds it
        /// when the save is read. Between the window opening and either of those the sphere carries
        /// no biomes at all, and everything that would read one has to know not to ask.</para>
        /// </summary>
        public bool IsWorldGenerated { get; private set; }

        public override void GenerateWorld()
        {
            Console.WriteLine("Generating microworld biomes using Perlin noise...");

            // Every vertex is overwritten below, but waterVertices is only ever ADDED to — so on a
            // second generation the previous world's lakes would still be registered for animation on
            // tiles that are now dry land.
            waterVertices.Clear();

            // Derive the world's noise offset from the master seed. A large offset moves
            // the sampled region of the (fixed) Perlin field, so each seed is a new world.
            var worldRng = GameRng.For("world-terrain");
            _worldNoiseOffset = new Vector3(
                (float)(worldRng.NextDouble() * 20000.0 - 10000.0),
                (float)(worldRng.NextDouble() * 20000.0 - 10000.0),
                (float)(worldRng.NextDouble() * 20000.0 - 10000.0));

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

                Vector3 sp = pos + _worldNoiseOffset;
                Vector3 p1 = (off1 + sp) / 12f;
                Vector3 p2 = (off2 + sp) / 3f;
                Vector3 p3 = (off3 + sp) / 8f;
                
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
                    SettlementNoise = perlinNoise2,
                    GlyphChar = glyphChar,
                    Color = color
                };
                
                // Track water vertices for animation (sea/ocean biomes without locations)
                if ((biome.Name == "sea" || biome.Name == "ocean") && !location.HasValue)
                {
                    waterVertices.Add(i);
                }
                
                SetVertexGlyph(i, glyphChar, TileColor(i, vertexData[i]), size);
                
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

            // After PostProcessWorld, because the farms and villages it places are part of the world
            // the regions divide — and before InitializeProtagonist, so that anything downstream of
            // the spawn can already ask which region it is standing in.
            BuildRegions();

            // Initialize protagonist at a random suitable location
            InitializeProtagonist();

            IsWorldGenerated = true;
        }

        // ── Regions ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// The world's division into regions, or null before a world has been generated.
        /// Purely descriptive for now: nothing in the game reads it except the developer overlay.
        /// </summary>
        public WorldRegionMap? Regions { get; private set; }

        /// <summary>
        /// True while the sphere is drawn by region rather than by biome. Toggled by the developer
        /// R key and by the CLI's <c>key R</c>; never on in a shipped build, which has no way in.
        /// </summary>
        public bool RegionOverlayEnabled { get; private set; }

        private void BuildRegions()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            Regions = WorldRegionMap.Build(new WorldRegionInput
            {
                VertexCount     = VertexCount,
                Neighbours      = GetNeighboringVertices,
                IsLand          = v => vertexData.TryGetValue(v, out var d)
                                       && !BiomeDatabase.WaterBiomes.Contains(d.Biome.Name),
                // DetermineBiome tests the mountain layer before it reads the settlement one, so
                // above the treeline the settlement noise decides nothing at all and its value there
                // says nothing about where people are.
                IsSettleable    = v => vertexData.TryGetValue(v, out var d)
                                       && !BiomeDatabase.WaterBiomes.Contains(d.Biome.Name)
                                       && !BiomeDatabase.MountainBiomes.Contains(d.Biome.Name),
                SettlementNoise = v => vertexData.TryGetValue(v, out var d) ? d.SettlementNoise : 0f,
                // A location sits on top of its biome and does not change what it costs to walk
                // across, which is exactly what BiomeTravelDatabase says about locations.
                StepCostDays    = v => vertexData.TryGetValue(v, out var d)
                                       ? BiomeTravelDatabase.GetFor(d.Biome.Name).DurationDays
                                       : 1f,
                Position        = GetVertexPosition,
            });
            sw.Stop();

            int land = Regions.Regions.Sum(r => r.CellCount);
            Console.WriteLine($"[Regions] {Regions.Regions.Count} region(s) over {Regions.LandmassCount} " +
                              $"landmass(es), {land} land cells, in {sw.ElapsedMilliseconds} ms.");
        }

        /// <summary>
        /// Flips the region overlay and repaints every tile. Returns the new state.
        /// </summary>
        public bool ToggleRegionOverlay()
        {
            if (Regions == null)
            {
                Console.WriteLine("[Regions] no world generated yet — nothing to colour.");
                return false;
            }

            RegionOverlayEnabled = !RegionOverlayEnabled;
            RefreshAllTiles();
            Console.WriteLine($"[Regions] overlay {(RegionOverlayEnabled ? "ON" : "OFF")} " +
                              $"({Regions.Regions.Count} regions).");
            return RegionOverlayEnabled;
        }

        /// <summary>
        /// Repaints every tile from its stored data, honouring the travel-range darkening — and
        /// puts the committed travel plan back on top, which the repaint would otherwise erase. The
        /// hovered preview is not restored because it is redrawn by the next mouse move anyway; a
        /// plan the player has already clicked out is not.
        /// </summary>
        private void RefreshAllTiles()
        {
            var path = _plannedPathVertices.ToList();
            var waypoints = _plannedWaypointVertices.ToList();

            for (int i = 0; i < VertexCount; i++)
            {
                if (i == _protagonistVertex) continue;
                if (vertexData.TryGetValue(i, out var data))
                    RestoreVertexData(i, data);
            }

            if (path.Count > 1) ShowPlannedPath(path, waypoints);
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
            // Generate a pseudo-random value based on position for consistency, mixed with
            // the master seed so the same vertex yields different locations across worlds.
            int seed = (int)(position.X * 1000 + position.Y * 2000 + position.Z * 3000);
            var random = new Random(Math.Abs(unchecked(seed ^ GameRng.MasterSeed)));
            
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

            Vector3 sp = pos + _worldNoiseOffset;
            Vector3 p1 = (off1 + sp) / 12f;
            Vector3 p2 = (off2 + sp) / 3f;
            Vector3 p3 = (off3 + sp) / 8f;
            
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
                    SetVertexGlyph(vertexIndex, newGlyph, TileColor(vertexIndex, data), data.Biome.Size);
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
                // Fresh seeded generator each call so the spawn is reproducible per seed
                // (and identical across in-session "new game" resets for the same world).
                var spawnRng = GameRng.For("spawn");
                // suitableVertices comes from dictionary enumeration; sort for a stable
                // ordering so the picked index maps to the same vertex every run.
                suitableVertices.Sort();
                int newVertex = suitableVertices[spawnRng.Next(suitableVertices.Count)];

                // --start-at <name>: spawn on a named biome or location instead. Debug only — a
                // feature that lives in one rare biome is otherwise reachable only by hunting for a
                // seed that happens to put one within travel range of the spawn point.
                if (Config.Debug.StartAt is { Length: > 0 } want)
                {
                    var match = suitableVertices.FirstOrDefault(v =>
                        vertexData.TryGetValue(v, out var d) &&
                        (d.Location?.Name ?? d.Biome.Name).Contains(want, StringComparison.OrdinalIgnoreCase),
                        -1);

                    if (match >= 0)
                    {
                        newVertex = match;
                        Console.WriteLine($"[debug] --start-at \"{want}\" → vertex {match}");
                    }
                    else
                    {
                        Cathedral.Game.DebugFlagAudit.Miss("--start-at", want,
                            "the normal spawn. The world has no such biome in reach — use --location-type to build one regardless");
                    }
                }

                PlaceProtagonist(newVertex, centerCamera: true); // PlaceProtagonist restores the old position first

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

                // Randomly pick farm or village (seeded on vertex index + master seed for
                // per-world determinism)
                LocationType chosen = new Random(unchecked(i ^ GameRng.MasterSeed)).Next(2) == 0 ? farm : village;

                // Place chosen location on candidate and refresh its visual
                var cd = vertexData[candidate];
                cd.Location = chosen;
                cd.GlyphChar = chosen.Glyph;
                cd.Color = new System.Numerics.Vector3(chosen.Color.X, chosen.Color.Y, chosen.Color.Z);
                vertexData[candidate] = cd;
                SetVertexGlyph(candidate, chosen.Glyph, TileColor(candidate, vertexData[candidate]), chosen.Size);
                placed++;
            }

            if (placed > 0)
                Console.WriteLine($"[PostProcess] Placed {placed} farm(s)/village(s) to satisfy field adjacency.");
        }

        /// <summary>
        /// The vertex colour handed to the sphere shader: rgb the shader reduces to a luminance, and
        /// the tile category in the alpha, which is what it actually tints from.
        ///
        /// <para>With the region overlay on, land takes its region's colour instead, under the
        /// alpha that tells the shader to draw the rgb as given rather than re-tint it — the one
        /// place in the game where a vertex colour reaches the screen unchanged. Water is left
        /// alone: purple is the sea's, and a region drawn in it would read as water.</para>
        /// </summary>
        private Vector4 TileColor(int vertexIndex, VertexWorldData data)
        {
            var swatch = RegionSwatchFor(vertexIndex);
            if (swatch != null)
                return new Vector4(swatch.Value.R, swatch.Value.G, swatch.Value.B,
                                   WorldRegionPalette.OverlayCategory);

            return new Vector4(data.Color.X / 255.0f, data.Color.Y / 255.0f, data.Color.Z / 255.0f,
                               GetTileCategory(data));
        }

        /// <summary>
        /// The region swatch this vertex should be drawn in, or null when the overlay is off, no
        /// world has been divided yet, or the vertex is water.
        /// </summary>
        private WorldRegionPalette.Swatch? RegionSwatchFor(int vertexIndex)
            => RegionOverlayEnabled ? Regions?.SwatchAt(vertexIndex) : null;

        private void RestoreVertexData(int vertexIndex, VertexWorldData data)
        {
            if (_outOfRangeVertices.Contains(vertexIndex))
                ApplyDarkening(vertexIndex);
            else
                SetVertexGlyph(vertexIndex, data.GlyphChar, TileColor(vertexIndex, data), data.Location?.Size ?? data.Biome.Size);
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
                    RestoreVertexData(nodeId, d);
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

        /// <summary>
        /// Cancels an in-flight travel without moving the protagonist. Clears the path
        /// visuals (active path glyphs + planned path) and stops the movement animation
        /// while leaving the avatar at its current vertex.
        /// </summary>
        public void CancelTravel()
        {
            ClearTravelPath();
            ClearPlannedPath();
            ClearHoveredPath();
            _currentPath         = null;
            _pendingMovementPath = null;
            _hoveredPath         = null;
            _pendingHoverPath    = null;
            _pathIndex           = 0;
            _moveTimer           = 0.0f;
            MovementPaused       = false;
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

            // Otherwise restore original biome/location appearance (with darkening if out of range).
            if (vertexData.TryGetValue(nodeId, out var data))
                RestoreVertexData(nodeId, data);
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

            for (int i = 1; i < _currentPath.Length; i++)
            {
                int nodeId = _currentPath.GetNode(i);
                if (nodeId != _protagonistVertex && vertexData.TryGetValue(nodeId, out var data))
                    RestoreVertexData(nodeId, data);
            }
        }

        private void UpdateMovement(float deltaTime)
        {
            if (MovementPaused) return;
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
                        int prevNode = _currentPath.GetNode(_pathIndex - 1);
                        if (prevNode != _protagonistVertex)
                            RestoreVertexData(prevNode, prevData);
                    }
                    
                    PlaceProtagonist(nextVertex, centerCamera: true); // Focus camera on protagonist with each step
                    ProtagonistSteppedToVertex?.Invoke(nextVertex);
                    
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
        /// Re-asserts the protagonist glyph on the current vertex.
        /// Call when returning to WorldView to recover from any glyph overwrite that
        /// may have occurred during path cleanup or travel-range recalculation.
        /// </summary>
        public void RefreshProtagonistGlyph()
        {
            if (_protagonistVertex >= 0)
                SetVertexGlyph(_protagonistVertex,
                    Config.GlyphSphere.ProtagonistChar,
                    Config.GlyphSphere.ProtagonistColor,
                    true);
        }

        /// <summary>
        /// Resets the protagonist to a new random starting position.
        /// Used when starting a new game from the main menu.
        /// </summary>
        public void ResetProtagonistPosition()
        {
            ClearMovementState();

            // Re-initialize protagonist at a new random position
            InitializeProtagonist();
            Console.WriteLine($"MicroworldInterface: Protagonist reset to vertex {_protagonistVertex}");
        }

        /// <summary>
        /// Drops every path, hover and in-flight movement. Shared by the two ways a run's position is
        /// decided — <see cref="ResetProtagonistPosition"/> rolls a new spawn,
        /// <see cref="PlaceAvatarAt"/> restores a saved one — because leaving a half-walked path
        /// behind would have the avatar resume the dead run's journey.
        /// </summary>
        public void ClearMovementState()
        {
            // Clear all path visuals before wiping the path references.
            ClearHoveredPath();
            _hoveredVertex = -1;
            ClearPlannedPath();
            ClearTravelPath();
            _hoverPathOrigin = -1;

            // Cancel any in-progress movement
            _currentPath = null;
            _hoveredPath = null;
            _pendingHoverPath = null;
            _pendingMovementPath = null;
            _pathIndex = 0;
            _moveTimer = 0.0f;
            _pendingHoverVertex = -1;
            MovementPaused = false;
        }

        /// <summary>
        /// Throws the world away and builds a new one from the current master seed — a new run in the
        /// same process. Call after <c>GameRng.Reseed</c>.
        ///
        /// <para>The avatar is forgotten first: it stands on a vertex of the old world, and the tile
        /// it was covering is about to be overwritten, so restoring that tile afterwards would stamp a
        /// dead world's terrain onto the new one. <see cref="InitializeProtagonist"/> rolls a fresh
        /// spawn at the end.</para>
        ///
        /// <para>The caller must still re-apply the travel range, which is derived from the
        /// protagonist rather than the world — <c>EnterWorldViewInteractive</c> already does.</para>
        /// </summary>
        public void RegenerateWorld()
        {
            ClearMovementState();
            _protagonistVertex        = -1;
            _originalProtagonistData  = null;
            _outOfRangeVertices.Clear();

            GenerateWorld();
            InitializeProtagonist();
            Console.WriteLine($"MicroworldInterface: world regenerated; protagonist at vertex {_protagonistVertex}");
        }

        /// <summary>
        /// Puts the avatar on <paramref name="vertexIndex"/> — the load counterpart of
        /// <see cref="ResetProtagonistPosition"/>, which re-rolls a random spawn and so cannot restore
        /// a saved position.
        ///
        /// <para>Goes through the same private placement the travel loop uses, so the tile under the
        /// avatar is captured and the previous one restored exactly as in play; a save that wrote the
        /// glyph directly would leave an '@' stamped on the old vertex for the rest of the run.</para>
        ///
        /// <para>Returns false when the vertex is not part of this world, which for a save means it
        /// was written against a different seed.</para>
        /// </summary>
        public bool PlaceAvatarAt(int vertexIndex)
        {
            if (!vertexData.ContainsKey(vertexIndex))
            {
                Console.Error.WriteLine(
                    $"MicroworldInterface: cannot place the avatar at vertex {vertexIndex} — no such vertex.");
                return false;
            }

            ClearMovementState();
            PlaceProtagonist(vertexIndex, centerCamera: true);
            Console.WriteLine($"MicroworldInterface: Avatar restored to vertex {_protagonistVertex}");
            return true;
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

            /// <summary>
            /// The second Perlin layer alone — the one <see cref="DetermineBiome"/> reads for city,
            /// field and forest, where lower is more settled. Kept beside the average because the
            /// region division seeds itself on this layer's peaks, and the average of all three has
            /// the water layer and the mountain layer mixed into it.
            /// </summary>
            public float SettlementNoise;

            public char GlyphChar;
            public System.Numerics.Vector3 Color;
        }

        /// <summary>Travel constraint that blocks every vertex in a fixed exclusion set.</summary>
        private sealed class RangeExclusionConstraint : Cathedral.Pathfinding.ITravelConstraint
        {
            private readonly HashSet<int> _excluded;
            public RangeExclusionConstraint(HashSet<int> excluded) => _excluded = excluded;
            public string Name => "range";
            public bool IsTraversable(int nodeId) => !_excluded.Contains(nodeId);
            public float GetCostMultiplier(int fromNode, int toNode) => 1.0f;
        }
    }
}