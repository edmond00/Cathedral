using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Cathedral.Debug;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Dialogue.Tree.Trees;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Nodes;
using Cathedral.Game.Npc;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Verbs;
using Cathedral.LLM;
using Cathedral.Terminal;
using Cathedral.Glyph;
using OpenTK.Mathematics;

namespace Cathedral.Game;

/// <summary>
/// Controls the Chain-of-Thought narration system for all locations.
/// Manages observation phase lifecycle and UI rendering.
/// </summary>
public class NarrativeController
{
    // State
    private readonly NarrativeState _narrationState = new();
    private readonly NarrationScrollBuffer _scrollBuffer;
    private readonly NarrativeUI _ui;
    private readonly TerminalThinkingModusMentisPopup _modusMentisPopup;
    private readonly TerminalItemSelectionPopup _itemSelectionPopup;
    private readonly TerminalSimpleChoicePopup _choicePopup;
    private readonly WorldContext _worldContext;
    
    // Dependencies
    private readonly Protagonist _protagonist;
    private NarrationNode _currentNode;
    private readonly ObservationPhaseController _observationController;
    private readonly ThinkingExecutor _thinkingExecutor;
    private readonly ActionExecutionController _actionExecutor;
    private readonly GlyphSphereCore _core;
    private readonly TerminalInputHandler _terminalInputHandler;
    
    // Mouse tracking
    private int _lastMouseX = 0;
    private int _lastMouseY = 0;
    
    // Pending action result (stored while waiting for dice roll continue)
    private ActionExecutionResult? _pendingActionResult = null;
    
    private readonly NarrationGraph _graph;
    private readonly int _locationId;
    
    // ── Scene system (new backend, coexists with NarrationGraph) ──
    private readonly Cathedral.Game.Scene.Scene? _scene;
    private PoV? _pov;
    
    // Pending fight/dialogue transitions (set by OnDiceRollContinue, consumed by game controller)
    private FightOutcome? _pendingFightOutcome = null;
    private DialogueOutcome? _pendingDialogueOutcome = null;
    
    // Random for dice rolls
    private readonly Random _diceRandom = new Random();

    // Active party member (starts as protagonist, switches to companion after Speak About)
    private PartyMember _activePartyMember = null!;
    // Companion list parallel to the companion selection choice popup choices
    private List<Companion> _pendingCompanions = new();
    // Per-member noetic point counters — keyed by DisplayName.
    // Preserved across hand-offs so returning to a member keeps their remaining points.
    private readonly Dictionary<string, int> _memberNoeticPoints = new();
    
    public NarrativeController(
        TerminalHUD terminal,
        PopupTerminalHUD popup,
        GlyphSphereCore core,
        LlamaServerManager llamaServer,
        ModusMentisSlotManager slotManager,
        TerminalInputHandler terminalInputHandler,
        ThinkingExecutor thinkingExecutor,
        ActionExecutionController actionExecutor,
        NarrationGraphFactory? graphFactory = null,
        int locationId = 0,
        WorldContext? worldContext = null,
        KeywordFallbackService? keywordFallbackService = null)
    {
        if (terminal == null)
            throw new ArgumentNullException(nameof(terminal));
        if (popup == null)
            throw new ArgumentNullException(nameof(popup));
        if (core == null)
            throw new ArgumentNullException(nameof(core));
        if (llamaServer == null)
            throw new ArgumentNullException(nameof(llamaServer));
        if (slotManager == null)
            throw new ArgumentNullException(nameof(slotManager));
        if (terminalInputHandler == null)
            throw new ArgumentNullException(nameof(terminalInputHandler));
        if (thinkingExecutor == null)
            throw new ArgumentNullException(nameof(thinkingExecutor));
        if (actionExecutor == null)
            throw new ArgumentNullException(nameof(actionExecutor));
        
        _ui = new NarrativeUI(terminal);
        // Calculate content width dynamically: terminal width - margins - scrollbar
        var layout = new NarrativeLayout(
            terminal.Width, 
            terminal.Height, 
            Config.NarrativeUI.TopPadding, 
            Config.NarrativeUI.BottomPadding,
            Config.NarrativeUI.LeftPadding,
            Config.NarrativeUI.RightPadding);
        int contentWidth = layout.CONTENT_WIDTH - 1; // -1 for scrollbar
        _scrollBuffer = new NarrationScrollBuffer(maxWidth: contentWidth, layout: layout);
        _modusMentisPopup = new TerminalThinkingModusMentisPopup(popup);
        _itemSelectionPopup = new TerminalItemSelectionPopup(popup);
        _choicePopup = new TerminalSimpleChoicePopup(popup);
        _core = core;
        _terminalInputHandler = terminalInputHandler;
        _worldContext = worldContext ?? new PlainBiomeContext();
        _locationId = locationId;
        
        // Initialize protagonist with random modiMentis and memory
        _protagonist = new Protagonist();
        _protagonist.InitializeModiMentis(ModusMentisRegistry.Instance, modusMentisCount: 50);
        _protagonist.InitializeMemory();
        _protagonist.AssignModiMentisToMemoryRandom();
        // Also generate random companions for testing the Speak About mechanic
        _protagonist.CompanionParty.AddRange(Companion.GenerateRandom(ModusMentisRegistry.Instance, count: 3));
        _activePartyMember = _protagonist;
        
        // Generate graph for this location using factory
        if (graphFactory == null)
            throw new ArgumentNullException(nameof(graphFactory), "NarrationGraphFactory is required - no fallback provided");

        _graph       = graphFactory.GenerateGraph(locationId);
        _currentNode = _graph.EntryNode;
        Console.WriteLine($"NarrativeController: Generated graph for location {locationId} with entry node '{_currentNode.NodeId}' ({_graph.Npcs.Count} NPCs)");
        LlmMonitorDebugManager.Show();
        
        // Initialize controllers
        _observationController = new ObservationPhaseController(llamaServer, slotManager, _worldContext, keywordFallbackService);
        _thinkingExecutor = thinkingExecutor;
        _actionExecutor = actionExecutor;
        
        Console.WriteLine($"NarrativeController: Initialized with node {_currentNode.NodeId}");
        Console.WriteLine($"NarrativeController: Protagonist has {_protagonist.ModiMentis.Count} modiMentis");
    }

    /// <summary>
    /// Constructs a NarrativeController backed by the new Scene system.
    /// The Scene is converted to a synthetic NarrationNode/NarrationGraph via SceneViewAdapter
    /// so the existing LLM pipeline can consume it transparently.
    /// </summary>
    public NarrativeController(
        TerminalHUD terminal,
        PopupTerminalHUD popup,
        GlyphSphereCore core,
        LlamaServerManager llamaServer,
        ModusMentisSlotManager slotManager,
        TerminalInputHandler terminalInputHandler,
        ThinkingExecutor thinkingExecutor,
        ActionExecutionController actionExecutor,
        Cathedral.Game.Scene.Scene scene,
        int locationId,
        WorldContext? worldContext = null,
        KeywordFallbackService? keywordFallbackService = null)
        : this(terminal, popup, core, llamaServer, slotManager, terminalInputHandler,
               thinkingExecutor, actionExecutor,
               CreateGraphFactoryForScene(scene, locationId),
               locationId, worldContext, keywordFallbackService)
    {
        _scene = scene;

        // Build initial PoV from the first area
        var firstArea = scene.AllAreas.FirstOrDefault();
        if (firstArea != null)
        {
            _pov = new PoV(firstArea, TimePeriod.Morning);
            Console.WriteLine($"NarrativeController [Scene]: PoV at {firstArea.DisplayName}");
        }

        // Show scene debug viewer alongside graph viewer
        SceneDebugManager.Show(scene, _pov, locationId);
    }

    /// <summary>
    /// Creates a synthetic NarrationGraphFactory that wraps a Scene for the existing constructor.
    /// </summary>
    private static NarrationGraphFactory CreateGraphFactoryForScene(Cathedral.Game.Scene.Scene scene, int locationId)
    {
        return new SceneSyntheticGraphFactory(scene, locationId);
    }

    /// <summary>
    /// Returns true when this controller is backed by the new Scene system.
    /// </summary>
    public bool IsSceneBacked => _scene != null;

    /// <summary>The scene backing this controller, or null for legacy graph mode.</summary>
    public Cathedral.Game.Scene.Scene? Scene => _scene;

    /// <summary>The current point of view, or null for legacy graph mode.</summary>
    public PoV? CurrentPoV => _pov;

    /// <summary>
    /// Start the observation phase (generates observations asynchronously).
    /// This clears all history - use for initial start only.
    /// </summary>
    public void StartObservationPhase()
    {
        _narrationState.Clear();
        _scrollBuffer.Clear();
        _activePartyMember = _protagonist;
        _memberNoeticPoints.Clear();
        
        // Place NPCs into nodes based on a randomly selected time period
        var period = TimePeriodExtensions.Random(_diceRandom);
        _graph.TimeUpdate(period);
        Console.WriteLine($"NarrativeController: Time period is {period}");

        _narrationState.IsLoadingObservations = true;
        _narrationState.LoadingMessage = Config.LoadingMessages.GeneratingObservations;

        // Fire-and-forget async task
        _ = GenerateObservationsAsync();

        Console.WriteLine("NarrativeController: Started observation phase");
    }
    
    /// <summary>
    /// Start the observation phase while preserving scroll buffer history.
    /// Used when transitioning to a new node after a successful action.
    /// </summary>
    private void StartObservationPhaseWithHistory()
    {
        // Note: ResetForNewNode() should already be called before this
        _activePartyMember = _protagonist;
        _memberNoeticPoints.Clear(); // New node — everyone starts with a fresh counter
        // Re-apply the current time period so new node gets its NPCs placed
        _graph.TimeUpdate(_graph.CurrentPeriod);
        
        // Just set loading state and start generation
        _narrationState.IsLoadingObservations = true;
        _narrationState.LoadingMessage = Config.LoadingMessages.GeneratingObservations;
        
        Console.WriteLine($"NarrativeController: Started observation phase (with history preserved)");
        Console.WriteLine($"  History lines: {_scrollBuffer.HistoryLineCount}");
        Console.WriteLine($"  Total lines: {_scrollBuffer.TotalLines}");
        Console.WriteLine($"  Scroll offset: {_scrollBuffer.ScrollOffset}");
        
        // Fire-and-forget async task
        _ = GenerateObservationsAsync();
    }
    
