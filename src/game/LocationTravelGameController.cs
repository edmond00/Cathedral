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
using Cathedral.Game.Npc;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Plain;
using Cathedral.Game.Creation;
using Cathedral.Game.Management;
using Cathedral.Game.Dialogue;
using Cathedral.Fight;
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
    private ModusMentisSlotManager? _modusMentisSlotManager = null;
    private ThinkingExecutor? _thinkingExecutor = null;
    
    // Embedded fight/dialogue adapters
    private FightModeAdapter? _fightAdapter = null;
    private DialogueModeAdapter? _dialogueAdapter = null;
    
    // LLM loading screen
    private LLMLoadingRenderer? _llmLoadingRenderer;
    private volatile bool _llmBecameReady = false;
    private volatile float _llmLoadProgress = 0f;
    private string _llmLoadStatus = "Starting...";
    private readonly object _llmLoadLock = new object();

    // Main menu
    private MainMenuRenderer? _mainMenuRenderer;
    private bool _hasGameStarted = false;
    
    // Protagonist creation
    private ProtagonistCreationRenderer? _protagonistCreationRenderer;
    private BodyArtData? _bodyArtData;
    private Protagonist? _protagonist;
    
    // Protagonist management
    private ManagementMenuRenderer? _managementMenuRenderer;
    
    // Game state
    private GameMode _currentMode;
    private LocationInstanceState? _currentLocationState;
    private int _currentLocationVertex = -1;
    private int _destinationVertex = -1;
    
    // Location state storage (keyed by vertex index)
    private readonly Dictionary<int, LocationInstanceState> _locationStates = new();
    
    // Feature generators for different location types
    private readonly Dictionary<string, LocationFeatureGenerator> _generators = new();
    
    // Narration graph factories for different biomes
    private readonly Dictionary<string, NarrationGraphFactory> _narrationFactories = new();
    
    // Action executors (used by NarrativeController)
    private LLMActionExecutor? _llmActionExecutor; // Optional - requires LLamaServerManager
    private CriticEvaluator? _criticEvaluator;
    
    // Events
    public event Action<GameMode, GameMode>? ModeChanged;
    public event Action<LocationInstanceState>? LocationExited;
    public event Action? TravelStarted;
    public event Action? TravelCompleted;

    // Properties
    public GameMode CurrentMode => _currentMode;
    public LocationInstanceState? CurrentLocationState => _currentLocationState;
    public bool IsAtLocation => _currentMode == GameMode.LocationInteraction && _currentLocationState != null;
    public bool HasGameStarted => _hasGameStarted;
    
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
        
        // Initialize with WorldView as default (SetMode(MainMenu) will transition properly)
        _currentMode = GameMode.WorldView;
        
        // Register location generators
        RegisterGenerator("forest", new ForestFeatureGenerator());
        
        // Register narration graph factories for biomes
        // Note: plain biome uses the Scene system (PlainSceneFactory) via the fallback path below
        
        // Wire up events from the microworld interface
        _interface.VertexClickEvent += OnVertexClicked;
        
        // Wire up global mouse click handler for popup interactions
        _core.GlobalMouseClicked += OnGlobalMouseClicked;
        
        // Initialize terminal UI (if terminal is available)
        InitializeTerminalUI();
        
        // Show main menu on startup
        SetMode(GameMode.MainMenu);
        
        Console.WriteLine("LocationTravelGameController: Initialized in MainMenu mode");
    }
    
    /// <summary>
    /// Updates the game controller (called every frame).
    /// </summary>
    /// <summary>
    /// Called from the ServerReady event (background thread) when the LLM model finishes loading.
    /// The actual mode transition happens on the next Update() tick (main thread).
    /// </summary>
    public void NotifyLLMReady()
    {
        _llmBecameReady = true;
    }

    /// <summary>
    /// Thread-safe update of LLM loading progress. Safe to call from background threads.
    /// </summary>
    public void UpdateLLMProgress(float progress, string status)
    {
        lock (_llmLoadLock)
        {
            _llmLoadProgress = progress;
            _llmLoadStatus   = status;
        }
    }

    public void Update()
    {
        // If LLM finished loading, transition to main menu (executed on main thread)
        if (_llmBecameReady)
        {
            _llmBecameReady = false;
            if (_currentMode == GameMode.LLMLoading)
            {
                // Show 100% briefly then transition
                _llmLoadingRenderer?.Update(1.0f, "Model loaded!");
                SetMode(GameMode.MainMenu);
            }
            return;
        }

        // Animate the LLM loading screen every frame
        if (_currentMode == GameMode.LLMLoading)
        {
            float progress;
            string status;
            lock (_llmLoadLock)
            {
                progress = _llmLoadProgress;
                status   = _llmLoadStatus;
            }
            _llmLoadingRenderer?.Update(progress, status);
            return;
        }

        // Update protagonist creation blink animation
        if (_currentMode == GameMode.ProtagonistCreation && _protagonistCreationRenderer != null)
        {
            _protagonistCreationRenderer.Update();
        }
        
        // Update management menu animation
        if (_currentMode == GameMode.ProtagonistManagement && _managementMenuRenderer != null)
        {
            _managementMenuRenderer.Update();
            return; // Management mode owns the popup (e.g. inventory drag); skip UpdatePopupTerminal
        }

        // Update Phase 6 controller if active
        if (_isInNarrativeMode && _narrativeController != null)
        {
            // Check if fight/dialogue mode is active (sub-modes within narrative)
            if (_currentMode == GameMode.Fighting && _fightAdapter != null)
            {
                _fightAdapter.Update(1.0 / 60.0); // Approximate delta for 60 FPS
                
                if (_fightAdapter.IsOver)
                {
                    OnFightCompleted();
                }
                return;
            }
            
            if (_currentMode == GameMode.Dialogue && _dialogueAdapter != null)
            {
                _dialogueAdapter.Update();
                
                if (_dialogueAdapter.HasRequestedExit)
                {
                    OnDialogueCompleted();
                }
                return;
            }
            
            // If popup is visible, handle all mouse updates here for consistent frame-rate timing
            // This ensures uniform refresh rate across the entire popup (both inside and outside terminal bounds)
            if (_narrativeController.IsPopupVisible && _core.Terminal != null)
            {
                Vector2 rawMouse = _core.Terminal.InputHandler.GetCorrectedMousePosition();
                _narrativeController.OnRawMouseMove(rawMouse);
            }
            
            _narrativeController.Update();
            
            // Check if narrative controller wants to enter fight mode
            if (_narrativeController.PendingFightOutcome != null)
            {
                StartFightMode(_narrativeController.PendingFightOutcome);
                _narrativeController.ClearPendingFight();
                return;
            }
            
            // Check if narrative controller wants to enter dialogue mode
            if (_narrativeController.PendingDialogueOutcome != null)
            {
                StartDialogueMode(_narrativeController.PendingDialogueOutcome);
                _narrativeController.ClearPendingDialogue();
                return;
            }
            
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
        
        // Initialize ModusMentisSlotManager for Phase 6
        if (executor != null)
        {
            _modusMentisSlotManager = new ModusMentisSlotManager(executor.GetLlamaServerManager());
            var thinkingPromptConstructor = new ThinkingPromptConstructor();
            _thinkingExecutor = new ThinkingExecutor(
                executor.GetLlamaServerManager(), 
                thinkingPromptConstructor, 
                _modusMentisSlotManager);
            Console.WriteLine("LocationTravelGameController: ModusMentisSlotManager and ThinkingExecutor initialized for Phase 6");
        }
        
        // Initialize Critic evaluator
        if (executor != null)
        {
            _criticEvaluator = new CriticEvaluator(executor.GetLlamaServerManager());

            _ = Task.Run(async () =>
            {
                try
                {
                    await _criticEvaluator.InitializeAsync();
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
            _core.Terminal.CellMouseReleased += OnTerminalCellMouseReleased;
            
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
        // Main menu handles its own clicks
        if (_currentMode == GameMode.MainMenu && _mainMenuRenderer != null)
        {
            _mainMenuRenderer.OnMouseClick(x, y);
            return;
        }
        
        // Protagonist creation handles its own clicks
        if (_currentMode == GameMode.ProtagonistCreation && _protagonistCreationRenderer != null)
        {
            _protagonistCreationRenderer.OnMouseClick(x, y);
            return;
        }
        
        // Protagonist management handles its own clicks
        if (_currentMode == GameMode.ProtagonistManagement && _managementMenuRenderer != null)
        {
            _managementMenuRenderer.OnMouseClick(x, y);
            return;
        }
        
        // All location interactions now use Phase 6 narrative mode
        if (_isInNarrativeMode && _narrativeController != null)
        {
            // Route to fight adapter if in fight mode
            if (_currentMode == GameMode.Fighting && _fightAdapter != null)
            {
                _fightAdapter.OnCellClicked(x, y);
                return;
            }
            
            // Route to dialogue adapter if in dialogue mode
            if (_currentMode == GameMode.Dialogue && _dialogueAdapter != null)
            {
                _dialogueAdapter.OnMouseClick(x, y);
                return;
            }
            
            // If popup is visible, use raw mouse coordinates
            if (_narrativeController.IsPopupVisible)
            {
                Vector2 rawMouse = _core.Terminal?.InputHandler.GetCorrectedMousePosition() ?? OpenTK.Mathematics.Vector2.Zero;
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
        // Protagonist creation handles right-clicks for score decrement
        if (_currentMode == GameMode.ProtagonistCreation && _protagonistCreationRenderer != null)
        {
            _protagonistCreationRenderer.OnRightClick(x, y);
            return;
        }
        
        // Protagonist management handles right-clicks
        if (_currentMode == GameMode.ProtagonistManagement && _managementMenuRenderer != null)
        {
            _managementMenuRenderer.OnRightClick(x, y);
            return;
        }
        
        // Only handle in Phase 6 narrative mode
        if (_isInNarrativeMode && _narrativeController != null)
        {
            _narrativeController.OnRightClick(x, y);
        }
    }

    /// <summary>
    /// Handles mouse-up events for drag-and-drop in management inventory.
    /// </summary>
    private void OnTerminalCellMouseReleased(int x, int y)
    {
        if (_currentMode == GameMode.ProtagonistManagement && _managementMenuRenderer != null)
        {
            _managementMenuRenderer.OnMouseUp(x, y);
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
                // Get mouse position for popup hit detection
                Vector2 correctedPosition = _core.Terminal?.InputHandler.GetCorrectedMousePosition() ?? mousePosition;
                Console.WriteLine($"LocationTravelGameController: Global click intercepted for popup at position {correctedPosition}");
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
        // Main menu handles its own hover
        if (_currentMode == GameMode.MainMenu && _mainMenuRenderer != null)
        {
            _mainMenuRenderer.OnMouseMove(x, y);
            return;
        }
        
        // Protagonist creation handles its own hover
        if (_currentMode == GameMode.ProtagonistCreation && _protagonistCreationRenderer != null)
        {
            _protagonistCreationRenderer.OnMouseMove(x, y);
            return;
        }
        
        // Protagonist management handles its own hover
        if (_currentMode == GameMode.ProtagonistManagement && _managementMenuRenderer != null)
        {
            _managementMenuRenderer.OnMouseMove(x, y);
            return;
        }
        
        // Phase 6 mode handles hover differently
        if (_isInNarrativeMode && _narrativeController != null)
        {
            // Route to fight adapter if in fight mode
            if (_currentMode == GameMode.Fighting && _fightAdapter != null)
            {
                _fightAdapter.OnCellHovered(x, y);
                return;
            }
            
            // Route to dialogue adapter if in dialogue mode
            if (_currentMode == GameMode.Dialogue && _dialogueAdapter != null)
            {
                _dialogueAdapter.OnMouseMove(x, y);
                return;
            }
            
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
            if (_currentMode == GameMode.Fighting && _fightAdapter != null)
            {
                _fightAdapter.OnMouseWheel(delta);
                return;
            }
            
            if (_currentMode == GameMode.Dialogue && _dialogueAdapter != null)
            {
                _dialogueAdapter.OnMouseWheel(delta);
                return;
            }
            
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
            case GameMode.LLMLoading:
                OnEnterLLMLoading();
                break;

            case GameMode.MainMenu:
                OnEnterMainMenu();
                break;
                
            case GameMode.WorldView:
                OnEnterWorldView();
                break;
                
            case GameMode.Traveling:
                OnEnterTraveling();
                break;
                
            case GameMode.LocationInteraction:
                OnEnterLocationInteraction();
                break;
                
            case GameMode.ProtagonistCreation:
                OnEnterProtagonistCreation();
                break;
                
            case GameMode.ProtagonistManagement:
                OnEnterProtagonistManagement();
                break;
                
            case GameMode.Fighting:
                OnEnterFighting();
                break;
                
            case GameMode.Dialogue:
                OnEnterDialogue();
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
        
        // Check if the protagonist is at a location (not just any vertex)
        var protagonistVertex = _interface.GetAvatarVertex();
        if (protagonistVertex == vertexIndex)
        {
            Console.WriteLine("LocationTravelGameController: Clicked on protagonist's current position");
            
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
    /// Called when protagonist arrives at a vertex.
    /// This should be called by MicroworldInterface when movement completes.
    /// </summary>
    public void OnProtagonistArrived(int vertexIndex)
    {
        if (_currentMode != GameMode.Traveling)
            return;

        Console.WriteLine($"LocationTravelGameController: Protagonist arrived at vertex {vertexIndex}");
        
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

    private void OnEnterLLMLoading()
    {
        Console.WriteLine("LocationTravelGameController: Entered LLMLoading mode");
        _core.SetNarrationMode(true);
        _core.SetWorldInteractionsEnabled(false);
        _interface.SetWorldInteractionsEnabled(false);

        if (_core.Terminal != null)
        {
            if (_llmLoadingRenderer == null)
                _llmLoadingRenderer = new LLMLoadingRenderer(_core.Terminal, "AI Model");

            float progress;
            string status;
            lock (_llmLoadLock)
            {
                progress = _llmLoadProgress;
                status   = _llmLoadStatus;
            }
            _llmLoadingRenderer.Update(progress, status);
        }
    }

    private void OnEnterMainMenu()
    {
        Console.WriteLine("LocationTravelGameController: Entered MainMenu mode");
        // Darken the sphere (visible but dim behind menu)
        _core.SetNarrationMode(true);
        // Disable world vertex interactions
        _core.SetWorldInteractionsEnabled(false);
        _interface.SetWorldInteractionsEnabled(false);
        
        if (_core.Terminal != null)
        {
            // Lazily create the menu renderer
            if (_mainMenuRenderer == null)
            {
                _mainMenuRenderer = new MainMenuRenderer(_core.Terminal);
            }
            
            // Configure buttons with callbacks
            _mainMenuRenderer.HasGameStarted = _hasGameStarted;
            _mainMenuRenderer.SetButtons(
                onNew: () =>
                {
                    ResetGameState();
                    SetMode(GameMode.ProtagonistCreation);
                },
                onContinue: () =>
                {
                    if (!_hasGameStarted)
                        ResetGameState(); // First time: treat as new game
                    SetMode(GameMode.WorldView);
                },
                onProtagonist: () =>
                {
                    SetMode(GameMode.ProtagonistManagement);
                },
                onExit: () =>
                {
                    _core.Close();
                }
            );
            
            // Render the menu
            _mainMenuRenderer.Render();
        }
    }
    
    private void OnEnterProtagonistCreation()
    {
        Console.WriteLine("LocationTravelGameController: Entered ProtagonistCreation mode");
        // Keep sphere darkened behind the creation screen
        _core.SetNarrationMode(true);
        _core.SetWorldInteractionsEnabled(false);
        _interface.SetWorldInteractionsEnabled(false);
        
        if (_core.Terminal != null)
        {
            // Load body art data if not already loaded
            if (_bodyArtData == null)
            {
                string artFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                    "assets", "art", "body", "human");
                // Fallback to project root path if bin path doesn't have assets
                if (!System.IO.Directory.Exists(artFolder))
                    artFolder = System.IO.Path.Combine(
                        System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".",
                        "..", "..", "..", "assets", "art", "body", "human");
                if (!System.IO.Directory.Exists(artFolder))
                    artFolder = System.IO.Path.Combine("assets", "art", "body", "human");
                    
                _bodyArtData = BodyArtData.Load(artFolder);
            }
            
            // Get the protagonist (already created by ResetGameState)
            var protagonist = _protagonist!;
            
            // Create the renderer
            _protagonistCreationRenderer = new ProtagonistCreationRenderer(_core.Terminal, protagonist, _bodyArtData);
            _protagonistCreationRenderer.OnContinue = () =>
            {
                Console.WriteLine("LocationTravelGameController: Protagonist creation complete, entering WorldView");
                // Re-initialize memory with the organ scores the player set during creation.
                // ResetGameState called InitializeMemory earlier with initial random scores;
                // now we rebuild modules to reflect the final configured values.
                protagonist.InitializeMemory();
                protagonist.ReinitializeHumorQueues();
                protagonist.AssignModiMentisToMemoryRandom();
                _protagonistCreationRenderer = null;
                SetMode(GameMode.WorldView);
            };
            
            // Render the creation screen
            _protagonistCreationRenderer.Render();
        }
    }
    
    private void OnEnterProtagonistManagement()
    {
        Console.WriteLine("LocationTravelGameController: Entered ProtagonistManagement mode");
        _core.SetNarrationMode(true);
        _core.SetWorldInteractionsEnabled(false);
        _interface.SetWorldInteractionsEnabled(false);
        
        if (_core.Terminal != null)
        {
            // Load body art data if not already loaded (same as creation mode)
            if (_bodyArtData == null)
            {
                string artFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                    "assets", "art", "body", "human");
                if (!System.IO.Directory.Exists(artFolder))
                    artFolder = System.IO.Path.Combine(
                        System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".",
                        "..", "..", "..", "assets", "art", "body", "human");
                if (!System.IO.Directory.Exists(artFolder))
                    artFolder = System.IO.Path.Combine("assets", "art", "body", "human");
                    
                _bodyArtData = BodyArtData.Load(artFolder);
            }
            
            var protagonist = _protagonist!;
            
            _managementMenuRenderer = new ManagementMenuRenderer(
                _core.Terminal, protagonist, _bodyArtData, _core.PopupTerminal);
            _managementMenuRenderer.OnBack = () =>
            {
                Console.WriteLine("LocationTravelGameController: Management menu closed, returning to main menu");
                _managementMenuRenderer = null;
                SetMode(GameMode.MainMenu);
            };
            
            _managementMenuRenderer.Render();
        }
    }
    
    private void OnEnterWorldView()
    {
        Console.WriteLine("LocationTravelGameController: Entered WorldView mode");
        // Set camera zoom for destination selection
        _core.Camera.SetDistance(Config.GlyphSphere.CameraZoomWorldView);
        // Disable narration mode (world is interactive and in focus)
        _core.SetNarrationMode(false);
        // Re-enable world interactions
        _core.SetWorldInteractionsEnabled(true);
        _interface.SetWorldInteractionsEnabled(true);
        // Hide or minimize terminal
        if (_core.Terminal != null)
        {
            _core.Terminal.Visible = false;
        }
    }

    private void OnEnterTraveling()
    {
        Console.WriteLine("LocationTravelGameController: Entered Traveling mode");
        // Set camera zoom for travel animation
        _core.Camera.SetDistance(Config.GlyphSphere.CameraZoomTraveling);
        // Disable narration mode (world is visible during travel)
        _core.SetNarrationMode(false);
        // Could show travel info in terminal
    }

    private void OnEnterLocationInteraction()
    {
        Console.WriteLine("LocationTravelGameController: Entered LocationInteraction mode");
        // Set camera zoom for location interaction/narration
        _core.Camera.SetDistance(Config.GlyphSphere.CameraZoomNarration);
        // Enable narration mode (world is background, terminal UI is focus)
        _core.SetNarrationMode(true);
        
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
    /// Prints the current narration graph structure to console (debug command).
    /// Only works when in narrative mode.
    /// </summary>
    public void PrintNarrativeGraph()
    {
        if (_isInNarrativeMode && _narrativeController != null)
        {
            _narrativeController.PrintGraphStructure();
        }
        else
        {
            Console.WriteLine("No active narrative graph (not in narrative mode)");
        }
    }
    
    /// <summary>
    /// Registers a narration graph factory for a specific biome.
    /// </summary>
    public void RegisterNarrationFactory(string biomeName, NarrationGraphFactory factory)
    {
        _narrationFactories[biomeName.ToLowerInvariant()] = factory;
        Console.WriteLine($"LocationTravelGameController: Registered narration factory for biome '{biomeName}'");
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
                Config.ExplorationPopup.LocationNameTextColor, 
                Config.ExplorationPopup.LocationNameBackgroundColor);
        }
    }
    
    /// <summary>
    /// Starts Phase 6 Chain-of-Thought narrative interaction.
    /// </summary>
    private void StartNarrativeInteraction(int vertexIndex)
    {
        if (_core.Terminal == null || _core.PopupTerminal == null || _llmActionExecutor == null || _modusMentisSlotManager == null)
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
            
            if (_criticEvaluator == null)
            {
                Console.WriteLine("LocationTravelGameController: Cannot enter Phase 6 mode - Critic not initialized");
                return;
            }

            // Create Action Execution Controller dependencies
            var outcomeApplicator = new OutcomeApplicator();
            var outcomeNarrator = new OutcomeNarrator(
                _llmActionExecutor.GetLlamaServerManager(),
                _modusMentisSlotManager
            );
            
            // Use the protagonist from game state (created in ResetGameState, possibly configured in ProtagonistCreation)
            if (_protagonist == null)
            {
                _protagonist = new Protagonist();
                _protagonist.InitializeModiMentis(ModusMentisRegistry.Instance, modusMentisCount: 50);
                _protagonist.InitializeMemory();
                _protagonist.AssignModiMentisToMemoryRandom();
                _protagonist.CompanionParty.AddRange(
                    Companion.GenerateRandom(ModusMentisRegistry.Instance, count: 3));
                var wolf = new Companion("Greywind", "A grey wolf with amber eyes.", SpeciesRegistry.Wolf);
                wolf.InitializeModiMentis(ModusMentisRegistry.Instance, modusMentisCount: 20);
                wolf.InitializeMemory();
                wolf.AssignModiMentisToMemoryRandom();
                _protagonist.CompanionParty.Add(wolf);
            }
            var protagonist = _protagonist;
            
            // Get the appropriate narration factory for this biome/location
            var biomeInfo = _interface.GetDetailedBiomeInfoAt(vertexIndex);
            var biomeName = biomeInfo.biome.Name.ToLowerInvariant();
            var worldContext = Narrative.WorldContext.From(biomeInfo.biome, biomeInfo.location);

            var actionExecutor = new ActionExecutionController(
                outcomeNarrator,
                outcomeApplicator,
                protagonist,
                _criticEvaluator,
                worldContext,
                vertexIndex
            );

            if (!_narrationFactories.TryGetValue(biomeName, out var graphFactory))
            {
                // Plain biome: use the new Scene system
                Console.WriteLine($"LocationTravelGameController: No narration factory for biome '{biomeName}', using PlainSceneFactory (new Scene system)");
                var sessionPath = _llmActionExecutor.GetLlamaServerManager().SessionLogDir;
                var sceneFactory = new PlainSceneFactory(sessionPath);
                var scene = sceneFactory.Build(vertexIndex);

                _narrativeController = new NarrativeController(
                    _core.Terminal,
                    _core.PopupTerminal,
                    _core,
                    _llmActionExecutor.GetLlamaServerManager(),
                    _modusMentisSlotManager,
                    inputHandler,
                    _thinkingExecutor,
                    actionExecutor,
                    scene,
                    vertexIndex,
                    worldContext
                );
            }
            else
            {
                // Create Phase 6 controller with graph factory and vertex as location ID
                _narrativeController = new NarrativeController(
                    _core.Terminal,
                    _core.PopupTerminal,
                    _core,
                    _llmActionExecutor.GetLlamaServerManager(),
                    _modusMentisSlotManager,
                    inputHandler,
                    _thinkingExecutor,
                    actionExecutor,
                    graphFactory,
                    vertexIndex,   // Use vertex index as location ID seed
                    worldContext   // Typed world context for flavor and display
                );
            }
            
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
            
            Console.WriteLine("LocationTravelGameController: Phase 6 narrative interaction started");
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
    /// Resets game state for a new game. Clears location states, resets protagonist position.
    /// </summary>
    public void ResetGameState()
    {
        Console.WriteLine("LocationTravelGameController: Resetting game state");
        
        // Exit narrative mode if active
        if (_isInNarrativeMode)
        {
            ExitNarrativeMode();
        }
        
        // Clear location state
        _currentLocationState = null;
        _currentLocationVertex = -1;
        _destinationVertex = -1;
        _locationStates.Clear();
        
        // Reset protagonist to a new random starting position
        _interface.ResetProtagonistPosition();
        
        // Create a fresh protagonist for the new game
        _protagonist = new Protagonist();
        _protagonist.InitializeModiMentis(ModusMentisRegistry.Instance, modusMentisCount: 50);
        _protagonist.InitializeMemory();
        _protagonist.AssignModiMentisToMemoryRandom();
        _protagonist.CompanionParty.AddRange(
            Companion.GenerateRandom(ModusMentisRegistry.Instance, count: 3));
        var wolf = new Companion("Greywind", "A grey wolf with amber eyes.", SpeciesRegistry.Wolf);
        wolf.InitializeModiMentis(ModusMentisRegistry.Instance, modusMentisCount: 20);
        wolf.InitializeMemory();
        wolf.AssignModiMentisToMemoryRandom();
        _protagonist.CompanionParty.Add(wolf);
        
        _hasGameStarted = true;
        Console.WriteLine("LocationTravelGameController: Game state reset complete");
    }
    
    // ── Fight/Dialogue mode entry methods ──────────────────────────────
    
    private void OnEnterFighting()
    {
        Console.WriteLine("LocationTravelGameController: Entered Fighting mode");
        // Keep narration mode visuals (darkened sphere, terminal visible)
        _core.SetNarrationMode(true);
        _core.SetWorldInteractionsEnabled(false);
        _interface.SetWorldInteractionsEnabled(false);
        
        if (_core.Terminal != null)
            _core.Terminal.Visible = true;
    }
    
    private void OnEnterDialogue()
    {
        Console.WriteLine("LocationTravelGameController: Entered Dialogue mode");
        _core.SetNarrationMode(true);
        _core.SetWorldInteractionsEnabled(false);
        _interface.SetWorldInteractionsEnabled(false);
        
        if (_core.Terminal != null)
            _core.Terminal.Visible = true;
    }
    
    /// <summary>
    /// Transitions from narrative mode into embedded fight mode.
    /// </summary>
    private void StartFightMode(FightOutcome fightOutcome)
    {
        if (_core.Terminal == null || _narrativeController == null)
            return;
        
        Console.WriteLine($"LocationTravelGameController: Starting fight with {fightOutcome.Target.DisplayName}");
        
        _fightAdapter = new FightModeAdapter(
            _core.Terminal,
            _core.PopupTerminal,
            fightOutcome.Target,
            _narrativeController.Protagonist);
        
        SetMode(GameMode.Fighting);
    }
    
    /// <summary>
    /// Transitions from narrative mode into embedded dialogue mode.
    /// </summary>
    private void StartDialogueMode(DialogueOutcome dialogueOutcome)
    {
        if (_core.Terminal == null || _narrativeController == null ||
            _llmActionExecutor == null || _modusMentisSlotManager == null)
            return;
        
        Console.WriteLine($"LocationTravelGameController: Starting dialogue with {dialogueOutcome.Target.DisplayName}");
        
        _dialogueAdapter = new DialogueModeAdapter(
            dialogueOutcome.Target,
            _narrativeController.Protagonist,
            _llmActionExecutor.GetLlamaServerManager(),
            _modusMentisSlotManager,
            _core.Terminal);
        
        _dialogueAdapter.Start();
        SetMode(GameMode.Dialogue);
    }
    
    /// <summary>
    /// Called when fight adapter reports completion. Returns to narrative mode.
    /// </summary>
    private void OnFightCompleted()
    {
        if (_fightAdapter == null || _narrativeController == null)
            return;
        
        var result = _fightAdapter.Result;
        var npc = _fightAdapter.TargetNpc;
        
        Console.WriteLine($"LocationTravelGameController: Fight completed - {result}");
        
        _narrativeController.OnFightCompleted(result, npc);
        _fightAdapter = null;
        
        if (result == FightAdapterResult.Death)
        {
            // Player died - exit to world view (force new protagonist)
            Console.WriteLine("LocationTravelGameController: Player died, exiting to world view");
            ExitNarrativeMode();
            return;
        }
        
        if (result == FightAdapterResult.Runaway)
        {
            // Player ran away - return to world view
            Console.WriteLine("LocationTravelGameController: Player ran away, exiting to world view");
            ExitNarrativeMode();
            return;
        }
        
        // Victory - return to narrative mode
        SetMode(GameMode.LocationInteraction);
    }
    
    /// <summary>
    /// Called when dialogue adapter reports completion. Returns to narrative mode.
    /// </summary>
    private void OnDialogueCompleted()
    {
        if (_dialogueAdapter == null || _narrativeController == null)
            return;
        
        var npc = _dialogueAdapter.TargetNpc;
        
        Console.WriteLine($"LocationTravelGameController: Dialogue completed with {npc.DisplayName}");
        
        _narrativeController.OnDialogueCompleted(npc);
        _dialogueAdapter = null;
        
        // Return to narrative mode
        SetMode(GameMode.LocationInteraction);
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
    /// Closes the Phase 6 thinking modusMentis popup if it's open.
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
    
    /// <summary>
    /// Routes keyboard input to the active sub-mode (fight or dialogue adapter).
    /// Called from the launcher's KeyDown handler.
    /// </summary>
    public void OnKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys key)
    {
        if (_currentMode == GameMode.Fighting && _fightAdapter != null)
        {
            _fightAdapter.OnKeyPress(key);
        }
        else if (_currentMode == GameMode.Dialogue && _dialogueAdapter != null)
        {
            _dialogueAdapter.OnKeyPress(key);
        }
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
