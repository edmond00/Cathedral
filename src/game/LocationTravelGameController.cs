using System;
using System.Collections.Generic;
using Cathedral.Glyph;
using Cathedral.Glyph.Microworld;
using Cathedral.Glyph.Microworld.LocationSystem;
using Cathedral.Glyph.Microworld.LocationSystem.Generators;
using Cathedral.Glyph.Interaction;

namespace Cathedral.Game;

/// <summary>
/// Main game state controller that coordinates the Location Travel Mode.
/// Manages transitions between game modes and maintains location state.
/// </summary>
public class LocationTravelGameController : IDisposable
{
    // Core systems
    private readonly GlyphSphereCore _core;
    private readonly MicroworldInterface _interface;
    private TerminalLocationUI? _terminalUI;
    
    // Game state
    private GameMode _currentMode;
    private LocationInstanceState? _currentLocationState;
    private int _currentLocationVertex = -1;
    private int _destinationVertex = -1;
    
    // Location state storage (keyed by vertex index)
    private readonly Dictionary<int, LocationInstanceState> _locationStates = new();
    
    // Feature generators for different location types
    private readonly Dictionary<string, LocationFeatureGenerator> _generators = new();
    
    // UI state for interaction
    private List<string> _currentActions = new();
    private string _currentNarrative = "";
    
    // Events
    public event Action<GameMode, GameMode>? ModeChanged;
    public event Action<LocationInstanceState>? LocationEntered;
    public event Action<LocationInstanceState>? LocationExited;
    public event Action? TravelStarted;
    public event Action? TravelCompleted;

    // Properties
    public GameMode CurrentMode => _currentMode;
    public LocationInstanceState? CurrentLocationState => _currentLocationState;
    public bool IsAtLocation => _currentMode == GameMode.LocationInteraction && _currentLocationState != null;

    public LocationTravelGameController(GlyphSphereCore core, MicroworldInterface microworldInterface)
    {
        _core = core ?? throw new ArgumentNullException(nameof(core));
        _interface = microworldInterface ?? throw new ArgumentNullException(nameof(microworldInterface));
        
        // Initialize with WorldView mode
        _currentMode = GameMode.WorldView;
        
        // Register location generators
        RegisterGenerator("forest", new ForestFeatureGenerator());
        
        // Wire up events from the microworld interface
        _interface.VertexClickEvent += OnVertexClicked;
        
        // Initialize terminal UI (if terminal is available)
        InitializeTerminalUI();
        
        Console.WriteLine("LocationTravelGameController: Initialized in WorldView mode");
    }
    
    /// <summary>
    /// Initializes the terminal UI and wires up events.
    /// </summary>
    private void InitializeTerminalUI()
    {
        if (_core.Terminal == null)
        {
            Console.WriteLine("LocationTravelGameController: No terminal available, UI disabled");
            return;
        }
        
        try
        {
            _terminalUI = new TerminalLocationUI(_core.Terminal);
            
            // Wire up terminal events for action selection
            _core.Terminal.CellClicked += OnTerminalCellClicked;
            _core.Terminal.CellHovered += OnTerminalCellHovered;
            
            Console.WriteLine("LocationTravelGameController: Terminal UI initialized");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LocationTravelGameController: Failed to initialize terminal UI - {ex.Message}");
            _terminalUI = null;
        }
    }
    
    /// <summary>
    /// Handles terminal cell clicks for action selection.
    /// </summary>
    private void OnTerminalCellClicked(int x, int y)
    {
        if (_currentMode != GameMode.LocationInteraction || _terminalUI == null)
            return;
        
        int? actionIndex = _terminalUI.GetHoveredAction(x, y);
        if (actionIndex.HasValue && actionIndex.Value >= 0 && actionIndex.Value < _currentActions.Count)
        {
            Console.WriteLine($"LocationTravelGameController: Action {actionIndex.Value + 1} selected: {_currentActions[actionIndex.Value]}");
            ExecuteAction(actionIndex.Value);
        }
    }
    
    /// <summary>
    /// Handles terminal cell hover for visual feedback.
    /// </summary>
    private void OnTerminalCellHovered(int x, int y)
    {
        if (_currentMode != GameMode.LocationInteraction || _terminalUI == null)
            return;
        
        _terminalUI.UpdateHover(x, y, _currentActions);
    }
    
