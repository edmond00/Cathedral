using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Cathedral.Debug;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Nodes;
using Cathedral.Game.Npc;
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
    private readonly string _biomeType;
    
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
    
    // NPC spawner for populating nodes with encounters
    private readonly NpcSpawner _npcSpawner = new();
    private readonly int _locationId;
    
    // Pending fight/dialogue transitions (set by OnDiceRollContinue, consumed by game controller)
    private FightOutcome? _pendingFightOutcome = null;
    private DialogueOutcome? _pendingDialogueOutcome = null;
    
    // Random for dice rolls
    private readonly Random _diceRandom = new Random();
    
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
        string biomeType = "forest")
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
        _core = core;
        _terminalInputHandler = terminalInputHandler;
        _biomeType = biomeType;
        _locationId = locationId;
        
        // Initialize protagonist with random modiMentis and memory
        _protagonist = new Protagonist();
        _protagonist.InitializeModiMentis(ModusMentisRegistry.Instance, modusMentisCount: 50);
        _protagonist.InitializeMemory();
        _protagonist.AssignModiMentisToMemoryRandom();
        
        // Generate graph for this location using factory
        if (graphFactory == null)
            throw new ArgumentNullException(nameof(graphFactory), "NarrationGraphFactory is required - no fallback provided");
        
        _currentNode = graphFactory.GenerateGraph(locationId);
        Console.WriteLine($"NarrativeController: Generated graph for location {locationId} with entry node '{_currentNode.NodeId}'");
        NarrationGraphDebugManager.Show(_currentNode, _locationId);
        LlmMonitorDebugManager.Show();
        
        // Initialize controllers
        _observationController = new ObservationPhaseController(llamaServer, slotManager);
        _thinkingExecutor = thinkingExecutor;
        _actionExecutor = actionExecutor;
        
        Console.WriteLine($"NarrativeController: Initialized with node {_currentNode.NodeId}");
        Console.WriteLine($"NarrativeController: Protagonist has {_protagonist.ModiMentis.Count} modiMentis");
    }
    
    /// <summary>
    /// Start the observation phase (generates observations asynchronously).
    /// This clears all history - use for initial start only.
    /// </summary>
    public void StartObservationPhase()
    {
        _narrationState.Clear();
        _scrollBuffer.Clear();
        
        // Populate NPCs for this node
        _npcSpawner.PopulateNode(_currentNode, _locationId);
        
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
        // Populate NPCs for the new node
        _npcSpawner.PopulateNode(_currentNode, _locationId);
        
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

            // Build outcomes with metadata using LinkedOutcome from the source block.
            // The block already knows which outcome each sentence described, so we don't need keyword-based lookup.
            var outcomesWithMetadata = new List<OutcomeWithMetadata>();

            if (sourceObservationBlock?.IsCircuitousSentence == true && sourceObservationBlock.FocusOriginNode != null)
            {
                // Circuitous sentence clicked: the action options are the circuitous target
                // (as a circuitous outcome) + the origin node + FeelGood
                outcomesWithMetadata.Add(OutcomeWithMetadata.Circuitous(
                    sourceObservationBlock.LinkedOutcome!,
                    sourceObservationBlock.FocusOriginNode));
                outcomesWithMetadata.Add(OutcomeWithMetadata.Straightforward(sourceObservationBlock.FocusOriginNode));
                outcomesWithMetadata.Add(OutcomeWithMetadata.Straightforward(new FeelGoodOutcome()));
            }
            else
            {
                // Normal sentence clicked: straightforward outcome + a few circuitous + FeelGood
                var linkedOutcome = sourceObservationBlock?.LinkedOutcome;
                if (linkedOutcome != null)
                    outcomesWithMetadata.Add(OutcomeWithMetadata.Straightforward(linkedOutcome));
                else
                {
                    // Fallback: use keyword-based lookup for blocks created before This refactor
                    foreach (var o in _currentNode.GetOutcomesForKeyword(keyword))
                        outcomesWithMetadata.Add(OutcomeWithMetadata.Straightforward(o));
                }

                if (Config.Narrative.EnableCircuitousOutcomes)
                {
                    var observationType = sourceObservationBlock?.SourceObservationType ?? ObservationType.Overall;
                    var circuitousOutcomes = _currentNode.GetCircuitousOutcomesForKeyword(keyword, observationType);
                    var filteredCircuitous = circuitousOutcomes
                        .Where(c => !outcomesWithMetadata.Any(o =>
                            o.Outcome.ToNaturalLanguageString().Equals(
                                c.Outcome.ToNaturalLanguageString(), StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                    if (filteredCircuitous.Count > Config.Narrative.MaxCircuitousOutcomes)
                    {
                        var rng = new Random();
                        filteredCircuitous = filteredCircuitous
                            .OrderBy(_ => rng.Next())
                            .Take(Config.Narrative.MaxCircuitousOutcomes)
                            .ToList();
                    }

                    foreach (var c in filteredCircuitous)
                        outcomesWithMetadata.Add(OutcomeWithMetadata.Circuitous(c.Outcome, c.IntermediateNode));

                    Console.WriteLine($"NarrativeController: Added {filteredCircuitous.Count} circuitous outcomes");
                }

                if (!outcomesWithMetadata.Any(o => o.Outcome is FeelGoodOutcome))
                    outcomesWithMetadata.Add(OutcomeWithMetadata.Straightforward(new FeelGoodOutcome()));
            }

            // Get action modiMentis
            var actionModiMentis = _protagonist.GetActionModiMentis();

            Console.WriteLine($"NarrativeController: Total {outcomesWithMetadata.Count} outcomes ({outcomesWithMetadata.Count(o => o.IsCircuitous)} circuitous), {actionModiMentis.Count} action modiMentis");

            // Use LinkedOutcome display name for context if available, otherwise keyword-based lookup
            var keywordSourceOutcomeName = sourceObservationBlock?.LinkedOutcome?.DisplayName
                ?? _currentNode.GetOutcomeOwningKeyword(keyword)?.DisplayName;

            // Call ThinkingExecutor to generate reasoning + actions
            var response = await _thinkingExecutor.GenerateThinkingAsync(
                thinkingModusMentis,
                keyword,
                keywordSourceOutcomeName,
                _currentNode,
                outcomesWithMetadata,
                actionModiMentis,
                _protagonist,
                CancellationToken.None);

            if (response == null || response.Actions.Count == 0)
            {
                // Display error - no fallback as per user request
                throw new Exception("Thinking LLM returned no actions");
            }
            
            Console.WriteLine($"NarrativeController: Generated {response.Actions.Count} actions ({response.Actions.Count(a => a.IsCircuitous)} circuitous)");
            
            // Create thinking block with reasoning + actions
            // ChainOrigin points to the observation block that contained the clicked keyword
            var thinkingBlock = new NarrationBlock(
                Type: NarrationBlockType.Thinking,
                ModusMentis: thinkingModusMentis,
                Text: response.ReasoningText,
                Keywords: null,
                Actions: response.Actions,
                ChainOrigin: sourceObservationBlock
            );
            
            // Set ChainOrigin for each action to point to this thinking block
            foreach (var action in response.Actions)
            {
                action.ChainOrigin = thinkingBlock;
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
            
            // === PHASE 1: EVALUATION (normal loading screen) ===
            _narrationState.IsLoadingAction = true;
            _narrationState.LoadingMessage = Config.LoadingMessages.EvaluatingAction;
            
            // Evaluate plausibility and difficulty
            var evalResult = await _actionExecutor.EvaluateActionAsync(
                action,
                _currentNode,
                action.ThinkingModusMentis,
                CancellationToken.None
            );
            
            // Handle plausibility failure
            if (!evalResult.IsPlausible)
            {
                Console.WriteLine($"NarrativeController: Action failed plausibility check");
                
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
            
            // Calculate dice parameters for the roll animation
            int numberOfDice = CalculateNumberOfDice(action);
            
            // Convert difficulty score (0.0-1.0) to dice difficulty (1-10)
            int actualDifficulty = Math.Max(1, (int)Math.Ceiling(evalResult.DifficultyScore * Math.Min(numberOfDice, 10)));
            actualDifficulty = Math.Clamp(actualDifficulty, 1, 10);
            
            // Start dice roll animation
            _narrationState.StartDiceRoll(numberOfDice, actualDifficulty);
            _narrationState.LoadingMessage = "Rolling dice...";
            
            // Roll for success
            double roll;
            bool succeeded;
            if (DebugMode.IsActive)
            {
                succeeded = DebugMode.GetDiceRollOverride(action.ActionText, evalResult.SuccessProbability);
                roll = succeeded ? 0.0 : 1.0; // Synthetic roll value for logging
            }
            else
            {
                roll = _diceRandom.NextDouble();
                succeeded = roll < evalResult.SuccessProbability;
            }
            
            Console.WriteLine($"NarrativeController: Roll {roll:F3} vs probability {evalResult.SuccessProbability:F3} → {(succeeded ? "SUCCESS" : "FAILURE")}");
            
            // Execute dice roll phase (failure outcome evaluation + narration generation)
            var result = await _actionExecutor.ExecuteDiceRollAsync(
                evalResult,
                succeeded,
                CancellationToken.None
            );
            
            Console.WriteLine($"NarrativeController: Action {(result.Succeeded ? "SUCCEEDED" : "FAILED")}");
            
            // Generate final dice values based on success/failure
            int[] finalDiceValues = GenerateDiceValuesForResult(numberOfDice, actualDifficulty, result.Succeeded);
            
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
    /// Calculate number of dice to roll based on action modusMentis proficiency.
    /// </summary>
    private int CalculateNumberOfDice(ParsedNarrativeAction action)
    {
        // Base: 4 dice
        int baseDice = 4;
        
        // Try to get modusMentis bonus from protagonist
        if (action.ActionModusMentis != null)
        {
            // Get organ score (affects dice count)
            int organScore = _protagonist.GetOrganScoreForModusMentis(action.ActionModusMentis);
            if (organScore == 0) organScore = 5; // fallback
            
            // Organ score adds 0-3 extra dice
            int bonusDice = (organScore - 1) / 3; // 1-3 = 0, 4-6 = 1, 7-9 = 2, 10 = 3
            baseDice += bonusDice;
        }
        
        return Math.Clamp(baseDice, 3, 8);
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
        else
        {
            Console.WriteLine("NarrativeController: Non-transition outcome, showing continue button");
            _narrationState.PendingTransitionNode = null;
            _narrationState.ShowContinueButton = true;
        }
        
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
    
    /// <summary>
    /// Update loop - called at 10 Hz by game controller.
    /// </summary>
    public void Update()
    {
        // Clear terminal
        _ui.Clear();
        
        // Render header
        _ui.RenderHeader(_currentNode.DisplayName, _narrationState.ThinkingAttemptsRemaining, _biomeType);
        
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
                                _narrationState.IsLoadingFocusObservation || 
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
        // If popup is visible, use raw screen coordinates for accurate hit detection
        if (_modusMentisPopup.IsVisible)
        {
            // Get cell size for hit detection
            var layoutInfo = _terminalInputHandler.GetLayoutInfo(_core.ClientSize);
            int cellPixelSize = (int)layoutInfo.CellSize.X; // Assume square cells
            
            _modusMentisPopup.UpdateHover(screenPosition.X, screenPosition.Y, _core.ClientSize, cellPixelSize);
        }
    }
    
    /// <summary>
    /// Handle raw mouse click event with screen pixel coordinates.
    /// Used when popup is visible to bypass terminal cell coordinate system.
    /// </summary>
    public void OnRawMouseClick(Vector2 screenPosition)
    {
        // If popup is visible, handle popup click with screen coordinates
        if (_modusMentisPopup.IsVisible)
        {
            // Get cell size for hit detection
            var layoutInfo = _terminalInputHandler.GetLayoutInfo(_core.ClientSize);
            int cellPixelSize = (int)layoutInfo.CellSize.X; // Assume square cells
            
            var selectedModusMentis = _modusMentisPopup.HandleClick(screenPosition.X, screenPosition.Y, _core.ClientSize, cellPixelSize);
            if (selectedModusMentis != null)
            {
                Console.WriteLine($"NarrativeController: Selected modusMentis: {selectedModusMentis.DisplayName}");
                
                // Get the keyword that was clicked (stored before popup appeared)
                if (_narrationState.HoveredKeyword != null)
                {
                    string keyword = _narrationState.HoveredKeyword.Keyword;
                    var sourceBlock = _narrationState.HoveredKeyword.SourceBlock;

                    // Check if we're selecting an observation modusMentis (right-click) or thinking modusMentis (left-click)
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
                        else if (sourceBlock?.IsCircuitousSentence == true && sourceBlock.FocusOriginNode != null)
                            focusOutcome = sourceBlock.FocusOriginNode;
                        else
                            focusOutcome = sourceBlock?.LinkedOutcome ?? _currentNode.GetOutcomeOwningKeyword(keyword);

                        if (focusOutcome != null)
                            _ = ExecuteFocusObservationAsync(selectedModusMentis, focusOutcome);
                        else
                            Console.WriteLine($"NarrativeController: Cannot focus - no outcome found for keyword '{keyword}'");
                    }
                    else
                    {
                        // Thinking phase (left-click)
                        _narrationState.IsLoadingThinking = true;
                        _narrationState.LoadingMessage = Config.LoadingMessages.ThinkingDeeply;

                        // Fire-and-forget async task
                        _ = ExecuteThinkingPhaseAsync(selectedModusMentis, keyword);
                    }
                }
            }
            else
            {
                Console.WriteLine("NarrativeController: Popup closed (clicked outside)");
                _narrationState.IsSelectingObservationModusMentis = false;
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
        
        // If popup is visible, raw mouse events are handled separately
        if (_modusMentisPopup.IsVisible)
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
                    NarrationGraphDebugManager.UpdateCurrentNode(_currentNode);
                    
                    // Convert current narration to history (grayed out, non-interactive)
                    _scrollBuffer.ConvertToHistory();
                    _narrationState.ResetForNewNode();
                    _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;
                    
                    // Start new observation phase WITHOUT clearing history
                    StartObservationPhaseWithHistory();
                }
                else
                {
                    Console.WriteLine("NarrativeController: Continue button clicked, exiting to world view");
                    // Signal exit by setting a flag that the game controller can check
                    _narrationState.RequestedExit = true;
                    // The calling controller should check HasRequestedExit() and exit mode
                }
            }
            // When continue button is shown, don't process other clicks (keywords/actions)
            // but scrollbar clicks are allowed (processed above)
            return;
        }
        
        // If popup is visible, handle popup click with screen coordinates
        if (_modusMentisPopup.IsVisible)
        {
            // Get screen mouse position
            Vector2 correctedScreenPos = _terminalInputHandler.GetCorrectedMousePosition();
            
            // Get cell size for hit detection
            var layoutInfo = _terminalInputHandler.GetLayoutInfo(_core.ClientSize);
            int cellPixelSize = (int)layoutInfo.CellSize.X; // Assume square cells
            
            var selectedModusMentis = _modusMentisPopup.HandleClick(correctedScreenPos.X, correctedScreenPos.Y, _core.ClientSize, cellPixelSize);
            if (selectedModusMentis != null)
            {
                Console.WriteLine($"NarrativeController: Selected modusMentis: {selectedModusMentis.DisplayName}");
                
                // Get the keyword that was clicked (stored before popup appeared)
                if (_narrationState.HoveredKeyword != null)
                {
                    string keyword = _narrationState.HoveredKeyword.Keyword;
                    var sourceBlock = _narrationState.HoveredKeyword.SourceBlock;

                    // Check if we're selecting an observation modusMentis (right-click) or thinking modusMentis (left-click)
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
                        else if (sourceBlock?.IsCircuitousSentence == true && sourceBlock.FocusOriginNode != null)
                            focusOutcome = sourceBlock.FocusOriginNode;
                        else
                            focusOutcome = sourceBlock?.LinkedOutcome ?? _currentNode.GetOutcomeOwningKeyword(keyword);

                        if (focusOutcome != null)
                            _ = ExecuteFocusObservationAsync(selectedModusMentis, focusOutcome);
                        else
                            Console.WriteLine($"NarrativeController: Cannot focus - no outcome found for keyword '{keyword}'");
                    }
                    else
                    {
                        // Thinking phase (left-click)
                        _narrationState.IsLoadingThinking = true;
                        _narrationState.LoadingMessage = Config.LoadingMessages.ThinkingDeeply;

                        // Fire-and-forget async task
                        _ = ExecuteThinkingPhaseAsync(selectedModusMentis, keyword);
                    }
                }
            }
            else
            {
                Console.WriteLine("NarrativeController: Popup closed (clicked outside)");
                _narrationState.IsSelectingObservationModusMentis = false;
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
                {
                    allActions.AddRange(block.Actions);
                }
            }
            
            if (clickedAction.ActionIndex < allActions.Count)
            {
                var action = allActions[clickedAction.ActionIndex];
                Console.WriteLine($"NarrativeController: Executing action '{action.ActionText}' with modusMentis '{action.ActionModusMentisId}'");
                
                // Fire-and-forget async task
                _ = ExecuteActionPhaseAsync(action);
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
            
            // Show thinking modusMentis selection popup (left-click = thinking)
            _narrationState.IsSelectingObservationModusMentis = false;
            var thinkingModiMentis = _protagonist.GetThinkingModiMentis();
            
            // Convert terminal cell coordinates to screen pixel coordinates
            Vector2 screenPos = _terminalInputHandler.CellToScreen(mouseX, mouseY, _core.ClientSize);
            
            _modusMentisPopup.Show(screenPos, thinkingModiMentis, "Select Thinking ModusMentis");
            Console.WriteLine($"NarrativeController: Showing {thinkingModiMentis.Count} thinking modiMentis at screen position ({screenPos.X}, {screenPos.Y})");;
        }
    }
    
    /// <summary>
    /// Handle right mouse click event - triggers focus observation on keywords.
    /// </summary>
    public void OnRightClick(int mouseX, int mouseY)
    {
        // Don't handle right-clicks if popup is visible or in loading state
        if (_modusMentisPopup.IsVisible)
            return;
        
        if (_narrationState.IsLoadingObservations || _narrationState.IsLoadingThinking || 
            _narrationState.IsLoadingAction || _narrationState.IsLoadingFocusObservation)
            return;
        
        // Don't handle if continue button is shown
        if (_narrationState.ShowContinueButton)
            return;
        
        // Check if right-clicked on a keyword
        KeywordRegion? clickedKeyword = _ui.GetHoveredKeyword(mouseX, mouseY);
        
        if (clickedKeyword != null && _narrationState.ThinkingAttemptsRemaining > 0)
        {
            Console.WriteLine($"NarrativeController: Right-clicked keyword: {clickedKeyword.Keyword}");
            
            // Show observation modusMentis selection popup (right-click = focus observation)
            _narrationState.IsSelectingObservationModusMentis = true;
            _narrationState.HoveredKeyword = clickedKeyword;  // Store for later use
            var observationModiMentis = _protagonist.GetObservationModiMentis();
            
            // Convert terminal cell coordinates to screen pixel coordinates
            Vector2 screenPos = _terminalInputHandler.CellToScreen(mouseX, mouseY, _core.ClientSize);
            
            _modusMentisPopup.Show(screenPos, observationModiMentis, "Select Observation ModusMentis");
            Console.WriteLine($"NarrativeController: Showing {observationModiMentis.Count} observation modiMentis for focus observation at screen position ({screenPos.X}, {screenPos.Y})");
        }
    }
    
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
    public bool IsPopupVisible => _modusMentisPopup.IsVisible;
    
    /// <summary>
    /// Close the thinking modusMentis popup if it's open.
    /// Returns true if popup was closed, false if it wasn't open.
    /// </summary>
    public bool ClosePopup()
    {
        if (_modusMentisPopup.IsVisible)
        {
            _modusMentisPopup.Hide();
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
    /// Resumes narration, optionally removing the NPC if dead.
    /// </summary>
    public void OnFightCompleted(Fight.FightAdapterResult result, NpcEntity npc)
    {
        Console.WriteLine($"NarrativeController: Fight completed with result {result} vs {npc.DisplayName}");
        
        string outcomeText = result switch
        {
            Fight.FightAdapterResult.Victory => $"You defeated {npc.DisplayName}.",
            Fight.FightAdapterResult.Runaway => $"You fled from {npc.DisplayName}.",
            Fight.FightAdapterResult.Death => $"You were slain by {npc.DisplayName}.",
            _ => "The fight ended."
        };
        
        if (result == Fight.FightAdapterResult.Victory && !npc.IsAlive)
        {
            // Remove dead NPC from node
            _npcSpawner.RemoveNpc(npc, _currentNode);
            _currentNode.SpawnedNpcs.RemoveAll(n => n.NpcId == npc.NpcId);
            _currentNode.PossibleOutcomes.RemoveAll(o =>
                (o is FightOutcome fo && fo.Target.NpcId == npc.NpcId) ||
                (o is DialogueOutcome dlo && dlo.Target.NpcId == npc.NpcId));
        }
        
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
        
        // Breadth-first traversal to avoid infinite loops from circular references
        var visited = new HashSet<string>();
        var queue = new Queue<NarrationNode>();
        queue.Enqueue(_currentNode);
        visited.Add(_currentNode.NodeId);
        
        int nodeCount = 0;
        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            nodeCount++;
            
            Console.WriteLine($"[{nodeCount}] Node: {node.NodeId}");
            Console.WriteLine($"    Display: {node.DisplayName}");
            Console.WriteLine($"    Context: {node.ContextDescription}");
            Console.WriteLine($"    Entry Node: {node.IsEntryNode}");
            Console.WriteLine($"    Keywords: {string.Join(", ", node.NodeKeywords)}");
            
            // Show items
            var items = node.GetAvailableItems();
            if (items.Count > 0)
            {
                Console.WriteLine($"    Items ({items.Count}):");
                foreach (var item in items)
                {
                    Console.WriteLine($"      - {item.DisplayName}: {string.Join(", ", item.OutcomeKeywords)}");
                }
            }
            
            // Show connections
            var childNodes = node.PossibleOutcomes.OfType<NarrationNode>().ToList();
            if (childNodes.Count > 0)
            {
                Console.WriteLine($"    Transitions ({childNodes.Count}):");
                foreach (var child in childNodes)
                {
                    Console.WriteLine($"      -> {child.NodeId}");
                    
                    if (!visited.Contains(child.NodeId))
                    {
                        visited.Add(child.NodeId);
                        queue.Enqueue(child);
                    }
                }
            }
            
            Console.WriteLine();
        }
        
        Console.WriteLine($"=== Total: {nodeCount} nodes, {visited.Count} unique ===\n");
    }
}
