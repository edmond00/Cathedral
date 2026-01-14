using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenTK.Mathematics;
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
    
    // Phase 6 Chain-of-Thought system
    private Phase6ForestController? _phase6Controller = null;
    private bool _isInPhase6Mode = false;
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
    
    // Action executors
    private readonly SimpleActionExecutor _simpleActionExecutor;
    private LLMActionExecutor? _llmActionExecutor; // Optional - requires LLamaServerManager
    private ActionOutcomeSimulator _actionOutcomeSimulator;
    private CriticEvaluator? _criticEvaluator;
    private ActionScorer? _actionScorer;
    
    // Current blueprint for location interaction
    private LocationBlueprint? _currentBlueprint;
    
    // UI state for interaction
    private List<ActionInfo> _currentActions = new();
    private List<ParsedAction> _currentParsedActions = new();
    private string _currentNarrative = "";
    private bool _waitingForClickToExit = false;
    private bool _isLoadingLLMContent = false;
    private string _loadingMessage = Config.LoadingMessages.Thinking;
    
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
        
        // Initialize action executors
        _simpleActionExecutor = new SimpleActionExecutor();
        _llmActionExecutor = null; // Will be set via SetLLMActionExecutor()
        _actionOutcomeSimulator = new ActionOutcomeSimulator();
        
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
    /// Updates the game controller (called every frame).
    /// Used to animate loading indicators.
    /// </summary>
    public void Update()
    {
        // Update Phase 6 controller if active
        if (_isInPhase6Mode && _phase6Controller != null)
        {
            // If popup is visible, handle all mouse updates here for consistent frame-rate timing
            // This ensures uniform refresh rate across the entire popup (both inside and outside terminal bounds)
            if (_phase6Controller.IsPopupVisible && _core.Terminal != null)
            {
                Vector2 rawMouse = _core.Terminal.InputHandler.GetCorrectedMousePosition();
                _phase6Controller.OnRawMouseMove(rawMouse);
            }
            
            _phase6Controller.Update();
            
            // Check if player requested exit (clicked Continue button)
            if (_phase6Controller.HasRequestedExit())
            {
                Console.WriteLine("LocationTravelGameController: Phase 6 exit requested");
                ExitPhase6Mode();
            }
            
            return;
        }
        
        // Update loading animation if currently loading
        if (_isLoadingLLMContent && _terminalUI != null)
        {
            _terminalUI.ShowLoadingIndicator(_loadingMessage);
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
        // Phase 6 mode handles clicks differently
        if (_isInPhase6Mode && _phase6Controller != null)
        {
            // If popup is visible, use raw mouse coordinates
            if (_phase6Controller.IsPopupVisible)
            {
                Vector2 rawMouse = _core.Terminal.InputHandler.GetCorrectedMousePosition();
                _phase6Controller.OnRawMouseClick(rawMouse);
                return;
            }
            
            _phase6Controller.OnMouseClick(x, y);
            return;
        }
        
        if (_currentMode != GameMode.LocationInteraction || _terminalUI == null)
            return;
        
        // If waiting for click to exit, any click returns to world view
        if (_waitingForClickToExit)
        {
            Console.WriteLine("LocationTravelGameController: User clicked, returning to world view");
            _waitingForClickToExit = false;
            SetMode(GameMode.WorldView);
            return;
        }
        
        int? actionIndex = _terminalUI.GetHoveredAction(x, y);
        if (actionIndex.HasValue && actionIndex.Value >= 0 && actionIndex.Value < _currentActions.Count)
        {
            Console.WriteLine($"LocationTravelGameController: Action {actionIndex.Value + 1} selected: {_currentActions[actionIndex.Value].ActionText}");
            ExecuteAction(actionIndex.Value);
        }
    }
    
    /// <summary>
    /// Handles terminal cell hover for visual feedback.
    /// </summary>
    private void OnTerminalCellHovered(int x, int y)
    {
        // Phase 6 mode handles hover differently
        if (_isInPhase6Mode && _phase6Controller != null)
        {
            // When popup is visible, mouse updates are handled in Update() loop for consistent timing
            // Only handle non-popup interactions here
            if (!_phase6Controller.IsPopupVisible)
            {
                _phase6Controller.OnMouseMove(x, y);
            }
            return;
        }
        
        if (_currentMode != GameMode.LocationInteraction || _terminalUI == null)
            return;
        
        _terminalUI.UpdateHover(x, y, _currentActions);
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
        if (_isInPhase6Mode && _phase6Controller != null)
        {
            _phase6Controller.OnMouseWheel(delta);
            return;
        }
        
        // Other modes don't have scroll functionality yet
    }
    
    /// <summary>
    /// Executes a selected action.
    /// </summary>
    private void ExecuteAction(int actionIndex)
    {
        // Fire and forget the async execution
        _ = ExecuteActionAsync(actionIndex);
    }

    /// <summary>
    /// Executes a selected action (async version).
    /// Now includes second Critic pass to evaluate difficulty and determine outcome.
    /// </summary>
    private async Task ExecuteActionAsync(int actionIndex)
    {
        if (_currentLocationState == null || _currentBlueprint == null || 
            actionIndex < 0 || actionIndex >= _currentActions.Count)
            return;
        
        string actionText = _currentActions[actionIndex].ActionText;
        
        Console.WriteLine($"\n{'=',-80}");
        Console.WriteLine($"ACTION EXECUTION PIPELINE");
        Console.WriteLine($"{'=',-80}");
        Console.WriteLine($"Selected action: {actionText}");
        
        // Show loading indicator if using LLM
        if (_llmActionExecutor != null && _terminalUI != null)
        {
            _isLoadingLLMContent = true;
            _loadingMessage = Config.LoadingMessages.EvaluatingDifficulty;
            _terminalUI.ShowLoadingIndicator(_loadingMessage);
        }
        
        // Get the last action for context (if any)
        var lastAction = _currentLocationState.GetLastAction();
        
        // [NEW] Second Critic Pass - Evaluate difficulty and failure consequences
        ActionDifficultyResult? difficultyResult = null;
        if (actionIndex < _currentParsedActions.Count && 
            _currentParsedActions[actionIndex] != null &&
            _criticEvaluator != null)
        {
            try
            {
                Console.WriteLine($"\n[SECOND CRITIC PASS] Evaluating selected action...");
                var difficultyEvaluator = new ActionDifficultyEvaluator(_criticEvaluator);
                difficultyResult = await difficultyEvaluator.EvaluateSelectedActionAsync(
                    _currentParsedActions[actionIndex],
                    _currentLocationState,
                    _currentBlueprint);
                
                Console.WriteLine($"[SECOND CRITIC PASS] Complete - Difficulty: {difficultyResult.Difficulty}, Likely failure: {difficultyResult.MostPlausibleFailure}");
                
                // Log the difficulty evaluation
                LLMLogger.LogCriticSecondPass(
                    actionText,
                    difficultyResult.Difficulty,
                    difficultyResult.DifficultyScore,
                    difficultyResult.MostPlausibleFailure,
                    difficultyResult.FailureConsequencePlausibilities);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SECOND CRITIC PASS] Failed: {ex.Message}");
                LLMLogger.LogError($"Second Critic Pass failed: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine($"\n[SECOND CRITIC PASS] Skipped - Critic not available or parsed action missing");
        }
        
        // Update loading message
        if (_llmActionExecutor != null && _terminalUI != null)
        {
            _loadingMessage = Config.LoadingMessages.DeterminingOutcome;
            _terminalUI.ShowLoadingIndicator(_loadingMessage);
        }
        
        // [7] Determine outcome - Now uses difficulty result from Critic
        ActionResult result;
        if (actionIndex < _currentParsedActions.Count && _currentParsedActions[actionIndex] != null)
        {
            Console.WriteLine($"\n[OUTCOME SIMULATION] Using programmatic outcome simulation");
            
            // Determine success/failure based on difficulty if available
            bool actionSucceeds = true;
            string? overrideFailureConsequence = null;
            
            if (difficultyResult != null)
            {
                var difficultyEvaluator = new ActionDifficultyEvaluator(_criticEvaluator!);
                Console.WriteLine($"[OUTCOME SIMULATION] Difficulty score: {difficultyResult.DifficultyScore:F4} ({difficultyResult.Difficulty})");
                Console.WriteLine($"[OUTCOME SIMULATION] Success probability: {(0.95 - (difficultyResult.DifficultyScore * 0.55)):F4} ({((0.95 - (difficultyResult.DifficultyScore * 0.55)) * 100):F1}%)");
                
                actionSucceeds = difficultyEvaluator.DetermineActionSuccess(
                    difficultyResult.DifficultyScore, 
                    actionText: actionText,
                    logRoll: true);
                overrideFailureConsequence = difficultyResult.MostPlausibleFailure;
                
                Console.WriteLine($"[OUTCOME SIMULATION] Final result: {(actionSucceeds ? "SUCCESS" : "FAILURE")}");
                
                if (!actionSucceeds && overrideFailureConsequence != null)
                {
                    Console.WriteLine($"[OUTCOME SIMULATION] Using Critic-determined failure consequence: {overrideFailureConsequence}");
                }
            }
            
            result = _actionOutcomeSimulator.SimulateOutcome(
                _currentParsedActions[actionIndex],
                _currentLocationState,
                _currentBlueprint,
                forceSuccess: difficultyResult != null ? actionSucceeds : (bool?)null,
                overrideFailureConsequence: overrideFailureConsequence);
        }
        else
        {
            // Fallback if parsed actions not available
            Console.WriteLine($"\n[OUTCOME SIMULATION] Parsed action not available, using simple executor");
            result = _simpleActionExecutor.ExecuteAction(actionText, _currentLocationState, _currentBlueprint);
        }
        
        // Clear loading flag
        _isLoadingLLMContent = false;
        
        // [9] Check outcome and handle failure/success
        if (!result.Success)
        {
            Console.WriteLine("LocationTravelGameController: Action FAILED - generating failure narrative");
            
            if (_llmActionExecutor != null && _terminalUI != null)
            {
                _isLoadingLLMContent = true;
                _loadingMessage = Config.LoadingMessages.NarratingDemise;
                _terminalUI.ShowLoadingIndicator(_loadingMessage);
            }
            
            // Create temporary player action for narrative context
            var failedAction = new PlayerAction
            {
                ActionText = actionText,
                Outcome = result.NarrativeOutcome,
                WasSuccessful = false
            };
            
            // Request failure narrative
            string? failureNarrative = null;
            if (_llmActionExecutor != null)
            {
                failureNarrative = await _llmActionExecutor.GenerateFailureNarrativeAsync(
                    _currentLocationState,
                    _currentBlueprint,
                    failedAction,
                    result.NarrativeOutcome);
            }
            
            _isLoadingLLMContent = false;
            
            // Update result with enhanced narrative
            result = ActionResult.CreateFailure(
                failureNarrative ?? result.NarrativeOutcome);
            
            // Apply the failed action to state
            var playerAction = new PlayerAction
            {
                ActionText = actionText,
                Outcome = result.NarrativeOutcome,
                WasSuccessful = false
            };
            
            _currentLocationState = _currentLocationState.ApplyActionResult(result, playerAction);
            _locationStates[_currentLocationVertex] = _currentLocationState;
            
            HandleInteractionEnd(result);
            return;
        }
        
        // SUCCESS - Continue with new actions
        Console.WriteLine("LocationTravelGameController: Action SUCCEEDED");
        
        // Create player action record
        var successPlayerAction = new PlayerAction
        {
            ActionText = actionText,
            Outcome = result.NarrativeOutcome,
            WasSuccessful = true,
            StateChanges = result.StateChanges,
            NewSublocation = result.NewSublocation,
            ItemGained = result.ItemsGained?.FirstOrDefault()
        };
        
        // Apply the result to the location state
        _currentLocationState = _currentLocationState.ApplyActionResult(result, successPlayerAction);
        _locationStates[_currentLocationVertex] = _currentLocationState;
        
        // Check if this is a successful exit
        if (result.EndsInteraction)
        {
            HandleInteractionEnd(result);
            return;
        }
        
        // Update narrative with result
        _currentNarrative = result.NarrativeOutcome;
        
        // Re-render the UI with new state (this will regenerate actions)
        await RenderLocationUIAsync();
    }
    
    /// <summary>
    /// Handles the end of a location interaction (success or failure).
    /// </summary>
    private void HandleInteractionEnd(ActionResult result)
    {
        if (_currentLocationState == null)
            return;
        
        // Show final message
        if (result.Success)
        {
            Console.WriteLine($"LocationTravelGameController: Location interaction ended successfully");
            _terminalUI?.ShowResultMessage(result.NarrativeOutcome + "\n\nClick anywhere to return to world view...", true);
        }
        else
        {
            Console.WriteLine($"LocationTravelGameController: Location interaction FAILED");
            _terminalUI?.ShowResultMessage($"FAILURE: {result.NarrativeOutcome}\n\nClick anywhere to return to world view...", false);
        }
        
        // Fire exit event
        LocationExited?.Invoke(_currentLocationState);
        
        // Set flag to wait for user click before exiting
        _waitingForClickToExit = true;
        
        Console.WriteLine("LocationTravelGameController: Waiting for user click to return to world view...");
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
        // Ignore clicks when Phase 6 narration is active
        if (_isInPhase6Mode)
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
            _currentBlueprint = generator.GenerateBlueprint(locationId);
            
            // Create initial state
            locationState = LocationInstanceState.FromBlueprint(locationId, _currentBlueprint);
            _locationStates[vertexIndex] = locationState;
            
            Console.WriteLine($"LocationTravelGameController: Created new location state for {locationId}");
        }
        else
        {
            // Existing location - increment visit count
            locationState = locationState.WithNewVisit();
            _locationStates[vertexIndex] = locationState;
            
            // Regenerate blueprint for existing location
            var generator = _generators.GetValueOrDefault("forest") ?? _generators.Values.First();
            _currentBlueprint = generator.GenerateBlueprint(locationState.LocationId);
            
            Console.WriteLine($"LocationTravelGameController: Returning to existing location (visit #{locationState.VisitCount})");
        }

        _currentLocationState = locationState;
        _currentLocationVertex = vertexIndex;
        SetMode(GameMode.LocationInteraction);
        LocationEntered?.Invoke(locationState);
    }

    /// <summary>
    /// Starts biome interaction mode (when there's no specific location).
    /// Treats the biome itself as an interactive location.
    /// </summary>
    private void StartBiomeInteraction(int vertexIndex, Cathedral.Glyph.Microworld.BiomeType biomeType)
    {
        // Check if this is a forest biome and Phase 6 is available
        if (biomeType.Name.Equals("Forest", StringComparison.OrdinalIgnoreCase) && 
            _llmActionExecutor != null && 
            _skillSlotManager != null &&
            _core.Terminal != null)
        {
            Console.WriteLine("LocationTravelGameController: Starting Phase 6 forest interaction");
            StartPhase6ForestInteraction(vertexIndex);
            return;
        }
        
        // Get or create location state for this biome
        if (!_locationStates.TryGetValue(vertexIndex, out var locationState))
        {
            // Generate a blueprint for this biome as a location
            var locationId = $"{biomeType.Name}_{vertexIndex}";
            
            // Use the biome name to select appropriate generator
            // Default to forest generator if biome type not found
            var generatorKey = _generators.ContainsKey(biomeType.Name) ? biomeType.Name : "forest";
            var generator = _generators.GetValueOrDefault(generatorKey) ?? _generators.Values.First();
            _currentBlueprint = generator.GenerateBlueprint(locationId);
            
            // Create initial state
            locationState = LocationInstanceState.FromBlueprint(locationId, _currentBlueprint);
            _locationStates[vertexIndex] = locationState;
            
            Console.WriteLine($"LocationTravelGameController: Created new biome interaction state for {biomeType.Name} at vertex {vertexIndex}");
        }
        else
        {
            // Existing location - increment visit count
            locationState = locationState.WithNewVisit();
            _locationStates[vertexIndex] = locationState;
            
            // Regenerate blueprint for existing biome
            var generatorKey = _generators.ContainsKey(biomeType.Name) ? biomeType.Name : "forest";
            var generator = _generators.GetValueOrDefault(generatorKey) ?? _generators.Values.First();
            _currentBlueprint = generator.GenerateBlueprint(locationState.LocationId);
            
            Console.WriteLine($"LocationTravelGameController: Returning to existing biome (visit #{locationState.VisitCount})");
        }

        _currentLocationState = locationState;
        _currentLocationVertex = vertexIndex;
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
        
        // Reset exit flag
        _waitingForClickToExit = false;
        
        // If Phase 6 is active, don't start the old location UI system
        if (_isInPhase6Mode)
        {
            Console.WriteLine("LocationTravelGameController: Phase 6 active, skipping old location UI");
            
            // Show terminal for Phase 6 interaction
            if (_core.Terminal != null)
            {
                _core.Terminal.Visible = true;
            }
            
            return;
        }
        
        // Reset LLM conversation histories to avoid pattern repetition from previous locations
        if (_llmActionExecutor != null)
        {
            Console.WriteLine("LocationTravelGameController: Resetting LLM instances for new location");
            _llmActionExecutor.ResetForNewLocation();
        }
        
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
        // Fire and forget the async rendering
        _ = RenderLocationUIAsync();
    }

    /// <summary>
    /// Renders the location UI with current state (async version).
    /// </summary>
    private async Task RenderLocationUIAsync()
    {
        // Don't render old location UI if Phase 6 is active
        if (_isInPhase6Mode)
        {
            Console.WriteLine("RenderLocationUIAsync: Phase 6 active, skipping old location UI rendering");
            return;
        }
        
        if (_terminalUI == null || _currentLocationState == null)
            return;
        
        // Get location blueprint
        var blueprint = GetCurrentLocationBlueprint();
        if (blueprint == null)
        {
            Console.WriteLine("RenderLocationUI: Cannot render UI - no blueprint available");
            return;
        }
        
        // Show loading indicator if using LLM
        if (_llmActionExecutor != null)
        {
            _isLoadingLLMContent = true;
            _loadingMessage = Config.LoadingMessages.GeneratingActions;
            _terminalUI.ShowLoadingIndicator(_loadingMessage);
        }
        
        // Generate actions using LLM if available - Use RegenerateActionsAsync to get Critic evaluation
        var lastAction = _currentLocationState.GetLastAction();
        if (_llmActionExecutor != null)
        {
            Console.WriteLine("RenderLocationUIAsync: Requesting actions from LLM with Critic evaluation...");
            await RegenerateActionsAsync();
            
            // Check if actions were successfully generated
            if (_currentActions == null || _currentActions.Count == 0)
            {
                Console.Error.WriteLine("RenderLocationUIAsync: Failed to generate actions from LLM");
                _isLoadingLLMContent = false;
                _terminalUI?.ShowResultMessage(
                    "ERROR: Failed to generate actions.\n\n" +
                    "The LLM did not return valid actions. This could be due to:\n" +
                    "- LLM server not responding\n" +
                    "- Invalid response format\n" +
                    "- Timeout\n\n" +
                    "Check logs/llm_communication_*.log for details.\n\n" +
                    "Click anywhere to return to world view...",
                    false
                );
                _waitingForClickToExit = true;
                return;
            }
        }
        else
        {
            Console.Error.WriteLine("RenderLocationUIAsync: LLM executor not available yet");
            _isLoadingLLMContent = false;
            _terminalUI?.ShowResultMessage(
                "PLEASE WAIT: LLM system is still initializing...\n\n" +
                "The LLM server is starting up. This can take 30-60 seconds.\n" +
                "Please wait a moment and try clicking on the location again.\n\n" +
                "If this message persists after a minute, check that:\n" +
                "- The LLM server started successfully\n" +
                "- Check logs/llm_communication_*.log for errors\n\n" +
                "Click anywhere to return to world view...",
                false
            );
            _waitingForClickToExit = true;
            return;
        }
        
        // Update loading message for narrative generation
        if (_llmActionExecutor != null)
        {
            _loadingMessage = Config.LoadingMessages.GeneratingNarrative;
            _terminalUI.ShowLoadingIndicator(_loadingMessage);
        }
        
        // Generate narrative using LLM if available
        if (_llmActionExecutor != null)
        {
            Console.WriteLine("RenderLocationUIAsync: Requesting narrative from LLM...");
            var llmNarrative = await _llmActionExecutor.GenerateNarrativeAsync(_currentLocationState, blueprint, lastAction, _currentActions);
            if (string.IsNullOrWhiteSpace(llmNarrative))
            {
                Console.Error.WriteLine("RenderLocationUIAsync: Failed to generate narrative from LLM");
                _isLoadingLLMContent = false;
                _terminalUI?.ShowResultMessage(
                    "ERROR: Failed to generate narrative.\n\n" +
                    "The LLM did not return a valid narrative description. This could be due to:\n" +
                    "- LLM server not responding\n" +
                    "- Invalid response format\n" +
                    "- Timeout\n\n" +
                    "Check logs/llm_communication_*.log for details.\n\n" +
                    "Click anywhere to return to world view...",
                    false
                );
                _waitingForClickToExit = true;
                return;
            }
            _currentNarrative = llmNarrative;
        }
        else
        {
            // This shouldn't happen since we checked earlier, but handle it anyway
            Console.Error.WriteLine("RenderLocationUIAsync: LLM executor not available for narrative");
            _currentNarrative = "ERROR: LLM not available";
        }
        
        // Clear loading flag
        _isLoadingLLMContent = false;
        
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
    /// Regenerates actions based on current state.
    /// </summary>
    private async Task RegenerateActionsAsync()
    {
        if (_currentLocationState == null || _currentBlueprint == null)
            return;

        var lastAction = _currentLocationState.GetLastAction();
        
        if (_llmActionExecutor != null)
        {
            // [1] Generate 12 actions (with raw JSON for parsing)
            Console.WriteLine("RegenerateActionsAsync: Requesting 12 actions from LLM...");
            var (llmActions, rawJson) = await _llmActionExecutor.GenerateActionsWithRawJsonAsync(
                _currentLocationState, 
                _currentBlueprint, 
                lastAction,
                numberOfActions: 12);
            
            if (llmActions == null || llmActions.Count == 0 || string.IsNullOrEmpty(rawJson))
            {
                Console.Error.WriteLine("RegenerateActionsAsync: Failed to regenerate actions from LLM");
                _terminalUI?.ShowResultMessage(
                    "ERROR: Failed to generate new actions.\n\n" +
                    "The LLM did not return valid actions after your last action.\n" +
                    "Check logs/llm_communication_*.log for details.\n\n" +
                    "Click anywhere to return to world view...",
                    false
                );
                _waitingForClickToExit = true;
                return;
            }
            
            // [2] Parse into ParsedAction objects with full consequence data
            var parsedActions = ActionParser.ParseDirectorResponse(rawJson);
            
            if (parsedActions == null || parsedActions.Count == 0)
            {
                Console.Error.WriteLine("RegenerateActionsAsync: Failed to parse actions from JSON");
                _currentParsedActions = llmActions.Select((action, index) => new ParsedAction
                {
                    ActionText = action.ActionText,
                    Skill = action.RelatedSkill,
                    OriginalIndex = index
                }).ToList();
                _currentActions = llmActions.Take(6).ToList();
                return;
            }
            
            // [3] Score via Critic (if available and we have enough actions)
            if (_actionScorer != null && _criticEvaluator != null && parsedActions.Count > 6)
            {
                try
                {
                    Console.WriteLine($"RegenerateActionsAsync: Scoring {parsedActions.Count} actions with Critic First Pass...");
                    var scoredActions = await _actionScorer.ScoreActionsAsync(
                        parsedActions, 
                        lastAction,
                        _currentLocationState.CurrentSublocation,
                        _currentBlueprint);
                    
                    // [4] Select top 6
                    var topActions = scoredActions.Take(6).ToList();
                    _currentParsedActions = topActions.Select(s => s.Action).ToList();
                    _currentActions = _currentParsedActions.Select(a => new ActionInfo(a.ActionText, a.Skill)).ToList();
                    
                    Console.WriteLine($"RegenerateActionsAsync: Selected top 6 actions based on Critic scores");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"RegenerateActionsAsync: Critic scoring failed - {ex.Message}");
                    Console.WriteLine("Falling back to using all generated actions");
                    _currentParsedActions = parsedActions.Take(6).ToList();
                    _currentActions = llmActions.Take(6).ToList();
                }
            }
            else
            {
                // No Critic or not enough actions - use first 6 actions
                var reason = _actionScorer == null ? "Critic not available" : "Not enough actions for scoring";
                Console.WriteLine($"RegenerateActionsAsync: {reason}, using first 6 actions");
                _currentParsedActions = parsedActions.Take(6).ToList();
                _currentActions = llmActions.Take(6).ToList();
            }
        }
        else
        {
            Console.Error.WriteLine("RegenerateActionsAsync: LLM executor not available");
            _currentActions = new List<ActionInfo> { new ActionInfo("ERROR: LLM not available") };
            _currentParsedActions = new List<ParsedAction>();
        }
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
    private void StartPhase6ForestInteraction(int vertexIndex)
    {
        if (_core.Terminal == null || _core.PopupTerminal == null || _llmActionExecutor == null || _skillSlotManager == null)
        {
            Console.Error.WriteLine("Phase6ForestController: Cannot start - missing dependencies");
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
            
            // Create a temporary Avatar for Phase 6 (will be initialized in Phase6ForestController)
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
            _phase6Controller = new Phase6ForestController(
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
            _isInPhase6Mode = true;
            _currentLocationVertex = vertexIndex;
            
            // Disable world map interactions while narration UI is active
            _interface.SetWorldInteractionsEnabled(false);
            _core.SetWorldInteractionsEnabled(false);
            
            // Set mode to LocationInteraction
            SetMode(GameMode.LocationInteraction);
            
            // Start observation phase (async)
            _phase6Controller.StartObservationPhase();
            
            Console.WriteLine("LocationTravelGameController: Phase 6 forest interaction started");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LocationTravelGameController: Failed to start Phase 6: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            
            // Fallback to normal interaction
            _isInPhase6Mode = false;
            _phase6Controller = null;
        }
    }
    
    /// <summary>
    /// Exits Phase 6 mode and returns to world view.
    /// </summary>
    public void ExitPhase6Mode()
    {
        if (!_isInPhase6Mode)
            return;
        
        Console.WriteLine("LocationTravelGameController: Exiting Phase 6 mode");
        
        // Re-enable world map and 3D interactions
        _interface.SetWorldInteractionsEnabled(true);
        _core.SetWorldInteractionsEnabled(true);
        
        _isInPhase6Mode = false;
        _phase6Controller = null;
        _currentLocationVertex = -1;
        
        SetMode(GameMode.WorldView);
    }
    
    /// <summary>
    /// Closes the Phase 6 thinking skill popup if it's open.
    /// Returns true if popup was closed, false otherwise.
    /// </summary>
    public bool ClosePhase6Popup()
    {
        if (_isInPhase6Mode && _phase6Controller != null)
        {
            return _phase6Controller.ClosePopup();
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
        
        // Dispose Critic evaluator
        _criticEvaluator?.Dispose();
        
        Console.WriteLine("LocationTravelGameController: Disposed");
    }
}
