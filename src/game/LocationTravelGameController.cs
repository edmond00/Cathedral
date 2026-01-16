using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Cathedral.Glyph;
using Cathedral.Glyph.Microworld;
using Cathedral.Glyph.Microworld.LocationSystem;
using Cathedral.Glyph.Microworld.LocationSystem.Generators;
using Cathedral.Glyph.Interaction;
using Cathedral.LLM;
using Cathedral.Game.Narrative;
using Cathedral.Terminal;

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
    
    // Chain-of-Thought narrative system
    private NarrativeController? _narrativeController = null;
    private bool _isInNarrativeMode = false;
    private SkillSlotManager? _skillSlotManager = null;
    private ThinkingExecutor? _thinkingExecutor = null;
    
    // Game state
    private GameMode _currentMode;
    private LocationInstanceState? _currentLocationState;
    private int _currentLocationVertex = -1;
    private int _destinationVertex = -1;
    
    // Location state storage (keyed by vertex index)
    private readonly Dictionary<int, LocationInstanceState> _locationStates = new();
    
    // Feature generators for different location types
    private readonly Dictionary<string, LocationFeatureGenerator> _generators = new();
    
    // Action executors (used by NarrativeController)
    private LLMActionExecutor? _llmActionExecutor; // Optional - requires LLamaServerManager
    private CriticEvaluator? _criticEvaluator;
    private ActionScorer? _actionScorer;
    
    // Events
    public event Action<GameMode, GameMode>? ModeChanged;
    public event Action<LocationInstanceState>? LocationExited;
    public event Action? TravelStarted;
    public event Action? TravelCompleted;

    // Properties
    public GameMode CurrentMode => _currentMode;
    public LocationInstanceState? CurrentLocationState => _currentLocationState;
    public bool IsAtLocation => _currentMode == GameMode.LocationInteraction && _currentLocationState != null;
    
    /// <summary>
    /// Gets the terminal input handler for coordinate conversion (null if no terminal).
    /// </summary>
    public TerminalInputHandler? GetTerminalInputHandler() => _core.Terminal?.InputHandler;

    public LocationTravelGameController(GlyphSphereCore core, MicroworldInterface microworldInterface)
    {
        _core = core ?? throw new ArgumentNullException(nameof(core));
        _interface = microworldInterface ?? throw new ArgumentNullException(nameof(microworldInterface));
        
        // Validate narrative world coherence at startup
        try
        {
            Cathedral.Game.Narrative.NarrativeWorldValidator.ValidateWorldCoherence();
            Cathedral.Game.Narrative.NarrativeWorldValidator.PrintWorldStructure();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FATAL ERROR: Narrative world validation failed: {ex.Message}");
            throw;
        }
        
        // Initialize LLM action executor (will be set via SetLLMActionExecutor())
        _llmActionExecutor = null;
        
        // Initialize with WorldView mode
        _currentMode = GameMode.WorldView;
        
        // Register location generators
        RegisterGenerator("forest", new ForestFeatureGenerator());
        
        // Wire up events from the microworld interface
        _interface.VertexClickEvent += OnVertexClicked;
        
        // Wire up global mouse click handler for popup interactions
        _core.GlobalMouseClicked += OnGlobalMouseClicked;
        
        // Initialize terminal UI (if terminal is available)
        InitializeTerminalUI();
        
        Console.WriteLine("LocationTravelGameController: Initialized in WorldView mode");
    }
    
    /// <summary>
    /// Updates the game controller (called every frame).
    /// </summary>
    public void Update()
    {
        // Update Phase 6 controller if active
        if (_isInNarrativeMode && _narrativeController != null)
        {
            // If popup is visible, handle all mouse updates here for consistent frame-rate timing
            // This ensures uniform refresh rate across the entire popup (both inside and outside terminal bounds)
            if (_narrativeController.IsPopupVisible && _core.Terminal != null)
            {
                Vector2 rawMouse = _core.Terminal.InputHandler.GetCorrectedMousePosition();
                _narrativeController.OnRawMouseMove(rawMouse);
            }
            
            _narrativeController.Update();
            
            // Check if player requested exit (clicked Continue button)
            if (_narrativeController.HasRequestedExit())
            {
                Console.WriteLine("LocationTravelGameController: Phase 6 exit requested");
                ExitNarrativeMode();
            }
            
            return;
        }
        
        // Update popup terminal with location info
        UpdatePopupTerminal();
    }

    /// <summary>
    /// Sets the LLM action executor for Phase 5.
    /// If not set, falls back to SimpleActionExecutor.
    /// </summary>
    public void SetLLMActionExecutor(LLMActionExecutor executor)
    {
        _llmActionExecutor = executor;
        
        // Initialize SkillSlotManager for Phase 6
        if (executor != null)
        {
            _skillSlotManager = new SkillSlotManager(executor.GetLlamaServerManager());
            var thinkingPromptConstructor = new ThinkingPromptConstructor();
            _thinkingExecutor = new ThinkingExecutor(
                executor.GetLlamaServerManager(), 
                thinkingPromptConstructor, 
                _skillSlotManager);
            Console.WriteLine("LocationTravelGameController: SkillSlotManager and ThinkingExecutor initialized for Phase 6");
        }
        
        // Also initialize Critic and ActionScorer
        if (executor != null)
        {
            _criticEvaluator = new CriticEvaluator(executor.GetLlamaServerManager());
            
            // Initialize Critic asynchronously
            _ = Task.Run(async () =>
            {
                try
                {
                    await _criticEvaluator.InitializeAsync();
                    _actionScorer = new ActionScorer(_criticEvaluator);
                    Console.WriteLine("LocationTravelGameController: Critic evaluator initialized");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"LocationTravelGameController: Failed to initialize Critic - {ex.Message}");
                    _criticEvaluator = null;
                }
            });
        }
        
        Console.WriteLine("LocationTravelGameController: LLM action executor enabled");
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
            _core.Terminal.CellRightClicked += OnTerminalCellRightClicked;
            _core.Terminal.CellHovered += OnTerminalCellHovered;
            _core.Terminal.MouseLeft += OnTerminalMouseLeft;
            
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
        // All location interactions now use Phase 6 narrative mode
        if (_isInNarrativeMode && _narrativeController != null)
        {
            // If popup is visible, use raw mouse coordinates
            if (_narrativeController.IsPopupVisible)
            {
                Vector2 rawMouse = _core.Terminal.InputHandler.GetCorrectedMousePosition();
                _narrativeController.OnRawMouseClick(rawMouse);
                return;
            }
            
            _narrativeController.OnMouseClick(x, y);
            return;
        }
        
        // Note: Legacy non-narrative mode click handling removed - all interactions use NarrativeController
    }
    
    /// <summary>
    /// Handles terminal cell right-clicks for focus observation.
    /// </summary>
    private void OnTerminalCellRightClicked(int x, int y)
    {
        // Only handle in Phase 6 narrative mode
        if (_isInNarrativeMode && _narrativeController != null)
        {
            _narrativeController.OnRightClick(x, y);
        }
    }
    
    /// <summary>
    /// Global mouse click handler - intercepts clicks for popups that extend outside terminal bounds.
    /// </summary>
    private bool OnGlobalMouseClicked(Vector2 mousePosition, MouseButton button)
    {
        // Only intercept when in narrative mode with popup visible
        if (_isInNarrativeMode && _narrativeController != null && _narrativeController.IsPopupVisible)
        {
            // Only handle left clicks for popup selection
            if (button == MouseButton.Left)
            {
                // Apply the same border height correction as GetCorrectedMousePosition()
                // to ensure consistent Y coordinates between hover and click detection
                Vector2 correctedPosition = _core.Terminal?.InputHandler.GetCorrectedMousePosition() ?? mousePosition;
                Console.WriteLine($"LocationTravelGameController: Global click intercepted for popup at corrected position {correctedPosition}");
                _narrativeController.OnRawMouseClick(correctedPosition);
                return true; // Consume the click
            }
        }
        
        return false; // Don't consume - let other handlers process
    }
    
    /// <summary>
    /// Handles terminal cell hover for visual feedback.
    /// </summary>
    private void OnTerminalCellHovered(int x, int y)
    {
        // Phase 6 mode handles hover differently
        if (_isInNarrativeMode && _narrativeController != null)
        {
            // When popup is visible, mouse updates are handled in Update() loop for consistent timing
            // Only handle non-popup interactions here
            if (!_narrativeController.IsPopupVisible)
            {
                _narrativeController.OnMouseMove(x, y);
            }
            return;
        }
        
        // Note: Legacy hover handling removed - all interactions use NarrativeController
    }
    
    /// <summary>
    /// Handles mouse leaving the terminal area.
    /// </summary>
    private void OnTerminalMouseLeft()
    {
        // No action needed - Update() loop handles popup mouse tracking
    }
    
    /// <summary>
    /// Called when mouse wheel is scrolled.
    /// </summary>
    public void OnMouseWheel(float delta)
    {
        // Phase 6 mode handles scrolling
        if (_isInNarrativeMode && _narrativeController != null)
        {
            _narrativeController.OnMouseWheel(delta);
            return;
        }
        
        // Other modes don't have scroll functionality yet
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
        
        Console.WriteLine($"LocationTravelGameController: Mode changed: {oldMode} ↁE{newMode}");
        
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
        // Ignore clicks when Phase 6 narration is active
        if (_isInNarrativeMode)
        {
            Console.WriteLine("LocationTravelGameController: Ignoring world map click during Phase 6 narration");
            return;
        }
        
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
    /// Both named locations and biomes use the same Phase 6 narrative UI.
    /// </summary>
    private void StartLocationInteraction(int vertexIndex, Cathedral.Glyph.Microworld.LocationType locationType)
    {
        Console.WriteLine($"LocationTravelGameController: Starting Phase 6 interaction for location '{locationType.Name}'");
        StartNarrativeInteraction(vertexIndex);
    }

    /// <summary>
    /// Starts biome interaction mode (when there's no specific location).
    /// Both biomes and named locations use the same Phase 6 narrative UI.
    /// </summary>
    private void StartBiomeInteraction(int vertexIndex, Cathedral.Glyph.Microworld.BiomeType biomeType)
    {
        Console.WriteLine($"LocationTravelGameController: Starting Phase 6 interaction for biome '{biomeType.Name}'");
        StartNarrativeInteraction(vertexIndex);
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
        
        // If Phase 6 is active, don't start the old location UI system
        if (_isInNarrativeMode)
        {
            Console.WriteLine("LocationTravelGameController: Phase 6 active, skipping old location UI");
            
            // Show terminal for Phase 6 interaction
            if (_core.Terminal != null)
            {
                _core.Terminal.Visible = true;
            }
            
            return;
        }
        
        // Mode 6 doesn't need to reset conversation histories (uses NarrativeController architecture)
        
        // Show terminal for interaction
        if (_core.Terminal != null)
        {
            _core.Terminal.Visible = true;
        }
        
        // Note: RenderLocationUI was removed - Phase 6 narrative mode handles all rendering via NarrativeController
    }

    /// <summary>
    /// Regenerates actions based on current state.
    /// </summary>
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
    
    /// <summary>
    /// Gets the location name at the specified vertex index.
    /// Returns null if no location exists or if vertex is invalid.
    /// </summary>
    public string? GetLocationNameAtVertex(int vertexIndex)
    {
        if (vertexIndex < 0)
            return null;
            
        var (biome, location, noise) = _interface.GetDetailedBiomeInfoAt(vertexIndex);
        
        if (location.HasValue)
        {
            return location.Value.Name;
        }
        
        // Return biome name as fallback
        return biome.Name;
    }
    
    /// <summary>
    /// Updates the popup terminal with location info based on hovered vertex.
    /// Should be called every frame or when hover changes.
    /// </summary>
    public void UpdatePopupTerminal()
    {
        if (_core.PopupTerminal == null)
            return;
            
        // Clear popup by default
        _core.PopupTerminal.Clear();
        
        // Only show popup during WorldView mode (for travel destination selection)
        if (_currentMode != GameMode.WorldView)
            return;
        
        // Get hovered vertex from core
        int hoveredVertex = _core.HoveredVertexIndex;
        
        // If no vertex is hovered or hovering over invalid vertex, leave popup empty
        if (hoveredVertex < 0)
            return;
        
        // Get location name at hovered vertex
        string? locationName = GetLocationNameAtVertex(hoveredVertex);
        
        if (!string.IsNullOrEmpty(locationName))
        {
            // Draw location name centered in the popup with white text on black background
            // Only cells with text will have black background, others remain transparent
            int centerY = _core.PopupTerminal.Height / 2;
            _core.PopupTerminal.DrawCenteredText(centerY, locationName, 
                Cathedral.Terminal.Utils.Colors.White, 
                Cathedral.Terminal.Utils.Colors.Black);
        }
    }
    
    /// <summary>
    /// Starts Phase 6 Chain-of-Thought forest interaction.
    /// </summary>
    private void StartNarrativeInteraction(int vertexIndex)
    {
        if (_core.Terminal == null || _core.PopupTerminal == null || _llmActionExecutor == null || _skillSlotManager == null)
        {
            Console.Error.WriteLine("NarrativeController: Cannot start - missing dependencies");
            return;
        }
        
        try
        {
            // Get terminal input handler for coordinate conversion
            var inputHandler = GetTerminalInputHandler();
            if (inputHandler is null)
            {
                Console.WriteLine("LocationTravelGameController: Cannot enter Phase 6 mode - no terminal input handler");
                return;
            }
            
            // Ensure ThinkingExecutor is initialized
            if (_thinkingExecutor is null)
            {
                Console.WriteLine("LocationTravelGameController: Cannot enter Phase 6 mode - ThinkingExecutor not initialized");
                return;
            }
            
            if (_criticEvaluator == null || _actionScorer == null)
            {
                Console.WriteLine("LocationTravelGameController: Cannot enter Phase 6 mode - Critic/ActionScorer not initialized");
                return;
            }
            
            // Create Action Execution Controller dependencies
            var difficultyEvaluator = new ActionDifficultyEvaluator(_criticEvaluator);
            var outcomeApplicator = new OutcomeApplicator();
            var outcomeNarrator = new OutcomeNarrator(
                _llmActionExecutor.GetLlamaServerManager(),
                _skillSlotManager
            );
            
            // Create a temporary Avatar for Phase 6 (will be initialized in NarrativeController)
            var avatar = new Avatar();
            avatar.InitializeSkills(SkillRegistry.Instance, skillCount: 50);
            
            var actionExecutor = new ActionExecutionController(
                _actionScorer,
                difficultyEvaluator,
                outcomeNarrator,
                outcomeApplicator,
                avatar
            );
            
            // Create Phase 6 controller
            _narrativeController = new NarrativeController(
                _core.Terminal,
                _core.PopupTerminal,
                _core,
                _llmActionExecutor.GetLlamaServerManager(),
                _skillSlotManager,
                inputHandler,
                _thinkingExecutor,
                actionExecutor
            );
            
            // Mark as active
            _isInNarrativeMode = true;
            _currentLocationVertex = vertexIndex;
            
            // Disable world map interactions while narration UI is active
            _interface.SetWorldInteractionsEnabled(false);
            _core.SetWorldInteractionsEnabled(false);
            
            // Set mode to LocationInteraction
            SetMode(GameMode.LocationInteraction);
            
            // Start observation phase (async)
            _narrativeController.StartObservationPhase();
            
            Console.WriteLine("LocationTravelGameController: Phase 6 forest interaction started");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LocationTravelGameController: Failed to start Phase 6: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            
            // Fallback to normal interaction
            _isInNarrativeMode = false;
            _narrativeController = null;
        }
    }
    
    /// <summary>
    /// Exits Phase 6 mode and returns to world view.
    /// </summary>
    public void ExitNarrativeMode()
    {
        if (!_isInNarrativeMode)
            return;
        
        Console.WriteLine("LocationTravelGameController: Exiting Phase 6 mode");
        
        // Re-enable world map and 3D interactions
        _interface.SetWorldInteractionsEnabled(true);
        _core.SetWorldInteractionsEnabled(true);
        
        _isInNarrativeMode = false;
        _narrativeController = null;
        _currentLocationVertex = -1;
        
        SetMode(GameMode.WorldView);
    }
    
    /// <summary>
    /// Closes the Phase 6 thinking skill popup if it's open.
    /// Returns true if popup was closed, false otherwise.
    /// </summary>
    public bool CloseNarrativePopup()
    {
        if (_isInNarrativeMode && _narrativeController != null)
        {
            return _narrativeController.ClosePopup();
        }
        return false;
    }

    public void Dispose()
    {
        // Unsubscribe from events
        if (_interface != null)
        {
            _interface.VertexClickEvent -= OnVertexClicked;
        }
        
        // Unsubscribe from global mouse click handler
        if (_core != null)
        {
            _core.GlobalMouseClicked -= OnGlobalMouseClicked;
        }
        
        // Dispose Critic evaluator
        _criticEvaluator?.Dispose();
        
        Console.WriteLine("LocationTravelGameController: Disposed");
    }
}