    /// <summary>
    /// Generate observations from selected modiMentis (async).
    /// </summary>
    private async Task GenerateObservationsAsync()
    {
        try
        {
            Console.WriteLine("NarrativeController: Calling ObservationPhaseController...");
            
            // Generate ONE overall observation (one sentence per sampled outcome)
            var blocks = await _observationController.ExecuteObservationPhaseAsync(
                _currentNode,
                _protagonist
            );
            
            Console.WriteLine($"NarrativeController: Generated {blocks.Count} observation blocks");
            
            // Add blocks to scroll buffer
            foreach (var block in blocks)
            {
                _scrollBuffer.AddBlock(block);
                _narrationState.AddBlock(block);
            }
            
            // Scroll to show the new observation at the bottom of the view
            _scrollBuffer.ScrollToBottom();
            _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;
            
            // Update state
            _narrationState.IsLoadingObservations = false;
            _narrationState.ErrorMessage = null;
            
            Console.WriteLine("NarrativeController: Observation phase complete");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"NarrativeController: Error generating observations: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            
            _narrationState.IsLoadingObservations = false;
            _narrationState.ErrorMessage = $"Failed to generate observations: {ex.Message}";
        }
    }
    
    /// <summary>
    /// Execute thinking phase with selected modusMentis and keyword (async).
    /// </summary>
    private async Task ExecuteThinkingPhaseAsync(ModusMentis thinkingModusMentis, string keyword)
    {
        // Get the source observation block from the hovered keyword (for modusMentis chain tracking)
        var sourceObservationBlock = _narrationState.HoveredKeyword?.SourceBlock;

        try
        {
            Console.WriteLine($"NarrativeController: Executing thinking with {thinkingModusMentis.DisplayName} on keyword '{keyword}'");

            // Resolve the outcome linked to the clicked keyword via KeywordOutcomeMap or LinkedOutcome
            ConcreteOutcome? targetOutcome = null;
            if (sourceObservationBlock?.KeywordOutcomeMap?.TryGetValue(keyword, out var kmo) == true)
                targetOutcome = kmo;
            else
                targetOutcome = sourceObservationBlock?.LinkedOutcome;

            if (targetOutcome == null)
            {
                throw new Exception($"No outcome found for keyword '{keyword}'");
            }

            // Get action modiMentis from the active party member
            var actionModiMentis = _activePartyMember.GetActionModiMentis();

            Console.WriteLine($"NarrativeController: Outcome '{targetOutcome.DisplayName}', {actionModiMentis.Count} action modiMentis");

            // Call ThinkingExecutor — new single-outcome 3-call pipeline
            var response = await _thinkingExecutor.GenerateThinkingAsync(
                thinkingModusMentis,
                targetOutcome,
                keyword,
                _currentNode,
                actionModiMentis,
                _protagonist,
                _worldContext,
                CancellationToken.None);

            if (response == null)
            {
                throw new Exception("Thinking LLM returned null response");
            }

            bool hasActions = response.Actions.Count > 0;
            Console.WriteLine(hasActions
                ? $"NarrativeController: Generated {response.Actions.Count} actions"
                : "NarrativeController: Thinking chose to ignore — no action generated");

            // Create thinking block with reasoning + actions (null when ignored)
            // ChainOrigin points to the observation block that contained the clicked keyword
            var thinkingBlock = new NarrationBlock(
                Type: NarrationBlockType.Thinking,
                ModusMentis: thinkingModusMentis,
                Text: response.ReasoningText,
                Keywords: null,
                Actions: hasActions ? response.Actions : null,
                ChainOrigin: sourceObservationBlock
            );
            
            // Set ChainOrigin for each action to point to this thinking block
            foreach (var action in response.Actions)
            {
                action.ChainOrigin = thinkingBlock;
            }

            // Pre-compute difficulty for each action while still in loading state
            if (response.Actions.Count > 0)
            {
                _narrationState.LoadingMessage = Config.LoadingMessages.EvaluatingDifficulty;
                foreach (var act in response.Actions)
                {
                    var criticContext = new CriticContext(
                        _currentNode, _worldContext, _locationId,
                        act.PreselectedOutcome?.ToNaturalLanguageString() ?? "");
                    var difficultyTree = CriticTrees.BuildDifficultyTree(act.ActionText, criticContext);
                    var difficultyResult = await _actionExecutor.CriticEvaluator.EvaluateTreeAsync(difficultyTree);
                    act.DifficultyLevel = CriticTrees.CalculateFinalDifficulty(act.Verb, difficultyResult);
                    Console.WriteLine($"NarrativeController: Pre-computed difficulty for '{act.DisplayText}': {act.DifficultyLevel}/10");
                }
            }
            
            // Add to scroll buffer
            _scrollBuffer.AddBlock(thinkingBlock);
            _narrationState.AddBlock(thinkingBlock);
            
            // Auto-scroll to bottom to show new thinking block
            _scrollBuffer.ScrollToBottom();
            _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset; // Sync scroll position
            
            // Update state
            _narrationState.IsLoadingThinking = false;
            _narrationState.ThinkingAttemptsRemaining--;
            _narrationState.ErrorMessage = null;
            
            Console.WriteLine($"NarrativeController: Thinking phase complete ({_narrationState.ThinkingAttemptsRemaining} attempts remaining)");
            
            // In debug mode, print available actions and their outcomes to console
            if (DebugMode.IsActive && response.Actions.Count > 0)
            {
                DebugMode.PrintAvailableActions(response.Actions);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"NarrativeController: Error during thinking phase: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            
            _narrationState.IsLoadingThinking = false;
            _narrationState.ErrorMessage = $"Thinking failed: {ex.Message}";
        }
    }
    
    /// <summary>
    /// Execute action phase: modusMentis check, outcome determination, and narration (async).
    /// Uses phased approach with different UI states:
    /// - Phase 1 (Evaluation): Normal loading screen during plausibility + difficulty checks
    /// - Phase 2 (Dice Roll): Dice rolling animation during failure evaluation + narration
    /// </summary>
    private async Task ExecuteActionPhaseAsync(ParsedNarrativeAction action)
    {
        try
        {
            Console.WriteLine($"NarrativeController: Starting action execution for '{action.ActionText}'");
            
            // In debug mode, prompt overall strategy before executing
            if (DebugMode.IsActive)
            {
                string outcomeSummary = action.PreselectedOutcome != null
                    ? $"{action.PreselectedOutcome.GetType().Name} → {action.PreselectedOutcome.DisplayName}"
                    : "unknown";
                DebugMode.PromptActionStrategy(action.ActionText, outcomeSummary);
            }
            
            // === CODED RULES CHECK (before LLM — fast, deterministic, absolute) ===

            // Determine if the action is illegal so we know whether to compute witness context.
            bool isIllegalAction = !action.Verb.IsLegal || (_pov?.Where.IsPrivate ?? false);

            // Compute witness context (visual = same area, audio = adjacent area).
            var witnessContext = (isIllegalAction && _scene != null && _pov != null)
                ? WitnessSelector.ComputeContext(_scene, _pov)
                : WitnessContext.None;

            // Compute threat context (enemy proximity) — always, not just for illegal actions.
            var threatContext = (_scene != null && _pov != null && _protagonist != null)
                ? ThreatSelector.ComputeContext(_scene, _pov, _protagonist)
                : ThreatContext.None;

            // Run all coded rules; a failure here is absolute — no LLM retry, no noetic cost.
            var ruleCtx = new Narrative.Rules.ActionRuleContext(
                action, _protagonist!, _scene, _pov, witnessContext, threatContext);
            var ruleResult = Narrative.Rules.ActionRulesChecker.Check(ruleCtx);
            if (!ruleResult.Passed)
            {
                Console.WriteLine($"NarrativeController: Coded rule blocked action — {ruleResult.ErrorMessage}");
                action.IsImpossible = true;
                _narrationState.IsLoadingAction = false;

                var ruleBlock = new NarrationBlock(
                    Type: NarrationBlockType.Outcome,
                    ModusMentis: action.ThinkingModusMentis,
                    Text: $"[IMPOSSIBLE] {ruleResult.ErrorMessage}",
                    Keywords: null,
                    Actions: null);
                _scrollBuffer.AddBlock(ruleBlock);
                _narrationState.AddBlock(ruleBlock);
                _scrollBuffer.ScrollToBottom();
                _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;

                Console.WriteLine($"NarrativeController: Coded rule failure - consumed 1 noetic point ({_narrationState.ThinkingAttemptsRemaining} remaining)");

                if (_narrationState.ThinkingAttemptsRemaining > 0)
                {
                    _narrationState.ThinkingAttemptsRemaining--;
                    return;
                }
                else
                {
                    _narrationState.ShowContinueButton = true;
                    return;
                }
            }

            // === PHASE 1: EVALUATION (normal loading screen) ===
            _narrationState.IsLoadingAction = true;
            _narrationState.LoadingMessage = Config.LoadingMessages.EvaluatingAction;

            // Evaluate plausibility and difficulty (+ witness detection + under-threat questions if relevant)
            var evalResult = await _actionExecutor.EvaluateActionAsync(
                action,
                _currentNode,
                action.ThinkingModusMentis,
                witnessContext,
                threatContext,
                CancellationToken.None
            );
            
            // Handle plausibility failure
            if (!evalResult.IsPlausible)
            {
                Console.WriteLine($"NarrativeController: Action failed plausibility check");
                action.IsImpossible = true;

                // Generate plausibility failure narration
                var plausibilityResult = await _actionExecutor.GeneratePlausibilityFailureNarrationAsync(
                    evalResult, CancellationToken.None);
                
                _narrationState.IsLoadingAction = false;
                
                // Add outcome narration block
                var plausibilityBlock = new NarrationBlock(
                    Type: NarrationBlockType.Outcome,
                    ModusMentis: plausibilityResult.ActionModusMentis ?? throw new InvalidOperationException("Action modusMentis cannot be null"),
                    Text: $"[IMPOSSIBLE] {plausibilityResult.Narration}",
                    Keywords: null,
                    Actions: null
                );
                _scrollBuffer.AddBlock(plausibilityBlock);
                _narrationState.AddBlock(plausibilityBlock);
                
                // Auto-scroll to bottom to show outcome
                _scrollBuffer.ScrollToBottom();
                _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;
                
                Console.WriteLine($"NarrativeController: Plausibility failure - consumed 1 noetic point ({_narrationState.ThinkingAttemptsRemaining} remaining)");
                
                // If player still has noetic points, let them try again (no graying, no continue button)
                if (_narrationState.ThinkingAttemptsRemaining > 0)
                {
                    Console.WriteLine($"NarrativeController: Player can retry with {_narrationState.ThinkingAttemptsRemaining} noetic points remaining");
                    // Decrement noetic points for attempting an impossible action
                    _narrationState.ThinkingAttemptsRemaining--;
                    // Don't show continue button, don't grey out - player can interact normally
                    return;
                }
                else
                {
                    Console.WriteLine("NarrativeController: No noetic points remaining - showing continue button");
                    // No more noetic points - show continue button and grey out like a normal failure
                    _narrationState.ShowContinueButton = true;
                    return;
                }
            }
            
            // === PHASE 2: DICE ROLL (dice rolling animation) ===
            Console.WriteLine($"NarrativeController: Action passed plausibility, starting dice roll phase");

            // Number of dice = total modusMentis level summed across the chain
            int numberOfDice = Math.Max(1, action.GetTotalModusMentisLevel());

            // Difficulty = number of 6s needed to succeed (1-10, from LLM evaluation)
            int actualDifficulty = evalResult.DifficultyLevel;

            // Start dice roll animation
            _narrationState.StartDiceRoll(numberOfDice, actualDifficulty);
            _narrationState.LoadingMessage = "Rolling dice...";

            // Roll each die independently (1–6) and count sixes
            int[] finalDiceValues;
            bool succeeded;
            if (DebugMode.IsActive && !DebugMode.IsAutoStrategy)
            {
                succeeded = DebugMode.GetDiceRollOverride(action.ActionText, evalResult.SuccessProbability);
                finalDiceValues = GenerateDiceValuesForResult(numberOfDice, actualDifficulty, succeeded);
            }
            else
            {
                finalDiceValues = new int[numberOfDice];
                for (int i = 0; i < numberOfDice; i++)
                    finalDiceValues[i] = _diceRandom.Next(1, 7);
                int sixesCount = finalDiceValues.Count(v => v == 6);
                succeeded = sixesCount >= actualDifficulty;
            }

            Console.WriteLine($"NarrativeController: Rolled {finalDiceValues.Count(v => v == 6)} sixes out of {numberOfDice} dice (need {actualDifficulty}) → {(succeeded ? "SUCCESS" : "FAILURE")}");

            // Execute dice roll phase (failure outcome evaluation + narration generation)
            var result = await _actionExecutor.ExecuteDiceRollAsync(
                evalResult,
                succeeded,
                CancellationToken.None
            );

            Console.WriteLine($"NarrativeController: Action {(result.Succeeded ? "SUCCEEDED" : "FAILED")}");

            // Store the action result for later (when player clicks continue on dice screen)
            _pendingActionResult = result;

            // Complete the dice roll (stops animation, shows final values and continue button)
            _narrationState.CompleteDiceRoll(finalDiceValues);
            _narrationState.IsLoadingAction = false;

            Console.WriteLine($"NarrativeController: Dice roll complete - {finalDiceValues.Count(v => v == 6)} sixes rolled, difficulty {actualDifficulty}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"NarrativeController: Error during action execution: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            
            _narrationState.IsLoadingAction = false;
            _narrationState.ClearDiceRoll();
            _narrationState.ErrorMessage = $"Action execution failed: {ex.Message}";
        }
    }
    
    /// <summary>
    /// Generate dice values that match the success/failure result.
    /// </summary>
    private int[] GenerateDiceValuesForResult(int numberOfDice, int difficulty, bool succeeded)
    {
        int[] values = new int[numberOfDice];
        
        if (succeeded)
        {
            // Ensure at least 'difficulty' sixes
            int sixesNeeded = difficulty;
            int sixesPlaced = 0;
            
            for (int i = 0; i < numberOfDice; i++)
            {
                if (sixesPlaced < sixesNeeded && i < numberOfDice - (sixesNeeded - sixesPlaced - 1))
                {
                    // Need to place a 6 (with some randomness)
                    if (_diceRandom.Next(3) == 0 || i >= numberOfDice - (sixesNeeded - sixesPlaced))
                    {
                        values[i] = 6;
                        sixesPlaced++;
                        continue;
                    }
                }
                values[i] = _diceRandom.Next(1, 7); // 1-6
                if (values[i] == 6) sixesPlaced++;
            }
            
            // Guarantee enough sixes if we still need some
            while (sixesPlaced < sixesNeeded)
            {
                int idx = _diceRandom.Next(numberOfDice);
                if (values[idx] != 6)
                {
                    values[idx] = 6;
                    sixesPlaced++;
                }
            }
        }
        else
        {
            // Ensure fewer than 'difficulty' sixes
            int maxSixes = difficulty - 1;
            int sixesPlaced = 0;
            
            for (int i = 0; i < numberOfDice; i++)
            {
                values[i] = _diceRandom.Next(1, 7); // 1-6
                if (values[i] == 6)
                {
                    sixesPlaced++;
                    if (sixesPlaced > maxSixes)
                    {
                        // Too many sixes, reroll
                        values[i] = _diceRandom.Next(1, 6); // 1-5
                    }
                }
            }
        }
        
        // Shuffle for natural appearance
        for (int i = values.Length - 1; i > 0; i--)
        {
            int j = _diceRandom.Next(i + 1);
            (values[i], values[j]) = (values[j], values[i]);
        }
        
        return values;
    }
    
    /// <summary>
    /// Handle continue button click on dice roll screen.
    /// Applies the pending action result and shows outcome.
    /// </summary>
    private void OnDiceRollContinue()
    {
        if (_pendingActionResult == null)
        {
            Console.WriteLine("NarrativeController: No pending action result for dice roll continue");
            _narrationState.ClearDiceRoll();
            return;
        }
        
        var result = _pendingActionResult;
        _pendingActionResult = null;
        
        Console.WriteLine($"NarrativeController: Dice roll continue - applying result");
        
        // Add outcome narration block
        var outcomeBlock = new NarrationBlock(
            Type: NarrationBlockType.Outcome,
            ModusMentis: result.ActionModusMentis ?? throw new InvalidOperationException("Action modusMentis cannot be null"),
            Text: $"[{(result.Succeeded ? "SUCCESS" : "FAILURE")}] {result.Narration}",
            Keywords: null,
            Actions: null
        );
        _scrollBuffer.AddBlock(outcomeBlock);
        _narrationState.AddBlock(outcomeBlock);
        
        // Auto-scroll to bottom to show outcome
        _scrollBuffer.ScrollToBottom();
        _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;
        
        // Clear dice roll state
        _narrationState.ClearDiceRoll();

        // === FAILURE-PATH WITNESS CONFRONTATION (step 4b) ===
        // On failure, the executor already asked the LLM whether the witness noticed.
        // If detected, override the normal failure flow with a caught-red-handed dialogue.
        if (!result.Succeeded && result.WitnessDetected && result.DetectedWitness != null && _pov != null)
        {
            var crimeType = DetermineCrimeType(result.Action.Verb, _pov.Where.IsPrivate);
            Console.WriteLine($"NarrativeController: Witness '{result.DetectedWitness.DisplayName}' detected failed illegal action (crime: {crimeType})");
            var catchTree = CaughtRedHandedTreeFactory.Create(crimeType, result.DetectedWitness.IsBrave);
            _pendingDialogueOutcome = new Cathedral.Game.Narrative.DialogueOutcome(result.DetectedWitness, tree: catchTree);
            return;
        }

        // === FAILURE-PATH ENEMY OPPORTUNITY ATTACK (step 4c) ===
        // On failure, the executor asked the LLM whether the enemy seized an opportunity.
        // If triggered, skip normal outcome and queue a fight immediately.
        if (!result.Succeeded && result.FightTriggered && result.FightEnemy != null)
        {
            Console.WriteLine($"NarrativeController: Enemy '{result.FightEnemy.DisplayName}' seized opportunity — triggering fight");
            _pendingFightOutcome = new FightOutcome(result.FightEnemy, $"opportunity attack by {result.FightEnemy.DisplayName}");
            return;
        }

        // Handle outcome based on type - show continue button for next step
        if (result.ActualOutcome is FightOutcome fightOutcome)
        {
            Console.WriteLine($"NarrativeController: Fight outcome with {fightOutcome.Target.DisplayName}, signaling fight mode");
            _pendingFightOutcome = fightOutcome;
            // Don't show continue button - the game controller will detect the pending fight and switch modes
        }
        else if (result.ActualOutcome is DialogueOutcome dialogueOutcome)
        {
            Console.WriteLine($"NarrativeController: Dialogue outcome with {dialogueOutcome.Target.DisplayName}, signaling dialogue mode");
            _pendingDialogueOutcome = dialogueOutcome;
            // Don't show continue button - the game controller will detect the pending dialogue and switch modes
        }
        else if (result.ActualOutcome is NarrationNode nextNode)
        {
            Console.WriteLine($"NarrativeController: Transition outcome to node {nextNode.NodeId}, showing continue button");
            _narrationState.PendingTransitionNode = nextNode;
            _narrationState.ShowContinueButton = true;
        }
        else if (result.ActualOutcome is VerbOutcome verbOutcome && _scene != null && _pov != null)
        {
            Console.WriteLine($"NarrativeController: Verb outcome '{verbOutcome.VerbView.Verb.VerbId}' on '{verbOutcome.Target?.DisplayName}', executing verb");
            verbOutcome.VerbView.Verb.Execute(_scene, _pov, _protagonist, verbOutcome.Target!);
            SceneDebugManager.UpdatePoV(_pov);

            // Check if the verb requested a dialogue session
            if (_scene.PendingDialogueRequest != null)
            {
                var req = _scene.PendingDialogueRequest;
                _scene.PendingDialogueRequest = null;
                _pendingDialogueOutcome = new Cathedral.Game.Narrative.DialogueOutcome(req.Npc, req.TreeId);
                Console.WriteLine($"NarrativeController: Dialogue verb triggered tree '{req.TreeId}' with {req.Npc.DisplayName}");
                return;
            }

            // Check if the verb requested a fight (e.g. AttackVerb)
            if (_scene.PendingFightRequest != null)
            {
                var req = _scene.PendingFightRequest;
                _scene.PendingFightRequest = null;
                _pendingFightOutcome = new FightOutcome(req.Npc, $"attack on {req.Npc.DisplayName}");
                Console.WriteLine($"NarrativeController: Attack verb triggered fight with {req.Npc.DisplayName}");
                return;
            }

            // MoveToAreaVerb: stay in scene and transition to the target area's node
            if (verbOutcome.VerbView.Verb is Cathedral.Game.Scene.Verbs.MoveToAreaVerb
                && verbOutcome.Target is Cathedral.Game.Scene.Area movedArea)
            {
                var nodeId = movedArea.DisplayName.ToLowerInvariant().Replace(' ', '_');
                if (_graph.AllNodes.TryGetValue(nodeId, out var areaNode))
                {
                    Console.WriteLine($"NarrativeController: MoveToAreaVerb — transitioning to node '{nodeId}'");
                    _narrationState.PendingTransitionNode = areaNode;
                    _narrationState.ShowContinueButton = true;
                    return;
                }
            }

            _narrationState.PendingTransitionNode = null;
            _narrationState.ShouldExitOnContinue = IsMovementAction(result.Action);
            _narrationState.ShowContinueButton = true;
        }
        else
        {
            Console.WriteLine("NarrativeController: Non-transition outcome, showing continue button");
            _narrationState.PendingTransitionNode = null;
            _narrationState.ShouldExitOnContinue = IsMovementAction(result.Action);
            _narrationState.ShowContinueButton = true;
        }

        // Refresh debug window to reflect any state changes
        if (_pov != null)
            SceneDebugManager.UpdatePoV(_pov);

        Console.WriteLine("NarrativeController: Action phase complete");
    }
    
    /// <summary>
    /// Execute focus observation phase: generate a detailed observation for a specific outcome (async).
    /// Triggered by right-clicking a keyword and selecting an observation modusMentis.
    /// </summary>
    private async Task ExecuteFocusObservationAsync(ModusMentis observationModusMentis, ConcreteOutcome focusOutcome)
    {
        try
        {
            Console.WriteLine($"NarrativeController: Executing focus observation with {observationModusMentis.DisplayName} on outcome '{focusOutcome.DisplayName}'");

            var blocks = await _observationController.GenerateFocusObservationAsync(
                focusOutcome,
                observationModusMentis,
                _currentNode,
                _protagonist
            );

            foreach (var block in blocks)
            {
                _scrollBuffer.AddBlock(block);
                _narrationState.AddBlock(block);
            }

            // Auto-scroll to bottom to show new observation
            _scrollBuffer.ScrollToBottom();
            _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;

            // Consume a thinking point (same pool as thinking)
            _narrationState.ThinkingAttemptsRemaining--;

            // Update state
            _narrationState.IsLoadingFocusObservation = false;
            _narrationState.ErrorMessage = null;

            Console.WriteLine($"NarrativeController: Focus observation phase complete ({_narrationState.ThinkingAttemptsRemaining} attempts remaining)");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"NarrativeController: Error during focus observation: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);

            _narrationState.IsLoadingFocusObservation = false;
            _narrationState.ErrorMessage = $"Focus observation failed: {ex.Message}";
        }
    }
    
    /// Persist the active member's current noetic counter into the per-member dictionary.
    private void SaveActiveNoeticPoints()
    {
        _memberNoeticPoints[_activePartyMember.DisplayName] = _narrationState.ThinkingAttemptsRemaining;
    }

    /// Load a member's noetic counter from the dictionary (full if they haven't acted yet).
    private void LoadNoeticPoints(PartyMember member)
    {
        if (!_memberNoeticPoints.TryGetValue(member.DisplayName, out var points))
            points = NarrativeUI.GetMaxThinkingAttempts();
        _narrationState.ThinkingAttemptsRemaining = points;
    }

    /// <summary>
    /// Speak About phase: active party member speaks directly to a companion about a keyword.
    /// Greys out current text, preserves noetic points, adds the speaking block as the new
    /// observation root, and switches the active party member to the companion.
    /// </summary>
    private async Task ExecuteSpeakingPhaseAsync(
        ModusMentis speakingModusMentis,
        Companion companion,
        KeywordRegion keywordRegion)
    {
        string keyword = keywordRegion.Keyword;
        var sourceBlock = keywordRegion.SourceBlock;

        try
        {
            Console.WriteLine($"NarrativeController: Speaking phase — skill={speakingModusMentis.DisplayName}, companion={companion.Name}, keyword='{keyword}'");

            // Resolve the outcome linked to this keyword
            ConcreteOutcome? linkedOutcome = null;
            if (sourceBlock?.KeywordOutcomeMap?.TryGetValue(keyword, out var ko) == true)
                linkedOutcome = ko;
            else
                linkedOutcome = sourceBlock?.LinkedOutcome;

            if (linkedOutcome == null)
            {
                Console.Error.WriteLine($"NarrativeController: Speaking — no outcome found for keyword '{keyword}'");
                _narrationState.IsLoadingSpeaking = false;
                return;
            }

            var speakingBlock = await _observationController.GenerateSpeakingTextAsync(
                keyword,
                speakingModusMentis,
                companion.Name,
                linkedOutcome,
                _currentNode,
                _protagonist,
                _worldContext
            );

            if (speakingBlock == null)
            {
                Console.Error.WriteLine("NarrativeController: Speaking generation returned null.");
                _narrationState.IsLoadingSpeaking = false;
                _narrationState.ErrorMessage = "Speaking failed — no text generated.";
                return;
            }

            // Grey out current content and reset without spending all noetic points
            _scrollBuffer.ConvertToHistory();
            _narrationState.ResetForPartyMemberChange();
            _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;

            // Speaking block is the new observation root for this sequence
            _scrollBuffer.AddBlock(speakingBlock);
            _narrationState.AddBlock(speakingBlock);
            _scrollBuffer.ScrollToBottom();
            _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;

            // Consume one noetic point from the speaker's own pool, then save it.
            _narrationState.ThinkingAttemptsRemaining--;
            SaveActiveNoeticPoints();

            // Switch to companion and load their own counter (fresh if first hand-off to them).
            _activePartyMember = companion;
            LoadNoeticPoints(companion);

            _narrationState.IsLoadingSpeaking = false;
            _narrationState.ErrorMessage = null;

            Console.WriteLine($"NarrativeController: Speaking phase complete — active party member is now {companion.Name}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"NarrativeController: Speaking phase error: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            _narrationState.IsLoadingSpeaking = false;
            _narrationState.ErrorMessage = $"Speaking failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Update loop - called at 10 Hz by game controller.
    /// </summary>
    public void Update()
    {
        // Clear terminal
        _ui.Clear();
        
        // Render header
        _ui.RenderHeader(_currentNode.DisplayName, _narrationState.ThinkingAttemptsRemaining, _worldContext, _activePartyMember.DisplayName, _graph.CurrentPeriod);
        
        // Show error if present
        if (_narrationState.ErrorMessage != null)
        {
            _ui.ShowError(_narrationState.ErrorMessage);
            _ui.RenderStatusBar("Press ESC to return to world view");
            return;
        }
        
        // Show dice roll screen if active (for action execution)
        if (_narrationState.IsDiceRollActive)
        {
            bool hasContinueButton = _ui.ShowDiceRollIndicator(
                _narrationState.DiceRollNumberOfDice,
                _narrationState.DiceRollDifficulty,
                _narrationState.IsDiceRolling,
                _narrationState.DiceRollFinalValues,
                _narrationState.IsDiceRollButtonHovered
            );
            
            string diceStatus = _narrationState.IsDiceRolling 
                ? "Rolling dice..." 
                : (_narrationState.DiceRollSucceeded ? "Success! Click Continue to see the outcome" : "Failed! Click Continue to see the outcome");
            _ui.RenderStatusBar(diceStatus);
            return;
        }
        
        // Show loading indicator if generating (for non-action loading, or action evaluation phase before dice roll)
        bool isLoadingNonDice = _narrationState.IsLoadingObservations || _narrationState.IsLoadingThinking ||
                                _narrationState.IsLoadingFocusObservation || _narrationState.IsLoadingSpeaking ||
                                (_narrationState.IsLoadingAction && !_narrationState.IsDiceRollActive);
        if (isLoadingNonDice)
        {
            _ui.ShowLoadingIndicator(_narrationState.LoadingMessage);
            string loadingStatus = _narrationState.IsLoadingObservations 
                ? "Generating observations..." 
                : _narrationState.IsLoadingThinking
                    ? "Generating thinking and actions..."
                    : _narrationState.IsLoadingAction
                        ? "Evaluating action..."
                        : "Generating focus observation...";
            _ui.RenderStatusBar(loadingStatus);
            return;
        }
        
        // Show continue button if flagged
        if (_narrationState.ShowContinueButton)
        {
            // Render narration blocks (non-interactive, dimmed)
            _ui.RenderObservationBlocks(
                _scrollBuffer,
                _narrationState.ScrollOffset,
                _narrationState.ThinkingAttemptsRemaining,
                null, // No keyword hover
                null, // No action hover
                true  // Dim content when continue button is shown
            );
            
            // Render scrollbar (still visible when continue button shown)
            _narrationState.ScrollbarThumb = _ui.RenderScrollbar(
                _scrollBuffer,
                _narrationState.ScrollOffset,
                _narrationState.IsScrollbarThumbHovered
            );
            
            // Render continue button
            var buttonRegion = _ui.RenderContinueButton(_narrationState.IsContinueButtonHovered);
            
            // Track button region for click detection (reuse ActionRegion for simplicity)
            _narrationState.ActionRegions.Clear();
            _narrationState.ActionRegions.Add(new ActionRegion(
                0, 
                buttonRegion.Y, 
                buttonRegion.Y, 
                buttonRegion.X, 
                buttonRegion.X + buttonRegion.Width
            ));
            
            _ui.RenderStatusBar("Click Continue to return to world view");
            return;
        }
        
        // Render observation blocks with keywords
        _ui.RenderObservationBlocks(
            _scrollBuffer,
            _narrationState.ScrollOffset,
            _narrationState.ThinkingAttemptsRemaining,
            _narrationState.HoveredKeyword,
            _narrationState.HoveredAction
        );
        
        // Render scrollbar and update thumb region
        _narrationState.ScrollbarThumb = _ui.RenderScrollbar(
            _scrollBuffer,
            _narrationState.ScrollOffset,
            _narrationState.IsScrollbarThumbHovered
        );
        
        // Render status bar - show modusMentis chain dice count when hovering over an action
        string statusMessage;
        if (_narrationState.HoveredAction?.Action != null)
        {
            // Calculate total modusMentis level from the modusMentis chain
            int totalDice = _narrationState.HoveredAction.Action.GetTotalModusMentisLevel();
            statusMessage = $"Click to attempt this action with {totalDice}{Config.Symbols.ModusMentisLevelIndicator} in the modusMentis check";
        }
        else if (_narrationState.ThinkingAttemptsRemaining > 0)
        {
            statusMessage = $"Hover keywords to highlight • Click keywords to think ({_narrationState.ThinkingAttemptsRemaining} attempts remaining)";
        }
        else
        {
            statusMessage = "No thinking attempts remaining • Explore keywords to continue";
        }
        _ui.RenderStatusBar(statusMessage, _narrationState.HoveredAction?.Action);
    }
    
    /// <summary>
    /// Handle raw mouse move event with screen pixel coordinates.
    /// Used when popup is visible to bypass terminal cell coordinate system.
    /// </summary>
    public void OnRawMouseMove(Vector2 screenPosition)
    {
        // Get cell size for hit detection (shared by both popups)
        var layoutInfo = _terminalInputHandler.GetLayoutInfo(_core.ClientSize);
        float cellPixelSize = layoutInfo.CellSize.X;

        if (_modusMentisPopup.IsVisible)
            _modusMentisPopup.UpdateHover(screenPosition.X, screenPosition.Y, _core.ClientSize, cellPixelSize);

        if (_itemSelectionPopup.IsVisible)
            _itemSelectionPopup.UpdateHover(screenPosition.X, screenPosition.Y, _core.ClientSize, cellPixelSize);

        if (_choicePopup.IsVisible)
            _choicePopup.UpdateHover(screenPosition.X, screenPosition.Y, _core.ClientSize, cellPixelSize);
    }
    
    /// <summary>
    /// Handle raw mouse click event with screen pixel coordinates.
    /// Used when popup is visible to bypass terminal cell coordinate system.
    /// </summary>
    public void OnRawMouseClick(Vector2 screenPosition)
    {
        var layoutInfo = _terminalInputHandler.GetLayoutInfo(_core.ClientSize);
        float cellPixelSize = layoutInfo.CellSize.X;

        // Choice popup (Think/Observe or Execute/Use Item) takes highest priority
        if (_choicePopup.IsVisible)
        {
            int? choiceIndex = _choicePopup.HandleClick(screenPosition.X, screenPosition.Y, _core.ClientSize, cellPixelSize);
            _narrationState.IsSelectingInteractionMode = false;
            DispatchChoiceSelection(choiceIndex);
            return;
        }

        // Item selection popup takes priority when visible
        if (_itemSelectionPopup.IsVisible)
        {
            var selectedItem = _itemSelectionPopup.HandleClick(screenPosition.X, screenPosition.Y, _core.ClientSize, cellPixelSize);
            if (selectedItem != null && _narrationState.ActionPendingItemCombination != null)
            {
                var pendingAction = _narrationState.ActionPendingItemCombination;
                _narrationState.IsSelectingItemForAction = false;
                _narrationState.ActionPendingItemCombination = null;
                _ = ExecuteItemCombinationAsync(pendingAction, selectedItem);
            }
            else
            {
                Console.WriteLine("NarrativeController: Item popup closed (clicked outside)");
                _narrationState.IsSelectingItemForAction = false;
                _narrationState.ActionPendingItemCombination = null;
            }
            return;
        }

        // If popup is visible, handle popup click with screen coordinates
        if (_modusMentisPopup.IsVisible)
        {
            var selectedModusMentis = _modusMentisPopup.HandleClick(screenPosition.X, screenPosition.Y, _core.ClientSize, cellPixelSize);
            if (selectedModusMentis != null)
            {
                Console.WriteLine($"NarrativeController: Selected modusMentis: {selectedModusMentis.DisplayName}");

                if (_narrationState.IsSelectingModusMentisForSpeaking)
                {
                    // Step 1 of Speak About: speaking modusMentis selected → show companion selection
                    _narrationState.IsSelectingModusMentisForSpeaking = false;
                    _narrationState.SpeakingModusMentisPending = selectedModusMentis;
                    _pendingCompanions = _protagonist.CompanionParty.ToList();
                    var companionNames = _pendingCompanions.Select(c => c.Name).ToList();
                    _narrationState.IsSelectingCompanionForSpeaking = true;
                    Vector2 screenPos2 = _terminalInputHandler.CellToScreen(_lastMouseX, _lastMouseY, _core.ClientSize);
                    _choicePopup.Show(screenPos2, companionNames, "Who do you address?");
                }
                // Get the keyword that was clicked (stored before popup appeared)
                else if (_narrationState.HoveredKeyword != null)
                {
                    string keyword = _narrationState.HoveredKeyword.Keyword;
                    var sourceBlock = _narrationState.HoveredKeyword.SourceBlock;

                    // Check if we're selecting an observation modusMentis or thinking modusMentis
                    if (_narrationState.IsSelectingObservationModusMentis)
                    {
                        // Focus observation phase
                        _narrationState.IsLoadingFocusObservation = true;
                        _narrationState.LoadingMessage = Config.LoadingMessages.GeneratingObservations;
                        _narrationState.IsSelectingObservationModusMentis = false;

                        // Resolve focus outcome: prefer KeywordOutcomeMap, then LinkedOutcome, then keyword lookup
                        ConcreteOutcome? focusOutcome = null;
                        if (sourceBlock?.KeywordOutcomeMap?.TryGetValue(keyword, out var fko) == true)
                            focusOutcome = fko;
                        else
                            focusOutcome = sourceBlock?.LinkedOutcome;

                        if (focusOutcome != null)
                            _ = ExecuteFocusObservationAsync(selectedModusMentis, focusOutcome);
                        else
                            Console.WriteLine($"NarrativeController: Cannot focus - no outcome found for keyword '{keyword}'");
                    }
                    else
                    {
                        // Thinking phase
                        _narrationState.IsLoadingThinking = true;
                        _narrationState.LoadingMessage = Config.LoadingMessages.ThinkingDeeply;
                        _ = ExecuteThinkingPhaseAsync(selectedModusMentis, keyword);
                    }
                }
            }
            else
            {
                Console.WriteLine("NarrativeController: Popup closed (clicked outside)");
                _narrationState.IsSelectingObservationModusMentis = false;
                _narrationState.IsSelectingModusMentisForSpeaking = false;
            }
        }
    }

    /// <summary>
    /// Handle mouse move event.
    /// </summary>
    public void OnMouseMove(int mouseX, int mouseY)
    {
        _lastMouseX = mouseX;
        _lastMouseY = mouseY;
        
        // If any popup is visible, raw mouse events are handled separately
        if (_modusMentisPopup.IsVisible || _itemSelectionPopup.IsVisible)
        {
            return;
        }
        
        // Handle dice roll screen hover
        if (_narrationState.IsDiceRollActive && !_narrationState.IsDiceRolling)
        {
            // Check if hovering over continue button on dice roll screen
            bool isOverButton = _ui.IsMouseOverDiceRollButton(mouseX, mouseY);
            if (isOverButton != _narrationState.IsDiceRollButtonHovered)
            {
                _narrationState.IsDiceRollButtonHovered = isOverButton;
            }
            return;
        }
        
        // Stop dragging if mouse button was released
        if (_narrationState.IsScrollbarDragging && !_terminalInputHandler.IsLeftMouseDown)
        {
            _narrationState.IsScrollbarDragging = false;
            Console.WriteLine("NarrativeController: Stopped scrollbar drag");
        }
        
        // Handle scrollbar dragging
        if (_narrationState.IsScrollbarDragging)
        {
            int deltaY = mouseY - _narrationState.ScrollbarDragStartY;
            
            var layout = new NarrativeLayout(
                _core.Terminal.Width, 
                _core.Terminal.Height, 
                Config.NarrativeUI.TopPadding, 
                Config.NarrativeUI.BottomPadding,
                Config.NarrativeUI.LeftPadding,
                Config.NarrativeUI.RightPadding);
            int trackHeight = layout.SCROLLBAR_TRACK_HEIGHT;
            int totalLines = _scrollBuffer.TotalLines;
            int visibleLines = layout.NARRATIVE_HEIGHT;
            
            int maxScrollOffset = layout.CalculateMaxScrollOffset(totalLines);
            
            // Calculate thumb size for proper scaling
            float visibleRatio = (float)visibleLines / totalLines;
            int thumbHeight = Math.Max(2, (int)(trackHeight * visibleRatio));
            int maxThumbY = trackHeight - thumbHeight;
            
            // Convert mouse delta to scroll offset delta
            float scrollRatio = maxThumbY > 0 ? (float)deltaY / maxThumbY : 0f;
            int newOffset = _narrationState.ScrollbarDragStartOffset + (int)(maxScrollOffset * scrollRatio);
            
            // Clamp and update scroll offset
            newOffset = Math.Clamp(newOffset, 0, maxScrollOffset);
            if (newOffset != _scrollBuffer.ScrollOffset)
            {
                _scrollBuffer.SetScrollOffset(newOffset);
                _narrationState.ScrollOffset = newOffset;
            }
            return;
        }
        
        // Update scrollbar thumb hover state (must be done before continue button check)
        bool isOverThumb = _ui.IsMouseOverScrollbarThumb(mouseX, mouseY, _narrationState.ScrollbarThumb);
        if (isOverThumb != _narrationState.IsScrollbarThumbHovered)
        {
            _narrationState.IsScrollbarThumbHovered = isOverThumb;
        }
        
        // If continue button is shown, check if mouse is over it
        if (_narrationState.ShowContinueButton && _narrationState.ActionRegions.Count > 0)
        {
            var buttonRegion = _narrationState.ActionRegions[0];
            bool isOverButton = buttonRegion.Contains(mouseX, mouseY);
            
            if (isOverButton != _narrationState.IsContinueButtonHovered)
            {
                _narrationState.IsContinueButtonHovered = isOverButton;
            }
            
            // Don't process keyword/action hover when continue button is shown, 
            // but scrollbar interactions are still allowed (processed above)
            return;
        }
        
        // Update hovered keyword region
        KeywordRegion? newHoveredKeyword = _ui.GetHoveredKeyword(mouseX, mouseY);
        
        if (newHoveredKeyword != _narrationState.HoveredKeyword)
        {
            _narrationState.HoveredKeyword = newHoveredKeyword;
            // UI will re-render on next Update() call
        }
        
        // Update hovered action region
        ActionRegion? newHoveredAction = _ui.GetHoveredAction(mouseX, mouseY);
        
        if (newHoveredAction != _narrationState.HoveredAction)
        {
            _narrationState.HoveredAction = newHoveredAction;
            // UI will re-render on next Update() call
        }
    }
    
    /// <summary>
    /// Handle mouse click event.
    /// </summary>
    public void OnMouseClick(int mouseX, int mouseY)
    {
        // Handle dice roll screen click
        if (_narrationState.IsDiceRollActive && !_narrationState.IsDiceRolling)
        {
            // Check if clicked on continue button
            if (_ui.IsMouseOverDiceRollButton(mouseX, mouseY))
            {
                Console.WriteLine("NarrativeController: Dice roll continue button clicked");
                OnDiceRollContinue();
            }
            return;
        }
        
        // Check if clicked on scrollbar thumb (start drag) - must be done before continue button check
        if (_ui.IsMouseOverScrollbarThumb(mouseX, mouseY, _narrationState.ScrollbarThumb))
        {
            _narrationState.IsScrollbarDragging = true;
            _narrationState.ScrollbarDragStartY = mouseY;
            _narrationState.ScrollbarDragStartOffset = _narrationState.ScrollOffset;
            Console.WriteLine($"NarrativeController: Started scrollbar drag at Y={mouseY}");
            return;
        }
        
        // Check if clicked on scrollbar track (jump scroll) - must be done before continue button check
        if (_ui.IsMouseOverScrollbarTrack(mouseX, mouseY, _narrationState.ScrollbarThumb))
        {
            int newOffset = _ui.CalculateScrollOffsetFromMouseY(mouseY, _scrollBuffer);
            _scrollBuffer.SetScrollOffset(newOffset);
            _narrationState.ScrollOffset = newOffset;
            Console.WriteLine($"NarrativeController: Jump scrolled to offset {newOffset}");
            return;
        }
        
        // If continue button is shown, check if clicked
        if (_narrationState.ShowContinueButton && _narrationState.ActionRegions.Count > 0)
        {
            var buttonRegion = _narrationState.ActionRegions[0];
            if (buttonRegion.Contains(mouseX, mouseY))
            {
                // Check if there's a pending transition to a new node
                if (_narrationState.PendingTransitionNode != null)
                {
                    Console.WriteLine($"NarrativeController: Continue button clicked, transitioning to {_narrationState.PendingTransitionNode.NodeId}");
                    
                    // Perform the transition
                    _currentNode = _narrationState.PendingTransitionNode;

                    // Convert current narration to history (grayed out, non-interactive)
                    _scrollBuffer.ConvertToHistory();
                    _narrationState.ResetForNewNode();
                    _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;

                    // Start new observation phase WITHOUT clearing history
                    StartObservationPhaseWithHistory();
                }
                else if (_narrationState.ShouldExitOnContinue)
                {
                    Console.WriteLine("NarrativeController: Continue button clicked — movement action, exiting to world view");
                    _narrationState.RequestedExit = true;
                }
                else
                {
                    Console.WriteLine("NarrativeController: Continue button clicked — staying in scene, restarting observation");
                    _scrollBuffer.ConvertToHistory();
                    _narrationState.ResetForNewNode();
                    _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;
                    StartObservationPhaseWithHistory();
                }
            }
            // When continue button is shown, don't process other clicks (keywords/actions)
            // but scrollbar clicks are allowed (processed above)
            return;
        }

        // If choice popup is visible, handle it first
        if (_choicePopup.IsVisible)
        {
            Vector2 correctedScreenPos = _terminalInputHandler.GetCorrectedMousePosition();
            var layoutInfoC = _terminalInputHandler.GetLayoutInfo(_core.ClientSize);
            float cellPixelSizeC = layoutInfoC.CellSize.X;

            int? choiceIndex = _choicePopup.HandleClick(correctedScreenPos.X, correctedScreenPos.Y, _core.ClientSize, cellPixelSizeC);
            _narrationState.IsSelectingInteractionMode = false;
            DispatchChoiceSelection(choiceIndex);
            return;
        }

        // If item selection popup is visible, handle item popup click
        if (_itemSelectionPopup.IsVisible)
        {
            Vector2 correctedScreenPos = _terminalInputHandler.GetCorrectedMousePosition();
            var layoutInfo = _terminalInputHandler.GetLayoutInfo(_core.ClientSize);
            float cellPixelSize = layoutInfo.CellSize.X;

            var selectedItem = _itemSelectionPopup.HandleClick(correctedScreenPos.X, correctedScreenPos.Y, _core.ClientSize, cellPixelSize);
            if (selectedItem != null && _narrationState.ActionPendingItemCombination != null)
            {
                var pendingAction = _narrationState.ActionPendingItemCombination;
                _narrationState.IsSelectingItemForAction = false;
                _narrationState.ActionPendingItemCombination = null;
                _ = ExecuteItemCombinationAsync(pendingAction, selectedItem);
            }
            else
            {
                Console.WriteLine("NarrativeController: Item popup closed (clicked outside)");
                _narrationState.IsSelectingItemForAction = false;
                _narrationState.ActionPendingItemCombination = null;
            }
            return;
        }

        // If modus mentis popup is visible, handle popup click with screen coordinates
        if (_modusMentisPopup.IsVisible)
        {
            // Get screen mouse position
            Vector2 correctedScreenPos = _terminalInputHandler.GetCorrectedMousePosition();

            // Get cell size for hit detection
            var layoutInfo = _terminalInputHandler.GetLayoutInfo(_core.ClientSize);
            float cellPixelSize = layoutInfo.CellSize.X;

            var selectedModusMentis = _modusMentisPopup.HandleClick(correctedScreenPos.X, correctedScreenPos.Y, _core.ClientSize, cellPixelSize);
            if (selectedModusMentis != null)
            {
                Console.WriteLine($"NarrativeController: Selected modusMentis: {selectedModusMentis.DisplayName}");

                if (_narrationState.IsSelectingModusMentisForSpeaking)
                {
                    // Step 1 of Speak About: speaking modusMentis selected → show companion selection
                    _narrationState.IsSelectingModusMentisForSpeaking = false;
                    _narrationState.SpeakingModusMentisPending = selectedModusMentis;
                    _pendingCompanions = _protagonist.CompanionParty.ToList();
                    var companionNames = _pendingCompanions.Select(c => c.Name).ToList();
                    _narrationState.IsSelectingCompanionForSpeaking = true;
                    Vector2 screenPos2 = _terminalInputHandler.CellToScreen(_lastMouseX, _lastMouseY, _core.ClientSize);
                    _choicePopup.Show(screenPos2, companionNames, "Who do you address?");
                }
                // Get the keyword that was clicked (stored before popup appeared)
                else if (_narrationState.HoveredKeyword != null)
                {
                    string keyword = _narrationState.HoveredKeyword.Keyword;
                    var sourceBlock = _narrationState.HoveredKeyword.SourceBlock;

                    // Check if we're selecting an observation modusMentis or thinking modusMentis
                    if (_narrationState.IsSelectingObservationModusMentis)
                    {
                        // Focus observation phase
                        _narrationState.IsLoadingFocusObservation = true;
                        _narrationState.LoadingMessage = Config.LoadingMessages.GeneratingObservations;
                        _narrationState.IsSelectingObservationModusMentis = false;

                        // Resolve focus outcome: prefer KeywordOutcomeMap, then LinkedOutcome, then keyword lookup
                        ConcreteOutcome? focusOutcome = null;
                        if (sourceBlock?.KeywordOutcomeMap?.TryGetValue(keyword, out var fko) == true)
                            focusOutcome = fko;
                        else
                            focusOutcome = sourceBlock?.LinkedOutcome;

                        if (focusOutcome != null)
                            _ = ExecuteFocusObservationAsync(selectedModusMentis, focusOutcome);
                        else
                            Console.WriteLine($"NarrativeController: Cannot focus - no outcome found for keyword '{keyword}'");
                    }
                    else
                    {
                        // Thinking phase
                        _narrationState.IsLoadingThinking = true;
                        _narrationState.LoadingMessage = Config.LoadingMessages.ThinkingDeeply;
                        _ = ExecuteThinkingPhaseAsync(selectedModusMentis, keyword);
                    }
                }
            }
            else
            {
                Console.WriteLine("NarrativeController: Popup closed (clicked outside)");
                _narrationState.IsSelectingObservationModusMentis = false;
                _narrationState.IsSelectingModusMentisForSpeaking = false;
            }
            return;
        }
        
        // Check if clicked on an action
        ActionRegion? clickedAction = _ui.GetHoveredAction(mouseX, mouseY);
        if (clickedAction != null)
        {
            // Collect all actions from all thinking blocks (globally indexed)
            var allActions = new List<ParsedNarrativeAction>();
            foreach (var block in _narrationState.Blocks)
            {
                if (block.Type == NarrationBlockType.Thinking && block.Actions != null)
                    allActions.AddRange(block.Actions);
            }

            if (clickedAction.ActionIndex < allActions.Count)
            {
                var action = allActions[clickedAction.ActionIndex];

                bool hasItems = action.CombinedItem == null && GetCombinableItems().Count > 0;
                bool canUseItem = hasItems && _narrationState.ThinkingAttemptsRemaining > 0;
                var disabledIndices = canUseItem ? new HashSet<int>() : new HashSet<int> { 1 };

                Console.WriteLine($"NarrativeController: Showing action mode choice for '{action.ActionText}' (hasItems={hasItems})");
                _narrationState.ActionPendingModeSelection = action;
                _narrationState.IsSelectingInteractionMode = true;
                _narrationState.InteractionModeIsForKeyword = false;
                Vector2 screenPos = _terminalInputHandler.CellToScreen(mouseX, mouseY, _core.ClientSize);
                _choicePopup.Show(screenPos, new List<string> { "Execute", "Use Item" }, "Action", disabledIndices);
            }
            else
            {
                Console.WriteLine($"NarrativeController: Failed to find action at index {clickedAction.ActionIndex}");
            }
            return;
        }

        // Check if clicked on a keyword
        KeywordRegion? clickedKeyword = _ui.GetHoveredKeyword(mouseX, mouseY);

        if (clickedKeyword != null && _narrationState.ThinkingAttemptsRemaining > 0)
        {
            Console.WriteLine($"NarrativeController: Clicked keyword: {clickedKeyword}");
            _narrationState.HoveredKeyword = clickedKeyword;
            _narrationState.IsSelectingInteractionMode = true;
            _narrationState.InteractionModeIsForKeyword = true;
            Vector2 screenPos = _terminalInputHandler.CellToScreen(mouseX, mouseY, _core.ClientSize);
            var speakChoices = new List<string> { "Think", "Observe", "Speak About" };
            var speakDisabled = new HashSet<int>();
            bool canSpeak = _activePartyMember.GetSpeakingModiMentis().Count > 0
                         && _narrationState.ThinkingAttemptsRemaining > 0
                         && _protagonist.CompanionParty.Count > 0;
            if (!canSpeak) speakDisabled.Add(2);
            _choicePopup.Show(screenPos, speakChoices, "Keyword Action", speakDisabled);
        }
    }

    /// <summary>
    /// Dispatches the result of the Think/Observe/SpeakAbout or Execute/Use Item choice popup.
    /// </summary>
    private void DispatchChoiceSelection(int? choiceIndex)
    {
        // Companion selection (step 2 of Speak About) — checked first because it also uses _choicePopup
        if (_narrationState.IsSelectingCompanionForSpeaking)
        {
            _narrationState.IsSelectingCompanionForSpeaking = false;
            if (choiceIndex.HasValue
                && choiceIndex.Value >= 0
                && choiceIndex.Value < _pendingCompanions.Count
                && _narrationState.SpeakingModusMentisPending != null
                && _narrationState.HoveredKeyword != null)
            {
                var companion   = _pendingCompanions[choiceIndex.Value];
                var speakingMM  = _narrationState.SpeakingModusMentisPending;
                _narrationState.SpeakingModusMentisPending = null;
                _pendingCompanions.Clear();
                Console.WriteLine($"NarrativeController: Speak About — companion={companion.Name}, skill={speakingMM.DisplayName}");
                _narrationState.IsLoadingSpeaking = true;
                _narrationState.LoadingMessage = Config.LoadingMessages.GeneratingObservations;
                _ = ExecuteSpeakingPhaseAsync(speakingMM, companion, _narrationState.HoveredKeyword);
            }
            else
            {
                Console.WriteLine("NarrativeController: Companion selection dismissed");
                _narrationState.SpeakingModusMentisPending = null;
                _pendingCompanions.Clear();
            }
            return;
        }

        if (_narrationState.InteractionModeIsForKeyword)
        {
            // Keyword choice: 0 = Think, 1 = Observe, 2 = Speak About
            if (choiceIndex == 0 && _narrationState.HoveredKeyword != null)
            {
                Console.WriteLine("NarrativeController: Choice — Think");
                _narrationState.IsSelectingObservationModusMentis = false;
                _narrationState.IsSelectingModusMentisForSpeaking = false;
                var thinkingModiMentis = _activePartyMember.GetThinkingModiMentis();
                Vector2 screenPos = _terminalInputHandler.CellToScreen(_lastMouseX, _lastMouseY, _core.ClientSize);
                _modusMentisPopup.Show(screenPos, thinkingModiMentis, "Select Thinking ModusMentis");
            }
            else if (choiceIndex == 1 && _narrationState.HoveredKeyword != null)
            {
                Console.WriteLine("NarrativeController: Choice — Observe");
                _narrationState.IsSelectingObservationModusMentis = true;
                _narrationState.IsSelectingModusMentisForSpeaking = false;
                var observationModiMentis = _activePartyMember.GetObservationModiMentis();
                Vector2 screenPos = _terminalInputHandler.CellToScreen(_lastMouseX, _lastMouseY, _core.ClientSize);
                _modusMentisPopup.Show(screenPos, observationModiMentis, "Select Observation ModusMentis");
            }
            else if (choiceIndex == 2 && _narrationState.HoveredKeyword != null)
            {
                Console.WriteLine("NarrativeController: Choice — Speak About");
                _narrationState.IsSelectingObservationModusMentis = false;
                _narrationState.IsSelectingModusMentisForSpeaking = true;
                var speakingModiMentis = _activePartyMember.GetSpeakingModiMentis();
                Vector2 screenPos = _terminalInputHandler.CellToScreen(_lastMouseX, _lastMouseY, _core.ClientSize);
                _modusMentisPopup.Show(screenPos, speakingModiMentis, "Select Speaking ModusMentis");
            }
            else
            {
                Console.WriteLine("NarrativeController: Keyword choice dismissed");
            }
        }
        else
        {
            // Action choice: 0 = Execute, 1 = Use Item
            var action = _narrationState.ActionPendingModeSelection;
            _narrationState.ActionPendingModeSelection = null;

            if (choiceIndex == 0 && action != null)
            {
                Console.WriteLine($"NarrativeController: Choice — Execute '{action.ActionText}'");
                _ = ExecuteActionPhaseAsync(action);
            }
            else if (choiceIndex == 1 && action != null)
            {
                var candidateItems = GetCombinableItems();
                if (candidateItems.Count > 0)
                {
                    Console.WriteLine($"NarrativeController: Choice — Use Item for '{action.ActionText}'");
                    _narrationState.IsSelectingItemForAction = true;
                    _narrationState.ActionPendingItemCombination = action;
                    Vector2 screenPos = _terminalInputHandler.CellToScreen(_lastMouseX, _lastMouseY, _core.ClientSize);
                    _itemSelectionPopup.Show(screenPos, candidateItems, "Combine Item with Action");
                }
                else
                {
                    Console.WriteLine("NarrativeController: No combinable items available.");
                }
            }
            else
            {
                Console.WriteLine("NarrativeController: Action choice dismissed");
            }
        }
    }

    /// <summary>
    /// Right-click is no longer used for narrative interactions.
    /// </summary>
    public void OnRightClick(int mouseX, int mouseY) { }
    
    /// <summary>
    /// Handle mouse wheel scroll event.
    /// </summary>
    public void OnMouseWheel(float delta)
    {
        if (delta > 0)
        {
            // Scroll up
            _scrollBuffer.ScrollUp(3);
        }
        else if (delta < 0)
        {
            // Scroll down
            _scrollBuffer.ScrollDown(3);
        }
        
        _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;
    }
    
    /// <summary>
    /// Check if we're still in loading state.
    /// </summary>
    public bool IsLoading => _narrationState.IsLoadingObservations;
    
    /// <summary>
    /// Check if there's an error.
    /// </summary>
    public bool HasError => _narrationState.ErrorMessage != null;
    
    /// <summary>
    /// Check if the thinking modusMentis popup is visible.
    /// </summary>
    public bool IsPopupVisible => _modusMentisPopup.IsVisible || _itemSelectionPopup.IsVisible || _choicePopup.IsVisible;
    
    /// <summary>
    /// Close the thinking modusMentis popup if it's open.
    /// Returns true if popup was closed, false if it wasn't open.
    /// </summary>
    public bool ClosePopup()
    {
        if (_choicePopup.IsVisible)
        {
            _choicePopup.Hide();
            _narrationState.IsSelectingInteractionMode = false;
            _narrationState.IsSelectingCompanionForSpeaking = false;
            _narrationState.SpeakingModusMentisPending = null;
            _pendingCompanions.Clear();
            return true;
        }
        if (_modusMentisPopup.IsVisible)
        {
            _modusMentisPopup.Hide();
            _narrationState.IsSelectingModusMentisForSpeaking = false;
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Get the current narration state.
    /// </summary>
    public NarrativeState GetState() => _narrationState;
    
    /// <summary>
    /// Check if player has requested to exit Phase 6 (clicked Continue button).
    /// </summary>
    public bool HasRequestedExit() => _narrationState.RequestedExit;
    
    /// <summary>
    /// Check if a fight outcome is pending (NarrativeController wants to enter fight mode).
    /// </summary>
    public FightOutcome? PendingFightOutcome => _pendingFightOutcome;
    
    /// <summary>
    /// Check if a dialogue outcome is pending (NarrativeController wants to enter dialogue mode).
    /// </summary>
    public DialogueOutcome? PendingDialogueOutcome => _pendingDialogueOutcome;
    
    /// <summary>
    /// The protagonist used by this narrative controller.
    /// </summary>
    public Protagonist Protagonist => _protagonist;
    
    /// <summary>
    /// Clear the pending fight outcome after the game controller has handled it.
    /// </summary>
    public void ClearPendingFight() => _pendingFightOutcome = null;
    
    /// <summary>
    /// Clear the pending dialogue outcome after the game controller has handled it.
    /// </summary>
    public void ClearPendingDialogue() => _pendingDialogueOutcome = null;
    
    /// <summary>
    /// Called by the game controller when returning from fight mode.
    /// Handles corpse spawning (victory), enemy affinity (runaway), and narration resumption.
    /// </summary>
    public void OnFightCompleted(
        Fight.FightAdapterResult result,
        NpcEntity npc,
        IReadOnlyList<NpcEntity>? allEnemyNpcs = null)
    {
        Console.WriteLine($"NarrativeController: Fight completed with result {result} vs {npc.DisplayName}");

        var enemies = allEnemyNpcs ?? new List<NpcEntity> { npc };

        if (result == Fight.FightAdapterResult.Victory)
        {
            // Spawn corpses for every dead enemy + focus on main enemy's corpse
            Spot? mainCorpse = null;
            foreach (var enemy in enemies)
            {
                if (!enemy.IsAlive && _scene != null && _pov != null)
                {
                    var corpse = enemy.GenerateCorpse(_pov.Where);
                    _scene.AddSpotToArea(_pov.Where, corpse);
                    _graph.NotifyNpcDead(enemy);
                    Console.WriteLine($"NarrativeController: Corpse spawned for {enemy.DisplayName}");

                    if (enemy == npc)
                        mainCorpse = corpse;
                }
            }

            // Focus on main enemy's corpse so the player can loot/inspect
            if (mainCorpse != null && _pov != null)
            {
                _pov.Focus = mainCorpse;
                SceneDebugManager.UpdatePoV(_pov);
            }
        }
        else if (result == Fight.FightAdapterResult.Runaway)
        {
            // Every alive fighter who participated now considers the protagonist an enemy
            foreach (var enemy in enemies)
            {
                if (enemy.IsAlive)
                {
                    enemy.AffinityTable.SetEnemy(_protagonist.DisplayName);
                    Console.WriteLine($"NarrativeController: {enemy.DisplayName} flagged as enemy after runaway");
                }
            }
        }

        string outcomeText = result switch
        {
            Fight.FightAdapterResult.Victory => $"You defeated {npc.DisplayName}.",
            Fight.FightAdapterResult.Runaway => $"You fled from {npc.DisplayName}.",
            Fight.FightAdapterResult.Death => $"You were slain by {npc.DisplayName}.",
            _ => "The fight ended."
        };

        // Add outcome to scroll buffer
        var block = new NarrationBlock(
            Type: NarrationBlockType.Outcome,
            ModusMentis: _protagonist.ModiMentis.FirstOrDefault()!,
            Text: outcomeText,
            Keywords: null,
            Actions: null
        );
        _scrollBuffer.AddBlock(block);
        _narrationState.AddBlock(block);
        _scrollBuffer.ScrollToBottom();
        _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;

        // For death/runaway, show continue button to exit
        if (result == Fight.FightAdapterResult.Death || result == Fight.FightAdapterResult.Runaway)
        {
            _narrationState.PendingTransitionNode = null;
            _narrationState.ShowContinueButton = true;
        }
    }
    
    /// <summary>
    /// Called by the game controller when returning from dialogue mode.
    /// Resumes narration.
    /// </summary>
    public void OnDialogueCompleted(NpcEntity npc)
    {
        Console.WriteLine($"NarrativeController: Dialogue completed with {npc.DisplayName}");
        
        var block = new NarrationBlock(
            Type: NarrationBlockType.Outcome,
            ModusMentis: _protagonist.ModiMentis.FirstOrDefault()!,
            Text: $"You finished talking with {npc.DisplayName}.",
            Keywords: null,
            Actions: null
        );
        _scrollBuffer.AddBlock(block);
        _narrationState.AddBlock(block);
        _scrollBuffer.ScrollToBottom();
        _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;
    }
    
    /// <summary>
    /// Prints the current narration graph structure to console for debugging.
    /// Shows all nodes, their connections, items, and keywords.
    /// </summary>
    public void PrintGraphStructure()
    {
        Console.WriteLine("\n=== Current Narration Graph Structure ===");
        Console.WriteLine($"Current Node: {_currentNode.NodeId}");
        Console.WriteLine();

        int nodeCount = 0;
        foreach (var (nodeId, node) in _graph.AllNodes)
        {
            nodeCount++;

            Console.WriteLine($"[{nodeCount}] Node: {nodeId}");
            Console.WriteLine($"    Display: {node.DisplayName}");
            Console.WriteLine($"    Context: {node.ContextDescription}");
            Console.WriteLine($"    Entry Node: {node.IsEntryNode}");
            Console.WriteLine($"    Outcomes: {node.GetAllDirectConcreteOutcomes().Count}");

            var items = node.GetAvailableItems();
            if (items.Count > 0)
            {
                Console.WriteLine($"    Items ({items.Count}):");
                foreach (var item in items)
                    Console.WriteLine($"      - {item.DisplayName}");
            }

            var observations = node.PossibleOutcomes.OfType<ObservationObject>().ToList();
            if (observations.Count > 0)
            {
                Console.WriteLine($"    Observations ({observations.Count}):");
                foreach (var obs in observations)
                    Console.WriteLine($"      -> {obs.ObservationId}");
            }

            Console.WriteLine();
        }

        Console.WriteLine($"=== Total: {nodeCount} nodes ===\n");
    }

    // ── Item combination helpers ──────────────────────────────────────────────

    /// <summary>
    /// Returns all items the protagonist currently holds that can be combined with an action:
    /// non-containers, or containers whose Contents list is empty.
    /// </summary>
    /// <summary>
    /// <summary>
    /// Returns true if the action should exit to world travel after the continue button is clicked.
    /// Uses action-text parsing to detect movement verbs.
    /// </summary>
    private static bool IsMovementAction(ParsedNarrativeAction? action)
    {
        if (action == null) return false;
        return CriticTrees.IsMovementVerb(action.ActionText);
    }

    /// Determines the <see cref="CriminalAffinityType"/> for a verb that was just executed.
    /// </summary>
    private static CriminalAffinityType DetermineCrimeType(Cathedral.Game.Scene.Verbs.Verb verb, bool areaIsPrivate)
    {
        return verb.VerbId switch
        {
            "steal"       => CriminalAffinityType.Thief,
            "grab"        => areaIsPrivate ? CriminalAffinityType.Thief : CriminalAffinityType.None,
            "slay"        => CriminalAffinityType.Murderer,
            "unlock_door" => CriminalAffinityType.Intruder,
            _             => areaIsPrivate ? CriminalAffinityType.Intruder : CriminalAffinityType.None,
        };
    }

    private List<Item> GetCombinableItems()
    {
        return _protagonist.GetAllItems()
            .Where(i => i is not ContainerItem c || c.Contents.Count == 0)
            .ToList();
    }

    /// <summary>
    /// Looks up the ParsedNarrativeAction at a given global index across all thinking blocks.
    /// Mirrors the lookup in OnMouseClick.
    /// </summary>
    private ParsedNarrativeAction? GetActionAtIndex(int actionIndex)
    {
        var allActions = new List<ParsedNarrativeAction>();
        foreach (var block in _narrationState.Blocks)
        {
            if (block.Type == NarrationBlockType.Thinking && block.Actions != null)
                allActions.AddRange(block.Actions);
        }
        return actionIndex >= 0 && actionIndex < allActions.Count ? allActions[actionIndex] : null;
    }

    /// <summary>
    /// Orchestrates item combination:
    ///   1. Critic checks if the item can help realise the action.
    ///   2. If yes → action modusMentis reformulates action text incorporating the item;
    ///              result appears as a new action button.
    ///   3. If no  → action modusMentis narrates a short failure description.
    /// </summary>
    private async Task ExecuteItemCombinationAsync(ParsedNarrativeAction action, Item item)
    {
        _narrationState.IsLoadingAction = true;
        _narrationState.LoadingMessage = Config.LoadingMessages.EvaluatingAction;

        try
        {
            // Resolve action modusMentis
            var actionModusMentis = action.ActionModusMentis
                ?? _protagonist.ModiMentis.FirstOrDefault(m => m.ModusMentisId == action.ActionModusMentisId);

            if (actionModusMentis == null)
            {
                Console.Error.WriteLine("NarrativeController: Cannot execute item combination — action modusMentis not resolved.");
                _narrationState.IsLoadingAction = false;
                return;
            }

            string itemContext = $"{item.DisplayName} ({item.Description})";
            Console.WriteLine($"NarrativeController: Item combination — action='{action.DisplayText}', item='{itemContext}'");

            // Build critic context
            var goalDescription = action.PreselectedOutcome?.ToNaturalLanguageString() ?? "";
            var criticContext = new CriticContext(_currentNode, _worldContext, _locationId, goalDescription);
            criticContext.CombinedItemContext = itemContext;

            // === CRITIC: can the item help? (two passes — either succeeding is enough) ===
            // Pass 1: original action-text phrasing (persona voice)
            var appropriatenessTree1 = CriticTrees.BuildItemAppropriatenessTreeByActionText(action.ActionText, itemContext, criticContext);
            var appropriatenessResult1 = await _actionExecutor.CriticEvaluator.EvaluateTreeAsync(appropriatenessTree1);
            Console.WriteLine($"NarrativeController: Item appropriateness pass 1 (action text): {(appropriatenessResult1.OverallSuccess ? "success" : "fail")}");

            // Pass 2: neutral goal-based phrasing (only if pass 1 failed)
            bool appropriatenessSuccess = appropriatenessResult1.OverallSuccess;
            var appropriatenessResult = appropriatenessResult1;
            if (!appropriatenessSuccess)
            {
                var appropriatenessTree2 = CriticTrees.BuildItemAppropriatenessTree(goalDescription, actionModusMentis.ShortDescription, item.DisplayName, criticContext);
                appropriatenessResult = await _actionExecutor.CriticEvaluator.EvaluateTreeAsync(appropriatenessTree2);
                appropriatenessSuccess = appropriatenessResult.OverallSuccess;
                Console.WriteLine($"NarrativeController: Item appropriateness pass 2 (neutral): {(appropriatenessSuccess ? "success" : "fail")}");
            }

            // Item combination always costs one noetic point, regardless of outcome
            _narrationState.ThinkingAttemptsRemaining = Math.Max(0, _narrationState.ThinkingAttemptsRemaining - 1);
            Console.WriteLine($"NarrativeController: Item combination consumed 1 noetic point ({_narrationState.ThinkingAttemptsRemaining} remaining)");

            if (appropriatenessSuccess)
            {
                Console.WriteLine($"NarrativeController: Item '{item.DisplayName}' approved — generating reasoning then reformulating.");

                // ── Step 1: reasoning (how does the item help?) ─────────────────
                string? reasoningText = await _thinkingExecutor.ExecuteItemReasoningAsync(
                    action, item, _currentNode, _protagonist, _worldContext);
                if (string.IsNullOrWhiteSpace(reasoningText))
                    reasoningText = $"I could use {item.DisplayName} to help with this.";

                // ── Step 2: reformulation (rewrite the action incorporating the item) ──
                string? reformulatedText = await _thinkingExecutor.ExecuteItemReformulationAsync(
                    action, item, _currentNode, _protagonist, _worldContext);
                if (string.IsNullOrWhiteSpace(reformulatedText))
                    reformulatedText = action.DisplayText;

                // ── Step 3: build the combined action ────────────────────────────
                // Chain leaf: a synthetic ModusMentis carrying item name + UsageLevel so that:
                //   - the action button shows [ItemName ◼◼] instead of [ActionSkill ◼◼◼]
                //   - GetTotalModusMentisLevel() = obs.Level + thinking.Level + action.Level + item.UsageLevel (no repetition)
                var itemModusMentis = new SyntheticItemModusMentis(item.ItemId, item.DisplayName, item.UsageLevel);

                var combinedAction = new ParsedNarrativeAction
                {
                    ActionText             = reformulatedText,
                    DisplayText            = reformulatedText,
                    ActionModusMentisId    = action.ActionModusMentisId,   // real skill for execution/slot lookup
                    ActionModusMentis      = action.ActionModusMentis,     // real skill for organ score etc.
                    CombinedActionModusMentis = itemModusMentis,           // item as chain leaf / display prefix
                    ThinkingModusMentis    = action.ThinkingModusMentis,
                    PreselectedOutcome     = action.PreselectedOutcome,
                    Keyword                = action.Keyword,
                    CombinedItem           = item,
                    DifficultyLevel        = action.DifficultyLevel,       // inherit difficulty so the glyph prefix renders
                };

                // ── Step 4: reasoning block (action skill as prefix, chains back to thinking) ──
                // Chain: combinedAction (item) → reasoningBlock (actionSkill) → thinking block → observation
                var reasoningBlock = new NarrationBlock(
                    Type: NarrationBlockType.Thinking,
                    ModusMentis: actionModusMentis,
                    Text: reasoningText,
                    Keywords: null,
                    Actions: new List<ParsedNarrativeAction> { combinedAction },
                    ChainOrigin: action.ChainOrigin   // = original thinking block
                );
                combinedAction.ChainOrigin = reasoningBlock;

                _scrollBuffer.AddBlock(reasoningBlock);
                _narrationState.AddBlock(reasoningBlock);
                _scrollBuffer.ScrollToBottom();
                _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;
            }
            else
            {
                Console.WriteLine($"NarrativeController: Item '{item.ItemId}' rejected — narrating failure.");

                string failureNarration = await _actionExecutor.OutcomeNarrator.NarrateItemCombinationFailureAsync(
                    action, item, actionModusMentis, appropriatenessResult.CombinedFailureReason);

                var failureBlock = new NarrationBlock(
                    Type: NarrationBlockType.Outcome,
                    ModusMentis: actionModusMentis,
                    Text: failureNarration,
                    Keywords: null,
                    Actions: null,
                    ChainOrigin: action.ChainOrigin
                );
                _scrollBuffer.AddBlock(failureBlock);
                _narrationState.AddBlock(failureBlock);
                _scrollBuffer.ScrollToBottom();
                _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"NarrativeController: Error during item combination: {ex.Message}");
        }
        finally
        {
            _narrationState.IsLoadingAction = false;
        }
    }
}