    /// <summary>
    /// Executes a selected action.
    /// </summary>
    private void ExecuteAction(int actionIndex)
    {
        if (_currentLocationState == null || actionIndex < 0 || actionIndex >= _currentActions.Count)
            return;
        
        string action = _currentActions[actionIndex];
        
        // TODO: This will be replaced with LLM-generated results in Phase 5
        // For now, just show a placeholder result
        _terminalUI?.ShowResultMessage($"You chose: {action}\n\n(Action execution will be implemented in Phase 5 with LLM integration)", true);
        
        Console.WriteLine($"LocationTravelGameController: Executed action - {action}");
        
        // TODO: Update location state based on action result
        // TODO: Check for failure conditions
        // TODO: Generate new actions based on result
    }

    /// <summary>
    /// Registers a location feature generator for a specific location type.
    /// </summary>
    public void RegisterGenerator(string locationType, LocationFeatureGenerator generator)
    {
        _generators[locationType] = generator;
        Console.WriteLine($"LocationTravelGameController: Registered generator for '{locationType}'");
    }

    /// <summary>
    /// Sets the current game mode and triggers appropriate transitions.
    /// </summary>
    public void SetMode(GameMode newMode)
    {
        if (_currentMode == newMode)
            return;

        var oldMode = _currentMode;
        _currentMode = newMode;
        
        Console.WriteLine($"LocationTravelGameController: Mode changed: {oldMode} → {newMode}");
        
        // Handle mode-specific setup
        switch (newMode)
        {
            case GameMode.WorldView:
                OnEnterWorldView();
                break;
                
            case GameMode.Traveling:
                OnEnterTraveling();
                break;
                
            case GameMode.LocationInteraction:
                OnEnterLocationInteraction();
                break;
        }
        
        ModeChanged?.Invoke(oldMode, newMode);
    }

    /// <summary>
    /// Handles vertex click events from the glyph sphere.
    /// </summary>
    private void OnVertexClicked(int vertexIndex, char glyph, OpenTK.Mathematics.Vector4 color, float noise)
    {
        // Only process clicks in WorldView mode
        if (_currentMode != GameMode.WorldView)
        {
            Console.WriteLine($"LocationTravelGameController: Ignoring click in {_currentMode} mode");
            return;
        }

        // Check if this vertex has a location
        var (location, biome) = _interface.GetCurrentLocationInfo();
        
        // For now, we'll treat the clicked vertex as a potential destination
        // In the future, we should check if it actually has a location
        Console.WriteLine($"LocationTravelGameController: Vertex {vertexIndex} clicked");
        
        // Check if the avatar is at a location (not just any vertex)
        var avatarVertex = _interface.GetAvatarVertex();
        if (avatarVertex == vertexIndex)
        {
            Console.WriteLine("LocationTravelGameController: Clicked on avatar's current position");
            
            // Enter interaction mode - use location if available, otherwise use biome
            var locationInfo = _interface.GetDetailedBiomeInfoAt(vertexIndex);
            if (locationInfo.location.HasValue)
            {
                Console.WriteLine($"LocationTravelGameController: Entering location '{locationInfo.location.Value.Name}'");
                _currentLocationVertex = vertexIndex;
                StartLocationInteraction(vertexIndex, locationInfo.location.Value);
            }
            else
            {
                Console.WriteLine($"LocationTravelGameController: No specific location, entering biome '{locationInfo.biome.Name}'");
                _currentLocationVertex = vertexIndex;
                StartBiomeInteraction(vertexIndex, locationInfo.biome);
            }
        }
        else
        {
            // Clicked on a different vertex - start travel
            StartTravel(vertexIndex);
        }
    }

    /// <summary>
    /// Starts travel to a destination vertex.
    /// </summary>
    private void StartTravel(int destinationVertex)
    {
        _destinationVertex = destinationVertex;
        SetMode(GameMode.Traveling);
        TravelStarted?.Invoke();
        
        Console.WriteLine($"LocationTravelGameController: Starting travel to vertex {destinationVertex}");
        
        // The MicroworldInterface already handles pathfinding and movement
        // We just need to wait for arrival notification
    }

    /// <summary>
    /// Called when avatar arrives at a vertex.
    /// This should be called by MicroworldInterface when movement completes.
    /// </summary>
    public void OnAvatarArrived(int vertexIndex)
    {
        if (_currentMode != GameMode.Traveling)
            return;

        Console.WriteLine($"LocationTravelGameController: Avatar arrived at vertex {vertexIndex}");
        
        TravelCompleted?.Invoke();
        
        // Enter interaction mode - use location if available, otherwise use biome
        var locationInfo = _interface.GetDetailedBiomeInfoAt(vertexIndex);
        if (locationInfo.location.HasValue)
        {
            Console.WriteLine($"LocationTravelGameController: Location found: {locationInfo.location.Value.Name}");
            _currentLocationVertex = vertexIndex;
            StartLocationInteraction(vertexIndex, locationInfo.location.Value);
        }
        else
        {
            Console.WriteLine($"LocationTravelGameController: No specific location, entering biome '{locationInfo.biome.Name}'");
            _currentLocationVertex = vertexIndex;
            StartBiomeInteraction(vertexIndex, locationInfo.biome);
        }
    }

