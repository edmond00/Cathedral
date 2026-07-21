using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Cathedral.Audio;
using Cathedral.Debug;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Dialogue.Tree.Trees;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Nodes;
using Cathedral.Game.Narrative.Routines;
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
    // Non-readonly so the Scene constructor can swap in a phase-specific variant after calling base().
    // Treated as effectively-final: set once during construction, never reassigned afterwards.
    private ObservationPhaseController _observationController;
    private readonly ThinkingExecutor _thinkingExecutor;
    private readonly ActionExecutionController _actionExecutor;
    private readonly GlyphSphereCore _core;
    private readonly TerminalInputHandler _terminalInputHandler;
    
    // Mouse tracking
    private int _lastMouseX = 0;
    private int _lastMouseY = 0;
    
    // Pending action result (stored while waiting for dice roll continue)
    private ActionExecutionResult? _pendingActionResult = null;
    
    // _graph and _scene are mutable so the reminescence flow can swap them when transitioning
    // between consecutive reminescences without rebuilding the controller.
    private NarrationGraph _graph;
    private readonly int _locationId;

    // ── Scene system (new backend, coexists with NarrationGraph) ──
    private Cathedral.Game.Scene.Scene? _scene;
    private PoV? _pov;
    
    // Pending fight/dialogue transitions (set by OnDiceRollContinue, consumed by game controller)
    private FightOutcome? _pendingFightOutcome = null;
    private DialogueOutcome? _pendingDialogueOutcome = null;

    // Records recordable successful verbs into a learned routine for this narration session.
    // Non-null only for scene-backed Exploration narration.
    private RoutineRecorder? _recorder = null;
    
    // Random for dice rolls — seeded from the master seed so runs are reproducible.
    private readonly Random _diceRandom = GameRng.For("dice");

    // Unified dice-roll overlay (animation + humor modifiers + hit-testing).
    private readonly DiceRollComponent _dice = new();

    // Pre-generated success/failure outcome candidates for the in-flight roll (Part E).
    // OnDiceRollContinue commits whichever matches the final (humor-modified) result.
    private ActionExecutionResult? _pendingSuccessResult;
    private ActionExecutionResult? _pendingFailureResult;
    // True when the pending result came from PrepareDualOutcomesAsync and therefore needs its
    // side-effects (XP, item consumption) committed at Continue. False for the Get-Up path,
    // which commits its own side-effects via reports only.
    private bool _pendingDeferredCommit;

    // ── Narration footer button (single button, three values) ────────────────────
    // The one bottom button in narration. In normal exploration it is LEAVE (or RUNAWAY when a
    // visible enemy/witness makes leaving risky) while idle, and CONTINUE while a succeeded action
    // is pending progression (node/area transition). In the childhood-reminescence and get-up
    // phases early exit is impossible, so it is only ever CONTINUE.
    private enum ExitButtonKind { Leave, RunawayEnemy, RunawayWitness, Continue }
    // Click region of the footer exit button as last rendered (Width == 0 ⇒ not shown this frame).
    private (int X, int Y, int Width) _exitButtonRegion;
    private bool _exitButtonHovered;
    // Set while an exit-runaway dice roll is in flight, so the dice Continue click resolves the exit
    // (via FinishExitRunaway) instead of the normal thinking-action OnDiceRollContinue.
    private bool _exitRunawayPending;
    private NpcEntity? _exitRunawayTarget;
    private bool _exitRunawayIsEnemy;

    // Ambient music engine (optional — null when MIDI is unavailable)
    private readonly AmbianceEngine? _ambianceEngine;

    // Fires a click sound through the engine (PCM path, no MIDI latency).
    private void PlayClickSound() => _ambianceEngine?.TriggerGameEvent(GameEventType.StrongInteraction);

    // Fires a hover sound when the cursor enters a new interactive element.
    private void PlayHoverSound() => _ambianceEngine?.TriggerGameEvent(GameEventType.SmallInteraction);

    // ── Dice-roll lifecycle helpers — wrap NarrationState calls + SFX cues. ───────
    // Open: neutral sound. Resolve: positive/negative depending on success. Close: neutral.
    private void NarrationDiceStart(int numberOfDice, int difficulty, PartyMember? humorMember = null,
        string? subtitle = null, string difficultyVerb = "to hit")
    {
        _narrationState.StartDiceRoll(numberOfDice, difficulty);
        _dice.Start(numberOfDice, difficulty, subtitle, difficultyVerb);
        if (humorMember != null)
        {
            int limit = HumorModifierLimit(humorMember);
            if (limit > 0) _dice.EnableHumorModifiers(humorMember.HumorQueues, limit);
        }
        _ambianceEngine?.TriggerGameEvent(GameEventType.NeutralOutcome);
    }

    private void NarrationDiceComplete(int[] finalValues)
    {
        _narrationState.CompleteDiceRoll(finalValues);
        _dice.Complete(finalValues);
        _ambianceEngine?.TriggerGameEvent(_dice.IsCurrentlySuccess
            ? GameEventType.PositiveOutcome : GameEventType.NegativeOutcome);
    }

    private void NarrationDiceClear()
    {
        bool wasActive = _narrationState.IsDiceRollActive;
        _narrationState.ClearDiceRoll();
        _dice.Hide();
        if (wasActive)
            _ambianceEngine?.TriggerGameEvent(GameEventType.NeutralOutcome);
    }

    /// <summary>Per-roll humor modifier budget from the viscera <c>humor_modifier_limit</c> stat.</summary>
    private static int HumorModifierLimit(PartyMember member)
        => member.DerivedStats.First(s => s.Name == "humor_modifier_limit").GetValue(member);

    /// <summary>
    /// Fired by the dice component when a humor modifier flips success↔failure. Swaps the active
    /// pending outcome candidate (pre-generated during the roll) and replays the outcome cue.
    /// </summary>
    private void OnDiceOutcomeFlipped(bool nowSuccess)
    {
        _pendingActionResult = nowSuccess ? _pendingSuccessResult : _pendingFailureResult;
        _ambianceEngine?.TriggerGameEvent(nowSuccess
            ? GameEventType.PositiveOutcome : GameEventType.NegativeOutcome);
    }

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
        Protagonist? existingProtagonist = null,
        AmbianceEngine? ambianceEngine = null)
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
        
        _ambianceEngine = ambianceEngine;
        _ui = new NarrativeUI(terminal);
        _dice.OnDiceTick      = () => _ambianceEngine?.TriggerGameEvent(GameEventType.SmallInteraction);
        _dice.OnButtonHover   = PlayHoverSound;
        _dice.OnButtonClick   = PlayClickSound;
        _dice.OnResultChanged = OnDiceOutcomeFlipped;
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
        
        // Use the protagonist passed in by the caller (LocationTravelGameController owns the
        // run's protagonist across phases). Fall back to a fresh one only as a safety net for
        // legacy call sites.
        if (existingProtagonist != null)
        {
            _protagonist = existingProtagonist;
        }
        else
        {
            _protagonist = new Protagonist();
            _protagonist.InitializeMemory();
        }
        _activePartyMember = _protagonist;

        // Noetic points for a fresh node/reset come from the active member's encephalon
        // (NoeticPointsStat), so a smarter mind gets more thinking attempts.
        _narrationState.MaxNoeticPointsProvider = () => (_activePartyMember ?? _protagonist).MaxNoeticPoints;

        // Generate graph for this location using factory
        if (graphFactory == null)
            throw new ArgumentNullException(nameof(graphFactory), "NarrationGraphFactory is required - no fallback provided");

        _graph       = graphFactory.GenerateGraph(locationId);
        _currentNode = _graph.EntryNode;
        Console.WriteLine($"NarrativeController: Generated graph for location {locationId} with entry node '{_currentNode.NodeId}' ({_graph.Npcs.Count} NPCs)");
        LlmMonitorDebugManager.Show();
        
        // Initialize controllers
        _observationController = new ObservationPhaseController(llamaServer, slotManager, _worldContext);
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
        Protagonist? existingProtagonist = null,
        AmbianceEngine? ambianceEngine = null)
        : this(terminal, popup, core, llamaServer, slotManager, terminalInputHandler,
               thinkingExecutor, actionExecutor,
               CreateGraphFactoryForScene(scene, locationId, existingProtagonist),
               locationId, worldContext, existingProtagonist,
               ambianceEngine)
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
    private static NarrationGraphFactory CreateGraphFactoryForScene(Cathedral.Game.Scene.Scene scene, int locationId, Protagonist? protagonist = null)
    {
        return new SceneSyntheticGraphFactory(scene, locationId, protagonist);
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
    public void StartObservationPhase(TimePeriod? forcedPeriod = null)
    {
        _narrationState.Clear();
        _scrollBuffer.Clear();
        _activePartyMember = _protagonist;
        _memberNoeticPoints.Clear();

        // Place NPCs into nodes based on the supplied time period, or a random one when none is given.
        var period = forcedPeriod ?? TimePeriodExtensions.Random(_diceRandom);
        _graph.TimeUpdate(period);
        Console.WriteLine($"NarrativeController: Time period is {period}");

        // Begin recording a routine for scene-backed Exploration sessions (other phases opt out).
        if (_scene != null && _scene.Phase == NarrationPhase.Exploration)
            _recorder = new RoutineRecorder(_protagonist, _locationId, period);

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
                _activePartyMember,
                _protagonist.CurrentLocationId,
                isReminescence: _scene?.Phase == NarrationPhase.ChildhoodReminescence
            );
            
            Console.WriteLine($"NarrativeController: Generated {blocks.Count} observation blocks");
            
            // Add blocks to scroll buffer
            foreach (var block in blocks)
            {
                _scrollBuffer.AddBlock(block);
                _narrationState.AddBlock(block);
            }
            _ambianceEngine?.PlaySoundEffect(SoundEffectType.NarrativeReveal);
            
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

            // Get action modiMentis from the active party member.
            // In the childhood reminescence phase REMEMBER may only be performed with the
            // ChildhoodReminescence modus mentis; thinking/observation can use any acquired MM.
            var actionModiMentis = _activePartyMember.GetActionModiMentis();
            if (_scene != null && _scene.Phase == NarrationPhase.ChildhoodReminescence)
            {
                actionModiMentis = actionModiMentis
                    .Where(m => m.ModusMentisId == "childhood_reminescence")
                    .ToList();
            }

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
                _locationId,
                _activePartyMember,
                isReminescence: _scene?.Phase == NarrationPhase.ChildhoodReminescence,
                autoSuccess: _scene?.Phase == NarrationPhase.ChildhoodReminescence
                             || _scene?.Phase == NarrationPhase.GetUp,
                cancellationToken: CancellationToken.None);

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

            // Difficulty is now computed inside ThinkingExecutor from the persona-fit answer
            // (verb base ± eager/willing/unsure modifier), so each action already carries its
            // DifficultyLevel. Auto-success phases (reminescence / get-up) carry difficulty 0 (○ glyph).

            // Add to scroll buffer
            _scrollBuffer.AddBlock(thinkingBlock);
            _narrationState.AddBlock(thinkingBlock);
            _ambianceEngine?.PlaySoundEffect(SoundEffectType.NarrativeReveal);

            // Persona-fit cancellation: the action skill refused (reluctant/opposed). Show the
            // first-person refusal as an outcome block; no action button is offered. The noetic
            // point is still consumed below via the normal thinking-complete decrement.
            if (!hasActions && response.RefusalText != null && response.RefusalModusMentis != null)
            {
                var refusalBlock = new NarrationBlock(
                    Type: NarrationBlockType.Outcome,
                    ModusMentis: response.RefusalModusMentis,
                    Text: response.RefusalText,
                    Keywords: null,
                    Actions: null);
                _scrollBuffer.AddBlock(refusalBlock);
                _narrationState.AddBlock(refusalBlock);
                Console.WriteLine("NarrativeController: Action refused by persona-fit — refusal narrated, no button.");
            }
            
            // Auto-scroll to bottom to show new thinking block
            _scrollBuffer.ScrollToBottom();
            _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset; // Sync scroll position
            
            // Update state
            _narrationState.IsLoadingThinking = false;
            if (_scene?.Phase != NarrationPhase.ChildhoodReminescence
                && _scene?.Phase != NarrationPhase.GetUp)
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

            // Whoever is the active party member performs this action: their skills, organ scores,
            // XP, wounds, and item consumption all apply (not necessarily the protagonist's).
            _actionExecutor.ActingMember = _activePartyMember;

            // === REMINESCENCE-PHASE SHORT-CIRCUIT ===
            // In the childhood-reminescence phase REMEMBER actions auto-succeed: no critic,
            // no dice, no noetic-point cost. Run the verb's Execute synchronously and emit
            // a success outcome block; the pending-transition handler picks it up next frame.
            if (_scene != null && _scene.Phase == NarrationPhase.ChildhoodReminescence)
            {
                await ExecuteReminescenceActionAsync(action);
                return;
            }

            // === GET-UP PHASE SHORT-CIRCUIT ===
            // GET UP skips the critic entirely and forces difficulty 1. Dice are still rolled.
            // Failure has no penalty and loops back; success queues a GetUpTransitionOutcome.
            if (_scene != null && _scene.Phase == NarrationPhase.GetUp)
            {
                await ExecuteGetUpActionAsync(action);
                return;
            }

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
                action, _activePartyMember, _scene, _pov, witnessContext, threatContext);
            var ruleResult = Narrative.Rules.ActionRulesChecker.Check(ruleCtx);
            if (!ruleResult.Passed)
            {
                Console.WriteLine($"NarrativeController: Coded rule blocked action — {ruleResult.ErrorMessage}");
                action.IsImpossible = true;

                // Re-express the refusal in the acting modus mentis's voice when one is resolvable
                // (e.g. caught-red-handed, under threat); fall back to the raw rule message otherwise.
                var refusalMm = _activePartyMember.ModiMentis
                    .FirstOrDefault(m => m.ModusMentisId == action.ActionModusMentisId)
                    ?? action.ActionModusMentis;
                string refusalText;
                if (refusalMm != null)
                {
                    refusalText = await _actionExecutor.OutcomeNarrator.NarrateRefusalAsync(
                        action, refusalMm, ruleResult.ErrorMessage ?? "", _activePartyMember, CancellationToken.None);
                    if (string.IsNullOrWhiteSpace(refusalText))
                        refusalText = $"[IMPOSSIBLE] {ruleResult.ErrorMessage}";
                }
                else
                {
                    refusalText = $"[IMPOSSIBLE] {ruleResult.ErrorMessage}";
                }

                _narrationState.IsLoadingAction = false;

                var ruleBlock = new NarrationBlock(
                    Type: NarrationBlockType.Outcome,
                    ModusMentis: refusalMm ?? action.ThinkingModusMentis,
                    Text: refusalText,
                    Keywords: null,
                    Actions: null);
                _scrollBuffer.AddBlock(ruleBlock);
                _narrationState.AddBlock(ruleBlock);
                _scrollBuffer.ScrollToBottom();
                _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;

                Console.WriteLine($"NarrativeController: Coded rule failure - consumed 1 noetic point ({_narrationState.ThinkingAttemptsRemaining} remaining)");

                if (_scene?.Phase == NarrationPhase.ChildhoodReminescence
                    || _scene?.Phase == NarrationPhase.GetUp)
                {
                    return;
                }
                else if (_narrationState.ThinkingAttemptsRemaining > 0)
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

                // Reminescence and GetUp phases never cost noetic — player can retry freely
                if (_scene?.Phase == NarrationPhase.ChildhoodReminescence
                    || _scene?.Phase == NarrationPhase.GetUp)
                {
                    return;
                }
                // If player still has noetic points, let them try again (no graying, no continue button)
                else if (_narrationState.ThinkingAttemptsRemaining > 0)
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

            // Start dice roll animation (with humor modifiers for the acting member)
            NarrationDiceStart(numberOfDice, actualDifficulty, _activePartyMember);
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

            // Pre-generate BOTH outcomes (success + failure) during the animation so the player
            // can flip the result with humor modifiers instantly. Side-effects are committed
            // later in OnDiceRollContinue for whichever outcome is final.
            var (successResult, failureResult) = await _actionExecutor.PrepareDualOutcomesAsync(
                evalResult,
                CancellationToken.None
            );

            _pendingSuccessResult = successResult;
            _pendingFailureResult = failureResult;
            _pendingActionResult  = succeeded ? successResult : failureResult;
            _pendingDeferredCommit = true;

            Console.WriteLine($"NarrativeController: Action prepared — rolled {(succeeded ? "SUCCESS" : "FAILURE")} (humor may change this)");

            // Complete the dice roll (stops animation, shows final values and continue button)
            NarrationDiceComplete(finalDiceValues);
            _narrationState.IsLoadingAction = false;

            Console.WriteLine($"NarrativeController: Dice roll complete - {finalDiceValues.Count(v => v == 6)} sixes rolled, difficulty {actualDifficulty}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"NarrativeController: Error during action execution: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);

            _narrationState.IsLoadingAction = false;
            NarrationDiceClear();
            _narrationState.ErrorMessage = $"Action execution failed: {ex.Message}";
        }
    }
    
    /// <summary>
    /// True when the most recent REMEMBER ended the childhood reminescence phase
    /// (the fragment's NextReminescenceId is &lt;END&gt;). Polled by the game controller
    /// to transition out of <c>GameMode.ChildhoodReminescence</c>.
    /// </summary>
    public bool ReminescencePhaseFinished { get; private set; }

    /// <summary>
    /// True when the GET UP action succeeded in the Get-Up scene. Polled by the game
    /// controller to transition out of <c>GameMode.GetUp</c> into world travel.
    /// </summary>
    public bool GetUpPhaseFinished { get; private set; }

    /// <summary>
    /// Number of REMEMBER fragments successfully resolved so far in the current
    /// childhood reminescence phase. Used by the game controller to progressively
    /// unlock music tracks (0 = noise only, 1 = +drone, 2 = +melody, …, 4 = full).
    /// </summary>
    public int ReminescenceCompletedCount { get; private set; }

    /// <summary>
    /// Consume a queued <see cref="ReminescenceTransitionRequest"/>: either rebuild the
    /// scene as the next reminescence (in place) or signal that the reminescence phase has
    /// ended.
    /// </summary>
    private void HandleReminescenceContinue(ReminescenceTransitionRequest req)
    {
        Console.WriteLine($"NarrativeController: reminescence continue — '{req.FromReminescenceId}' → '{req.NextReminescenceId}'");

        if (_scene == null) return;
        _scene.PendingReminescenceTransition = null;

        ReminescenceCompletedCount++;   // each completed REMEMBER unlocks one more music track

        if (Cathedral.Game.Narrative.Reminescence.ReminescenceRegistry.IsEnd(req.NextReminescenceId))
        {
            // Final fragment of the reminescence phase — the game controller transitions us
            // out on the next frame.
            ReminescencePhaseFinished = true;
            _narrationState.ShowContinueButton = false;
            _narrationState.RequestedExit = true;
            return;
        }

        var nextData = Cathedral.Game.Narrative.Reminescence.ReminescenceRegistry.Get(req.NextReminescenceId);
        if (nextData == null)
        {
            Console.Error.WriteLine($"NarrativeController: unknown reminescence id '{req.NextReminescenceId}', ending phase.");
            ReminescencePhaseFinished = true;
            _narrationState.RequestedExit = true;
            return;
        }

        // Rebuild the scene + graph for the next reminescence using the same controller
        // (no game-mode change) so the player keeps the existing terminal panel and history.
        var factory = new Cathedral.Game.Scene.Reminescence.ReminescenceSceneFactory(nextData);
        var newScene = factory.Build(_locationId);
        ReplaceScene(newScene);

        // Preserve the prior narration as history and start a fresh observation phase.
        _scrollBuffer.ConvertToHistory();
        _narrationState.ResetForNewNode();
        _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;
        StartObservationPhaseWithHistory();
    }

    /// <summary>
    /// Replaces the active scene/graph in place. Used by the reminescence flow to swap one
    /// reminescence scene for the next without rebuilding the whole NarrativeController.
    /// </summary>
    private void ReplaceScene(Cathedral.Game.Scene.Scene newScene)
    {
        _scene = newScene;
        _pov   = new PoV(newScene.AllAreas[0], TimePeriod.Morning);

        var newFactory = new Cathedral.Game.Scene.SceneSyntheticGraphFactory(newScene, _locationId, _protagonist);
        _graph         = newFactory.GenerateGraph(_locationId);
        _currentNode   = _graph.EntryNode;

        Console.WriteLine($"NarrativeController: scene replaced with reminescence '{newScene.CurrentReminescenceId}'");
    }

    /// <summary>
    /// Consumes a pending Get-Up success transition: signals to the game controller that the
    /// protagonist has risen and world travel should begin.
    /// </summary>
    private void HandleGetUpContinue()
    {
        Console.WriteLine("NarrativeController: Get-Up complete — protagonist risen, transitioning to world travel");
        if (_scene != null) _scene.PendingGetUpTransition = false;
        GetUpPhaseFinished = true;
        _narrationState.ShowContinueButton = false;
        _narrationState.RequestedExit = true;
    }

    /// <summary>
    /// Get-Up phase action path: GET UP rolls dice at forced difficulty 1 with no critic and
    /// no noetic cost. On success, queues a GetUpTransitionOutcome via normal verb reports.
    /// On failure, no damage or penalty; the scene loops back when Continue is clicked.
    /// </summary>
    private async Task ExecuteGetUpActionAsync(ParsedNarrativeAction action)
    {
        if (_scene == null || _pov == null)
        {
            Console.Error.WriteLine("NarrativeController: ExecuteGetUpActionAsync called without scene/pov");
            return;
        }

        action.DifficultyLevel = 1;

        // Roll dice at forced difficulty 1 (1 six needed to succeed).
        int numberOfDice = Math.Max(1, action.GetTotalModusMentisLevel());
        const int getUpDifficulty = 1;

        NarrationDiceStart(numberOfDice, getUpDifficulty);
        _narrationState.LoadingMessage = "Rolling dice...";

        int[] finalDiceValues = new int[numberOfDice];
        for (int i = 0; i < numberOfDice; i++)
            finalDiceValues[i] = _diceRandom.Next(1, 7);
        int sixesCount = finalDiceValues.Count(v => v == 6);
        bool succeeded = sixesCount >= getUpDifficulty;

        Console.WriteLine(
            $"NarrativeController: GetUp dice — {sixesCount}/{numberOfDice} sixes (need {getUpDifficulty}) → {(succeeded ? "SUCCESS" : "FAILURE")}");

        // Choose narration hint for the LLM based on success/failure.
        var actionMm = action.ActionModusMentis ?? action.ChainModusMentis;
        OutcomeBase outcomeForPrompt = succeeded
            ? new InlineOutcome("getting up", "with great effort you push yourself to your feet and continue your travel")
            : new InlineOutcome("the effort", "your exhausted body refuses to rise — you slump back against the tree");

        _narrationState.IsLoadingAction = true;
        _narrationState.LoadingMessage  = Cathedral.Config.LoadingMessages.EvaluatingAction;

        string narration;
        try
        {
            narration = await _actionExecutor.OutcomeNarrator.NarrateOutcomeAsync(
                action,
                actionMm,
                outcomeForPrompt,
                succeeded,
                difficulty: CriticTrees.DifficultyLevelToScore(getUpDifficulty),
                _protagonist,
                System.Threading.CancellationToken.None);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"NarrativeController: GetUp narration failed — {ex.Message}");
            narration = succeeded
                ? "With great effort, you force yourself to your feet."
                : "Your body refuses to cooperate. You slump back against the tree.";
        }

        _narrationState.IsLoadingAction = false;

        // Store as pending action result — OnDiceRollContinue applies verb reports and shows outcome.
        // ActualOutcome is always the VerbOutcome so the GetUpVerb's Success/FailureReports are invoked.
        _pendingActionResult = new ActionExecutionResult
        {
            Action              = action,
            ActionModusMentis   = actionMm,
            ThinkingModusMentis = action.ThinkingModusMentis ?? actionMm,
            Difficulty          = CriticTrees.DifficultyLevelToScore(getUpDifficulty),
            DifficultyLevel     = getUpDifficulty,
            Succeeded           = succeeded,
            ActualOutcome       = action.PreselectedOutcome != null
                                      ? (OutcomeBase)action.PreselectedOutcome
                                      : new InlineOutcome("get up", "rise"),
            Narration           = narration,
        };

        NarrationDiceComplete(finalDiceValues);
        _narrationState.IsLoadingAction = false;

        Console.WriteLine($"NarrativeController: GetUp action narrated — {(succeeded ? "pending transition" : "failure, will loop")}");
    }

    /// <summary>
    /// Reminescence-phase action path: REMEMBER never fails, never costs noetic, never
    /// invokes the critic. Like any other action, the outcome narration is generated by
    /// the LLM through the action modusMentis persona (ChildhoodReminescenceModusMentis)
    /// so the text is written in the character's own voice rather than being hardcoded.
    /// </summary>
    private async Task ExecuteReminescenceActionAsync(ParsedNarrativeAction action)
    {
        if (_scene == null || _pov == null)
        {
            Console.Error.WriteLine("NarrativeController: ExecuteReminescenceActionAsync called without scene/pov");
            return;
        }

        action.DifficultyLevel = 0;

        // Pull the verb target out of the preselected outcome chain.
        Element? target = null;
        if (action.PreselectedOutcome is VerbOutcome vo)
            target = vo.Target;

        if (target == null)
        {
            Console.Error.WriteLine("NarrativeController: REMEMBER action has no target — aborting");
            return;
        }

        // Collect and apply all verb reports (skills, items, history, transition).
        System.Collections.Generic.IReadOnlyList<OutcomeReport> reminescenceReportList;
        try
        {
            reminescenceReportList = action.Verb.SuccessReports(_scene, _pov, _protagonist, target);
            foreach (var report in reminescenceReportList)
                report.Apply(_protagonist, _scene, _pov);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"NarrativeController: REMEMBER verb threw — {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            reminescenceReportList = System.Array.Empty<OutcomeReport>();
        }

        // Resolve the action modusMentis (ChildhoodReminescenceModusMentis) from the action.
        // Fall back to ChainModusMentis if ActionModusMentis is unexpectedly null.
        var actionMm = action.ActionModusMentis ?? action.ChainModusMentis;

        // Build the neutral outcome sentence directly from the fragment: a plain "I tried to
        // remember …, and succeeded." framing plus the concrete recovered memory (OutcomeText).
        // This is handed to the narrator as an override so the persona rewrite styles the actual
        // memory rather than the flowery thinking-phase action label.
        var fpoi = target as Cathedral.Game.Scene.Reminescence.FragmentPointOfInterest;
        var reminescenceNeutral = fpoi != null
            ? NeutralNarration.ReminescenceOutcome(fpoi.Fragment.Name, fpoi.Fragment.OutcomeText)
            : null;
        var outcomeForPrompt = fpoi != null
            ? (OutcomeBase)new InlineOutcome(
                displayName:    fpoi.Fragment.Name,
                naturalLanguage: $"remember: {fpoi.Fragment.OutcomeText}")
            : new InlineOutcome("memory", "remember this childhood moment");

        // Generate outcome narration through the LLM exactly as any other action.
        _narrationState.IsLoadingAction = true;
        _narrationState.LoadingMessage  = Config.LoadingMessages.EvaluatingAction;

        string narrationText;
        try
        {
            narrationText = await _actionExecutor.OutcomeNarrator.NarrateOutcomeAsync(
                action,
                actionMm,
                outcomeForPrompt,
                succeeded:  true,
                difficulty: 0.0,
                _protagonist,
                CancellationToken.None,
                neutralOverride: reminescenceNeutral);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"NarrativeController: outcome narration failed — {ex.Message}");
            // Fallback: show the raw concrete memory text.
            narrationText = fpoi != null
                ? fpoi.Fragment.OutcomeText
                : "You remember.";
        }

        _narrationState.IsLoadingAction = false;

        // UI-visible chips come directly from the SuccessReports() list (ShowInUI filter).
        var uiReminescenceReports = reminescenceReportList
            .Where(r => r.ShowInUI)
            .ToList();

        var outcomeBlock = new NarrationBlock(
            Type:           NarrationBlockType.Outcome,
            ModusMentis:    actionMm,
            Text:           narrationText,
            Keywords:       null,
            Actions:        null,
            ChainOrigin:    action,
            OutcomeReports: uiReminescenceReports.Count > 0 ? uiReminescenceReports : null);
        _scrollBuffer.AddBlock(outcomeBlock);
        _narrationState.AddBlock(outcomeBlock);
        // REMEMBER always succeeds (and often grants a skill/item) — cue the positive
        // outcome sting, matching the normal action-resolution path.
        _ambianceEngine?.TriggerGameEvent(GameEventType.PositiveOutcome);
        _scrollBuffer.ScrollToBottom();
        _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;

        _narrationState.PendingTransitionNode = null;
        _narrationState.ShowContinueButton    = true;
        Console.WriteLine($"NarrativeController: REMEMBER narrated — pending transition '{_scene.PendingReminescenceTransition?.NextReminescenceId}'");
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
            NarrationDiceClear();
            return;
        }
        
        var result = _pendingActionResult;
        _pendingActionResult = null;
        _pendingSuccessResult = null;
        _pendingFailureResult = null;
        bool deferredCommit = _pendingDeferredCommit;
        _pendingDeferredCommit = false;

        Console.WriteLine($"NarrativeController: Dice roll continue - committing {(result.Succeeded ? "SUCCESS" : "FAILURE")} outcome");

        if (deferredCommit)
        {
            // Keep only the chosen branch's narration in the narrator slot history (discard the
            // speculative other branch that was generated during the roll).
            _actionExecutor.OutcomeNarrator.CommitNarrationHistory(result.Succeeded);

            // Commit deferred side-effects for the FINAL (possibly humor-modified) outcome.
            if (result.Succeeded)
                foreach (var chainModusMentis in result.Action.GetModusMentisChain())
                    _activePartyMember.AwardModusMentisXp(chainModusMentis);
            if (result.ItemConsumed && result.Action.CombinedItem != null)
            {
                _activePartyMember.RemoveItem(result.Action.CombinedItem);
                Console.WriteLine($"NarrativeController: Item consumed — {result.Action.CombinedItem.ItemId}");
            }
        }

        // Collect all outcome reports: verb-specific + LLM-decided (wound).
        var allReports = new System.Collections.Generic.List<OutcomeReport>();
        if (result.ActualOutcome is VerbOutcome verbTarget && _scene != null && _pov != null)
        {
            var verbReports = result.Succeeded
                ? verbTarget.VerbView.Verb.SuccessReports(_scene, _pov, _activePartyMember, verbTarget.Target!, verbTarget.VerbView)
                : verbTarget.VerbView.Verb.FailureReports(_scene, _pov, _activePartyMember, verbTarget.Target!);
            allReports.AddRange(verbReports);
        }
        allReports.AddRange(result.LlmDecidedReports);

        // Record this verb into the in-progress routine BEFORE applying reports, so the recorder
        // evaluates the verb against the pre-move PoV. Only successful recordable verbs are captured.
        if (result.Succeeded && _recorder != null && _scene != null && _pov != null
            && result.ActualOutcome is VerbOutcome)
        {
            _recorder.OnVerbSucceeded(result.Action, _scene, _pov, _activePartyMember, result.ItemConsumed);
        }

        // Remember the area before reports apply, so we can detect any area-moving verb (move, follow
        // path, stairs, climb, door) and continue narration at the destination node — not just MoveToArea.
        var areaBefore = _pov?.Where;

        // Apply every report's game-state change in order — to the acting member, so a companion's
        // loot, learned skills, and suffered wounds land on the companion, not the protagonist.
        foreach (var report in allReports)
            report.Apply(_activePartyMember, _scene, _pov);

        // UI-visible chips for the outcome block.
        var uiReports = allReports.Where(r => r.ShowInUI).ToList();

        // Add outcome narration block
        var outcomeBlock = new NarrationBlock(
            Type: NarrationBlockType.Outcome,
            ModusMentis: result.ActionModusMentis ?? throw new InvalidOperationException("Action modusMentis cannot be null"),
            Text: $"[{(result.Succeeded ? "SUCCESS" : "FAILURE")}] {result.Narration}",
            Keywords: null,
            Actions: null,
            OutcomeReports: uiReports.Count > 0 ? uiReports : null
        );
        _scrollBuffer.AddBlock(outcomeBlock);
        _narrationState.AddBlock(outcomeBlock);
        _ambianceEngine?.TriggerGameEvent(result.Succeeded ? GameEventType.PositiveOutcome : GameEventType.NegativeOutcome);
        
        // Auto-scroll to bottom to show outcome
        _scrollBuffer.ScrollToBottom();
        _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;

        // Clear dice roll state
        NarrationDiceClear();
        _ambianceEngine?.SetFilter(MusicFilter.None);

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

        // === FAILURE-PATH ENEMY OPPORTUNITY ATTACK ===
        // An action failed under threat: the enemy seizes the moment and attacks with the initiative.
        if (!result.Succeeded && result.FightTriggered && result.FightEnemy != null)
        {
            Console.WriteLine($"NarrativeController: Enemy '{result.FightEnemy.DisplayName}' attacks after failed action under threat — enemy initiative");
            _pendingFightOutcome = new FightOutcome(result.FightEnemy, $"opportunity attack by {result.FightEnemy.DisplayName}")
            {
                EnemyInitiative = true
            };
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
            Console.WriteLine($"NarrativeController: Verb outcome '{verbOutcome.VerbView.Verb.VerbId}' on '{verbOutcome.Target?.DisplayName}', reports already applied");
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

            // Any area-moving verb (move, follow path, stairs, climb, open door): stay in scene and
            // transition to the destination area's node. Detected generically by the PoV's area
            // changing, so all connector verbs behave like MoveToAreaVerb (consistent PoV/node and a
            // live session that survives across connectors — required for multi-step routine chains).
            if (_pov != null && areaBefore != null && _pov.Where.Id != areaBefore.Id)
            {
                var nodeId = _pov.Where.DisplayName.ToLowerInvariant().Replace(' ', '_');
                if (_graph.AllNodes.TryGetValue(nodeId, out var areaNode))
                {
                    Console.WriteLine($"NarrativeController: area changed to '{_pov.Where.DisplayName}' — transitioning to node '{nodeId}'");
                    _narrationState.PendingTransitionNode = areaNode;
                    _narrationState.ShowContinueButton = true;
                    return;
                }
            }

            // GetUp phase: failure loops back (no ShouldExitOnContinue); success is handled
            // via PendingGetUpTransition in the Continue button handler.
            if (_scene?.Phase == NarrationPhase.GetUp)
            {
                _narrationState.PendingTransitionNode = null;
                _narrationState.ShouldExitOnContinue = false;
                _narrationState.ShowContinueButton = true;
                if (_pov != null) SceneDebugManager.UpdatePoV(_pov);
                Console.WriteLine($"NarrativeController: GetUp action complete ({(result.Succeeded ? "pending transition" : "will loop")})");
                return;
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
                _protagonist.CurrentLocationId,
                _activePartyMember,
                isReminescence: _scene?.Phase == NarrationPhase.ChildhoodReminescence
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
            if (_scene?.Phase != NarrationPhase.ChildhoodReminescence
                && _scene?.Phase != NarrationPhase.GetUp)
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
            points = member.MaxNoeticPoints;
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
                _activePartyMember,
                _protagonist.CurrentLocationId,
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

        // Sync music filter based on current loading/dice state
        if (_ambianceEngine != null)
        {
            bool diceActive = _narrationState.IsDiceRollActive;
            var desired = diceActive                  ? MusicFilter.DiceRoll
                        : _narrationState.IsAnyLoading ? MusicFilter.Loading
                        : MusicFilter.None;
            if (_ambianceEngine.ActiveFilter != desired)
                _ambianceEngine.SetFilter(desired);
        }

        // Header: agent name (left) + noetic counter (right, hidden in phases without cost) —
        // replaced by an animated progress bar spanning the full row while the LLM is
        // generating text, since the name/points aren't meaningful mid-generation.
        bool showNoetic = _scene?.Phase != NarrationPhase.ChildhoodReminescence
                       && _scene?.Phase != NarrationPhase.GetUp;
        if (_narrationState.IsAnyLoading)
            _ui.RenderHeaderProgressBar();
        else
            _ui.RenderHeader(_activePartyMember.DisplayName, _narrationState.ThinkingAttemptsRemaining,
                _activePartyMember.MaxNoeticPoints, showNoetic);

        // The footer exit button is only (re)rendered in the interactive states below. Clear its
        // click region each frame so stale zones don't linger during dice/loading/error states.
        _exitButtonRegion = default;

        // Footer scene info — shown as the default footer in every state
        string sceneInfo = BuildSceneInfoLine();

        // Show error if present
        if (_narrationState.ErrorMessage != null)
        {
            _ui.ShowError(_narrationState.ErrorMessage);
            _ui.RenderStatusBar(sceneInfo);
            return;
        }

        // Dice roll overlay (action execution): narration stays visible but greyed out
        // underneath a small dice box — same presentation as fight mode.
        if (_narrationState.IsDiceRollActive)
        {
            _dice.Advance();
            RenderNarrationContent();
            _ui.RenderDiceComponent(_dice, _narrationState.IsDiceRollButtonHovered);

            string diceStatus = _narrationState.IsDiceRolling
                ? "Rolling dice..."
                : (_dice.IsCurrentlySuccess ? "Success! Click Continue to see the outcome" : "Failed! Click Continue to see the outcome");
            _ui.RenderStatusBar(diceStatus);
            return;
        }

        // LLM generating (non-action loading, or action evaluation phase before dice roll):
        // keep the narration visible but greyed out below the animated header bar, with the
        // waiting message (animated ellipsis) repeated on the footer status line.
        if (_narrationState.IsAnyLoading)
        {
            RenderNarrationContent();
            _ui.DimContentBelowHeader();
            _ui.RenderWaitingStatus(_narrationState.LoadingMessage);
            return;
        }

        // Show continue button if flagged
        if (_narrationState.ShowContinueButton)
        {
            RenderNarrationContent(dimContent: true);

            // Post-action progression uses the single footer button, shown here as CONTINUE.
            RenderFooterButton(showNoetic);
            _ui.RenderStatusBar(sceneInfo);
            return;
        }

        // Render observation blocks with keywords
        RenderNarrationContent(_narrationState.HoveredKeyword, _narrationState.HoveredAction);

        // Single footer button — LEAVE/RUNAWAY while idle in exploration (the only interactive
        // control once noetic points are exhausted, since keyword regions render inert then).
        RenderFooterButton(showNoetic);

        // Footer always shows scene info in the idle observation state
        _ui.RenderStatusBar(sceneInfo);
    }

    /// <summary>
    /// Render the panel's normal content: observation blocks plus the scrollbar
    /// (updating the stored thumb region for hit-testing).
    /// </summary>
    private void RenderNarrationContent(
        KeywordRegion? hoveredKeyword = null,
        ActionRegion?  hoveredAction  = null,
        bool           dimContent     = false)
    {
        _ui.RenderObservationBlocks(
            _scrollBuffer,
            _narrationState.ScrollOffset,
            _narrationState.ThinkingAttemptsRemaining,
            hoveredKeyword,
            hoveredAction,
            dimContent);

        _narrationState.ScrollbarThumb = _ui.RenderScrollbar(
            _scrollBuffer,
            _narrationState.ScrollOffset,
            _narrationState.IsScrollbarThumbHovered);
    }

    /// <summary>
    /// Builds the scene-info line displayed in the footer:
    /// "BiomeName — location name | Time of day".
    /// </summary>
    private string BuildSceneInfoLine()
    {
        string location = _currentNode.DisplayName.Replace("_", " ");
        string biome    = _worldContext?.DisplayName ?? "";
        string time     = _graph.CurrentPeriod.ToString();

        if (biome.Length > 0)
            return $"{biome}  —  {location}  |  {time}";
        return $"{location}  |  {time}";
    }

    /// <summary>
    /// Renders the single narration footer button (CONTINUE / LEAVE / RUNAWAY) and records its
    /// click region for <see cref="OnMouseMove"/>/<see cref="OnMouseClick"/>.
    /// <para>
    /// CONTINUE shows while a succeeded action awaits progression (<see cref="NarrativeState.ShowContinueButton"/>)
    /// and throughout the no-early-exit phases; otherwise the idle exploration state shows LEAVE/RUNAWAY.
    /// In a no-early-exit phase's idle state (nothing pending) no button is drawn — matching the old flow.
    /// </para>
    /// </summary>
    private void RenderFooterButton(bool showNoetic)
    {
        bool postAction = _narrationState.ShowContinueButton;

        // Reminescence / get-up: only surface the button (as CONTINUE) while progression is pending.
        if (!showNoetic && !postAction)
        {
            _exitButtonRegion = default;
            return;
        }

        ExitButtonKind kind = postAction ? ExitButtonKind.Continue : ComputeExitContext().kind;
        string label = kind switch
        {
            ExitButtonKind.Continue => "CONTINUE",
            ExitButtonKind.Leave    => "LEAVE",
            _                       => "RUNAWAY",
        };
        _exitButtonRegion = _ui.RenderExitButton(label, _exitButtonHovered);
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

        // UpdateHover returns true when the highlighted option changed — play a tick then,
        // so hovering options inside a popup gives the same feedback as elsewhere.
        bool hoverChanged = false;
        if (_modusMentisPopup.IsVisible)
            hoverChanged |= _modusMentisPopup.UpdateHover(screenPosition.X, screenPosition.Y, _core.ClientSize, cellPixelSize);

        if (_itemSelectionPopup.IsVisible)
            hoverChanged |= _itemSelectionPopup.UpdateHover(screenPosition.X, screenPosition.Y, _core.ClientSize, cellPixelSize);

        if (_choicePopup.IsVisible)
            hoverChanged |= _choicePopup.UpdateHover(screenPosition.X, screenPosition.Y, _core.ClientSize, cellPixelSize);

        if (hoverChanged) PlayHoverSound();
    }
    
    /// <summary>
    /// Handle raw mouse click event with screen pixel coordinates.
    /// Used when popup is visible to bypass terminal cell coordinate system.
    /// </summary>
    public void OnRawMouseClick(Vector2 screenPosition)
    {
        PlayClickSound();
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
        
        // If any popup is visible, suppress narrative hover sounds entirely.
        if (_modusMentisPopup.IsVisible || _itemSelectionPopup.IsVisible || _choicePopup.IsVisible)
        {
            return;
        }
        
        // Handle dice roll screen hover
        if (_narrationState.IsDiceRollActive && !_narrationState.IsDiceRolling)
        {
            var region = _dice.ContinueButtonRegion;
            bool isOverButton = mouseY == region.Y && mouseX >= region.X && mouseX < region.X + region.Width;
            if (isOverButton != _narrationState.IsDiceRollButtonHovered)
            {
                _narrationState.IsDiceRollButtonHovered = isOverButton;
                if (isOverButton) PlayHoverSound();
            }
            _dice.HandleHumorHover(mouseX, mouseY);
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

        // Footer button (CONTINUE / LEAVE / RUNAWAY) hover — present in idle and post-action states.
        if (_exitButtonRegion.Width > 0)
        {
            bool overExit = mouseY == _exitButtonRegion.Y
                         && mouseX >= _exitButtonRegion.X
                         && mouseX <  _exitButtonRegion.X + _exitButtonRegion.Width;
            if (overExit != _exitButtonHovered)
            {
                _exitButtonHovered = overExit;
                if (overExit) PlayHoverSound();
            }
        }

        // In the post-action (CONTINUE) state and while the LLM is generating, content is
        // inert — skip keyword/action hover (scrollbar interactions above are still allowed).
        if (_narrationState.ShowContinueButton || _narrationState.IsAnyLoading)
            return;
        
        // Update hovered keyword region
        KeywordRegion? newHoveredKeyword = _ui.GetHoveredKeyword(mouseX, mouseY);
        
        if (newHoveredKeyword != _narrationState.HoveredKeyword)
        {
            if (newHoveredKeyword != null) PlayHoverSound();
            _narrationState.HoveredKeyword = newHoveredKeyword;
            // UI will re-render on next Update() call
        }
        
        // Update hovered action region
        ActionRegion? newHoveredAction = _ui.GetHoveredAction(mouseX, mouseY);
        
        if (newHoveredAction != _narrationState.HoveredAction)
        {
            if (newHoveredAction != null) PlayHoverSound();
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
            // Continue button takes priority over the humor layer.
            var region = _dice.ContinueButtonRegion;
            bool overContinue = mouseY == region.Y && mouseX >= region.X && mouseX < region.X + region.Width;
            if (overContinue)
            {
                Console.WriteLine("NarrativeController: Dice roll continue button clicked");
                PlayClickSound();
                // An exit-runaway roll resolves the exit instead of committing a thinking outcome.
                if (_exitRunawayPending)
                    FinishExitRunaway();
                else
                    OnDiceRollContinue();
                return;
            }
            _dice.HandleHumorClick(mouseX, mouseY);
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

        // Single footer button (CONTINUE / LEAVE / RUNAWAY) — clickable in idle and post-action states.
        if (_exitButtonRegion.Width > 0
            && mouseY == _exitButtonRegion.Y
            && mouseX >= _exitButtonRegion.X
            && mouseX <  _exitButtonRegion.X + _exitButtonRegion.Width)
        {
            PlayClickSound();
            HandleFooterButtonClicked();
            return;
        }

        // In the post-action (CONTINUE) state and while the LLM is generating, the content
        // is inert — swallow other clicks (scrollbar clicks were already handled above).
        if (_narrationState.ShowContinueButton || _narrationState.IsAnyLoading)
            return;

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
            PlayClickSound();
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
            PlayClickSound();
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
                if (thinkingModiMentis.Count == 0)
                    throw new InvalidOperationException("NarrativeController: No thinking modus mentis available for selection.");
                Vector2 screenPos = _terminalInputHandler.CellToScreen(_lastMouseX, _lastMouseY, _core.ClientSize);
                _modusMentisPopup.Show(screenPos, thinkingModiMentis, "Select Thinking ModusMentis");
            }
            else if (choiceIndex == 1 && _narrationState.HoveredKeyword != null)
            {
                Console.WriteLine("NarrativeController: Choice — Observe");
                _narrationState.IsSelectingObservationModusMentis = true;
                _narrationState.IsSelectingModusMentisForSpeaking = false;
                var observationModiMentis = _activePartyMember.GetObservationModiMentis();
                if (observationModiMentis.Count == 0)
                    throw new InvalidOperationException("NarrativeController: No observation modus mentis available for selection.");
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

    /// <summary>The world context of the current location (for dialogue template fields).</summary>
    public WorldContext? WorldContext => _worldContext;

    /// <summary>The current location's vertex id (for dialogue template fields).</summary>
    public int LocationId => _locationId;

    /// <summary>
    /// Unified next-phase request. Maps the legacy pending fight/dialogue outcomes onto the
    /// <see cref="PhaseTransition"/> abstraction so the game controller can consume one channel.
    /// </summary>
    public PhaseTransition? PendingPhaseTransition
    {
        get
        {
            if (_pendingFightOutcome != null)
                return new StartFightTransition(_pendingFightOutcome.Target, _pendingFightOutcome.CombatContext, _pendingFightOutcome.EnemyInitiative);
            if (_pendingDialogueOutcome != null)
                return new StartDialogueTransition(_pendingDialogueOutcome.Target,
                    _pendingDialogueOutcome.TreeId, _pendingDialogueOutcome.Tree);
            return null;
        }
    }

    /// <summary>Clears whichever pending transition was just consumed.</summary>
    public void ClearPendingPhaseTransition()
    {
        _pendingFightOutcome    = null;
        _pendingDialogueOutcome = null;
    }

    /// <summary>
    /// Saves any routine still being recorded for this session. Called by the game controller when
    /// the narration phase ends (returns to world travel).
    /// </summary>
    public void FinalizeRoutineRecording()
    {
        _recorder?.FinalizeAtNarrationEnd();
        _recorder = null;
    }

    /// <summary>
    /// Starts narration positioned at a specific area and time period (used when continuing into
    /// narration after a routine replay ends at a moved-to area). Falls back to a normal start when
    /// the area cannot be resolved.
    /// </summary>
    public void StartAtArea(string areaLemma, TimePeriod time)
    {
        if (_scene != null && _pov != null)
        {
            var area = _scene.AllAreas.FirstOrDefault(a =>
                string.Equals(a.ReferenceLemma, areaLemma, StringComparison.OrdinalIgnoreCase));
            if (area != null)
            {
                _pov.Where = area;
                _pov.When  = time;
                var nodeId = area.DisplayName.ToLowerInvariant().Replace(' ', '_');
                if (_graph.AllNodes.TryGetValue(nodeId, out var node))
                    _currentNode = node;
            }
        }
        StartObservationPhase(time);
    }

    /// <summary>
    /// Clear the pending fight outcome after the game controller has handled it.
    /// </summary>
    public void ClearPendingFight() => _pendingFightOutcome = null;
    
    /// <summary>
    /// Clear the pending dialogue outcome after the game controller has handled it.
    /// </summary>
    public void ClearPendingDialogue() => _pendingDialogueOutcome = null;

    // ── Narration-exit (LEAVE / RUNAWAY) ─────────────────────────────────────────

    /// <summary>
    /// Decides what the footer exit button should offer given the current location:
    /// <list type="bullet">
    /// <item><b>RunawayEnemy</b> — a visual enemy is present in the current area (highest priority).</item>
    /// <item><b>RunawayWitness</b> — the current area is private (illegal) with a visual witness present.</item>
    /// <item><b>Leave</b> — otherwise; exiting is free.</item>
    /// </list>
    /// Only same-area (visual) threats/witnesses count — leaving is about the location you stand in.
    /// </summary>
    private (ExitButtonKind kind, NpcEntity? target) ComputeExitContext()
    {
        if (_scene == null || _pov == null || _protagonist == null)
            return (ExitButtonKind.Leave, null);

        // Enemy in the current location takes precedence over a witness confrontation.
        var threat = ThreatSelector.ComputeContext(_scene, _pov, _protagonist);
        if (threat.Level == ThreatLevel.Visual && threat.Threat != null)
            return (ExitButtonKind.RunawayEnemy, threat.Threat);

        // Illegal (private) location with a bystander who could see you leave.
        if (_pov.Where.IsPrivate)
        {
            var witness = WitnessSelector.ComputeContext(_scene, _pov);
            if (witness.Type == WitnessType.Visual && witness.Witness != null)
                return (ExitButtonKind.RunawayWitness, witness.Witness);
        }

        return (ExitButtonKind.Leave, null);
    }

    /// <summary>Number of d6 rolled in an exit-runaway check — the protagonist's feet stat, min 1.</summary>
    private int ProtagonistRunawayDiceCount()
    {
        return _protagonist.DerivedStats.First(s => s.Name == "runaway_dice").GetValue(_protagonist);
    }

    /// <summary>
    /// Invoked when the single footer button is clicked. In the post-action state it acts as CONTINUE
    /// (<see cref="HandleContinueClicked"/>); otherwise LEAVE exits immediately and the RUNAWAY variants
    /// begin a runaway dice roll against the enemy/witness (resolved in <see cref="FinishExitRunaway"/>).
    /// </summary>
    private void HandleFooterButtonClicked()
    {
        // Post-action progression (and every press in the no-early-exit phases) is a CONTINUE.
        if (_narrationState.ShowContinueButton)
        {
            HandleContinueClicked();
            return;
        }

        var (kind, target) = ComputeExitContext();
        switch (kind)
        {
            case ExitButtonKind.Leave:
                Console.WriteLine("NarrativeController: LEAVE clicked — exiting narration");
                _narrationState.RequestedExit = true;
                break;
            case ExitButtonKind.RunawayEnemy when target != null:
                Console.WriteLine($"NarrativeController: RUNAWAY clicked — enemy '{target.DisplayName}' present");
                _ = BeginExitRunawayAsync(target, isEnemy: true);
                break;
            case ExitButtonKind.RunawayWitness when target != null:
                Console.WriteLine($"NarrativeController: RUNAWAY clicked — witness '{target.DisplayName}' present");
                _ = BeginExitRunawayAsync(target, isEnemy: false);
                break;
        }
    }

    /// <summary>
    /// Handles a CONTINUE press: drives the get-up / reminescence terminal transitions and node/area
    /// transitions. With nothing pending, the no-early-exit phases restart observations (as the old
    /// Continue button did) while normal exploration simply returns to the interactive observation
    /// state without regenerating observations.
    /// </summary>
    private void HandleContinueClicked()
    {
        // Get-Up success transition: protagonist risen, world travel begins.
        if (_scene != null && _scene.PendingGetUpTransition)
        {
            HandleGetUpContinue();
            return;
        }

        // Reminescence transition takes priority over a normal node transition.
        if (_scene != null && _scene.PendingReminescenceTransition is { } req)
        {
            HandleReminescenceContinue(req);
            return;
        }

        // Transition to a new node (e.g. an action moved the player to a different area).
        if (_narrationState.PendingTransitionNode != null)
        {
            Console.WriteLine($"NarrativeController: CONTINUE — transitioning to node {_narrationState.PendingTransitionNode.NodeId}");
            _currentNode = _narrationState.PendingTransitionNode;
            _scrollBuffer.ConvertToHistory();
            _narrationState.ResetForNewNode();
            _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;
            StartObservationPhaseWithHistory();
            return;
        }

        // A movement/leave action requested exit-on-continue (the LEAVE button is the primary exit
        // now, but honor an explicit movement-exit action too).
        if (_narrationState.ShouldExitOnContinue)
        {
            Console.WriteLine("NarrativeController: CONTINUE — movement action, exiting to world view");
            _narrationState.RequestedExit = true;
            return;
        }

        // Nothing pending. No-early-exit phases regenerate observations (as before); normal
        // exploration returns to the interactive state without restarting observations.
        bool noEarlyExit = _scene?.Phase == NarrationPhase.ChildhoodReminescence
                        || _scene?.Phase == NarrationPhase.GetUp;
        if (noEarlyExit)
        {
            Console.WriteLine("NarrativeController: CONTINUE — restarting observations (no-early-exit phase)");
            _scrollBuffer.ConvertToHistory();
            _narrationState.ResetForNewNode();
            _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;
            StartObservationPhaseWithHistory();
        }
        else
        {
            Console.WriteLine("NarrativeController: CONTINUE — returning to interactive observation (no restart)");
            _narrationState.ShowContinueButton = false;
        }
    }

    /// <summary>
    /// Plays the runaway dice-roll overlay (single roll, ≥1 six to flee — same rule as combat).
    /// Resolution happens on the dice Continue click in <see cref="FinishExitRunaway"/>.
    /// </summary>
    private async Task BeginExitRunawayAsync(NpcEntity target, bool isEnemy)
    {
        _exitRunawayPending  = true;
        _exitRunawayTarget   = target;
        _exitRunawayIsEnemy  = isEnemy;

        int diceCount = ProtagonistRunawayDiceCount();
        NarrationDiceStart(diceCount, 1, subtitle: "RUNAWAY CHECK — feet", difficultyVerb: "to flee");
        _narrationState.LoadingMessage = "Rolling dice...";

        // Brief animation window (no async work backs this roll, unlike thinking checks).
        await Task.Delay(900);

        var values = new int[diceCount];
        for (int i = 0; i < diceCount; i++) values[i] = _diceRandom.Next(1, 7);
        NarrationDiceComplete(values);
    }

    /// <summary>
    /// Resolves an exit-runaway roll after the player clicks Continue on the dice overlay.
    /// Success → exit narration. Failure → start a fight (enemy) or the caught-red-handed
    /// trespass dialogue (witness). Enemy precedence is already baked into <c>isEnemy</c>.
    /// </summary>
    private void FinishExitRunaway()
    {
        bool success   = _dice.IsCurrentlySuccess;
        var  target    = _exitRunawayTarget;
        bool isEnemy   = _exitRunawayIsEnemy;

        _exitRunawayPending = false;
        _exitRunawayTarget  = null;
        NarrationDiceClear();

        if (success)
        {
            Console.WriteLine("NarrativeController: RUNAWAY succeeded — exiting narration");
            _narrationState.RequestedExit = true;
            return;
        }

        if (target == null)
        {
            // Defensive: nothing to escalate to, just leave.
            _narrationState.RequestedExit = true;
            return;
        }

        if (isEnemy)
        {
            Console.WriteLine($"NarrativeController: RUNAWAY failed — fight starts vs '{target.DisplayName}'");
            _pendingFightOutcome = new FightOutcome(target, $"failed to run away from {target.DisplayName}");
        }
        else
        {
            Console.WriteLine($"NarrativeController: RUNAWAY failed — witness '{target.DisplayName}' confronts trespass");
            var catchTree = CaughtRedHandedTreeFactory.Create(CriminalAffinityType.Intruder, target.IsBrave);
            _pendingDialogueOutcome = new DialogueOutcome(target, tree: catchTree);
        }
    }
    
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
            // The whole party fights together, so every alive enemy now considers each party
            // member (protagonist + companions) an enemy after the party fled.
            var partyNames = new List<string> { _protagonist.DisplayName };
            partyNames.AddRange(_protagonist.CompanionParty.Select(c => c.DisplayName));

            foreach (var enemy in enemies)
            {
                if (!enemy.IsAlive) continue;
                foreach (var name in partyNames)
                    enemy.AffinityTable.SetEnemy(name);
                Console.WriteLine($"NarrativeController: {enemy.DisplayName} flagged the whole party ({partyNames.Count} member(s)) as enemies after runaway");
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
        return _activePartyMember.GetAllItems()
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
                ?? _activePartyMember.ModiMentis.FirstOrDefault(m => m.ModusMentisId == action.ActionModusMentisId);

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

            // === CRITIC: can the item help? (single pass, neutral goal-based phrasing) ===
            var appropriatenessTree = CriticTrees.BuildItemAppropriatenessTree(goalDescription, item.DisplayName, criticContext);
            var appropriatenessResult = await _actionExecutor.ItemUseCritic.EvaluateTreeAsync(appropriatenessTree);
            bool appropriatenessSuccess = appropriatenessResult.OverallSuccess;
            Console.WriteLine($"NarrativeController: Item appropriateness (neutral): {(appropriatenessSuccess ? "success" : "fail")}");

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
                // Chain leaf: a synthetic ModusMentis carrying item name + effective usage level so that:
                //   - the action button shows [ItemName ◼◼] instead of [ActionSkill ◼◼◼]
                //   - GetTotalModusMentisLevel() = obs.Level + thinking.Level + action.Level + effectiveUsage (no repetition)
                // The item's UsageLevel is capped by the hands-derived "item_usage_cap" stat so that
                // characters with stronger (or unwounded) hands extract more bonus from potent tools.
                int usageCap = _activePartyMember.DerivedStats
                    .First(s => s.Name == "item_usage_cap").GetValue(_activePartyMember);
                int effectiveUsageLevel = System.Math.Min(item.UsageLevel, usageCap);
                Console.WriteLine($"NarrativeController: Item usage level {item.UsageLevel} capped to {effectiveUsageLevel} (hands cap {usageCap}).");
                var itemModusMentis = new SyntheticItemModusMentis(item.ItemId, item.DisplayName, effectiveUsageLevel);

                var combinedAction = new ParsedNarrativeAction
                {
                    ActionText             = reformulatedText,
                    DisplayText            = reformulatedText,
                    // Keep a neutral phrasing for the outcome template ("I tried to … using an item")
                    // so it doesn't re-embed the styled reformulation; empty when the source had none.
                    NeutralActionText      = string.IsNullOrWhiteSpace(action.NeutralActionText)
                                                 ? string.Empty
                                                 : $"{action.NeutralActionText} using {item.WithArticle()}",
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