    /// <summary>
    /// Starts location interaction mode.
    /// </summary>
    private void StartLocationInteraction(int vertexIndex, Cathedral.Glyph.Microworld.LocationType locationType)
    {
        // Get or create location state
        if (!_locationStates.TryGetValue(vertexIndex, out var locationState))
        {
            // Generate a blueprint for this location
            var locationId = $"location_{vertexIndex}";
            
            // For now, use forest generator for all locations
            // TODO: Map location types to appropriate generators
            var generator = _generators.GetValueOrDefault("forest") ?? _generators.Values.First();
            var blueprint = generator.GenerateBlueprint(locationId);
            
            // Create initial state
            locationState = LocationInstanceState.FromBlueprint(locationId, blueprint);
            _locationStates[vertexIndex] = locationState;
            
            Console.WriteLine($"LocationTravelGameController: Created new location state for {locationId}");
        }
        else
        {
            // Existing location - increment visit count
            locationState = locationState.WithNewVisit();
            _locationStates[vertexIndex] = locationState;
            
            Console.WriteLine($"LocationTravelGameController: Returning to existing location (visit #{locationState.VisitCount})");
        }

        _currentLocationState = locationState;
        SetMode(GameMode.LocationInteraction);
        LocationEntered?.Invoke(locationState);
    }

    /// <summary>
    /// Starts biome interaction mode (when there's no specific location).
    /// Treats the biome itself as an interactive location.
    /// </summary>
    private void StartBiomeInteraction(int vertexIndex, Cathedral.Glyph.Microworld.BiomeType biomeType)
    {
        // Get or create location state for this biome
        if (!_locationStates.TryGetValue(vertexIndex, out var locationState))
        {
            // Generate a blueprint for this biome as a location
            var locationId = $"{biomeType.Name}_{vertexIndex}";
            
            // Use the biome name to select appropriate generator
            // Default to forest generator if biome type not found
            var generatorKey = _generators.ContainsKey(biomeType.Name) ? biomeType.Name : "forest";
            var generator = _generators.GetValueOrDefault(generatorKey) ?? _generators.Values.First();
            var blueprint = generator.GenerateBlueprint(locationId);
            
            // Create initial state
            locationState = LocationInstanceState.FromBlueprint(locationId, blueprint);
            _locationStates[vertexIndex] = locationState;
            
            Console.WriteLine($"LocationTravelGameController: Created new biome interaction state for {biomeType.Name} at vertex {vertexIndex}");
        }
        else
        {
            // Existing location - increment visit count
            locationState = locationState.WithNewVisit();
            _locationStates[vertexIndex] = locationState;
            
            Console.WriteLine($"LocationTravelGameController: Returning to existing biome (visit #{locationState.VisitCount})");
        }

        _currentLocationState = locationState;
        SetMode(GameMode.LocationInteraction);
        LocationEntered?.Invoke(locationState);
    }

    /// <summary>
    /// Ends the current location interaction and returns to world view.
    /// </summary>
    public void EndLocationInteraction()
    {
        if (_currentMode != GameMode.LocationInteraction)
            return;

        Console.WriteLine("LocationTravelGameController: Ending location interaction");
        
        var exitedLocation = _currentLocationState;
        _currentLocationState = null;
        _currentLocationVertex = -1;
        
        SetMode(GameMode.WorldView);
        
        if (exitedLocation != null)
        {
            LocationExited?.Invoke(exitedLocation);
        }
    }

    /// <summary>
    /// Updates the current location state (called after actions).
    /// </summary>
    public void UpdateLocationState(LocationInstanceState newState)
    {
        if (_currentMode != GameMode.LocationInteraction)
        {
            Console.WriteLine("LocationTravelGameController: Cannot update location state outside LocationInteraction mode");
            return;
        }

        _currentLocationState = newState;
        
        // Update stored state
        if (_currentLocationVertex >= 0)
        {
            _locationStates[_currentLocationVertex] = newState;
        }
    }

    /// <summary>
    /// Gets the blueprint for the current location.
    /// </summary>
    public LocationBlueprint? GetCurrentLocationBlueprint()
    {
        if (_currentLocationState == null)
            return null;

        var generator = _generators.GetValueOrDefault(_currentLocationState.LocationType);
        if (generator == null)
        {
            Console.WriteLine($"LocationTravelGameController: No generator found for type '{_currentLocationState.LocationType}'");
            return null;
        }

        return generator.GenerateBlueprint(_currentLocationState.LocationId);
    }

    // Mode entry handlers
    private void OnEnterWorldView()
    {
        Console.WriteLine("LocationTravelGameController: Entered WorldView mode");
        // Hide or minimize terminal
        if (_core.Terminal != null)
        {
            _core.Terminal.Visible = false;
        }
    }

    private void OnEnterTraveling()
    {
        Console.WriteLine("LocationTravelGameController: Entered Traveling mode");
        // Could show travel info in terminal
    }

    private void OnEnterLocationInteraction()
    {
        Console.WriteLine("LocationTravelGameController: Entered LocationInteraction mode");
        
        // Show terminal for interaction
        if (_core.Terminal != null)
        {
            _core.Terminal.Visible = true;
        }
        
        // Render initial location UI
        if (_terminalUI != null && _currentLocationState != null)
        {
            RenderLocationUI();
        }
    }
    
    /// <summary>
    /// Renders the location UI with current state.
    /// </summary>
    private void RenderLocationUI()
    {
        if (_terminalUI == null || _currentLocationState == null)
            return;
        
        // Get location blueprint
        var blueprint = GetCurrentLocationBlueprint();
        if (blueprint == null)
        {
            Console.WriteLine("RenderLocationUI: Cannot render UI - no blueprint available");
            return;
        }
        
        // Generate mock actions for now (will be replaced with LLM in Phase 5)
        _currentActions = GenerateMockActions();
        
        // Generate mock narrative
        _currentNarrative = GenerateMockNarrative(blueprint);
        
        // Get current state info
        string locationName = blueprint.LocationType; // Using type as name for now
        string sublocation = _currentLocationState.CurrentSublocation;
        int turnCount = _currentLocationState.CurrentTurnCount;
        
        // Get environmental states
        string timeOfDay = _currentLocationState.CurrentStates.GetValueOrDefault("time_of_day", "midday");
        string weather = _currentLocationState.CurrentStates.GetValueOrDefault("weather", "clear");
        
        // Render complete UI
        _terminalUI.RenderComplete(
            locationName: locationName,
            sublocation: sublocation,
            turnCount: turnCount,
            narrative: _currentNarrative,
            actions: _currentActions,
            timeOfDay: timeOfDay,
            weather: weather
        );
        
        Console.WriteLine($"LocationTravelGameController: Terminal UI rendered for {locationName} ({sublocation})");
    }
    
    /// <summary>
    /// Generates mock actions for testing (will be replaced with LLM in Phase 5).
    /// </summary>
    private List<string> GenerateMockActions()
    {
        return new List<string>
        {
            "Examine the surrounding area carefully",
            "Search for useful items or resources",
            "Look for signs of wildlife or other travelers",
            "Rest and observe the environment for a while",
            "Continue deeper into the forest along the main path",
            "Return to world view (ESC also works)"
        };
    }
    
    /// <summary>
    /// Generates mock narrative for testing (will be replaced with LLM in Phase 5).
    /// </summary>
    private string GenerateMockNarrative(LocationBlueprint blueprint)
    {
        return $"You find yourself at a {blueprint.LocationType}. The {_currentLocationState?.CurrentStates.GetValueOrDefault("weather", "clear")} sky filters through the canopy above. " +
               $"Birds chirp in the distance, and the path ahead splits in multiple directions. " +
               $"What will you do?\n\n" +
               $"(This is a mock narrative. LLM-generated narratives will be added in Phase 5.)";
    }

    /// <summary>
    /// Gets debug information about current state.
    /// </summary>
    public string GetDebugInfo()
    {
        var info = $"=== Location Travel Game Controller ===\n";
        info += $"Current Mode: {_currentMode}\n";
        info += $"Current Location: {_currentLocationState?.ToString() ?? "None"}\n";
        info += $"Location Vertex: {_currentLocationVertex}\n";
        info += $"Destination Vertex: {_destinationVertex}\n";
        info += $"Cached Locations: {_locationStates.Count}\n";
        info += $"Registered Generators: {string.Join(", ", _generators.Keys)}\n";
        return info;
    }

    public void Dispose()
    {
        // Unsubscribe from events
        if (_interface != null)
        {
            _interface.VertexClickEvent -= OnVertexClicked;
        }
        
        Console.WriteLine("LocationTravelGameController: Disposed");
    }
}
