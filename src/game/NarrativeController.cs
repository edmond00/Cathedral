using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Cathedral.Audio;
using Cathedral.Debug;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Dialogue.Tree.Trees;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Preview;
using Cathedral.Game.Narrative.Routines;
using Cathedral.Game.Narrative.Rules;
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

    /// <summary>The Tab map (developer keys). See <see cref="DebugLocationMap"/>.</summary>
    private readonly DebugLocationMap _debugMap = new();

    /// <summary>
    /// The mouse-following popup, shared with the modal popups below: the map's pointer label goes up
    /// in it while none of them is open (<see cref="UpdateMapLabel"/>).
    /// </summary>
    private readonly PopupTerminalHUD _popup;
    private bool _mapLabelShown;
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
    
    // Get-Up defers its outcome narration to the dice CONTINUE (like the main action path), so the
    // dice animation runs for the fixed Config.Dice duration instead of blocking on LLM generation.
    // Set while the get-up dice animate; invoked once by OnDiceRollContinue.
    private Func<Task>? _pendingGetUpOutcome = null;
    
    // _graph and _scene are mutable so the reminescence flow can swap them when transitioning
    // between consecutive reminescences without rebuilding the controller.
    private NarrationGraph _graph;
    private readonly int _locationId;

    // ── Scene system (new backend, coexists with NarrationGraph) ──
    private Cathedral.Game.Scene.Scene? _scene;
    private PoV? _pov;

    // Period-aware placement of the scene's NPCs into graph nodes; null on the pure-graph path.
    private Cathedral.Game.Scene.SceneNpcPlacement? _npcPlacement;
    
    // Pending fight/dialogue transitions (set by OnDiceRollContinue, consumed by game controller)
    private FightTriggerOutcome? _pendingFightOutcome = null;

    /// <summary>
    /// A fight that has been decided but not yet entered, waiting on the CONTINUE press that closes
    /// the segment. <see cref="_pendingFightOutcome"/> is read by the game controller on the very next
    /// tick, so anything written there switches mode before a frame is drawn — which is right for a
    /// fight the player was dragged into and wrong for one they started, since the outcome chips
    /// (what the first blow was, what it broke, what it taught) are the only account they get of the
    /// swing and the fight screen paints over the narration that carries them.
    /// </summary>
    private FightTriggerOutcome? _deferredFightOutcome = null;
    private DialogueTriggerOutcome? _pendingDialogueOutcome = null;

    // What the next observation phase opens on, when something other than the persona has decided it,
    // and the observation modus mentis to narrate it with (null: pick one). Consumed by that phase.
    // Set by a conversation (the person just spoken to), an introduction (the person just presented)
    // and a Meet routine (the person it is about). An Element rather than a person, so anything with an
    // observation of its own can be opened on the same way — see TryGenerateFocusObservationAsync.
    private Element? _openingFocus = null;
    private ModusMentis? _openingFocusMM = null;

    // Learns routines from this narration session. Non-null only for scene-backed Exploration narration.
    private RoutineRecorder? _recorder = null;
    
    // Random for dice rolls — the run-long shared stream, not a per-controller generator: a new
    // NarrativeController is built for every narration phase, and a fresh Random on the same derived
    // seed made the first roll of each phase repeat the previous phase's first roll.
    private readonly Random _diceRandom = GameRng.Stream("dice");

    // Unified dice-roll overlay (animation + humor modifiers + hit-testing).
    private readonly DiceRollComponent _dice = new();

    /// <summary>
    /// The box that shows the hours passing (and, for sitting and waiting, asks how many). Modal over
    /// the narration like the dice box; <see cref="_afterTimeBox"/> is what its Continue resumes.
    /// </summary>
    private readonly TimePassageBox _timeBox = new();
    private Action? _afterTimeBox;

    /// <summary>
    /// Set when the passage of the action being committed was already shown — a chosen wait plays it
    /// before its narration — so the commit does not play it a second time.
    /// </summary>
    private bool _passagePlayed;

    /// <summary>
    /// A move shown on the location map: the party's glyph walking, a cell a step, from the card of
    /// the area left along the way taken to the card of the area reached, then a Continue. Modal like
    /// the time box, and played after it when an act moves both the hour and the party (a stalk).
    /// Presentation only: the move itself has already been made.
    /// </summary>
    private List<DebugLocationMap.WalkStep>? _walk;
    private IReadOnlyList<(Area To, string? Link)> _walkHops = Array.Empty<(Area, string?)>();
    /// <summary>What the move recorded that the map did not know before: revealed as the walk reaches it.</summary>
    private HashSet<string> _walkFresh = new();
    private int _walkShown;
    private DateTime _walkStart;
    private double _walkStepMs;
    private Action? _afterWalk;
    private (int X, int Y, int Width) _walkContinueRegion;
    private bool _walkContinueHovered;
    /// <summary>Whether the view has been centred on the party since it arrived; after that it is the arrows'.</summary>
    private bool _walkArrivalCentred;
    private string? _lastWalk;

    /// <summary>
    /// A successful backtrack asking where to: the areas known ways reach, lit on the location map for
    /// a click. The outcome is narrated once one is chosen (<see cref="Scene.BacktrackTo"/>). Null when
    /// nothing asks.
    /// </summary>
    private HashSet<Area>? _backtrackChoices;
    private Area? _backtrackHovered;
    private bool _backtrackCentred;
    private Action<Area>? _onBacktrackChosen;

    /// <summary>A cell a step, but no walk longer than <see cref="WalkMaxMs"/> however far it goes.</summary>
    private const double WalkStepMs = 90, WalkMaxMs = 6000;

    // In-flight roll (main path): the evaluation to narrate and the current (humor-modified) result.
    // The actual outcome is generated only once the player presses the dice CONTINUE — see
    // OnDiceRollContinue → GenerateOutcomePreviewAsync — so no narration is produced during the roll.
    private ActionEvaluationResult? _pendingEval;
    private bool _pendingSucceeded;
    // Separator caption for the segment the post-action CONTINUE closes ("after trying to grab a
    // rope"). Set when an in-place outcome shows the CONTINUE button; consumed by HandleContinueClicked.
    private string? _pendingSegmentLabel;

    /// <summary>
    /// Whether the action that closed the current segment succeeded. Only a success refills noetic
    /// points on the CONTINUE that follows — a failed attempt costs the point it spent and leaves the
    /// player to think again with what is left, which is what makes a point worth spending at all.
    /// Defaults true so every other path into a segment (a fight, a conversation, an area move)
    /// refills as before.
    /// </summary>
    private bool _pendingSegmentSucceeded = true;

    // ── Narration footer button (single button, three values) ────────────────────
    // The one bottom button in narration. In normal exploration it is LEAVE (or RUNAWAY when a
    // visible enemy/witness makes leaving risky) while idle, and CONTINUE while a succeeded action
    // is pending progression (node/area transition). In the childhood-reminescence and get-up
    // phases early exit is impossible, so it is only ever CONTINUE.
    private enum ExitButtonKind { Leave, RunawayEnemy, RunawayWitness, Continue }
    // Click region of the footer exit button as last rendered (Width == 0 ⇒ not shown this frame).
    private (int X, int Y, int Width) _exitButtonRegion;
    private bool _exitButtonHovered;

    /// <summary>The header's ↹ (the map and back); default while it is not drawn.</summary>
    private (int X, int Y, int Width) _mapButtonRegion;
    private bool _mapButtonHovered;
    // Set while an exit-runaway dice roll is in flight, so the dice Continue click resolves the exit
    // (via FinishExitRunaway) instead of the normal thinking-action OnDiceRollContinue.
    private bool _exitRunawayPending;
    private NpcEntity? _exitRunawayTarget;
    private bool _exitRunawayIsEnemy;

    // ── LLM generation preview box ───────────────────────────────────────────────
    // Streams the text being generated over the greyed-out menu, gated by a CONTINUE button.
    // Active from the first BeginPart until the last part's CONTINUE commits its block(s).
    private readonly LlmPreviewSession _previewSession = new();
    private (int X, int Y, int Width) _previewContinueRegion; // last-rendered CONTINUE region (Width 0 ⇒ none)
    private bool _previewContinueHovered;
    // The doubt's YES / NO, when the preview box asks (Width 0 ⇒ none); which one is hovered, if any.
    private ((int X, int Y, int Width) Yes, (int X, int Y, int Width) No) _previewAnswerRegions;
    private bool? _previewAnswerHovered;
    private string _lastPreviewText = ""; // last rendered preview text, to tick on change

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
        PlayDiceVerdictCue(_dice.IsCurrentlySuccess);
    }

    /// <summary>
    /// Feedback for a settled roll: the PCM click, then the success/failure sting. The click is what
    /// guarantees the reveal is heard at all — the stings are MIDI and go silent without an open
    /// device, which is why a humor-modified result seemed to be the only one that made a sound (the
    /// humor button plays its own click on the way).
    /// </summary>
    private void PlayDiceVerdictCue(bool success)
    {
        PlayClickSound();
        _ambianceEngine?.TriggerGameEvent(success
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
    /// Fired by the dice component when a humor modifier flips success↔failure. Records the new final
    /// result (the outcome is generated later, on the dice CONTINUE) and replays the outcome cue.
    /// </summary>
    private void OnDiceOutcomeFlipped(bool nowSuccess)
    {
        _pendingSucceeded = nowSuccess;
        _ambianceEngine?.TriggerGameEvent(nowSuccess
            ? GameEventType.PositiveOutcome : GameEventType.NegativeOutcome);
    }

    // Active party member (starts as protagonist, switches to companion after Speak About)
    private PartyMember _activePartyMember = null!;
    // Companion list parallel to the companion selection choice popup choices
    private List<PartyMember> _pendingCompanions = new();
    // Per-member noetic point counters — keyed by DisplayName.
    // Preserved across hand-offs so returning to a member keeps their remaining points.
    private readonly Dictionary<string, int> _memberNoeticPoints = new();

    // What has already been looked at in the current narration phase. Every observation request
    // narrows its choice list by this, so a phase explores the scene instead of circling one object;
    // it is cleared wherever the live text greys into history (see CloseNarrationSegment).
    private readonly ObservationLedger _observationLedger = new();

    // The area and time of day the last area introduction described. An overall observation opens on
    // an introduction only when either has changed since — every action ends a phase, and repeating
    // "I find myself inside the barn" after each one would be noise. Area by reference: a new visit
    // builds new Area objects, so arriving again is a change too.
    private Cathedral.Game.Scene.Area? _introducedArea;
    private TimePeriod? _introducedPeriod;

    // True from a visit's arrival until the observation that opens it: that block must name somebody
    // when anybody is in view (ObservationPhaseController.AppendPersonObservationIfMissingAsync).
    // Only an arrival sets it — StartNarrativeInteraction is the one caller passing an arrival hour.
    private bool _arrivalObservationPending;

    // What the current narration phase has already put an action button on screen for. The thinking
    // side of the same rule the ledger above applies to observations: every goal list narrows by it,
    // so the points spent in one phase explore the scene instead of circling one action. Cleared in
    // the same place, for the same reason (see CloseNarrationSegment).
    private readonly ActionProposalLedger _proposalLedger = new();

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
        _timeBox.OnTick       = () => _ambianceEngine?.TriggerGameEvent(GameEventType.SmallInteraction);
        _observationLedger.Observed += NoteObserved;
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
        // Whoever is acting when a block commits is who the journal credits it to: the Speak About
        // hand-off commits the speech first and switches member after, so the speech stays the speaker's.
        _scrollBuffer.BlockAdded += block => JournalRecorder.Narration(block, _activePartyMember);
        _popup = popup;
        _modusMentisPopup = new TerminalThinkingModusMentisPopup(popup);
        _itemSelectionPopup = new TerminalItemSelectionPopup(popup);
        _choicePopup = new TerminalSimpleChoicePopup(popup);
        _core = core;
        _terminalInputHandler = terminalInputHandler;
        _worldContext = worldContext ?? new PlainBiomeContext();
        _locationId = locationId;

        // Publish where this scene is, so every prompt's closing reminder can name it (a forest must
        // not be furnished with town streets). Both constructors funnel through here, and every
        // narration session builds a controller, so the ambient value cannot go stale behind a move.
        SceneSetting.SetPlace(_worldContext.GenerateContextDescription(locationId));

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
        Console.WriteLine($"NarrativeController: Generated graph for location {locationId} with entry node '{_currentNode.NodeId}' ({_graph.AllNodes.Count} nodes)");
        LlmMonitorDebugManager.Show();
        
        // Initialize controllers
        _observationController = new ObservationPhaseController(llamaServer, slotManager, _worldContext)
        {
            MustCommit = () => _scene?.Phase is NarrationPhase.ChildhoodReminescence or NarrationPhase.GetUp,
            Standing   = () => _scene != null && _pov != null ? (_scene, _pov) : null,
        };
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
        _npcPlacement = new Cathedral.Game.Scene.SceneNpcPlacement(scene, _graph.AllNodes.Values);

        // Build initial PoV from the area the graph opened on — the first the factory built, or the
        // one --start-area names. Same helper, so PoV and entry node can never point at different rooms.
        var firstArea = Cathedral.Game.Scene.SceneSyntheticGraphFactory.ResolveEntryArea(scene);
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
    /// The shared text history for this narration session. Every text-bearing phase of the visit
    /// writes into it — dialogue appends its lines live, fight/trade append their summaries — so the
    /// player can scroll back through the whole visit. It is emptied only when a new session starts
    /// (i.e. after world travel), since each session builds its own controller.
    /// </summary>
    public NarrationScrollBuffer ScrollBuffer => _scrollBuffer;

    /// <summary>
    /// Grey the current live text into history, closed by a labelled separator rule, and reset the
    /// node state (which refills noetic points). The node, scene and PoV are left untouched, so
    /// anything a phase added to the world — corpses from a fight, for instance — is still there.
    /// Call this when LEAVING narration for another phase; no observation pass is started.
    ///
    /// <para>This is the phase boundary every return-to-narration path funnels through, so it is also
    /// where the <see cref="ObservationLedger"/> is emptied: the scene becomes fully observable again,
    /// even standing in the same place with the same point of view.</para>
    /// </summary>
    /// <param name="separatorLabel">Caption for the rule, naming the segment that follows.</param>
    /// <param name="refillNoetic">
    /// False keeps the counter where the closing segment left it. Passed by the one caller that
    /// closes on a <b>failed</b> action: refilling there meant a miss cost nothing, since the very
    /// next CONTINUE handed the whole budget back. Every other boundary refills.
    /// </param>
    public void CloseNarrationSegment(string? separatorLabel = null, bool refillNoetic = true)
    {
        int kept = _narrationState.ThinkingAttemptsRemaining;
        _scrollBuffer.ConvertToHistory(separatorLabel);
        _narrationState.ResetForNewNode();
        if (!refillNoetic) _narrationState.ThinkingAttemptsRemaining = kept;
        _observationLedger.Clear();
        Cathedral.Game.Scene.ActionPathPlanner.Forget();
        _proposalLedger.Clear();
        _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;
    }

    /// <summary>
    /// <see cref="CloseNarrationSegment"/> followed by a fresh observation pass in the SAME node and
    /// scene. Call this when RETURNING to narration from another phase (or on a node transition,
    /// where the caller reassigns <c>_currentNode</c> first): the player gets full noetic points and
    /// narration that describes the scene as it now stands.
    /// </summary>
    public void BeginNarrationSegment(string? separatorLabel = null, bool refillNoetic = true)
    {
        CloseNarrationSegment(separatorLabel, refillNoetic);
        StartObservationPhaseWithHistory(refillNoetic);
    }

    /// <summary>
    /// Append a one-line note from a non-narration phase (e.g. a trade summary) to the shared
    /// history, so the phase leaves a trace in the log.
    /// </summary>
    public void AppendPhaseNote(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        _scrollBuffer.AddBlock(new NarrationBlock(
            Type: NarrationBlockType.Outcome,
            ModusMentis: null!,
            Text: text,
            Keywords: null,
            Actions: null));
        _scrollBuffer.ScrollToBottom();
        _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;
    }

    /// <summary>
    /// Start the observation phase (generates observations asynchronously).
    /// This clears all history - use for initial start only.
    /// </summary>
    /// <param name="openingFocus">What the first observation is of — a Meet routine's person — or null
    /// to let the persona choose as usual. Set here rather than after, because the phase is generated
    /// on a task this method starts.</param>
    /// <param name="arrivalPeriod">The location's own hour (<see cref="Scene.ArrivalPeriod"/>) when this
    /// is an arrival; <c>--period</c> still overrides it, and without either the hour is drawn.</param>
    public void StartObservationPhase(TimePeriod? forcedPeriod = null, Element? openingFocus = null,
                                      TimePeriod? arrivalPeriod = null)
    {
        _narrationState.Clear();
        _scrollBuffer.Clear();
        _activePartyMember = _protagonist;
        _memberNoeticPoints.Clear();
        _observationLedger.Clear();
        Cathedral.Game.Scene.ActionPathPlanner.Forget();
        _arrivalObservationPending = arrivalPeriod != null;
        NoteWhereWeStand();
        // New session — any focus left over from an earlier one is stale.
        _openingFocus   = openingFocus;
        _openingFocusMM = null;

        // Place NPCs into nodes based on the supplied time period, else --period, else the location's
        // fixed arrival hour, else a random one. --period pins it for the whole run: several rules
        // (every entry door shutting at night) only fire in one period. The draw is still made when the
        // arrival hour wins and its result discarded, so every later roll of a seeded run is the roll
        // it was before arrival hours existed.
        var period = forcedPeriod ?? Config.Debug.ForcedPeriod
                     ?? Discarding(TimePeriodExtensions.Random(_diceRandom), arrivalPeriod);
        ApplyTimePeriod(period);
        Console.WriteLine($"NarrativeController: Time period is {period}");

        ArmRoutineRecorder();

        _narrationState.IsLoadingObservations = true;
        _narrationState.LoadingMessage = ObservationLoadingMessage();

        // Fire-and-forget async task
        _ = GenerateObservationsAsync();

        Console.WriteLine("NarrativeController: Started observation phase");
    }
    
    /// <summary>
    /// Initializes a scene-backed session enough to open a sub-phase (trade / work) directly, WITHOUT
    /// running an observation pass. Used by the Buy, Sell and Work routines, which enter straight into
    /// the menu. Places NPCs for the period so the merchant is present, and arms a recorder so anything
    /// the player does after the sub-phase is still recordable — exactly like a normal visit, minus the
    /// opening narration.
    ///
    /// <para><paramref name="startAreaKey"/> is the routine's area. It is not cosmetic: the scene is
    /// rebuilt here, so without it the point of view sits on the location's default opening area and
    /// everything after the sub-phase happens there — a routine into a forge would put the player back
    /// in the village square the moment the trade menu closed. Set BEFORE <see cref="ApplyTimePeriod"/>,
    /// which places NPCs and re-gates every verb against the point of view.</para>
    /// </summary>
    private static TimePeriod Discarding(TimePeriod drawn, TimePeriod? instead) => instead ?? drawn;

    public void PrepareForRoutineSubPhase(TimePeriod period, string? startAreaKey = null)
    {
        _narrationState.Clear();
        _scrollBuffer.Clear();
        _activePartyMember = _protagonist;
        _memberNoeticPoints.Clear();
        _observationLedger.Clear();
        Cathedral.Game.Scene.ActionPathPlanner.Forget();
        _proposalLedger.Clear();
        _openingFocus   = null;
        _openingFocusMM = null;

        _approachFrom = _pov?.Where;
        MovePointOfViewToArea(startAreaKey);
        ApplyTimePeriod(period);
        Console.WriteLine($"NarrativeController: routine sub-phase prepared at period {period}"
                        + $", area '{_pov?.Where.DisplayName ?? "-"}'");

        ArmRoutineRecorder();
    }

    /// <summary>
    /// Starts learning routines for this session, from where it opened. Exploration only: childhood
    /// and get-up have no real location to come back to.
    /// </summary>
    private void ArmRoutineRecorder()
    {
        if (_scene != null && _scene.Phase == NarrationPhase.Exploration)
            _recorder = new RoutineRecorder(_protagonist, _locationId, _worldContext?.DisplayName ?? "", _pov?.Where);
    }

    // ── Routine moments the game controller sees first ─────────────────────────
    // A conversation, a trade and a hire are all settled outside this class (the dialogue runs in its
    // own mode, and --auto-dialogue settles it without one), so the controller reports them here; the
    // point of view they were agreed at is this class's.

    /// <summary>A conversation was opened with <paramref name="npc"/>.</summary>
    public void RecordConversationRoutine(NpcEntity npc)
    {
        if (_recorder != null && _pov != null) _recorder.OnConversation(_pov, npc);
    }

    /// <summary>A merchant agreed to trade.</summary>
    public void RecordTradeRoutine(NpcEntity npc, Cathedral.Game.Npc.Trade.TradeMode mode)
    {
        if (_recorder != null && _pov != null) _recorder.OnTrade(_pov, npc, mode);
    }

    /// <summary>A master agreed to take the player on.</summary>
    public void RecordWorkRoutine(NpcEntity npc, Cathedral.Game.Narrative.Work.Job job)
    {
        if (_recorder != null && _pov != null) _recorder.OnWork(_pov, npc, job);
    }

    /// <summary>
    /// Whether the player is standing somewhere they have no business being — what the narration
    /// header shows, and the condition under which nothing they do is lawful and no routine is learned.
    /// False outside a real location.
    /// </summary>
    public bool IsTrespassing
        => _scene?.Phase == NarrationPhase.Exploration && _pov != null && PrivacyModel.IsTrespassing(_scene, _pov.Where);

    /// <summary>
    /// A coded refusal shown without re-voicing — no modus mentis to voice it, or the rewrite came
    /// back empty. A missing tool keeps its longer sentence, which is the part worth reading.
    /// </summary>
    private static string RawRefusal(ParsedNarrativeAction action, Narrative.Rules.ActionRuleResult result)
        => result.NeededTool != null
            ? $"[IMPOSSIBLE] {OutcomeNarrator.RefusalNeutral(action, result.ErrorMessage ?? "", result.NeededTool)}"
            : $"[IMPOSSIBLE] {result.ErrorMessage}";

    /// <summary>
    /// Start the observation phase while preserving scroll buffer history.
    /// Always reached through <see cref="BeginNarrationSegment"/>, which performs the required
    /// grey-into-history + node reset first.
    /// <para>
    /// <b>The active member carries across the segment boundary.</b> Speak About hands the narration
    /// to a companion, and the hand-off has to survive the action that follows it — resetting to the
    /// protagonist here took the narration straight back the moment the companion's own action
    /// resolved. This is the same visit continuing (an action, a fight, a conversation, a move to
    /// another area); a genuinely fresh session goes through <see cref="StartObservationPhase"/>,
    /// which still opens on the protagonist.
    /// </para>
    /// </summary>
    private void StartObservationPhaseWithHistory(bool refillNoetic = true)
    {
        // The one case the hand-off cannot survive: a companion who has left the party since (dead of
        // old age, or dropped over the party cap). There is no one to narrate as, so the protagonist
        // takes over.
        if (_activePartyMember != _protagonist && !_protagonist.CompanionParty.Contains(_activePartyMember))
        {
            Console.WriteLine($"NarrativeController: active member '{_activePartyMember.DisplayName}' is no longer in the party — narration returns to the protagonist");
            _activePartyMember = _protagonist;
        }

        if (refillNoetic)
        {
            _memberNoeticPoints.Clear(); // New node — everyone starts with a fresh counter
            // ResetForNewNode refilled the counter already, but from whoever was active when the
            // segment closed; restate it from the member who will actually act, since their maxima
            // differ. A segment closed by a FAILED action refills nothing: the counter (and every
            // companion's saved counter) carries straight over.
            _narrationState.ThinkingAttemptsRemaining = _activePartyMember.MaxNoeticPoints;
        }
        NoteWhereWeStand();

        // Re-apply the current time period so this segment's nodes get their NPCs (re)placed and
        // their state-dependent verbs re-expanded (affinity above all: "introduce myself" is for
        // strangers only, and a dialogue may just have changed that).
        ApplyTimePeriod(_graph.CurrentPeriod);

        // Just set loading state and start generation
        _narrationState.IsLoadingObservations = true;
        _narrationState.LoadingMessage = ObservationLoadingMessage();

        Console.WriteLine($"NarrativeController: Started observation phase (with history preserved)");
        Console.WriteLine($"  History lines: {_scrollBuffer.HistoryLineCount}");
        Console.WriteLine($"  Total lines: {_scrollBuffer.TotalLines}");
        Console.WriteLine($"  Scroll offset: {_scrollBuffer.ScrollOffset}");
        
        // Fire-and-forget async task
        _ = GenerateObservationsAsync();
    }

    /// <summary>
    /// Applies a time period to the scene-backed graph: records it on the graph <b>and on the PoV</b>,
    /// repositions the scene's NPCs into the nodes where their schedule places them for that period
    /// (via <see cref="SceneNpcPlacement"/>), then re-expands every observation's verbs against the
    /// now-current state. NPC placement and verb gating therefore always share one period — the
    /// source of the earlier "NPCs at the wrong place / no actions offered" bug was letting them
    /// diverge.
    ///
    /// <para>This is the <b>single writer</b> of the period. <c>NarrationGraph.CurrentPeriod</c> and
    /// <c>PoV.When</c> are two views of one fact — node placement reads the first, every verb's
    /// <c>IsPossible</c> reads the second — so nothing else may set either. A verb that shifts time
    /// returns a <c>TimeShiftOutcome</c>; <see cref="CommitOutcomeResult"/> notices the PoV change
    /// once reports have applied and routes it back through here.</para>
    /// </summary>
    private void ApplyTimePeriod(TimePeriod period)
    {
        _graph.SetCurrentPeriod(period);
        if (_pov != null) _pov.When = period;
        // A place at night plays its night music; the opening phases are not a place.
        _ambianceEngine?.SetNight(_scene?.Phase == NarrationPhase.Exploration && period == TimePeriod.Night);
        _npcPlacement?.PlaceForPeriod(period);
        RefreshSceneVerbs();
    }

    /// <summary>
    /// Re-expands the verb SubOutcomes of every scene-backed observation (NPCs, PoIs + their items,
    /// spots — everything <see cref="IVerbRefreshable"/>; areas keep their static transition verb)
    /// against the current scene state, at the graph's current period. Called on each period change
    /// (through <see cref="ApplyTimePeriod"/>) and before each thinking request, so the offered
    /// goals always reflect the world — and the NPCs actually present — as they now stand.
    /// </summary>
    /// <summary>
    /// The graph node standing for <paramref name="area"/>, matched on area identity.
    ///
    /// <para>This used to re-derive the node's id from the area's display name. That silently picked
    /// the wrong node once a location held two rooms with the same name — which every multi-building
    /// location does, since each building has its own hall and bedrooms — landing the player in
    /// another building's room. Node ids stay human-readable for logs; routing goes by identity.</para>
    /// </summary>
    private NarrationNode? NodeForArea(Cathedral.Game.Scene.Area area)
        => _graph.AllNodes.Values.FirstOrDefault(
            n => n is SyntheticNarrationNode { Area: { } a } && a.Id == area.Id);

    private void RefreshSceneVerbs()
    {
        if (_scene == null) return;

        foreach (var node in _graph.AllNodes.Values)
        {
            if (node is not SyntheticNarrationNode { Area: { } area } synthetic) continue;
            SyncSpawnedObservations(synthetic, area);
            // Gate at the live period. NPCs were placed into this node by SceneNpcPlacement using
            // the same Scene.GetNpcsAt query the verb gates use, so a present NPC's verbs always
            // survive here, and an absent one simply has no observation object to refresh.
            var pov = new PoV(area, _graph.CurrentPeriod);
            foreach (var outcome in node.PossibleOutcomes)
            {
                // Stamp before refreshing: an observation whose text depends on the time (a door
                // saying whether it looks locked) must describe the same period its verbs were gated
                // at, or the player reads "seems open" and is offered UNLOCK.
                (outcome as IPeriodStampable)?.StampPeriod(_graph.CurrentPeriod);
                // Gated against the member who will actually act, not the protagonist: after a
                // Speak-About hand-off that is a companion, and a beast companion must not be offered
                // the verbs its body cannot perform (every dialogue verb, everything needing hands).
                if (outcome is IVerbRefreshable refreshable)
                    refreshable.RefreshVerbs(_scene, pov, _activePartyMember);
            }
        }
    }

    /// <summary>
    /// Reconciles a node's point-of-interest observations with the PoIs its area actually holds: adds
    /// one for a PoI that has none, drops one whose PoI has left the area.
    ///
    /// <para>The narration graph is built once, at scene creation, from the areas as the factory left
    /// them — so anything the game <i>spawns</i> during play has no observation object, and an object
    /// with no observation object cannot be looked at or acted on however correct it is in
    /// <c>area.PointsOfInterest</c>. That is what made a corpse unreachable: the body was in the area
    /// and in the scene's element table, right in every respect except the one that shows it to the
    /// player.</para>
    ///
    /// <para>Verbs are left to the caller's refresh loop, which runs over <c>PossibleOutcomes</c>
    /// straight after this and expands the new object at the live period like any other.</para>
    /// </summary>
    private void SyncSpawnedObservations(SyntheticNarrationNode node, Cathedral.Game.Scene.Area area)
    {
        node.PossibleOutcomes.RemoveAll(
            o => o is SyntheticObservationObject obs && !area.PointsOfInterest.Contains(obs.PointOfInterest));

        var present = node.PossibleOutcomes
            .OfType<SyntheticObservationObject>()
            .Select(o => o.PointOfInterest)
            .ToHashSet();

        foreach (var poi in area.PointsOfInterest)
        {
            if (present.Contains(poi)) continue;

            // Empty verb lists: the refresh pass that follows expands the real ones, for the PoI and
            // for every item in it, at the period actually in force.
            var entry = new SceneViewEntry(poi, new List<VerbAction>());
            var itemEntries = poi.Items
                .Select(ie => new SceneViewEntry(ie, new List<VerbAction>()))
                .ToList();

            node.PossibleOutcomes.Add(new SyntheticObservationObject(poi, entry, itemEntries, area));
            Console.WriteLine($"NarrativeController: '{poi.DisplayName}' added to node '{node.NodeId}'");
        }
    }

    /// <summary>
    /// The footer message for an observation wait. The childhood phase runs the same code, but there
    /// are no surroundings there and the narration says a memory surfaces — so it says so too.
    /// </summary>
    private string ObservationLoadingMessage()
        => _scene?.Phase == NarrationPhase.ChildhoodReminescence
            ? Config.LoadingMessages.Remembering
            : Config.LoadingMessages.GeneratingObservations;

    /// <summary>
    /// Generate observations from selected modiMentis (async).
    /// </summary>
    private async Task GenerateObservationsAsync()
    {
        try
        {
            Console.WriteLine("NarrativeController: Calling ObservationPhaseController...");

            _previewSession.Reset();

            // Deferred commit: the generated block(s) are appended to the buffer only when the player
            // presses CONTINUE on the last preview part (see FinalizePreview). Playing the reveal sound
            // and scrolling ride along at commit time so the reveal matches the button press.
            void CommitObservation(List<NarrationBlock> blocks)
            {
                Console.WriteLine($"NarrativeController: Committing {blocks.Count} observation blocks");
                foreach (var block in blocks)
                {
                    _scrollBuffer.AddBlock(block);
                    _narrationState.AddBlock(block);
                }
                _ambianceEngine?.PlaySoundEffect(SoundEffectType.NarrativeReveal);
                _scrollBuffer.ScrollToBottom();
                _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;
            }

            // If something has decided what this phase opens on — the person a dialogue was just held
            // with, or the one a Meet routine is about — open with a single observation of it (see
            // GenerateFocusedObservationAsync). Consumed once; if it is not observable from here we fall
            // through to the normal phase.
            var focus   = _openingFocus ?? _scene?.PendingOpeningFocus;
            var focusMM = _openingFocusMM;
            if (_scene != null) _scene.PendingOpeningFocus = null;
            bool arrival = _arrivalObservationPending;
            _arrivalObservationPending = false;
            _openingFocus   = null;
            _openingFocusMM = null;

            // Arrival-first: somebody who heard a failed action and has just walked in opens the
            // phase. Ahead of the corpse opener — both are one-shot events, but an arrival is the
            // newest of them and the one that has just changed what the player may safely do next
            // (they are a Visual presence now, so the exit is a RUNAWAY roll). Drained whether or not
            // it is used, like the corpse list, so a stale arrival cannot open a phase two moves on.
            var arrivals = _scene?.PendingArrivalObservations.ToList() ?? new List<SceneNpc>();
            _scene?.PendingArrivalObservations.Clear();

            bool handled = arrivals.Count > 0
                        && await TryGenerateArrivalObservationAsync(arrivals, CommitObservation);

            // Corpse-next: bodies made since the last phase. Ahead of the threat opener because a
            // corpse is a one-shot event consumed right here, while an enemy still standing will lead
            // the next phase — and the one after — anyway. The list is drained whether or not it is
            // used, so a body left in another area cannot open a phase two moves later.
            var corpses = _scene?.PendingCorpseObservations.ToList()
                          ?? new List<Cathedral.Game.Npc.Corpse.CorpsePointOfInterest>();
            _scene?.PendingCorpseObservations.Clear();

            if (!handled)
                handled = corpses.Count > 0
                       && await TryGenerateCorpseObservationAsync(corpses, CommitObservation);

            // Threat-first: a same-area (visual) enemy opens the phase with a forced, caution-flavoured
            // observation of that enemy — the same condition that turns the exit button into RUNAWAY.
            // This takes precedence over the opening focus.
            if (!handled)
                handled = await TryGenerateThreatObservationAsync(CommitObservation);

            // Otherwise, if something was put in focus, open with a single observation of it.
            if (!handled)
                handled = focus != null
                    && await TryGenerateFocusObservationAsync(focus, focusMM, CommitObservation);

            if (!handled)
            {
                // Open on an area introduction when the area or the time of day changed since the
                // last one. Exploration only: childhood reminescence and get-up have no real area.
                TimePeriod? introPeriod = null;
                if ((_scene?.Phase is null or NarrationPhase.Exploration)
                    && _currentNode is Cathedral.Game.Scene.SyntheticNarrationNode { Area: { } introArea }
                    && (!ReferenceEquals(introArea, _introducedArea) || _introducedPeriod != _graph.CurrentPeriod))
                {
                    introPeriod       = _graph.CurrentPeriod;
                    _introducedArea   = introArea;
                    _introducedPeriod = _graph.CurrentPeriod;
                }

                // Generate ONE overall observation (one sentence per sampled outcome), streamed into the box.
                await _observationController.ExecuteObservationPhaseAsync(
                    _currentNode,
                    _activePartyMember,
                    _protagonist.CurrentLocationId,
                    isReminescence: _scene?.Phase == NarrationPhase.ChildhoodReminescence,
                    ledger: _observationLedger,
                    preview: _previewSession,
                    commit: CommitObservation,
                    introPeriod: introPeriod,
                    ensurePersonKeyword: arrival
                );
            }

            // Generation is done; the box now waits for CONTINUE. Clearing the loading flag lets the
            // CLI settle (idle) so a test can drive the CONTINUE clicks.
            _narrationState.IsLoadingObservations = false;
            _narrationState.ErrorMessage = null;

            Console.WriteLine("NarrativeController: Observation phase complete");
        }
        catch (Exception ex)
        {
            _previewSession.Reset();
            _narrationState.IsLoadingObservations = false;
            _narrationState.ErrorMessage = ReportPhaseFailure("generating observations", ex);
        }
    }

    /// <summary>
    /// The one way a narration phase reports that it could not finish: writes a full crash report,
    /// preserves the run's log under a name the next launch will not overwrite, and returns the text
    /// to show the player.
    ///
    /// <para><b>The phase stays dead on purpose.</b> There is no retry and no way to continue —
    /// <see cref="Render"/> draws this message and nothing else, leaving Escape (the pause menu, and
    /// so the way out) as the only input. A run that could be resumed is a run the player will
    /// resume, and then <c>log.txt</c> grows for another hour, the failure ends up buried in the
    /// middle of it, and the next launch truncates it entirely. Stopping is what produces a report
    /// worth reading.</para>
    ///
    /// <para>The message names the preserved file, because a bug report without it costs a round
    /// trip and usually the log as well.</para>
    /// </summary>
    private string ReportPhaseFailure(string what, Exception ex)
    {
        string? crashLog = CrashReport.Capture(what, ex);

        // Ordered by what the player needs, because ShowError truncates at the separator line and a
        // long exception message would otherwise push the one actionable sentence off the screen.
        var message = $"Something went wrong while {what}. This is a bug, not something you did.";
        if (crashLog != null)
            message += $"\n\nPlease send '{crashLog}' from the game's folder to the developer. It holds everything needed to diagnose this.";
        else if (GameLog.IsOpen)
            // The copy failed, or this is the fourth failure this run. The live log still has every
            // report in it — it just will not survive the next launch, so say so.
            message += $"\n\nPlease send '{GameLog.FileName}' from the game's folder to the developer BEFORE starting the game again — the next launch replaces it.";
        else
            message += "\n\nThe game could not write its log (is its folder read-only?), so there is nothing to send. Please describe what you were doing when this happened.";
        message += "\n\nPress Escape for the menu.";
        message += $"\n\n{ex.GetType().Name}: {ex.Message}";
        return message;
    }

    /// <summary>
    /// Runs the under-threat opener when a same-area (visual) enemy is present: resolves the threat to
    /// an observable object in the current node and, if found, leads the phase with a single
    /// caution-flavoured observation of it via
    /// <see cref="ObservationPhaseController.GenerateThreatObservationAsync"/>. Returns false — so the
    /// caller falls through to post-dialogue / normal observation — when there is no visual threat, it
    /// has no observation object, or nothing was produced. The threat condition mirrors
    /// <see cref="ComputeExitContext"/> (the RUNAWAY trigger).
    /// </summary>
    private async Task<bool> TryGenerateThreatObservationAsync(Action<List<NarrationBlock>> commit)
    {
        if (_scene == null || _pov == null || _protagonist == null) return false;

        var threat = ThreatSelector.ComputeContext(_scene, _pov, _protagonist);
        if (threat.Level != ThreatLevel.Visual || threat.Threat == null) return false;

        var threatOutcome = _currentNode.GetAllDirectConcreteOutcomes()
            .OfType<SyntheticNpcObservationObject>()
            .FirstOrDefault(o => ReferenceEquals(o.NpcEntity, threat.Threat));
        if (threatOutcome == null)
        {
            Console.WriteLine($"NarrativeController: Visual threat '{threat.Threat.DisplayName}' has no observation object — normal observation.");
            return false;
        }

        var blocks = await _observationController.GenerateThreatObservationAsync(
            threatOutcome, _protagonist.CurrentLocationId, _activePartyMember,
            ledger: _observationLedger, preview: _previewSession, commit: commit);
        return blocks.Count > 0;
    }

    /// <summary>
    /// Runs the arrival opener: somebody heard a failed action from the next room and has walked in,
    /// so the phase opens on them and nothing else. Reuses the threat opener's caution-flavoured
    /// generation — the feeling is the same one, and an arrival is very often a threat by the time it
    /// finishes crossing the room.
    ///
    /// <para>Only arrivals still standing in the current area are narrated: the player may have moved
    /// on between the failure and this phase, and announcing a person who came to a room nobody is in
    /// would read as somebody materialising. Returns false when nothing was left to narrate, so the
    /// caller falls through to the corpse / threat / normal openers.</para>
    /// </summary>
    private async Task<bool> TryGenerateArrivalObservationAsync(
        List<SceneNpc> arrivals, Action<List<NarrationBlock>> commit)
    {
        if (_scene == null || _pov == null || _protagonist == null) return false;

        foreach (var arrival in arrivals)
        {
            if (!arrival.IsAlive) continue;
            if (_scene.GetAreaOf(arrival, _pov.When)?.Id != _pov.Where.Id) continue;

            var outcome = _currentNode.GetAllDirectConcreteOutcomes()
                .OfType<SyntheticNpcObservationObject>()
                .FirstOrDefault(o => ReferenceEquals(o.NpcEntity, arrival.Entity));
            if (outcome == null)
            {
                Console.WriteLine($"NarrativeController: arriving '{arrival.Entity.DisplayName}' has no observation object — normal observation.");
                continue;
            }

            // An enemy came looking for their quarry; a witness came to see what the noise was. Only
            // the first is a threat, and saying otherwise told the player a curious reaper meant harm.
            bool isEnemy = arrival.Entity is Cathedral.Game.Npc.NpcEntity npc
                           && npc.AffinityTable.IsEnemy(_protagonist.AffinityKey);
            Console.WriteLine($"NarrativeController: arrival of '{arrival.Entity.DisplayName}' ({(isEnemy ? "enemy" : "witness")}) opens the phase");
            var blocks = await _observationController.GenerateThreatObservationAsync(
                outcome, _protagonist.CurrentLocationId, _activePartyMember,
                ledger: _observationLedger, preview: _previewSession, commit: commit,
                trailingNeutral: isEnemy ? NeutralNarration.ArrivalEnemyCaution()
                                         : NeutralNarration.ArrivalWitnessNotice());
            if (blocks.Count > 0) return true;
        }

        return false;
    }

    /// <summary>
    /// Runs the corpse opener: resolves each spawned corpse to its observation object in the current
    /// node and, for those still here, opens the phase by observing them and only them (see
    /// <see cref="ObservationPhaseController.GenerateCorpseObservationAsync"/>). Returns false — so the
    /// caller falls through to the threat / post-dialogue / normal openers — when none of the bodies is
    /// in this node, which is the case whenever the kill happened somewhere the player has since left.
    /// </summary>
    private async Task<bool> TryGenerateCorpseObservationAsync(
        List<Cathedral.Game.Npc.Corpse.CorpsePointOfInterest> corpses, Action<List<NarrationBlock>> commit)
    {
        var nodeOutcomes = _currentNode.GetAllDirectConcreteOutcomes()
            .OfType<SyntheticObservationObject>()
            .ToList();

        // Node order is irrelevant here — the bodies are narrated in the order they fell.
        var corpseOutcomes = corpses
            .Select(c => nodeOutcomes.FirstOrDefault(o => ReferenceEquals(o.PointOfInterest, c)))
            .Where(o => o != null)
            .Select(o => (NarrativeAnchor)o!)
            .ToList();

        if (corpseOutcomes.Count == 0)
        {
            Console.WriteLine("NarrativeController: no spawned corpse is observable in this node — normal observation.");
            return false;
        }

        var blocks = await _observationController.GenerateCorpseObservationAsync(
            corpseOutcomes, _protagonist.CurrentLocationId, _activePartyMember,
            ledger: _observationLedger, preview: _previewSession, commit: commit);
        return blocks.Count > 0;
    }

    /// <summary>
    /// Opens the phase on <paramref name="focus"/> when it can: finds the observation object that is
    /// <i>of</i> it in the current node and generates a single observation of that via
    /// <see cref="ObservationPhaseController.GenerateFocusedObservationAsync"/> (which reuses
    /// <paramref name="originMM"/> when still learned, else picks one). Returns false — so the caller
    /// runs the normal phase — when it is not observable from here or nothing was produced.
    /// </summary>
    private async Task<bool> TryGenerateFocusObservationAsync(
        Element focus, ModusMentis? originMM, Action<List<NarrationBlock>> commit)
    {
        var anchor = _currentNode.GetAllDirectConcreteOutcomes().FirstOrDefault(o => IsObservationOf(o, focus));
        if (anchor == null)
        {
            Console.WriteLine($"NarrativeController: focus '{focus.DisplayName}' is not observable here — normal observation.");
            return false;
        }

        var blocks = await _observationController.GenerateFocusedObservationAsync(
            anchor, originMM, _protagonist.CurrentLocationId, _activePartyMember,
            ledger: _observationLedger, preview: _previewSession, commit: commit);
        if (blocks.Count > 0)
            _lastOpeningFocus = focus switch
            {
                SceneNpc npc        => $"opening focus=npc id=\"{npc.Entity.PersistentId}\"",
                PointOfInterest poi => $"opening focus=poi name=\"{poi.DisplayName}\"",
                Area area           => $"opening focus=area name=\"{area.DisplayName}\"",
                _                   => $"opening focus=other name=\"{focus.DisplayName}\"",
            };
        return blocks.Count > 0;
    }

    // What the last phase opened on by an opening focus, for `inspect opening` — the one way a script
    // can tell a Meet routine (or a finished conversation) opened on its person: the prose is
    // placeholder under --playground, and a person's name is generated content.
    private string? _lastOpeningFocus;

    /// <summary>
    /// Whether <paramref name="anchor"/> is the observation of <paramref name="focus"/>. The two kinds
    /// of thing a phase can be opened on today; anything that gets an observation object of its own can
    /// be added here and focused the same way.
    /// </summary>
    private static bool IsObservationOf(NarrativeAnchor anchor, Element focus) => focus switch
    {
        SceneNpc npc        => anchor is SyntheticNpcObservationObject o && ReferenceEquals(o.SceneNpc, npc),
        PointOfInterest poi => anchor is SyntheticObservationObject o && ReferenceEquals(o.PointOfInterest, poi),
        Area area           => anchor is SyntheticAreaObservationObject o && ReferenceEquals(o.Area, area),
        _                   => false,
    };

    /// <summary>The scene's own element for a person, which is what a focus holds.</summary>
    private SceneNpc? SceneNpcOf(NpcEntity npc)
        => _scene?.Npcs.FirstOrDefault(n => ReferenceEquals(n.Entity, npc));

    /// <summary>
    /// Records a pending dialogue and captures the continuity context for the observation phase that will
    /// follow it: the NPC being talked to, and the observation modus mentis that originated this chain of
    /// thought (traced back through <paramref name="chainOrigin"/>; null for dialogues that did not come
    /// from an observation→thinking→action chain, e.g. a caught-red-handed confrontation).
    /// </summary>
    /// <summary>The tree id of the last conversation queued — `inspect dialogue`, for scripts.</summary>
    private string _lastDialogueTreeId = "-";

    private void SetPendingDialogue(DialogueTriggerOutcome outcome, ModusMentisChainElement? chainOrigin)
    {
        _pendingDialogueOutcome = outcome;
        _lastDialogueTreeId     = (outcome.Tree?.TreeId ?? outcome.TreeId ?? "-")
                                + (CaughtRedHandedTreeFactory.IsCameForward(outcome.Tree) ? " came_forward" : "");
        _openingFocus           = SceneNpcOf(outcome.Target);
        _openingFocusMM         = TraceObservationModusMentis(chainOrigin);
    }

    /// <summary>
    /// Walks the modus-mentis chain back to its observation root and returns that observation's modus
    /// mentis, or null if the chain has no observation origin.
    /// </summary>
    private static ModusMentis? TraceObservationModusMentis(ModusMentisChainElement? element)
    {
        for (var current = element; current != null; current = current.ChainOrigin)
            if (current is NarrationBlock { Type: NarrationBlockType.Observation } observation)
                return observation.ModusMentis;
        return null;
    }

    /// <summary>
    /// The context the coded action rules judge <paramref name="action"/> in on the click
    /// (<c>ActionRulesChecker.Check</c>): witnesses (when the act is illegal), threat, and a confronting
    /// witness. Reads the scene and draws nothing.
    /// </summary>
    private Narrative.Rules.ActionRuleContext BuildActionRuleContext(ParsedNarrativeAction action)
    {
        // Determine if the action is illegal so we know whether to compute witness context.
        // Contextual: the verb, its target and who counts the actor an enemy all speak to it.
        bool isIllegalAction = _scene != null && _pov != null
            && action.Verb.IsIllegal(_scene, _pov, action.PreselectedOutcome?.Target, _activePartyMember);

        // Compute witness context (visual = same area, audio = adjacent area).
        var witnessContext = (isIllegalAction && _scene != null && _pov != null)
            ? WitnessSelector.ComputeContext(_scene, _pov)
            : WitnessContext.None;

        // Compute threat context (enemy proximity) — always, not just for illegal actions.
        var threatContext = (_scene != null && _pov != null && _protagonist != null)
            ? ThreatSelector.ComputeContext(_scene, _pov, _protagonist)
            : ThreatContext.None;

        // A witness confronting you: a drawn-in witness who came for you, or anyone in sight on
        // private ground. An enemy in the room outranks them, as everywhere else.
        var confrontation = (_scene != null && _pov != null)
            ? ConfrontationSelector.Current(_scene, _pov, _protagonist)
            : null;
        var confronter = confrontation is { IsWitness: true } ? confrontation.Npc : null;

        return new Narrative.Rules.ActionRuleContext(
            action, _activePartyMember, _scene, _pov, witnessContext, threatContext)
        {
            Confronter = confronter,
        };
    }


    /// <summary>
    /// The action rules asked ahead of the click, for Reconsider: the refusal message the click would
    /// give, or null when the act would pass. The rules that read a combined item are not asked
    /// (<c>ActionRulesChecker.Probe</c>).
    /// </summary>
    private string? ProbeActionRules(ParsedNarrativeAction action)
    {
        var result = Narrative.Rules.ActionRulesChecker.Probe(BuildActionRuleContext(action));
        return result.Passed ? null : (result.ErrorMessage ?? "");
    }

    /// <summary>
    /// Whether an act on screen may be reconsidered: a noetic point to spend, a thinking modus mentis
    /// to do it, and not in the childhood reminescence or the get-up phase, whose one act has one means.
    /// </summary>
    private bool CanReconsider()
        => _narrationState.ThinkingAttemptsRemaining > 0
           && _scene?.Phase != NarrationPhase.ChildhoodReminescence
           && _scene?.Phase != NarrationPhase.GetUp
           && _activePartyMember.GetThinkingModiMentis().Count > 0;

    /// <summary>
    /// Reconsider (<see cref="ThinkingExecutor.ReconsiderAsync"/>): <paramref name="thinkingModusMentis"/>
    /// thinks over an act already offered, and when it keeps it, gives it to another means able to carry it
    /// out on the click. The new act is a thinking block of its own, and the old button greys out
    /// (<see cref="ParsedNarrativeAction.Superseded"/>); when the mind wants no part of it, or no other
    /// means could, only the thought is added and the old button stands. One noetic point either way.
    /// </summary>
    private async Task ExecuteReconsiderAsync(ModusMentis thinkingModusMentis, ParsedNarrativeAction previous)
    {
        try
        {
            Console.WriteLine($"NarrativeController: Reconsidering '{previous.ActionText}' with {thinkingModusMentis.DisplayName}");
            _previewSession.Reset();

            var response = await _thinkingExecutor.ReconsiderAsync(
                thinkingModusMentis, previous, _currentNode, _activePartyMember.GetActionModiMentis(),
                _worldContext, _locationId, _activePartyMember, _scene, _pov, ProbeActionRules,
                _previewSession, CancellationToken.None);

            var action = response.Actions.FirstOrDefault();
            var origin = previous.ChainOrigin?.ChainOrigin;
            var blocks = response.Segments.Select(seg => new NarrationBlock(
                Type: NarrationBlockType.Thinking,
                ModusMentis: seg.ModusMentis,
                Text: seg.Text,
                Keywords: null,
                Actions: action != null && ReferenceEquals(seg, response.Segments[^1]) ? response.Actions : null,
                ChainOrigin: origin)).ToList();
            if (action != null) action.ChainOrigin = blocks[^1];

            void CommitReconsider()
            {
                if (action != null)
                {
                    previous.Superseded = true;
                }
                foreach (var block in blocks)
                {
                    _scrollBuffer.AddBlock(block);
                    _narrationState.AddBlock(block);
                }
                _ambianceEngine?.PlaySoundEffect(SoundEffectType.NarrativeReveal);
                _scrollBuffer.ScrollToBottom();
                _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;
            }
            if (response.PreviewLastPart is { } lastPart)
            {
                lastPart.AttachCommit(CommitReconsider);
                lastPart.MarkComplete();
                _previewSession.EndProduction();
            }
            else
            {
                CommitReconsider();
                _previewSession.Reset();
            }

            _narrationState.IsLoadingThinking = false;
            _narrationState.ThinkingAttemptsRemaining--;
            _narrationState.ErrorMessage = null;
            Console.WriteLine(action != null
                ? $"NarrativeController: Reconsidered — '{action.ActionText}' by {action.ActionModusMentis?.DisplayName} replaces it ({_narrationState.ThinkingAttemptsRemaining} attempts remaining)"
                : $"NarrativeController: Reconsidered — the act stands as it was ({_narrationState.ThinkingAttemptsRemaining} attempts remaining)");
            if (DebugMode.IsActive && action != null)
                DebugMode.PrintAvailableActions(response.Actions);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"NarrativeController: Error during reconsider: {ex.Message}");
            _previewSession.Reset();
            _narrationState.IsLoadingThinking = false;
            _narrationState.ErrorMessage = ReportPhaseFailure("thinking", ex);
        }
    }

    /// <summary>
    /// Execute thinking phase for the clicked keyword occurrence (async). Takes the region rather
    /// than the word: the region is what knows which occurrence was clicked, and therefore which
    /// object it acts on when two sentences of a block use the same word.
    /// </summary>
    private async Task ExecuteThinkingPhaseAsync(ModusMentis thinkingModusMentis, KeywordRegion keywordRegion)
    {
        string keyword = keywordRegion.Keyword;
        // Source observation block, for modusMentis chain tracking
        var sourceObservationBlock = keywordRegion.SourceBlock;

        try
        {
            Console.WriteLine($"NarrativeController: Executing thinking with {thinkingModusMentis.DisplayName} on keyword '{keyword}'");

            _previewSession.Reset();

            var targetOutcome = keywordRegion.ResolvedAnchor;
            if (targetOutcome == null)
            {
                throw new Exception($"No outcome found for keyword '{keyword}'");
            }

            // Verb gates read mutable world state (affinity, item presence, …) — re-expand every
            // scene verb list right before the goal choice so it can only offer what is possible NOW.
            RefreshSceneVerbs();

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
                // The scene and PoV are what the coded choice rules judge from: they decide whether a
                // goal on offer is a crime, which decides what this mind is shown.
                _scene,
                _pov,
                isReminescence: _scene?.Phase == NarrationPhase.ChildhoodReminescence,
                autoSuccess: _scene?.Phase == NarrationPhase.ChildhoodReminescence
                             || _scene?.Phase == NarrationPhase.GetUp,
                preview: _previewSession,
                proposed: _proposalLedger,
                cancellationToken: CancellationToken.None);

            if (response == null)
            {
                throw new Exception("Thinking LLM returned null response");
            }

            bool hasActions = response.Actions.Count > 0;
            Console.WriteLine(hasActions
                ? $"NarrativeController: Generated {response.Actions.Count} actions"
                : "NarrativeController: Thinking chose to ignore — no action generated");

            // One block per segment, in order: the thinking modus mentis's thoughts as thinking blocks
            // (ChainOrigin pointing to the observation block that held the clicked keyword), and each
            // refusal as an outcome block in the voice that refused. Without iterative thinking that is
            // one thought, or a thought and its refusal. The actions belong to the last thought.
            var segments = response.Segments.Count > 0
                ? response.Segments
                : new List<ThinkingSegment> { new(thinkingModusMentis, response.ReasoningText, IsThought: true) };
            var lastThought = segments.LastOrDefault(seg => seg.IsThought);
            var blocks = new List<NarrationBlock>();
            NarrationBlock? thinkingBlock = null;
            foreach (var seg in segments)
            {
                var block = seg.IsThought
                    ? new NarrationBlock(
                        Type: NarrationBlockType.Thinking,
                        ModusMentis: seg.ModusMentis,
                        Text: seg.Text,
                        Keywords: null,
                        Actions: hasActions && ReferenceEquals(seg, lastThought) ? response.Actions : null,
                        ChainOrigin: sourceObservationBlock)
                    : new NarrationBlock(
                        Type: NarrationBlockType.Outcome,
                        ModusMentis: seg.ModusMentis,
                        Text: seg.Text,
                        Keywords: null,
                        Actions: null);
                if (ReferenceEquals(seg, lastThought)) thinkingBlock = block;
                blocks.Add(block);
            }

            // Set ChainOrigin for each action to point to the thinking block that carries it
            foreach (var action in response.Actions)
            {
                action.ChainOrigin = thinkingBlock;
            }

            // Difficulty is now computed inside ThinkingExecutor from the persona-fit answer
            // (verb base ± eager/willing/unsure modifier), so each action already carries its
            // DifficultyLevel. Auto-success phases (reminescence / get-up) carry difficulty 0 (○ glyph).

            // A refusal (persona-fit, or a coded rule asked ahead) is an outcome block and offers no
            // button. The noetic point is still consumed below via the normal thinking-complete
            // decrement: one point for the whole chain of retries.
            if (!hasActions && response.RefusalText != null)
                Console.WriteLine("NarrativeController: VerbAction refused — refusal narrated, no button.");
            if (segments.Count > 2)
                Console.WriteLine($"NarrativeController: iterative thinking produced {segments.Count} blocks.");

            // Deferred commit: the thinking block (and refusal, and its action button) become visible
            // only when the player presses CONTINUE on the last preview part. Reveal sound + scroll ride
            // along at commit time so they match the button press.
            void CommitThinking()
            {
                // An action is "proposed" the moment its button is on screen, which is here — so this
                // is where the phase records it and stops offering it again. A goal the action modus
                // mentis refused reaches no button and is deliberately not recorded: the refusal was
                // that mind's, and asking again with another one is the move that should work.
                foreach (var proposedAction in response.Actions)
                    if (proposedAction.PreselectedOutcome is VerbAction proposedVerb)
                        _proposalLedger.Propose(proposedVerb);

                foreach (var block in blocks)
                {
                    _scrollBuffer.AddBlock(block);
                    _narrationState.AddBlock(block);
                }
                _ambianceEngine?.PlaySoundEffect(SoundEffectType.NarrativeReveal);
                _scrollBuffer.ScrollToBottom();
                _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;
            }

            // Attach the commit and complete the last part only after it is attached (race-free), or
            // commit immediately when previewing produced no part.
            if (response.PreviewLastPart is { } lastPart)
            {
                lastPart.AttachCommit(CommitThinking);
                lastPart.MarkComplete();
                _previewSession.EndProduction();
            }
            else
            {
                CommitThinking();
                _previewSession.Reset();
            }

            // Update state at generation end (not deferred): the noetic point is spent now, and the
            // loading flag clears so the CLI settles for the CONTINUE clicks.
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
            _previewSession.Reset();
            _narrationState.IsLoadingThinking = false;
            _narrationState.ErrorMessage = ReportPhaseFailure("thinking", ex);
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
            _cliLastExecutedVerbId = action.PreselectedOutcome?.Verb.VerbId;

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

            // Run all coded rules; a failure here is absolute — no LLM retry, no noetic cost.
            var ruleCtx = BuildActionRuleContext(action);
            var ruleResult = Narrative.Rules.ActionRulesChecker.Check(ruleCtx);
            if (!ruleResult.Passed)
            {
                Console.WriteLine($"NarrativeController: Coded rule blocked action — {ruleResult.ErrorMessage}");
                // A refused act stays lit: Reconsider may still give it to a means the rule lets through
                // (a discreet one past a witness), and a missing tool leaves "Use Tool" as the way on.
                if (ruleResult.NeededTool != null) action.NeedsTool = true;

                // Re-express the refusal in the acting modus mentis's voice when one is resolvable
                // (e.g. caught-red-handed, under threat); fall back to the raw rule message otherwise.
                // The refusal streams into a preview box like every other outcome text.
                var refusalMm = _activePartyMember.ModiMentis
                    .FirstOrDefault(m => m.ModusMentisId == action.ActionModusMentisId)
                    ?? action.ActionModusMentis;
                _previewSession.Reset();
                var refusalPart = refusalMm != null ? _previewSession.BeginPart(PreviewTitles.For(refusalMm)) : null;
                string refusalText;
                if (refusalMm != null)
                {
                    refusalText = await _actionExecutor.OutcomeNarrator.NarrateRefusalAsync(
                        action, refusalMm, ruleResult.ErrorMessage ?? "", _activePartyMember, CancellationToken.None,
                        preview: refusalPart?.Sink, neededTool: ruleResult.NeededTool);
                    if (string.IsNullOrWhiteSpace(refusalText))
                        refusalText = RawRefusal(action, ruleResult);
                }
                else
                {
                    refusalText = RawRefusal(action, ruleResult);
                }

                _narrationState.IsLoadingAction = false;

                var ruleBlock = new NarrationBlock(
                    Type: NarrationBlockType.Outcome,
                    ModusMentis: refusalMm ?? action.ThinkingModusMentis,
                    Text: refusalText,
                    Keywords: null,
                    Actions: null);
                void CommitRuleFailure()
                {
                    _scrollBuffer.AddBlock(ruleBlock);
                    _narrationState.AddBlock(ruleBlock);
                    _scrollBuffer.ScrollToBottom();
                    _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;
                }
                if (refusalPart != null)
                {
                    refusalPart.AttachCommit(CommitRuleFailure);
                    refusalPart.MarkComplete();
                    _previewSession.EndProduction();
                }
                else CommitRuleFailure();

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
                    // Out of points. The CONTINUE that follows closes the segment on a refusal, so
                    // it refills nothing — same rule as a failed roll. LEAVE is the way out.
                    _pendingSegmentSucceeded = false;
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
                ruleCtx.WitnessContext,
                ruleCtx.ThreatContext,
                CancellationToken.None
            );
            evalResult.Confronter = ruleCtx.Confronter;
            
            // Handle plausibility failure
            if (!evalResult.IsPlausible)
            {
                Console.WriteLine($"NarrativeController: VerbAction failed plausibility check");

                // Generate plausibility failure narration, streamed into a preview box.
                _previewSession.Reset();
                var plausPart = evalResult.ActionModusMentis != null
                    ? _previewSession.BeginPart(PreviewTitles.For(evalResult.ActionModusMentis))
                    : null;
                var plausibilityResult = await _actionExecutor.GeneratePlausibilityFailureNarrationAsync(
                    evalResult, CancellationToken.None, preview: plausPart?.Sink);

                _narrationState.IsLoadingAction = false;

                // Add outcome narration block (deferred to the preview CONTINUE).
                var plausibilityBlock = new NarrationBlock(
                    Type: NarrationBlockType.Outcome,
                    ModusMentis: plausibilityResult.ActionModusMentis ?? throw new InvalidOperationException("VerbAction modusMentis cannot be null"),
                    Text: $"[IMPOSSIBLE] {plausibilityResult.Narration}",
                    Keywords: null,
                    Actions: null
                );
                void CommitPlausibility()
                {
                    _scrollBuffer.AddBlock(plausibilityBlock);
                    _narrationState.AddBlock(plausibilityBlock);
                    _scrollBuffer.ScrollToBottom();
                    _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;
                }
                if (plausPart != null)
                {
                    plausPart.AttachCommit(CommitPlausibility);
                    plausPart.MarkComplete();
                    _previewSession.EndProduction();
                }
                else CommitPlausibility();

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
                    // No more noetic points - show continue button and grey out like a normal failure,
                    // which includes not refilling the pool on the CONTINUE that closes the segment.
                    _pendingSegmentSucceeded = false;
                    _narrationState.ShowContinueButton = true;
                    return;
                }
            }
            
            // === PHASE 2: DICE ROLL (dice rolling animation) ===
            Console.WriteLine($"NarrativeController: VerbAction passed plausibility, starting dice roll phase");

            // Number of dice = total modusMentis level summed across the chain
            int numberOfDice = Math.Max(1, action.GetTotalModusMentisLevel(_activePartyMember));

            // Difficulty = number of 6s needed to succeed (1-10, from LLM evaluation)
            int actualDifficulty = evalResult.DifficultyLevel;

            // Roll each die independently (1–6) and count sixes. The forced-outcome branch runs
            // BEFORE the animation starts because a forced success may have to lower the difficulty
            // (see below) and the animation is drawn against it.
            int[] finalDiceValues;
            bool succeeded;
            if (DebugMode.IsActive && !DebugMode.IsAutoStrategy)
            {
                succeeded = DebugMode.GetDiceRollOverride(action.ActionText, numberOfDice, actualDifficulty);

                // A forced success has to be a roll the pool can actually show. A verb harder than
                // the chain that reached it — tame is difficulty 4 off three modi mentis — needs more
                // sixes than there are dice, which no arrangement satisfies: the roll is then simply
                // impossible, and `strategy succeed` asking for it used to hang the game outright
                // (GenerateDiceValuesForResult spinning for a six it had nowhere left to put). Cap the
                // demand at the pool, the way the fight path guarantees a forced success one die.
                if (succeeded && actualDifficulty > numberOfDice)
                {
                    Console.WriteLine($"NarrativeController: forced success needs {actualDifficulty} sixes from " +
                                      $"{numberOfDice} dice — difficulty capped at {numberOfDice} for this roll");
                    actualDifficulty = numberOfDice;
                }

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

            // Start dice roll animation (with humor modifiers for the acting member)
            NarrationDiceStart(numberOfDice, actualDifficulty, _activePartyMember);
            _narrationState.LoadingMessage = Config.LoadingMessages.RollingDice;

            Console.WriteLine($"NarrativeController: Rolled {finalDiceValues.Count(v => v == 6)} sixes out of {numberOfDice} dice (need {actualDifficulty}) → {(succeeded ? "SUCCESS" : "FAILURE")}");

            // Do NOT generate any narration during the animation. Remember the evaluation and the
            // rolled result; humor modifiers may still flip _pendingSucceeded. The actual (single)
            // outcome is generated only when the player presses the dice CONTINUE — see
            // OnDiceRollContinue → GenerateOutcomePreviewAsync — and streams into a preview box.
            // What a failure penalty will need to take hold: the pool as the thinking left it (losing
            // focus needs a point to lose), the hour (wasting it needs one after), and an exit to
            // wander out of — which only exploration has.
            evalResult.Situation = new FailureSituation(
                _activePartyMember, _pov, _narrationState.ThinkingAttemptsRemaining,
                CanLoseTheWay: _scene?.Phase == NarrationPhase.Exploration);

            _pendingEval      = evalResult;
            _pendingSucceeded = succeeded;

            Console.WriteLine($"NarrativeController: Rolled {(succeeded ? "SUCCESS" : "FAILURE")} (humor may change this) — outcome generated on continue");

            // Fixed animation window: no async work backs this roll anymore (the outcome is generated
            // only on Continue), so pause like the runaway/fight rolls do to let the dice animate.
            await Task.Delay(Config.Dice.AnimationDurationMs);

            // Complete the dice roll (stops animation, shows final values and continue button)
            NarrationDiceComplete(finalDiceValues);
            _narrationState.IsLoadingAction = false;

            Console.WriteLine($"NarrativeController: Dice roll complete - {finalDiceValues.Count(v => v == 6)} sixes rolled, difficulty {actualDifficulty}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"NarrativeController: Error during action execution: {ex.Message}");
            _previewSession.Reset();
            _narrationState.IsLoadingAction = false;
            NarrationDiceClear();
            _narrationState.ErrorMessage = ReportPhaseFailure("executing an action", ex);
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

        // Preserve the prior narration as history and start a fresh observation phase, under the
        // fragment that led here — the childhood phase is one long scroll of memories and an
        // unlabelled rule between them says nothing about which one just surfaced.
        BeginNarrationSegment(string.IsNullOrWhiteSpace(req.FragmentName)
            ? null
            : $"after remembering {req.FragmentName}");
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
        _npcPlacement  = new Cathedral.Game.Scene.SceneNpcPlacement(newScene, _graph.AllNodes.Values);

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
        int numberOfDice = Math.Max(1, action.GetTotalModusMentisLevel(_activePartyMember));
        const int getUpDifficulty = 1;

        // Humor modifiers are offered here exactly as in the main action roll: rising from the ground
        // exhausted is precisely the check a spent humor should be able to swing.
        NarrationDiceStart(numberOfDice, getUpDifficulty, _activePartyMember);
        _narrationState.LoadingMessage = Config.LoadingMessages.RollingDice;

        int[] finalDiceValues = new int[numberOfDice];
        for (int i = 0; i < numberOfDice; i++)
            finalDiceValues[i] = _diceRandom.Next(1, 7);
        int sixesCount = finalDiceValues.Count(v => v == 6);
        bool succeeded = sixesCount >= getUpDifficulty;

        Console.WriteLine(
            $"NarrativeController: GetUp dice — {sixesCount}/{numberOfDice} sixes (need {getUpDifficulty}) → {(succeeded ? "SUCCESS" : "FAILURE")}");

        var actionMm = action.ActionModusMentis ?? action.ChainModusMentis;

        // Defer the outcome narration to the dice CONTINUE — like the main action path — so the
        // animation runs for the fixed Config.Dice duration rather than blocking on the LLM. The
        // rolled result is only provisional: a humor modifier may still flip it (OnDiceOutcomeFlipped
        // writes _pendingSucceeded), so the closure reads the field at CONTINUE time, not now.
        _pendingSucceeded    = succeeded;
        _pendingGetUpOutcome = () => GenerateGetUpOutcomeAsync(action, actionMm, _pendingSucceeded, getUpDifficulty);

        // Fixed animation window (no async work backs this roll now that narration is deferred).
        await Task.Delay(Config.Dice.AnimationDurationMs);

        NarrationDiceComplete(finalDiceValues);
        _narrationState.IsLoadingAction = false;

        Console.WriteLine($"NarrativeController: GetUp dice rolled — {(succeeded ? "SUCCESS" : "FAILURE")}, outcome generated on continue");
    }

    /// <summary>
    /// Generates the Get-Up outcome narration after the dice CONTINUE and commits the result.
    /// Mirrors the main action path's deferred generation, preview box included: the text streams
    /// into <see cref="_previewSession"/> and the block is committed on the preview CONTINUE.
    /// <paramref name="succeeded"/> is the final result, humor modifiers already applied.
    /// </summary>
    private async Task GenerateGetUpOutcomeAsync(ParsedNarrativeAction action, ModusMentis actionMm,
        bool succeeded, int getUpDifficulty)
    {
        try
        {
            // This runs after the dice, on the CONTINUE: what is being generated is the result of the
            // attempt, not the attempt itself.
            _narrationState.LoadingMessage = Config.LoadingMessages.NarratingOutcome;

            // Choose the narration hint for the LLM based on success/failure.
            INarratable outcomeForPrompt = succeeded
                ? new InlineNarratable("getting up", "with great effort you push yourself to your feet and continue your travel")
                : new InlineNarratable("the effort", "your exhausted body refuses to rise — you slump back against the tree");

            _previewSession.Reset();
            var part = _previewSession.BeginPart(PreviewTitles.For(actionMm));

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
                    System.Threading.CancellationToken.None,
                    preview: part.Sink);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"NarrativeController: GetUp narration failed — {ex.Message}");
                narration = succeeded
                    ? "With great effort, you force yourself to your feet."
                    : "Your body refuses to cooperate. You slump back against the tree.";
                // The sink never completed — show the fallback text in the box so the player still
                // gets a CONTINUE to press rather than an empty, stuck preview.
                part.Sink.OnComplete(narration);
            }

            // ActualOutcome is always the VerbAction so the GetUpVerb's Success/FailureReports fire.
            var result = new ActionExecutionResult
            {
                Action              = action,
                ActionModusMentis   = actionMm,
                ThinkingModusMentis = action.ThinkingModusMentis ?? actionMm,
                Difficulty          = CriticTrees.DifficultyLevelToScore(getUpDifficulty),
                DifficultyLevel     = getUpDifficulty,
                Succeeded           = succeeded,
                ActualOutcome       = action.PreselectedOutcome != null
                                          ? (INarratable)action.PreselectedOutcome
                                          : new InlineNarratable("get up", "rise"),
                Narration           = narration,
            };

            _narrationState.IsLoadingAction = false;
            part.AttachCommit(() => CommitOutcomeResult(result, deferredCommit: false));
            part.MarkComplete();
            _previewSession.EndProduction();

            Console.WriteLine($"NarrativeController: GetUp outcome generated — {(succeeded ? "pending transition" : "failure, will loop")}, commits on preview continue");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"NarrativeController: GetUp outcome generation failed — {ex.Message}");
            _previewSession.Reset();
            _narrationState.IsLoadingAction = false;
            NarrationDiceClear();
            _narrationState.ErrorMessage = ReportPhaseFailure("resolving the get-up outcome", ex);
        }
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
        if (action.PreselectedOutcome is VerbAction vo)
            target = vo.Target;

        if (target == null)
        {
            Console.Error.WriteLine("NarrativeController: REMEMBER action has no target — aborting");
            return;
        }

        // Collect and apply all verb reports (skills, items, history, transition), then the dice
        // chain's own lesson — REMEMBER rolls nothing, but the modi mentis that observed the
        // fragment, chose to reach for it and voiced the memory did the work all the same, and
        // every other action in the game pays them for it (see CommitOutcomeResult). Appended after
        // the fragment's own grants so a skill the fragment has just taught is already known when
        // its practice report is built.
        // Deliberately uncaught: this used to swallow everything and carry on with an empty report
        // list, showing a memory that succeeded and granted nothing.
        //
        // Today that is merely misleading — every throw on this path happens while the list is being
        // BUILT (SuccessReports rejects an unregistered modus mentis; ItemGrantOutcome's constructor
        // reads DisplayName off its item), so nothing has been applied yet and the world is still
        // consistent. What the catch was hiding was a missing reward, not a corrupt character.
        //
        // The reason to remove it anyway is that the ordering which makes it harmless is an accident
        // nothing enforces. These outcomes are applied one at a time, so the first Apply that throws —
        // a future one validating an anatomy the way WoundRegistry.CanBeSufferedBy does, say — leaves
        // the earlier ones already written and the catch would discard every chip for them. Letting it
        // propagate reaches ExecuteActionPhaseAsync's handler, which writes a crash report and stops
        // the phase. Stopping is what keeps a half-applied character off disk: autosave fires on entry
        // to the world map, and a phase that cannot finish never gets there.
        var reminescenceReports = new System.Collections.Generic.List<Outcome>(
            action.Verb.SuccessReports(_scene, _pov, _protagonist, target));
        foreach (var report in reminescenceReports)
            report.ApplyTo(OutcomeContext.For(_protagonist, _scene, _pov));

        foreach (var chainModusMentis in action.GetModusMentisChain())
        {
            var practice = ModusMentisPracticeOutcome.For(_protagonist, chainModusMentis);
            if (practice == null) continue;
            practice.ApplyTo(OutcomeContext.For(_protagonist, _scene, _pov));
            reminescenceReports.Add(practice);
        }

        System.Collections.Generic.IReadOnlyList<Outcome> reminescenceReportList = reminescenceReports;

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
            ? (INarratable)new InlineNarratable(
                displayName:    fpoi.Fragment.Name,
                naturalLanguage: $"remember: {fpoi.Fragment.OutcomeText}")
            : new InlineNarratable("memory", "remember this childhood moment");

        // Generate outcome narration through the LLM exactly as any other action — streamed into the
        // preview box, with the memory block committed on its CONTINUE. The footer says what is
        // actually being recovered: nothing is attempted here, a childhood memory comes back.
        _narrationState.IsLoadingAction = true;
        _narrationState.LoadingMessage  = Config.LoadingMessages.Remembering;

        _previewSession.Reset();
        var previewPart = _previewSession.BeginPart(PreviewTitles.For(actionMm));

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
                neutralOverride: reminescenceNeutral,
                preview: previewPart.Sink);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"NarrativeController: outcome narration failed — {ex.Message}");
            // Fallback: show the raw concrete memory text.
            narrationText = fpoi != null
                ? fpoi.Fragment.OutcomeText
                : "You remember.";
            // The sink never completed — put the fallback in the box so CONTINUE still appears.
            previewPart.Sink.OnComplete(narrationText);
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

        previewPart.AttachCommit(() =>
        {
            _scrollBuffer.AddBlock(outcomeBlock);
            _narrationState.AddBlock(outcomeBlock);
            // REMEMBER always succeeds (and often grants a skill/item) — cue the positive
            // outcome sting, matching the normal action-resolution path.
            _ambianceEngine?.TriggerGameEvent(GameEventType.PositiveOutcome);
            _scrollBuffer.ScrollToBottom();
            _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;

            _narrationState.PendingTransitionNode = null;
            _narrationState.ShowContinueButton    = true;
        });
        previewPart.MarkComplete();
        _previewSession.EndProduction();

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
            // Ensure at least 'difficulty' sixes — but never ask for more sixes than there are dice.
            // The tail loop below places one six per pass and has nowhere to put the surplus, so an
            // unclamped demand spins forever. Callers clamp too; this is the backstop, because a hang
            // here freezes the whole game with nothing on screen to say why.
            int sixesNeeded = Math.Min(difficulty, numberOfDice);
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
    /// Handle continue button click on the dice-roll screen. Both paths defer to now: the outcome is
    /// generated (streamed into a preview box) and committed on the preview CONTINUE.
    /// </summary>
    private void OnDiceRollContinue()
    {
        // Get-Up path: narration was deferred to now — generate the outcome text (into the preview
        // box) and commit on its CONTINUE, exactly as the main path does. The closure reads the
        // final _pendingSucceeded, so a humor modifier applied during the roll is honoured.
        if (_pendingGetUpOutcome != null)
        {
            var generate = _pendingGetUpOutcome;
            _pendingGetUpOutcome = null;
            NarrationDiceClear();
            _narrationState.IsLoadingAction = true;
            _ = Task.Run(generate);
            return;
        }

        // Main path: generate ONLY the final (possibly humor-modified) outcome now, into a preview box.
        if (_pendingEval != null)
        {
            var eval = _pendingEval;
            _pendingEval = null;
            bool succeeded = _pendingSucceeded;
            NarrationDiceClear();

            // A wait that succeeded asks how long first: the hour waited until is in the narration.
            if (succeeded && eval.Action.Verb is SitAndWaitVerb && _pov != null && _scene != null)
            {
                AskWaitUntil(eval, succeeded);
                return;
            }

            // A backtrack that succeeded asks where to first: the area gone back to is in the narration.
            if (succeeded && eval.Action.Verb is BacktrackVerb && _pov != null && _scene != null)
            {
                AskBacktrackTo(eval, succeeded);
                return;
            }

            _narrationState.IsLoadingAction = true;
            _ = Task.Run(() => GenerateOutcomePreviewAsync(eval, succeeded));
            return;
        }

        Console.WriteLine("NarrativeController: No pending action result for dice roll continue");
        NarrationDiceClear();
    }

    /// <summary>
    /// Asks which period a successful wait lasts until, then plays the passage to it; the outcome is
    /// narrated once the player presses Continue. The choice is <see cref="Scene.WaitUntil"/>, which
    /// <see cref="SitAndWaitVerb"/> reads for its time shift.
    /// </summary>
    private void AskWaitUntil(ActionEvaluationResult eval, bool succeeded)
    {
        var from = _pov!.When;
        // A will's wait was settled by its path: the hour it needs, with nothing to ask.
        if (eval.Action.PreselectedOutcome is { Variant: TimePeriod settled })
        {
            Console.WriteLine($"NarrativeController: a will's wait — until {settled}, not asked");
            WaitChosen(settled);
            return;
        }
        _timeBox.OnChosen = WaitChosen;
        _timeBox.ShowChoice(from, Enumerable.Range(1, 5).Select(i => from.Advance(i)));

        void WaitChosen(TimePeriod chosen)
        {
            var (until, note) = WaitStopsAt(from, chosen);
            if (_scene != null) _scene.WaitUntil = until;
            _passagePlayed = true;
            PlayClickSound();
            Console.WriteLine($"NarrativeController: waiting until {until}" + (until != chosen ? $" (chose {chosen})" : ""));
            ShowTimePassage(from, until, note, then: () =>
            {
                _narrationState.IsLoadingAction = true;
                _ = Task.Run(() => GenerateOutcomePreviewAsync(eval, succeeded));
            });
        }
    }

    /// <summary>
    /// Asks where a successful backtrack goes, on the location map, then narrates the outcome. A will's
    /// backtrack was settled by its path (the action's variant) and asks nothing.
    /// </summary>
    private void AskBacktrackTo(ActionEvaluationResult eval, bool succeeded)
    {
        void Go(Area to)
        {
            if (_scene != null) _scene.BacktrackTo = to;
            Console.WriteLine($"NarrativeController: going back to {to.DisplayName}");
            _narrationState.IsLoadingAction = true;
            _ = Task.Run(() => GenerateOutcomePreviewAsync(eval, succeeded));
        }

        if (eval.Action.PreselectedOutcome is { Variant: Area settled })
        {
            Console.WriteLine($"NarrativeController: a will's backtrack — to {settled.DisplayName}, not asked");
            Go(settled);
            return;
        }

        var reach = AreaVisits.BackReach(_scene!, _pov!.Where, _pov.When);
        if (reach.Count == 0) { Go(_pov.Where); return; }
        _backtrackChoices = reach.Keys.ToHashSet();
        _backtrackHovered = null;
        _backtrackCentred = false;
        _onBacktrackChosen = to =>
        {
            PlayClickSound();
            _backtrackChoices = null;
            _backtrackHovered = null;
            DebugLocationMap.WalkPannable = false;
            Go(to);
        };
    }

    /// <summary>
    /// Where a wait chosen to last until <paramref name="chosen"/> actually ends. A wait through the
    /// night, somewhere the player is trespassing in a house that has somebody to come home to it,
    /// ends at night: that is when the householder comes in and finds them.
    /// </summary>
    private (TimePeriod Until, string? Note) WaitStopsAt(TimePeriod from, TimePeriod chosen)
    {
        int steps   = TimePassageBox.Steps(from, chosen);
        int toNight = TimePassageBox.Steps(from, TimePeriod.Night);
        if (chosen != TimePeriod.Night && toNight > 0 && toNight < steps
            && _scene != null && _pov != null && _scene.Phase == NarrationPhase.Exploration
            && PrivacyModel.IsTrespassing(_scene, _pov.Where)
            && ConfrontationSelector.Householder(_scene, _pov.Where, _protagonist?.AffinityKey) != null)
            return (TimePeriod.Night, "Somebody comes in before the night is out.");
        return (chosen, null);
    }

    /// <summary>Plays a passage of the hours; <paramref name="then"/> runs when its Continue is pressed.</summary>
    private void ShowTimePassage(TimePeriod from, TimePeriod to, string? note = null, Action? then = null)
    {
        _timeBox.ShowPassage(from, to, note);
        _afterTimeBox = then;
    }

    private void OnTimePassageContinue()
    {
        PlayClickSound();
        _timeBox.Hide();
        var then = _afterTimeBox;
        _afterTimeBox = null;
        then?.Invoke();
    }

    /// <summary>
    /// Records a move on the map (<see cref="AreaVisits.Move"/>) and shows it walked: what the move
    /// taught the map is revealed as the walk reaches it.
    /// </summary>
    private void MoveOnMap(Area from, Area to, Element? by, bool back = false)
    {
        if (_scene == null || _pov == null) return;
        var areasBefore = new HashSet<string>(_scene.VisitedAreas);
        var linksBefore = new HashSet<string>(_scene.TakenLinks);
        var hops = back ? AreaVisits.MoveBack(_scene, from, to, _pov.When)
                        : AreaVisits.Move(_scene, from, to, by, _pov.When);
        var fresh = _scene.VisitedAreas.Where(k => !areasBefore.Contains(k))
                    .Concat(_scene.TakenLinks.Where(k => !linksBefore.Contains(k)))
                    .ToHashSet();
        ShowWalk(from, hops, fresh);
    }

    /// <summary>What an observation took in, for the map's seen state (exploration only).</summary>
    private void NoteObserved(NarrativeAnchor observed)
    {
        if (_scene == null || _pov == null || _scene.Phase != NarrationPhase.Exploration) return;
        AreaVisits.See(_scene, _pov.Where, observed);
    }

    /// <summary>
    /// Shows the party walking from <paramref name="from"/> along <paramref name="hops"/> (what
    /// <see cref="AreaVisits.Move"/> recorded) on the location map. Waits for the time box when one is
    /// up, and takes over what its Continue would have resumed.
    /// </summary>
    private void ShowWalk(Area from, IReadOnlyList<(Area To, string? Link)> hops, HashSet<string> fresh)
    {
        if (_scene == null || hops.Count == 0) return;
        var steps = _debugMap.Walk(_scene, from, hops);
        if (steps.Count < 2) return;
        _lastWalk = $"walk from {from.DisplayName} to {hops[^1].To.DisplayName} steps={steps.Count} "
                  + $"via={string.Join(",", hops.Select(h => h.Link == null ? "jump" : "line"))}";
        Console.WriteLine($"NarrativeController: {_lastWalk}");

        if (_timeBox.IsVisible)
        {
            var then = _afterTimeBox;
            _afterTimeBox = () => { StartWalk(steps, hops, fresh); _afterWalk = then; };
            return;
        }
        StartWalk(steps, hops, fresh);
    }

    private void StartWalk(List<DebugLocationMap.WalkStep> steps, IReadOnlyList<(Area To, string? Link)> hops,
                           HashSet<string> fresh)
    {
        _walk = steps;
        _walkHops = hops;
        _walkFresh = fresh;
        DebugLocationMap.WalkPannable = false;
        _walkShown = 0;
        _walkStart = DateTime.UtcNow;
        // A script has nothing to watch: the walk is settled at once, and still waits for Continue.
        _walkStepMs = Cli.CliMode.IsActive && !Record.RecordMode.IsActive ? 0 : Math.Min(WalkStepMs, WalkMaxMs / steps.Count);
        _afterWalk = null;
        _walkContinueRegion = default;
        _walkContinueHovered = false;
        _walkArrivalCentred = false;
    }

    private bool WalkSettled => _walk != null && _walkShown >= _walk.Count - 1;

    /// <summary>
    /// What the walk has not reached yet at the cell shown: the ways ahead of it and the areas it has
    /// not entered. A way is reached once the glyph is on it, an area once it is in its card; anything
    /// else the move recorded (the areas a path runs past) once the walk is over.
    /// </summary>
    private HashSet<string> WalkWithheld()
    {
        if (_walk == null || WalkSettled) return new();
        var step = _walk[_walkShown];
        var held = new HashSet<string>(_walkFresh);
        for (int k = 0; k <= step.Hop && k < _walkHops.Count; k++)
        {
            var (to, link) = _walkHops[k];
            if (link != null) held.Remove(link);
            if (k < step.Hop || step.Here == to) held.Remove(to.StableKey);
        }
        return held;
    }

    /// <summary>Moves the glyph on by the time elapsed, ticking at each card it enters.</summary>
    private void AdvanceWalk()
    {
        if (_walk == null || WalkSettled) return;
        int last = _walk.Count - 1;
        int at = _walkStepMs <= 0 ? last
               : Math.Min(last, (int)((DateTime.UtcNow - _walkStart).TotalMilliseconds / _walkStepMs));
        if (at == _walkShown) return;
        if (_walk[at].Here != _walk[_walkShown].Here)
            _ambianceEngine?.TriggerGameEvent(GameEventType.SmallInteraction);
        _walkShown = at;
    }

    private void OnWalkContinue()
    {
        PlayClickSound();
        _walk = null;
        DebugLocationMap.WalkPannable = false;
        _walkContinueRegion = default;
        _walkContinueHovered = false;
        var then = _afterWalk;
        _afterWalk = null;
        then?.Invoke();
    }

    /// <summary>
    /// Moves the calendar on when the period has wrapped from night into dawn: a day has turned. The
    /// visit's periods and the world's days are otherwise separate clocks (<see cref="GameClock"/>),
    /// and a passage never spans a full day, so one wrap is one day.
    /// </summary>
    private static void TurnTheDayIfWrapped(TimePeriod before, TimePeriod after)
    {
        if ((int)after >= (int)before) return;
        GameClock.Advance(1);
        Console.WriteLine($"NarrativeController: night turned to dawn — day {(int)GameClock.Days}");
    }

    /// <summary>
    /// Generates the single true outcome after the dice, streaming it into the preview box; the block
    /// commit and all side-effects fire when the player presses the preview CONTINUE.
    /// </summary>
    private async Task GenerateOutcomePreviewAsync(ActionEvaluationResult eval, bool succeeded)
    {
        try
        {
            _previewSession.Reset();
            _narrationState.LoadingMessage = Config.LoadingMessages.NarratingOutcome;
            string title = eval.ActionModusMentis != null ? PreviewTitles.For(eval.ActionModusMentis) : "OUTCOME";
            var part = _previewSession.BeginPart(title);
            // Gather the verb's outcome reports up-front so their verbatims can be woven into the
            // narration; the same instances are reused at commit time (see CommitOutcomeResult).
            var verbReports = GatherVerbReports(eval.Action.PreselectedOutcome, succeeded, eval.Action.CombinedItem);
            var result = await _actionExecutor.PrepareSingleOutcomeAsync(eval, succeeded, verbReports, part.Sink, CancellationToken.None);
            _narrationState.IsLoadingAction = false;
            part.AttachCommit(() => CommitOutcomeResult(result, deferredCommit: true));
            part.MarkComplete();

            // The emotional coda. Resolved from the reports that are ABOUT to be applied rather than
            // from applied ones, because the whole point of the preview session is that a commit
            // happens on the player's press, not here — so waiting for the apply would mean waiting
            // for a click the emotion's own generation is supposed to be running ahead of.
            //
            // A second part rather than a longer first one: the outcome was written by the ACTION
            // modus mentis and this is written by the EMOTION one, so it is a different persona in a
            // different slot, and the preview box already gates one part per CONTINUE. That is where
            // "own block, own press" comes from — nothing here schedules it.
            await GenerateEmotionPreviewAsync(result.OutcomeReports ?? verbReports, result.LlmDecidedReports);

            _previewSession.EndProduction();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"NarrativeController: Outcome generation failed: {ex.Message}");
            _previewSession.Reset();
            _narrationState.IsLoadingAction = false;
            NarrationDiceClear();
            _narrationState.ErrorMessage = ReportPhaseFailure("resolving an outcome", ex);
        }
    }

    /// <summary>
    /// Streams the emotional coda into a preview part of its own, when what the action produced
    /// stirred anything in the body that produced it.
    ///
    /// <para><b>Silent by default and by design.</b> Most actions move nothing a disposition cares
    /// about, and <see cref="EmotionResolver.Resolve"/> returns null for those — no part is begun, no
    /// request is made, and the player presses CONTINUE once as they always did. The extra press is
    /// bought only where there is something to read.</para>
    ///
    /// <para>The commit does the two things in the order the player sees them: the humors reach the
    /// spleen (<see cref="EmotionOutcome"/>'s own Apply, through <c>ApplyTo</c> like every other
    /// consequence) and the block lands carrying its chip. It is attached rather than run here for the
    /// same reason the outcome's is — a press must be able to arrive before, during or after
    /// generation without the state changing twice.</para>
    ///
    /// <para>A failure here costs the coda and nothing else. An emotion is a garnish on an action that
    /// has already resolved, and taking the phase down over one would turn a decorative request into a
    /// third way for a visit to end — so this is the one narration path in the controller that
    /// swallows its exception instead of calling <c>ReportPhaseFailure</c>.</para>
    /// </summary>
    private async Task GenerateEmotionPreviewAsync(
        IReadOnlyList<Outcome>? verbReports,
        IReadOnlyList<Outcome>? llmDecidedReports)
    {
        var all = new List<Outcome>();
        if (verbReports       != null) all.AddRange(verbReports);
        if (llmDecidedReports != null) all.AddRange(llmDecidedReports);

        var felt = EmotionResolver.Resolve(_activePartyMember, all);
        if (felt == null) return;
        var emotion = felt.Value;

        try
        {
            _narrationState.IsLoadingAction = true;
            _narrationState.LoadingMessage  = Config.LoadingMessages.NarratingOutcome;
            var part = _previewSession.BeginPart(PreviewTitles.For(emotion.ModusMentis));
            string text = await _actionExecutor.OutcomeNarrator.NarrateEmotionAsync(
                emotion, CancellationToken.None, part.Sink);
            _narrationState.IsLoadingAction = false;

            var chip = emotion.ToOutcome();
            part.AttachCommit(() =>
            {
                chip.ApplyTo(OutcomeContext.For(_activePartyMember, _scene, _pov));
                var block = new NarrationBlock(
                    Type:        NarrationBlockType.Emotion,
                    ModusMentis: emotion.ModusMentis,
                    Text:        text,
                    Keywords:    null,
                    Actions:     null,
                    OutcomeReports: new List<Outcome> { chip }
                );
                _scrollBuffer.AddBlock(block);
                _narrationState.AddBlock(block);
                _scrollBuffer.ScrollToBottom();
                _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;
                Console.WriteLine($"NarrativeController: emotion — {emotion.ModusMentis.ModusMentisId} felt "
                                + $"{chip.Humor.Name} x{emotion.Count} (spleen)");
            });
            part.MarkComplete();
        }
        catch (Exception ex)
        {
            _narrationState.IsLoadingAction = false;
            Console.Error.WriteLine($"NarrativeController: emotion narration failed, skipping the coda: {ex.Message}");
        }
    }

    /// <summary>
    /// Gathers the verb-specific outcome reports for a resolved action. Success reports carry the
    /// chosen <see cref="VerbAction"/> (so verbs that expanded into several actions read their variant)
    /// and the implement the player combined, if any — which only <c>attack</c> reads, since which
    /// weapon is in the hand decides which blow can be thrown. Failure reports use the target-only
    /// overload. Returns an empty list for non-verb outcomes.
    /// </summary>
    private System.Collections.Generic.IReadOnlyList<Outcome> GatherVerbReports(INarratable? outcome, bool succeeded,
                                                                                Item? tool = null)
    {
        if (outcome is VerbAction verbTarget && _scene != null && _pov != null && verbTarget.Target != null)
        {
            var verb = verbTarget.Verb;
            if (!succeeded)
                return verb.FailureReports(_scene, _pov, _activePartyMember, verbTarget.Target);

            var reports = new System.Collections.Generic.List<Outcome>(
                verb.SuccessReports(_scene, _pov, _activePartyMember, verbTarget.Target, verbTarget, tool));

            // Doing a thing is how the thing is learned. Appended last so the lesson reads as the
            // consequence of whatever the verb actually did, not as its headline.
            var lesson = ModusMentisGrantOutcome.For(
                _activePartyMember, ChooseLesson(verb, verbTarget.Target)?.ModusMentisId);
            if (lesson != null) reports.Add(lesson);

            return reports;
        }
        return System.Array.Empty<Outcome>();
    }


    /// <summary>
    /// The one lesson this action teaches. The whole decision belongs to the verb — see
    /// <see cref="Verb.ResolveLesson"/>, which asks the verb's own contextual answer first, then the
    /// target's declaration, then the verb's defaults, skipping whatever this body cannot hold.
    /// </summary>
    private ModusMentis? ChooseLesson(Verb verb, Element target)
    {
        var threat = ThreatLevel.None;
        if (_protagonist != null)
        {
            try { threat = ThreatSelector.ComputeContext(_scene!, _pov!, _protagonist).Level; } catch { }
        }

        return verb.ResolveLesson(new LessonContext(_scene!, _pov!, _activePartyMember!, target)
        {
            Hostile = threat,
        });
    }

    /// <summary>
    /// Applies a resolved action outcome: the chain's practice XP, outcome reports, the outcome
    /// narration block, and any fight/dialogue/transition it triggers. <paramref name="deferredCommit"/>
    /// additionally settles what only the deferred path produced — the speculative branch's
    /// narration history.
    /// Called synchronously for Get-Up and via the outcome preview's CONTINUE for the main path.
    /// </summary>
    private void CommitOutcomeResult(ActionExecutionResult result, bool deferredCommit)
    {
        Console.WriteLine($"NarrativeController: committing {(result.Succeeded ? "SUCCESS" : "FAILURE")} outcome");

        // The dice chain's own lesson — one report per modus mentis that fed the roll (observation →
        // thinking → action), so each shows its own chip instead of the XP moving in silence. Built
        // here and applied with the rest below; a capped modus mentis reports nothing.
        var practiceReports = new System.Collections.Generic.List<Outcome>();

        // Owed on EVERY success, both paths. This sat inside the `deferredCommit` branch below,
        // which made it unreachable for the one caller that commits synchronously: the Get-Up
        // phase rolled its dice against the chain's levels, narrated the outcome, and paid the
        // chain nothing — no XP and no chip to say so. The reminescence phase beside it grants the
        // same reports by hand for exactly this reason. Deferral is about which BRANCH won (the
        // roll may still be flipped by a humor), not about whether practice is earned; the two
        // things below genuinely are deferred, because only the deferred path generates a
        // speculative second branch or combines an item.
        if (result.Succeeded)
            foreach (var chainModusMentis in result.Action.GetModusMentisChain())
            {
                var practice = ModusMentisPracticeOutcome.For(_activePartyMember, chainModusMentis);
                if (practice != null) practiceReports.Add(practice);
            }

        if (deferredCommit)
        {
            // Keep only the chosen branch's narration in the narrator slot history (discard the
            // speculative other branch that was generated during the roll).
            _actionExecutor.OutcomeNarrator.CommitNarrationHistory(result.Succeeded);
        }

        // Collect all outcome reports: verb-specific + LLM-decided (wound).
        System.Collections.Generic.List<Outcome> allReports;
        if (result.OutcomeReports != null)
        {
            // Main action path: reports were gathered up-front so their verbatims could feed the
            // narration. Reuse those exact instances — re-gathering would run any item factory a
            // second time and materialise duplicate items.
            allReports = new System.Collections.Generic.List<Outcome>(result.OutcomeReports);
        }
        else
        {
            // Fallback (e.g. Get-Up): no reports were pre-gathered, so build them now.
            allReports = new System.Collections.Generic.List<Outcome>();
            allReports.AddRange(GatherVerbReports(result.ActualOutcome, result.Succeeded, result.Action?.CombinedItem));
            allReports.AddRange(result.LlmDecidedReports);
        }

        // The chain's practice chips read as the quiet coda to whatever the verb actually did, so
        // they go last — after the verb's own reports and after its lesson.
        allReports.AddRange(practiceReports);

        // Let the recorder see a successful verb BEFORE its reports apply: a gathering routine is learned
        // off the source while the item is still on it, from where the player stood to take it.
        if (result.Succeeded && _recorder != null && _scene != null && _pov != null
            && result.ActualOutcome is VerbAction)
        {
            _recorder.OnVerbSucceeded(result.Action, _scene, _pov);
        }

        // Remember where and when we were before reports apply. The area drives continuing narration
        // at the destination node (any area-moving verb — move, follow path, stairs, climb, door — not
        // just MoveToArea); the period drives re-placing NPCs for the new time of day.
        var areaBefore   = _pov?.Where;
        var periodBefore = _pov?.When;

        // Apply every report's game-state change in order — to the acting member, so a companion's
        // loot, learned skills, and suffered wounds land on the companion, not the protagonist.
        foreach (var report in allReports)
            report.ApplyTo(OutcomeContext.For(_activePartyMember, _scene, _pov));

        // The two failure penalties the scene cannot carry out itself, because what they change is
        // the narration's: the noetic pool, and whether the visit goes on. A failed act does not
        // refill the pool at the CONTINUE that follows, so a point taken here stays taken.
        bool lostWay = false;
        if (_scene != null)
        {
            if (_scene.PendingFocusLoss > 0)
            {
                _narrationState.ThinkingAttemptsRemaining =
                    Math.Max(0, _narrationState.ThinkingAttemptsRemaining - _scene.PendingFocusLoss);
                Console.WriteLine($"NarrativeController: focus lost — {_narrationState.ThinkingAttemptsRemaining} noetic point(s) left");
                _scene.PendingFocusLoss = 0;
            }
            lostWay = _scene.PendingLostWay;
            _scene.PendingLostWay = false;
            if (lostWay) Console.WriteLine("NarrativeController: the way is lost — the visit ends on CONTINUE");
        }

        // A verb that shifted the period only wrote PoV.When; route it back through the single writer
        // so the graph's period, NPC placement and verb gating all follow it to the new time of day.
        if (_pov != null && periodBefore != null && periodBefore != _pov.When)
        {
            Console.WriteLine($"NarrativeController: time of day advanced {periodBefore} → {_pov.When}");
            TurnTheDayIfWrapped(periodBefore.Value, _pov.When);
            if (!_passagePlayed) ShowTimePassage(periodBefore.Value, _pov.When);
            ApplyTimePeriod(_pov.When);

            // Night came round on somebody waiting where they had no business to: the householder
            // comes to bed and finds them. Sitting out the hours and an hour wasted both bring it on;
            // hiding does not, since the whole point of hiding is not being found.
            if (_scene != null && _pov.When == TimePeriod.Night
                && _scene.Phase == NarrationPhase.Exploration
                && result.Action?.Verb is not HideAndWaitVerb)
                ConfrontationSelector.SummonHouseholder(_scene, _pov, _protagonist?.AffinityKey);
        }
        _passagePlayed = false;
        if (_scene != null) { _scene.WaitUntil = null; _scene.BacktrackTo = null; }

        // Every area walked into is a go-to routine, not only the one the location is left from.
        if (_recorder != null && _pov != null && areaBefore != null && areaBefore.Id != _pov.Where.Id)
            _recorder.OnAreaEntered(_pov);

        // And it is on the map from now on, with the way it was reached (the whole walk, for a move
        // that lands some areas away — following somebody, tracking, a path, a voyage).
        if (_scene != null && _pov != null && areaBefore != null && areaBefore.Id != _pov.Where.Id
            && _scene.Phase == NarrationPhase.Exploration)
            MoveOnMap(areaBefore, _pov.Where, result.Action?.PreselectedOutcome?.Target,
                      back: result.Action?.Verb is BacktrackVerb);

        // UI-visible chips for the outcome block.
        var uiReports = allReports.Where(r => r.ShowInUI).ToList();

        // Add outcome narration block
        var outcomeBlock = new NarrationBlock(
            Type: NarrationBlockType.Outcome,
            ModusMentis: result.ActionModusMentis ?? throw new InvalidOperationException("VerbAction modusMentis cannot be null"),
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

        // === A FIGHT ASKED FOR BY THE ACTION ITSELF ===
        // Hoisted above the failure ladder below, and read off the scene rather than off the outcome,
        // because a report is what asks for it (FightTriggerOutcome.Apply writes the request) and
        // reports are applied for both a success and a failure. Two things follow, both deliberate:
        //
        //  - a fight the player STARTED outranks the one their failure invited. Swing at somebody and
        //    miss in front of a witness, and the brawl you began is the story, not the trespass
        //    conversation the miss would otherwise have opened;
        //  - the request cannot outlive the action that made it. Left on the scene while an early
        //    return took some other branch, it would open a fight two actions later, against whoever
        //    it happened to name.
        //
        // Deferred to the CONTINUE rather than raised now: the chips are the only account the player
        // gets of what the blow did, and the fight screen replaces the narration the frame it opens.
        if (_scene?.PendingFightRequest is { } fightRequest)
        {
            _scene.PendingFightRequest = null;
            _deferredFightOutcome = new FightTriggerOutcome(fightRequest.Npc, $"attack on {fightRequest.Npc.DisplayName}")
            {
                EnemyInitiative = !result.Succeeded,
            };
            Console.WriteLine($"NarrativeController: fight with {fightRequest.Npc.DisplayName} held for CONTINUE"
                            + (result.Succeeded ? "" : " (missed — enemy initiative)"));
            _narrationState.PendingTransitionNode = null;
            _narrationState.ShouldExitOnContinue  = false;
            _narrationState.ShowContinueButton    = true;
            _pendingSegmentLabel                  = SegmentLabelFor(result);
            _pendingSegmentSucceeded              = result.Succeeded;
            if (_pov != null) SceneDebugManager.UpdatePoV(_pov);
            return;
        }

        // === FAILURE-PATH SOCIAL CONSEQUENCE ===
        // Three rungs of one ladder, decided deterministically by the executor from effective
        // proximity (no LLM). At most one is set. See ActionExecutionController.FailureConsequences.

        // Seen by an enemy: they attack, with the initiative.
        if (!result.Succeeded && result.FightWithEnemy != null)
        {
            Console.WriteLine($"NarrativeController: Enemy '{result.FightWithEnemy.DisplayName}' attacks after failed action in plain sight — enemy initiative");
            _pendingFightOutcome = new FightTriggerOutcome(result.FightWithEnemy, $"opportunity attack by {result.FightWithEnemy.DisplayName}")
            {
                EnemyInitiative = true
            };
            return;
        }

        // Seen by a witness: they confront you, and the tree decides what that becomes.
        if (!result.Succeeded && result.CaughtByWitness != null && _pov != null)
        {
            var catchTree = ConfrontationSelector.TreeFor(_scene, result.CaughtByWitness, result.Action.Verb, _pov.Where.IsPrivate);
            Console.WriteLine($"NarrativeController: Witness '{result.CaughtByWitness.DisplayName}' caught the failed illegal action ({catchTree.TreeId})");
            SetPendingDialogue(new Cathedral.Game.Scene.DialogueTriggerOutcome(result.CaughtByWitness, tree: catchTree), result.Action);
            return;
        }

        // Only heard, from a room away: they come to look. That rung is NpcDrawnInOutcome, applied with
        // the other reports above, so it carries a chip and a line in the narration.

        // Handle outcome based on type - show continue button for next step
        // A fight needs no arm here: every route to one goes through scene.PendingFightRequest, which
        // is answered above — including an outcome that IS a FightTriggerOutcome, since it writes the
        // request from its own Apply like any other report.
        if (result.ActualOutcome is VerbAction verbOutcome && _scene != null && _pov != null)
        {
            Console.WriteLine($"NarrativeController: Verb outcome '{verbOutcome.Verb.VerbId}' on '{verbOutcome.Target?.DisplayName}', reports already applied");
            SceneDebugManager.UpdatePoV(_pov);

            // Check if the verb requested a dialogue session
            if (_scene.PendingDialogueRequest != null)
            {
                var req = _scene.PendingDialogueRequest;
                _scene.PendingDialogueRequest = null;
                SetPendingDialogue(new Cathedral.Game.Scene.DialogueTriggerOutcome(req.Npc, req.TreeId, req.Tree), result.Action);
                Console.WriteLine($"NarrativeController: Dialogue verb triggered tree '{req.Tree?.TreeId ?? req.TreeId}' with {req.Npc.DisplayName}");
                return;
            }

            // Any area-moving verb (move, follow path, stairs, climb, open door): stay in scene and
            // transition to the destination area's node. Detected generically by the PoV's area
            // changing, so all connector verbs behave like MoveToAreaVerb (consistent PoV/node and a
            // live session that survives across connectors — required for multi-step routine chains).
            if (_pov != null && areaBefore != null && _pov.Where.Id != areaBefore.Id)
            {
                if (NodeForArea(_pov.Where) is { } areaNode)
                {
                    Console.WriteLine($"NarrativeController: area changed to '{_pov.Where.DisplayName}' — transitioning to node '{areaNode.NodeId}'");
                    _narrationState.PendingTransitionNode = areaNode;
                    _narrationState.ShowContinueButton = true;
                    // Same caption every other resolved action gets. This early return skipped it,
                    // so the whole family of area-moving verbs — move, follow path, stairs, climb,
                    // door, crossing — closed its segment under a blank rule while an in-place action
                    // closed under "after trying to …".
                    _pendingSegmentLabel     = SegmentLabelFor(result);
                    _pendingSegmentSucceeded = result.Succeeded;
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
            _narrationState.ShouldExitOnContinue = lostWay || IsMovementAction(result.Action);
            _narrationState.ShowContinueButton = true;
            _pendingSegmentLabel = SegmentLabelFor(result);
            _pendingSegmentSucceeded = result.Succeeded;
        }
        else
        {
            Console.WriteLine("NarrativeController: Non-transition outcome, showing continue button");
            _narrationState.PendingTransitionNode = null;
            _narrationState.ShouldExitOnContinue = lostWay || IsMovementAction(result.Action);
            _narrationState.ShowContinueButton = true;
            _pendingSegmentLabel = SegmentLabelFor(result);
            _pendingSegmentSucceeded = result.Succeeded;
        }

        // Refresh debug window to reflect any state changes
        if (_pov != null)
            SceneDebugManager.UpdatePoV(_pov);

        Console.WriteLine("NarrativeController: VerbAction phase complete");
    }
    
    /// <summary>
    /// Execute focus observation phase: generate a detailed observation for a specific outcome (async).
    /// Triggered by right-clicking a keyword and selecting an observation modusMentis.
    /// </summary>
    private async Task ExecuteFocusObservationAsync(ModusMentis observationModusMentis, NarrativeAnchor focusOutcome)
    {
        try
        {
            Console.WriteLine($"NarrativeController: Executing focus observation with {observationModusMentis.DisplayName} on outcome '{focusOutcome.DisplayName}'");

            _previewSession.Reset();

            // Deferred commit: reveal the focus block(s) when the player presses CONTINUE.
            void CommitFocus(List<NarrationBlock> blocks)
            {
                foreach (var block in blocks)
                {
                    _scrollBuffer.AddBlock(block);
                    _narrationState.AddBlock(block);
                }
                _ambianceEngine?.PlaySoundEffect(SoundEffectType.NarrativeReveal);
                _scrollBuffer.ScrollToBottom();
                _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;
            }

            await _observationController.GenerateFocusObservationAsync(
                focusOutcome,
                observationModusMentis,
                _currentNode,
                _protagonist.CurrentLocationId,
                _activePartyMember,
                isReminescence: _scene?.Phase == NarrationPhase.ChildhoodReminescence,
                ledger: _observationLedger,
                preview: _previewSession,
                commit: CommitFocus
            );

            // Consume a thinking point (same pool as thinking) at generation end.
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
            _narrationState.IsLoadingFocusObservation = false;
            _narrationState.ErrorMessage = ReportPhaseFailure("observing in focus", ex);
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
        PartyMember companion,
        KeywordRegion keywordRegion)
    {
        string keyword = keywordRegion.Keyword;

        try
        {
            Console.WriteLine($"NarrativeController: Speaking phase — skill={speakingModusMentis.DisplayName}, companion={companion.DisplayName}, keyword='{keyword}'");

            var linkedOutcome = keywordRegion.ResolvedAnchor;
            if (linkedOutcome == null)
            {
                Console.Error.WriteLine($"NarrativeController: Speaking — no outcome found for keyword '{keyword}'");
                _narrationState.IsLoadingSpeaking = false;
                return;
            }

            _previewSession.Reset();

            // Deferred commit: when the player presses CONTINUE on the last spoken line, grey the old
            // content, add the speaking block, spend a noetic point and hand off to the companion.
            void CommitSpeaking(NarrationBlock speakingBlock)
            {
                _scrollBuffer.ConvertToHistory();
                _narrationState.ResetForPartyMemberChange();
                // The companion takes over the narration with their own attention: whatever the
                // speaker had already looked at — or been offered a button for — does not constrain
                // what draws them or what they may be shown.
                _observationLedger.Clear();
                Cathedral.Game.Scene.ActionPathPlanner.Forget();
                _proposalLedger.Clear();
                _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;

                _scrollBuffer.AddBlock(speakingBlock);
                _narrationState.AddBlock(speakingBlock);
                _ambianceEngine?.PlaySoundEffect(SoundEffectType.NarrativeReveal);
                _scrollBuffer.ScrollToBottom();
                _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;

                // Consume one noetic point from the speaker's own pool, then save it.
                _narrationState.ThinkingAttemptsRemaining--;
                SaveActiveNoeticPoints();

                // Switch to companion and load their own counter (fresh if first hand-off to them).
                _activePartyMember = companion;
                LoadNoeticPoints(companion);
            }

            var speakingResult = await _observationController.GenerateSpeakingTextAsync(
                keyword,
                speakingModusMentis,
                companion.DisplayName,
                linkedOutcome,
                _currentNode,
                _activePartyMember,
                _protagonist.CurrentLocationId,
                _worldContext,
                preview: _previewSession,
                commit: CommitSpeaking
            );

            if (speakingResult == null)
            {
                Console.Error.WriteLine("NarrativeController: Speaking generation returned null.");
                _previewSession.Reset();
                _narrationState.IsLoadingSpeaking = false;
                _narrationState.ErrorMessage = "Speaking failed — no text generated.";
                return;
            }

            _narrationState.IsLoadingSpeaking = false;
            _narrationState.ErrorMessage = null;

            Console.WriteLine($"NarrativeController: Speaking phase complete — active party member is now {companion.DisplayName}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"NarrativeController: Speaking phase error: {ex.Message}");
            _previewSession.Reset();
            _narrationState.IsLoadingSpeaking = false;
            _narrationState.ErrorMessage = ReportPhaseFailure("speaking", ex);
        }
    }

    /// <summary>
    /// Records the area the party stands in as visited, at every phase that opens. The moves record
    /// themselves; this is what catches where a visit begins — the arrival, a routine resumed on its
    /// area ("you know the way", so nothing on the way to it), a --start-area.
    /// </summary>
    private void NoteWhereWeStand()
    {
        if (_scene != null && _pov != null && _scene.Phase == NarrationPhase.Exploration)
            AreaVisits.Arrive(_scene, _pov.Where);
    }

    /// <summary>Tab or the header's ↹: the map of the location in place of the narration text.</summary>
    public void ToggleDebugMap() => _debugMap.Toggle();

    /// <summary>
    /// Whether the player may open the map: in exploration, the one phase played in a real location.
    /// The childhood and the get-up have a scene of their own and nowhere to go in it, so there it is
    /// still the developer's Tab only.
    /// </summary>
    public bool CanToggleMap => _scene?.Phase == NarrationPhase.Exploration;

    /// <summary>The ↹'s middle cell, for the CLI; null while it is not drawn.</summary>
    public (int X, int Y)? MapButtonCell =>
        _mapButtonRegion.Width > 0 ? (_mapButtonRegion.X + 1, _mapButtonRegion.Y) : null;

    private bool OverMapButton(int x, int y) =>
        _mapButtonRegion.Width > 0 && y == _mapButtonRegion.Y
        && x >= _mapButtonRegion.X && x < _mapButtonRegion.X + _mapButtonRegion.Width;

    /// <summary>An arrow key with the map up: pans it. False when the map is hidden or the key is another.</summary>
    public bool PanDebugMap(OpenTK.Windowing.GraphicsLibraryFramework.Keys key) => _debugMap.HandleKey(key);

    /// <summary>A modal popup (modus mentis / item / choice) is open over the panel.</summary>
    private bool IsAnyPopupVisible =>
        _modusMentisPopup.IsVisible || _itemSelectionPopup.IsVisible || _choicePopup.IsVisible;

    /// <summary>
    /// Update loop - called at 10 Hz by game controller.
    /// </summary>
    public void Update()
    {
        RenderPanel();

        // A popup is a modal choice, so the panel behind it is greyed out the same way
        // the generation preview box greys it: recolouring the text rather than darkening
        // the whole HUD, so the popup itself (a separate terminal) stays at full strength.
        // Applied after RenderPanel because DimContent only affects what is already drawn.
        if (IsAnyPopupVisible)
            _ui.DimContent();

        UpdateMapLabel();
    }

    /// <summary>
    /// The label above the pointer for what it is over on the map, while the map is what the panel
    /// shows and nothing sits over it (a dice box, the hours passing, a preview). Taken down when the
    /// map goes; left alone while a modal popup holds the shared popup.
    /// </summary>
    private void UpdateMapLabel()
    {
        if (IsAnyPopupVisible) { _mapLabelShown = false; return; }
        bool mapUp = _ui.MapDrawn && !_narrationState.IsDiceRollActive && !_timeBox.IsVisible
                  && !_previewSession.IsActive;
        var label = mapUp ? _debugMap.LabelAt(_lastMouseX, _lastMouseY) : null;
        if (label == null && !_mapLabelShown) return;
        DebugLocationMap.ShowLabel(_popup, label);
        _mapLabelShown = label != null;
    }

    private void RenderPanel()
    {
        // Clear terminal
        _ui.Clear();

        // Sync music filter based on current loading/dice state
        if (_ambianceEngine != null)
        {
            // Play the dice-roll music only while the dice are actually tumbling — it stops the
            // instant the animation settles and the values are locked in, not when the player
            // dismisses the settled overlay with Continue.
            bool diceRolling = _narrationState.IsDiceRollActive && _narrationState.IsDiceRolling;
            var desired = diceRolling                 ? MusicFilter.DiceRoll
                        : _narrationState.IsAnyLoading ? MusicFilter.Loading
                        : MusicFilter.None;
            if (_ambianceEngine.ActiveFilter != desired)
                _ambianceEngine.SetFilter(desired);
        }

        // Header: agent name (left) + noetic counter (right, hidden in phases without cost).
        // Always rendered — while the LLM is generating it's greyed out along with the rest
        // of the panel (see below) rather than replaced.
        bool showNoetic = _scene?.Phase != NarrationPhase.ChildhoodReminescence
                       && _scene?.Phase != NarrationPhase.GetUp;
        _ui.RenderHeader(_activePartyMember.DisplayName, _narrationState.ThinkingAttemptsRemaining,
            _activePartyMember.MaxNoeticPoints, showNoetic,
            trespassing: _scene?.Phase == NarrationPhase.Exploration && _pov != null ? IsTrespassing : null);

        // The ↹, except while the map is already the panel's for a reason of its own: a walk playing
        // out, or a backtrack asking where to. Both own every click until they are done.
        _mapButtonRegion = CanToggleMap && _walk == null && _backtrackChoices == null
            ? _ui.RenderMapButton(_mapButtonHovered, DebugLocationMap.Visible)
            : default;

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
                ? Config.LoadingMessages.RollingDice
                : (_dice.IsCurrentlySuccess ? "Success! Click Continue to see the outcome" : "Failed! Click Continue to see the outcome");
            _ui.RenderStatusBar(diceStatus);
            return;
        }

        // The hours passing (or, for a wait, the choice of how many): narration greyed underneath.
        if (_timeBox.IsVisible)
        {
            _timeBox.Advance();
            RenderNarrationContent();
            _ui.RenderTimePassage(_timeBox);
            _ui.RenderStatusBar(_timeBox.IsChoosing ? "Choose the hour to wait until"
                              : _timeBox.IsAnimating ? "Time passes..." : "Click Continue");
            return;
        }

        // A backtrack asking where to: the map, with every area known ways reach lit for a click.
        if (_backtrackChoices != null && _pov != null && _scene != null)
        {
            DebugLocationMap.WalkPannable = true;
            var at = _debugMap.CardMiddle(_scene, _pov.Where);
            _ui.RenderDebugMap(_debugMap, _scene, _pov.Where, _graph.CurrentPeriod, at,
                               follow: !_backtrackCentred, choosable: _backtrackChoices, hovered: _backtrackHovered);
            _backtrackCentred = true;
            _ui.RenderStatusBar(_backtrackHovered != null
                ? $"Go back to {_backtrackHovered.DisplayName}"
                : "Choose where to go back to");
            return;
        }

        // A move on the map: the party walks the way taken, then Continue. The map stands in for the
        // narration, as it does when opened by hand.
        if (_walk != null)
        {
            AdvanceWalk();
            var step = _walk[_walkShown];
            DebugLocationMap.WalkPannable = WalkSettled;
            _ui.RenderDebugMap(_debugMap, _scene, step.Here, _graph.CurrentPeriod, (step.X, step.Y),
                               follow: !_walkArrivalCentred, withheld: WalkWithheld());
            if (WalkSettled) _walkArrivalCentred = true;
            _walkContinueRegion = WalkSettled ? _ui.RenderExitButton("CONTINUE", _walkContinueHovered) : default;
            _ui.RenderStatusBar(WalkSettled ? "Click Continue" : "Walking...");
            return;
        }

        // LLM generation preview: a box streaming the text being written, gated by CONTINUE. Shown
        // for the whole life of the preview session (generation in flight AND the wait-for-CONTINUE
        // after), which is why it precedes the plain IsAnyLoading branch below.
        if (_previewSession.IsActive)
        {
            RenderNarrationContent();
            var snap = _previewSession.Snapshot();
            // Play the hover tick whenever new preview text streams in (typewriter feedback).
            if (snap.DisplayText != _lastPreviewText)
            {
                PlayHoverSound();
                _lastPreviewText = snap.DisplayText;
            }
            var region = _ui.RenderPreviewBox(snap, _previewContinueHovered, _previewAnswerHovered);
            _previewContinueRegion = region.Present ? (region.X, region.Y, region.Width) : default;
            _previewAnswerRegions  = _ui.PreviewAnswerRegions;
            // No footer button while the box is up — clear its region so a stale one can't be clicked.
            _exitButtonRegion = default;
            _ui.RenderWaitingStatus(_narrationState.LoadingMessage);
            return;
        }
        _previewContinueRegion = default;
        _previewAnswerRegions  = default;
        _lastPreviewText = "";

        // LLM generating (non-action loading, or action evaluation phase before dice roll):
        // keep the narration visible but greyed out (header included), with a centered
        // progress bar animation and the waiting message (animated ellipsis) on the footer.
        if (_narrationState.IsAnyLoading)
        {
            RenderNarrationContent();
            _ui.DimContent();
            _ui.RenderCenterProgressBar();
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
        // Tab (developer keys): the map of the location in place of the text, and no scrollbar.
        if (DebugLocationMap.Visible)
        {
            _ui.RenderDebugMap(_debugMap, _scene, _pov?.Where, _graph.CurrentPeriod);
            return;
        }

        _ui.RenderObservationBlocks(
            _scrollBuffer,
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

        // Over the map the one button goes back to the narration, whatever it would otherwise be:
        // LEAVE pressed from a map would quit the location, and CONTINUE would move the story on
        // behind it. Both are there again once the map is closed.
        if (DebugLocationMap.Visible)
        {
            _exitButtonRegion = _ui.RenderExitButton("RETURN", _exitButtonHovered);
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

        // Choice popup (Think/Observe or Execute/Use Tool) takes highest priority
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
                // The same routing as the pointer path through the shell and the --cli choose: the broken
                // modus mentis guard and Reconsider are there, and a copy of it here had neither.
                ApplyModusMentisSelection(selectedModusMentis);
            }
            else
            {
                Console.WriteLine("NarrativeController: Popup closed (clicked outside)");
                _narrationState.IsSelectingObservationModusMentis = false;
                _narrationState.IsSelectingModusMentisForSpeaking = false;
                _narrationState.ActionPendingReconsider = null;
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

        if (_debugMap.SetHover(mouseX, mouseY)) PlayHoverSound();

        bool overMap = OverMapButton(mouseX, mouseY);
        if (overMap != _mapButtonHovered)
        {
            _mapButtonHovered = overMap;
            if (overMap) PlayHoverSound();
        }

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

        if (_timeBox.IsVisible)
        {
            if (_timeBox.HandleHover(mouseX, mouseY)) PlayHoverSound();
            return;
        }

        if (_walk != null)
        {
            var r = _walkContinueRegion;
            bool over = r.Width > 0 && mouseY == r.Y && mouseX >= r.X && mouseX < r.X + r.Width;
            if (over != _walkContinueHovered)
            {
                _walkContinueHovered = over;
                if (over) PlayHoverSound();
            }
            return;
        }

        if (_backtrackChoices != null)
        {
            var under = _debugMap.AreaAt(mouseX, mouseY);
            if (under != null && !_backtrackChoices.Contains(under)) under = null;
            if (under != _backtrackHovered)
            {
                _backtrackHovered = under;
                if (under != null) PlayHoverSound();
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

        // Preview box CONTINUE hover (shown while the generation-preview session is active).
        if (_previewAnswerRegions.Yes.Width > 0)
        {
            bool? over = Over(_previewAnswerRegions.Yes) ? true : Over(_previewAnswerRegions.No) ? false : null;
            if (over != _previewAnswerHovered)
            {
                _previewAnswerHovered = over;
                if (over != null) PlayHoverSound();
            }
            bool Over((int X, int Y, int Width) r) => mouseY == r.Y && mouseX >= r.X && mouseX < r.X + r.Width;
        }

        if (_previewContinueRegion.Width > 0)
        {
            bool overPreview = mouseY == _previewContinueRegion.Y
                            && mouseX >= _previewContinueRegion.X
                            && mouseX <  _previewContinueRegion.X + _previewContinueRegion.Width;
            if (overPreview != _previewContinueHovered)
            {
                _previewContinueHovered = overPreview;
                if (overPreview) PlayHoverSound();
            }
        }

        // In the post-action (CONTINUE) state, while the LLM is generating, and while the preview box
        // is up, content is inert — skip keyword/action hover (scrollbar interactions stay allowed).
        if (_narrationState.ShowContinueButton || _narrationState.IsAnyLoading || _previewSession.IsActive)
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
        // Choosing where to go back to is modal: the pad pans, a lit card is the answer.
        if (_backtrackChoices != null)
        {
            if (_debugMap.HandleClick(mouseX, mouseY)) { PlayClickSound(); return; }
            if (_debugMap.AreaAt(mouseX, mouseY) is { } to && _backtrackChoices.Contains(to))
                _onBacktrackChosen?.Invoke(to);
            return;
        }

        // A walk on the map is modal: a click while it plays finishes it, and its Continue goes on.
        if (_walk != null)
        {
            var r = _walkContinueRegion;
            if (!WalkSettled) _walkStepMs = 0;
            else if (_debugMap.HandleClick(mouseX, mouseY)) PlayClickSound();   // the pad, once arrived
            else if (r.Width > 0 && mouseY == r.Y && mouseX >= r.X && mouseX < r.X + r.Width) OnWalkContinue();
            return;
        }

        // The ↹ in the header: the map and back, whatever box is up over the panel — none of them
        // covers the header, and the map is only what is drawn under them.
        if (OverMapButton(mouseX, mouseY))
        {
            PlayClickSound();
            ToggleDebugMap();
            return;
        }

        // The map's pan pad sits in the panel's top-right corner, clear of every box drawn over it.
        if (_debugMap.HandleClick(mouseX, mouseY))
        {
            PlayClickSound();
            return;
        }

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

        // The time box is modal: its period buttons while it asks, its Continue once the hours have
        // passed, and nothing underneath it either way.
        if (_timeBox.IsVisible)
        {
            if (_timeBox.IsOnContinue(mouseX, mouseY)) OnTimePassageContinue();
            else _timeBox.HandlePeriodClick(mouseX, mouseY);
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

        // Preview box YES / NO — the doubt of a thinking (DoubtStat): the answer goes to the waiting
        // thinking, and the part is committed as CONTINUE would.
        if (_previewAnswerRegions.Yes.Width > 0 && mouseY == _previewAnswerRegions.Yes.Y)
        {
            bool? answer = mouseX >= _previewAnswerRegions.Yes.X && mouseX < _previewAnswerRegions.Yes.X + _previewAnswerRegions.Yes.Width ? true
                         : mouseX >= _previewAnswerRegions.No.X  && mouseX < _previewAnswerRegions.No.X  + _previewAnswerRegions.No.Width  ? false
                         : null;
            if (answer is { } yes)
            {
                PlayClickSound();
                _previewAnswerHovered = null;
                _previewSession.TryAnswer(yes);
                return;
            }
        }

        // Preview box CONTINUE — commit the current part and advance to the next (or end the session).
        if (_previewContinueRegion.Width > 0
            && mouseY == _previewContinueRegion.Y
            && mouseX >= _previewContinueRegion.X
            && mouseX <  _previewContinueRegion.X + _previewContinueRegion.Width)
        {
            PlayClickSound();
            _previewContinueHovered = false;
            _previewSession.TryContinue();
            return;
        }

        // Single footer button (CONTINUE / LEAVE / RUNAWAY) — clickable in idle and post-action states.
        if (_exitButtonRegion.Width > 0
            && mouseY == _exitButtonRegion.Y
            && mouseX >= _exitButtonRegion.X
            && mouseX <  _exitButtonRegion.X + _exitButtonRegion.Width)
        {
            PlayClickSound();
            if (DebugLocationMap.Visible) _debugMap.Toggle();   // RETURN
            else HandleFooterButtonClicked();
            return;
        }

        // In the post-action (CONTINUE) state, while the LLM is generating, and while the preview box
        // is up, the content is inert — swallow other clicks (scrollbar clicks handled above).
        if (_narrationState.ShowContinueButton || _narrationState.IsAnyLoading || _previewSession.IsActive)
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
                ApplyModusMentisSelection(selectedModusMentis);
            }
            else
            {
                Console.WriteLine("NarrativeController: Popup closed (clicked outside)");
                _narrationState.IsSelectingObservationModusMentis = false;
                _narrationState.IsSelectingModusMentisForSpeaking = false;
                _narrationState.ActionPendingReconsider = null;
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
                if (action.Superseded)
                {
                    Console.WriteLine($"NarrativeController: '{action.ActionText}' was reconsidered — it no longer answers a click.");
                    return;
                }

                // Reaching for a tool is part of the action already on screen, not a second thought:
                // the point was spent when the action was thought of, and only one item may ever be
                // combined with it (hasItems tests CombinedItem), so nothing loops. Gating this on
                // remaining points made the five tool-gated verbs unreachable outright once the pool
                // shrank — the thinking phase that produces the action spends the point that "Use
                // Tool" then wanted, so `dig`, `mine`, `fish`, `cut_wood` and `break` were offered,
                // greyed out, and impossible.
                bool hasItems = action.CombinedItem == null && GetCombinableItems().Count > 0;
                var disabledIndices = hasItems ? new HashSet<int>() : new HashSet<int> { 1 };
                // Refused for want of a tool: executing it bare-handed again is the same refusal.
                if (action.NeedsTool) disabledIndices.Add(0);
                // Reconsidering is a thinking, and costs its point; the two opening phases have one act
                // and one means, so there is nothing to reconsider in them.
                if (!CanReconsider()) disabledIndices.Add(2);

                Console.WriteLine($"NarrativeController: Showing action mode choice for '{action.ActionText}' (hasItems={hasItems})");
                _narrationState.ActionPendingModeSelection = action;
                _narrationState.IsSelectingInteractionMode = true;
                _narrationState.InteractionModeIsForKeyword = false;
                Vector2 screenPos = _terminalInputHandler.CellToScreen(mouseX, mouseY, _core.ClientSize);
                _choicePopup.Show(screenPos, new List<string> { "Execute", "Use Tool", "Reconsider" }, "Action", disabledIndices);
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
            // The title names what was clicked. It read "Keyword VerbAction" — a leftover of the
            // Outcome→VerbAction rename, which described the popup's plumbing rather than telling
            // the player what the three options were about to be aimed at.
            _choicePopup.Show(screenPos, speakChoices, $"“{clickedKeyword.Keyword}”", speakDisabled);
        }
    }

    /// <summary>
    /// Route a modus-mentis popup selection to the phase it starts: the companion picker (step 2 of
    /// Speak About), a focus observation, or a thinking phase. Shared by the mouse path and the
    /// --cli <c>choose</c> command.
    /// </summary>
    private void ApplyModusMentisSelection(ModusMentis selectedModusMentis)
    {
        Console.WriteLine($"NarrativeController: Selected modusMentis: {selectedModusMentis.DisplayName}");

        // A modus mentis this body can no longer carry is still offered and refused here rather than
        // filtered out of the popup — see BrokenModusMentis. This is the one place all three
        // player-chosen faculties pass through, so the guard is written once.
        if (_activePartyMember.IsModusMentisBroken(selectedModusMentis))
        {
            var faculty = _narrationState.IsSelectingModusMentisForSpeaking
                ? NeutralNarration.BrokenFaculty.Speech
                : _narrationState.IsSelectingObservationModusMentis
                    ? NeutralNarration.BrokenFaculty.Observation
                    : NeutralNarration.BrokenFaculty.Thinking;

            _narrationState.IsSelectingModusMentisForSpeaking = false;
            _narrationState.IsSelectingObservationModusMentis = false;
            _narrationState.ActionPendingReconsider = null;
            _ = RefuseBrokenModusMentisAsync(selectedModusMentis, faculty);
            return;
        }

        if (_narrationState.ActionPendingReconsider is { } reconsidered)
        {
            _narrationState.ActionPendingReconsider = null;
            _narrationState.IsLoadingThinking = true;
            _narrationState.LoadingMessage = Config.LoadingMessages.ThinkingDeeply;
            _ = ExecuteReconsiderAsync(selectedModusMentis, reconsidered);
            return;
        }

        if (_narrationState.IsSelectingModusMentisForSpeaking)
        {
            // Step 1 of Speak About: speaking modusMentis selected → show companion selection
            _narrationState.IsSelectingModusMentisForSpeaking = false;
            _narrationState.SpeakingModusMentisPending = selectedModusMentis;
            _pendingCompanions = _protagonist.CompanionParty.ToList();
            var companionNames = _pendingCompanions.Select(c => c.DisplayName).ToList();
            _narrationState.IsSelectingCompanionForSpeaking = true;
            Vector2 screenPos2 = _terminalInputHandler.CellToScreen(_lastMouseX, _lastMouseY, _core.ClientSize);
            _choicePopup.Show(screenPos2, companionNames, "Who do you address?");
            return;
        }

        // Get the keyword that was clicked (stored before popup appeared)
        if (_narrationState.HoveredKeyword == null) return;

        var clickedRegion = _narrationState.HoveredKeyword;
        string keyword = clickedRegion.Keyword;

        // Check if we're selecting an observation modusMentis or thinking modusMentis
        if (_narrationState.IsSelectingObservationModusMentis)
        {
            // Focus observation phase
            _narrationState.IsLoadingFocusObservation = true;
            _narrationState.LoadingMessage = ObservationLoadingMessage();
            _narrationState.IsSelectingObservationModusMentis = false;

            var focusOutcome = clickedRegion.ResolvedAnchor;
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
            _ = ExecuteThinkingPhaseAsync(selectedModusMentis, clickedRegion);
        }
    }

    /// <summary>
    /// Narrates a modus mentis the body can no longer carry, in its own voice, and spends the noetic
    /// point the attempt cost. The observation / thinking / speaking counterpart of
    /// <c>BrokenModusMentisRule</c>, which does the same job for an action — see
    /// <see cref="BrokenModusMentis"/> for why this refuses rather than withholding.
    ///
    /// <para>Written against the same three moving parts as the coded-rule failure in
    /// <see cref="ExecuteActionPhaseAsync"/>: the text streams into a preview part so it reads like
    /// any other narration, the block is committed on that part's CONTINUE, and the point is spent
    /// unless the phase is one of the two that never charges for an attempt. Out of points, the
    /// segment closes without refilling, exactly as a failed roll does — LEAVE is then the way
    /// out.</para>
    ///
    /// <para>It swallows its own exception rather than calling <c>ReportPhaseFailure</c>. This is a
    /// refusal, not a phase: taking the visit down because the account of a ruined arm could not be
    /// generated would turn a wound into a lost run.</para>
    /// </summary>
    private async Task RefuseBrokenModusMentisAsync(
        ModusMentis modusMentis, NeutralNarration.BrokenFaculty faculty)
    {
        try
        {
            Console.WriteLine(
                $"NarrativeController: '{modusMentis.DisplayName}' is broken on this body " +
                $"(effective level {_activePartyMember.GetEffectiveModusMentisLevel(modusMentis)}) — " +
                $"refusing {faculty}");

            _previewSession.Reset();
            var part = _previewSession.BeginPart(PreviewTitles.For(modusMentis));

            string text = await _actionExecutor.OutcomeNarrator.NarrateBrokenModusMentisAsync(
                modusMentis, _activePartyMember, faculty, CancellationToken.None, preview: part?.Sink);
            if (string.IsNullOrWhiteSpace(text))
                text = $"[IMPOSSIBLE] {BrokenModusMentis.NeutralFor(_activePartyMember, modusMentis, faculty)}";

            var block = new NarrationBlock(
                Type: NarrationBlockType.Outcome,
                ModusMentis: modusMentis,
                Text: text,
                Keywords: null,
                Actions: null);

            void Commit()
            {
                _scrollBuffer.AddBlock(block);
                _narrationState.AddBlock(block);
                _scrollBuffer.ScrollToBottom();
                _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;
            }

            if (part != null)
            {
                part.AttachCommit(Commit);
                part.MarkComplete();
                _previewSession.EndProduction();
            }
            else Commit();

            if (_scene?.Phase == NarrationPhase.ChildhoodReminescence
                || _scene?.Phase == NarrationPhase.GetUp)
                return;

            if (_narrationState.ThinkingAttemptsRemaining > 0)
            {
                _narrationState.ThinkingAttemptsRemaining--;
                Console.WriteLine("NarrativeController: Broken modus mentis — consumed 1 noetic point " +
                                  $"({_narrationState.ThinkingAttemptsRemaining} remaining)");
            }
            else
            {
                _pendingSegmentSucceeded = false;
                _narrationState.ShowContinueButton = true;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"NarrativeController: Error narrating broken modus mentis: {ex.Message}");
            _previewSession.Reset();
        }
        finally
        {
            _narrationState.IsLoadingThinking = false;
            _narrationState.IsLoadingFocusObservation = false;
        }
    }

    /// <summary>
    /// Dispatches the result of the Think/Observe/SpeakAbout or Execute/Use Tool choice popup.
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
                Console.WriteLine($"NarrativeController: Speak About — companion={companion.DisplayName}, skill={speakingMM.DisplayName}");
                _narrationState.IsLoadingSpeaking = true;
                _narrationState.LoadingMessage = Config.LoadingMessages.Speaking;
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
            // VerbAction choice: 0 = Execute, 1 = Use Tool, 2 = Reconsider
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
                    Console.WriteLine($"NarrativeController: Choice — Use Tool for '{action.ActionText}'");
                    _narrationState.IsSelectingItemForAction = true;
                    _narrationState.ActionPendingItemCombination = action;
                    Vector2 screenPos = _terminalInputHandler.CellToScreen(_lastMouseX, _lastMouseY, _core.ClientSize);
                    _itemSelectionPopup.Show(screenPos, candidateItems, "Use with this action");
                }
                else
                {
                    Console.WriteLine("NarrativeController: No combinable items available.");
                }
            }
            else if (choiceIndex == 2 && action != null && CanReconsider())
            {
                Console.WriteLine($"NarrativeController: Choice — Reconsider '{action.ActionText}'");
                _narrationState.ActionPendingReconsider = action;
                _narrationState.IsSelectingObservationModusMentis = false;
                _narrationState.IsSelectingModusMentisForSpeaking = false;
                Vector2 screenPos = _terminalInputHandler.CellToScreen(_lastMouseX, _lastMouseY, _core.ClientSize);
                _modusMentisPopup.Show(screenPos, _activePartyMember.GetThinkingModiMentis(), "Reconsider with");
            }
            else
            {
                Console.WriteLine("NarrativeController: VerbAction choice dismissed");
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
        // A popup that overflows its box owns the wheel: scrolling the narration behind a modal the
        // player is reading through would move text they cannot see anyway.
        if (_modusMentisPopup.IsVisible)
        {
            _modusMentisPopup.Scroll(delta > 0 ? -3 : 3);
            return;
        }
        if (_itemSelectionPopup.IsVisible)
        {
            _itemSelectionPopup.Scroll(delta > 0 ? -3 : 3);
            return;
        }

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

    // ── CLI driving surface (--cli) ───────────────────────────────────────────
    // These exist so an automated run can act on named handles instead of pixel coordinates.
    // Note the popup selectors deliberately bypass OnMouseClick: its popup branches read the live
    // OS cursor via GetCorrectedMousePosition(), which no injected coordinate can influence.

    /// <summary>
    /// Snapshot of the narration state a --cli `state` command reports. <c>Observed</c> is how many
    /// objects this narration phase has already looked at (see <see cref="ObservationLedger"/>) out of
    /// how many the current node offers — the pair a script watches to prove observations stop
    /// repeating themselves, and that the count resets when a phase does.
    /// </summary>
    public (bool AnyLoading, string LoadingMessage, bool DiceActive, bool DiceRolling,
            bool ShowContinue, int Noetic, int MaxNoetic, string? Error, int Observed, int Observable)
        CliSnapshot() => (
            _narrationState.IsAnyLoading,
            _narrationState.LoadingMessage,
            _narrationState.IsDiceRollActive,
            _narrationState.IsDiceRolling,
            _narrationState.ShowContinueButton,
            _narrationState.ThinkingAttemptsRemaining,
            _activePartyMember.MaxNoeticPoints,
            _narrationState.ErrorMessage,
            _observationLedger.Count,
            _currentNode.GetAllDirectConcreteOutcomes().Count);

    /// <summary>Distinct clickable keywords in the current frame, in reading order.</summary>
    public IReadOnlyList<string> CliKeywords()
        => _ui.KeywordRegions.Select(r => r.Keyword).Distinct().ToList();

    /// <summary>Clickable action lines in the current frame as (index, text) pairs.</summary>
    public IReadOnlyList<(int Index, string Text)> CliActions()
        => _ui.ActionRegions
            .Select(r => (r.ActionIndex, r.Action?.ActionText ?? "?"))
            .Distinct()
            .ToList();

    /// <summary>The footer button label and whether it is currently clickable.</summary>
    public (bool Present, int X, int Y, int Width) CliExitButton() =>
        (_exitButtonRegion.Width > 0, _exitButtonRegion.X, _exitButtonRegion.Y, _exitButtonRegion.Width);

    /// <summary>The dice [ Continue ] button region, if the dice overlay is showing one.</summary>
    public (bool Present, int X, int Y, int Width) CliDiceContinue()
    {
        var r = _dice.ContinueButtonRegion;
        return (_narrationState.IsDiceRollActive && !_narrationState.IsDiceRolling && r.Width > 0, r.X, r.Y, r.Width);
    }

    /// <summary>The time box's state: none, choosing, passing (animating) or settled.</summary>
    public string CliTimeBoxState()
        => !_timeBox.IsVisible ? "none"
         : _timeBox.IsChoosing ? "choosing"
         : _timeBox.IsAnimating ? "passing" : "settled";

    /// <summary>Whether a backtrack is asking where to go: none or choosing.</summary>
    public string CliBacktrackState() => _backtrackChoices == null ? "none" : "choosing";

    /// <summary>
    /// The areas a backtrack offers while it asks, by name, with the screen cell of each card as last
    /// drawn (null when the card is off the view — pan to it as a player would).
    /// </summary>
    public IReadOnlyList<(string Name, (int X, int Y)? Cell)> CliBacktrackChoices()
        => _backtrackChoices == null ? Array.Empty<(string, (int, int)?)>()
         : _backtrackChoices.OrderBy(a => a.DisplayName, StringComparer.Ordinal)
                            .Select(a => (a.DisplayName, _debugMap.ScreenCellOf(a))).ToList();

    /// <summary>Pans the chooser's map so the named area's card is in view; false when it is not on offer.</summary>
    public bool CliShowBacktrackChoice(string name)
    {
        var area = _backtrackChoices?.FirstOrDefault(a => a.DisplayName.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (area == null) return false;
        _debugMap.CentreOn(area);
        return true;
    }

    /// <summary>The walk on the map: none, walking (animating) or settled.</summary>
    public string CliWalkState() => _walk == null ? "none" : WalkSettled ? "settled" : "walking";

    /// <summary>The walk's [ CONTINUE ] region, once the party has arrived.</summary>
    public (bool Present, int X, int Y, int Width) CliWalkContinue()
    {
        var r = _walkContinueRegion;
        return (WalkSettled && r.Width > 0, r.X, r.Y, r.Width);
    }

    /// <summary>The periods the time box offers while it asks, with their button regions.</summary>
    public IReadOnlyList<(TimePeriod Period, int X, int Y, int Width)> CliTimeBoxChoices() => _timeBox.PeriodRegions;

    /// <summary>The time box's [ Continue ] region, once the hours have passed.</summary>
    public (bool Present, int X, int Y, int Width) CliTimeBoxContinue()
    {
        var r = _timeBox.ContinueButtonRegion;
        return (_timeBox.IsSettled && r.Width > 0, r.X, r.Y, r.Width);
    }

    /// <summary>Whether the generation-preview box is up, its title, text and CONTINUE region (if clickable).</summary>
    public (bool Active, string Title, string Text, bool Complete) CliPreview()
    {
        var s = _previewSession.Snapshot();
        return (s.Active, s.Title, s.DisplayText, s.Complete);
    }

    /// <summary>Whether the preview box is asking YES or NO (the doubt of a thinking) rather than offering CONTINUE.</summary>
    public bool CliPreviewAsking() => _previewSession.Snapshot().Asking;

    /// <summary>
    /// Whether a doubt waits on an answer anywhere in the preview queue — shown, or behind parts still
    /// to be continued past. The thinking is blocked on it, so the CLI counts it as idle.
    /// </summary>
    public bool CliPreviewAwaitsAnswer() => _previewSession.AwaitsAnswer;

    /// <summary>The preview box's [ YES ] or [ NO ] region, when the shown part asks (the doubt of a thinking).</summary>
    public (bool Present, int X, int Y, int Width) CliPreviewAnswer(bool yes)
    {
        var r = yes ? _previewAnswerRegions.Yes : _previewAnswerRegions.No;
        return (r.Width > 0, r.X, r.Y, r.Width);
    }

    /// <summary>The preview box [ CONTINUE ] button region, when generation is done and it is clickable.</summary>
    public (bool Present, int X, int Y, int Width) CliPreviewContinue() =>
        (_previewContinueRegion.Width > 0, _previewContinueRegion.X, _previewContinueRegion.Y, _previewContinueRegion.Width);

    /// <summary>Labels of the currently visible popup, or null when no popup is up.</summary>
    public (string Kind, IReadOnlyList<string> Labels)? CliPopup()
    {
        if (_choicePopup.IsVisible)
            return ("choice", _choicePopup.Choices.Select((c, i) =>
                _choicePopup.IsChoiceEnabled(i) ? c : $"{c} (disabled)").ToList());
        if (_modusMentisPopup.IsVisible)
            return ("modus-mentis", _modusMentisPopup.Choices.Select(m => m.DisplayName).ToList());
        if (_itemSelectionPopup.IsVisible)
            return ("item", _itemSelectionPopup.Choices.Select(i => i.DisplayName).ToList());
        return null;
    }

    /// <summary>
    /// Answer the visible popup by index. Returns an error string, or null on success.
    /// </summary>
    /// <summary>A stable name for what a will's first step acts on: a person by archetype id, anything else by name.</summary>
    private static string CliTargetId(Element? target) => target switch
    {
        SceneNpc { Entity: Cathedral.Game.Npc.NpcEntity npc } => npc.Archetype.ArchetypeId,
        null => "-",
        _    => target.DisplayName.Replace(' ', '_'),
    };

    /// <summary>The index of the modus mentis <paramref name="id"/> in the open modus mentis popup, or -1.</summary>
    public int CliModusMentisPopupIndex(string id)
        => _modusMentisPopup.IsVisible
            ? _modusMentisPopup.Choices.ToList().FindIndex(m => m.ModusMentisId.Equals(id, StringComparison.OrdinalIgnoreCase))
            : -1;

    public string? CliChoosePopup(int index)
    {
        if (_choicePopup.IsVisible)
        {
            if (!_choicePopup.IsChoiceEnabled(index))
                return $"choice {index} is disabled or out of range";
            _choicePopup.Hide();
            _narrationState.IsSelectingInteractionMode = false;
            DispatchChoiceSelection(index);
            return null;
        }

        if (_modusMentisPopup.IsVisible)
        {
            var list = _modusMentisPopup.Choices;
            if (index < 0 || index >= list.Count) return $"modus-mentis {index} out of range";
            var chosen = list[index];
            _modusMentisPopup.Hide();
            ApplyModusMentisSelection(chosen);
            return null;
        }

        if (_itemSelectionPopup.IsVisible)
        {
            var list = _itemSelectionPopup.Choices;
            if (index < 0 || index >= list.Count) return $"item {index} out of range";
            var chosen = list[index];
            _itemSelectionPopup.Hide();
            var pending = _narrationState.ActionPendingItemCombination;
            _narrationState.IsSelectingItemForAction = false;
            _narrationState.ActionPendingItemCombination = null;
            if (pending != null) _ = ExecuteItemCombinationAsync(pending, chosen);
            return null;
        }

        return "no popup is visible";
    }

    /// <summary>Click the first region whose keyword matches (case-insensitive).</summary>
    /// <summary>
    /// Click a narration keyword by name, or — when <paramref name="keyword"/> is a number — by
    /// position in the offered list.
    ///
    /// <para>The index form exists because a verb test cannot know the word. Which keyword an
    /// observation highlights is chosen from the prose (under <c>--playground</c> it is the object's
    /// own noun, in a real run the noun most associated with it), so a script that has already pinned
    /// the phase to one object with <c>--observe-only</c> knows exactly <i>what</i> it is looking at
    /// and still cannot spell the handle. Guessing it from the object's display name is what made
    /// two thirds of the generated verb tests fail on "no clickable keyword 'stern' on screen".</para>
    ///
    /// <para>Name-matching stays the default and stays preferred for hand-written scripts: it reads
    /// as intent and survives a reordering. Use the index where the point is "whatever this phase
    /// opened on".</para>
    /// </summary>
    /// <summary>
    /// The verb id of the last action actually put through execution, or null before any. Read by
    /// the CLI's <c>expect-verb</c>: the outcome banner ("SUCCESS") is the same for every verb, so it
    /// is the only thing a verb test can assert that a wrong verb cannot satisfy.
    /// </summary>
    public string? CliLastExecutedVerbId() => _cliLastExecutedVerbId;

    /// <summary>
    /// A flat, greppable dump of the game state an <see cref="Outcome"/> can change. Read by the
    /// CLI's <c>inspect</c>, which is what makes the <c>cli/outcome/</c> range possible: a chip
    /// proves the player was told something happened, and this proves it actually did.
    ///
    /// <para>Everything is named by <b>stable id</b> — item ids, modus mentis ids, archetype ids —
    /// never by display name, because names come from content and a test that spells one breaks the
    /// day somebody rewrites it. One fact per line, prefixed by subject, so a script asserts with a
    /// plain <c>expect</c>.</para>
    /// </summary>
    public IReadOnlyList<string> CliInspect(string subject)
    {
        var outp = new List<string>();
        bool All(string s) => subject is "all" or "" || subject == s;
        var actor = _activePartyMember ?? (PartyMember)_protagonist;

        if (All("items"))
            foreach (var i in actor.GetAllItems())
                outp.Add($"item {i.ItemId}");

        if (All("coins"))
            outp.Add($"coins gold={_protagonist.Party.Gold} silver={_protagonist.Party.Silver} "
                   + $"copper={_protagonist.Party.Copper}");

        // The phase's budget, and the acting body's implement band. Both appear in the `state` line
        // too, but `state` only emits — `expect` scans the rendered terminal and never sees it, so
        // this is the seam that makes them assertable, exactly as `inspect menu` is for a greyed
        // button. The budget is what proves a refused combination was CHARGED for: the refusal
        // narration reads much like any other failure, so nothing on screen distinguishes a
        // combination that cost a point from one that did not.
        if (All("noetic"))
            outp.Add($"noetic points={_narrationState.ThinkingAttemptsRemaining}/{actor.MaxNoeticPoints} "
                   + $"tool_proficiency={ToolUsageProficiencyStat.Of(actor)}");

        // The open choice popup, with each option's state. `regions` prints the same thing, but only
        // emits — and a greyed option differs from a live one by colour alone, so this is what lets a
        // script assert that a tool refusal leaves "Use Tool" as the only way forward.
        if (All("popup") && _choicePopup.IsVisible)
            outp.Add("popup options=[" + string.Join(",", _choicePopup.Choices.Select((c, i) =>
                         _choicePopup.IsChoiceEnabled(i) ? c : $"{c}(disabled)")) + "]");

        // trespass= is what the header's TRESPASSING tag shows, and the condition under which every act
        // is a crime — assertable, where the tag is only a colour on screen. confronted= is the kind of
        // confrontation the actor is in (ConfrontationSelector.Current), the one the exit button reads.
        // The time box: whether it asks, is passing, or waits on Continue, and between which hours.
        if (All("time"))
            outp.Add($"time box={CliTimeBoxState()} destination={(_timeBox.IsVisible ? _timeBox.Destination.ToString() : "-")}");

        // Every will the actor holds and its path from here (ActionPathPlanner): how many steps, the
        // first to take and the goal, which of its targets it reaches, or none when no target can be
        // reached in this place. What a will
        // observes and proposes is derived from exactly this, so it is the seam a script asserts on.
        if (All("will") && _scene != null && _pov != null)
            foreach (var mm in actor.LearnedModiMentis.Where(m => m.Will != null).OrderBy(m => m.ModusMentisId))
            {
                var path = ActionPathPlanner.Plan(_scene, _pov, actor, mm.Will!);
                outp.Add(path == null
                    ? $"will {mm.ModusMentisId} steps=none"
                    : $"will {mm.ModusMentisId} steps={path.Steps.Count} first=\"{path.First.Verbatim}\" goal=\"{path.Goal.Verbatim}\""
                      + $" target={CliTargetId(path.First.Action.Target)}"
                      + (path.ToolStep >= 0 ? $" tool_step={path.ToolStep + 1}" : "")
                      // Which of the will's targets the path reaches, by declaration order (1 = first), and its purpose.
                      + (path.Target is { } aim ? $" aim={AimOf(mm.Will!, aim)}/{mm.Will!.Count} purpose=\"{aim.Purpose}\"" : ""));
            }

        // A will's targets are built afresh on every read, so the one a path reaches is found by what it is.
        static int AimOf(WillTargets will, WillTarget aim)
            => will.ToList().FindIndex(t => t.Purpose == aim.Purpose && t.Describe() == aim.Describe()) + 1;

        if (All("where"))
        {
            var confrontation = _scene != null && _pov != null ? ConfrontationSelector.Current(_scene, _pov, _protagonist) : null;
            outp.Add($"where area=\"{_pov?.Where.DisplayName ?? "-"}\" period={_pov?.When.ToString() ?? "-"} "
                   + $"day={(int)Cathedral.Game.Narrative.GameClock.Days} trespass={(IsTrespassing ? "yes" : "no")} "
                   + $"confronted={(confrontation == null ? "none" : $"{confrontation.Kind}:{confrontation.Npc.Archetype.ArchetypeId}")}");
        }

        // What the location map knows: every area stood in, every way gone by (AreaVisits). Read from
        // the mental map of the place, so after a revisit this is what the last visit left behind —
        // or nothing, once the place is forgotten.
        if (All("map") && _scene != null)
        {
            foreach (var area in _scene.AllAreas.Where(a => _scene.VisitedAreas.Contains(a.StableKey)))
                outp.Add($"map visited {area.DisplayName}");
            foreach (var (a, b, key, connector) in AreaVisits.AllLinks(_scene).Where(l => _scene.TakenLinks.Contains(l.Key)))
                outp.Add(connector == null
                    ? $"map taken between {string.Join(" and ", new[] { a.DisplayName, b.DisplayName }.OrderBy(n => n, StringComparer.Ordinal))}"
                    : $"map taken {connector.DisplayName}");
            foreach (var area in _scene.AllAreas.Where(a => _scene.SeenAreas.Contains(a.StableKey)
                                                         && !_scene.VisitedAreas.Contains(a.StableKey)))
                outp.Add($"map seen {area.DisplayName}");
            if (_lastWalk != null) outp.Add($"map {_lastWalk}");
        }

        if (All("party"))
            foreach (var c in _protagonist.CompanionParty)
                outp.Add($"companion \"{c.DisplayName}\" species=\"{c.PartyDescription}\"");

        if (All("opening"))
            outp.Add(_lastOpeningFocus ?? "opening focus=none");

        // Which conversation was last queued, by tree id — the confrontation's id carries the crime it
        // accuses you of (caught_red_handed_murderer), which nothing on screen states plainly.
        if (All("dialogue"))
            outp.Add($"dialogue last={_lastDialogueTreeId}");

        // Learned routines — see Routine.CliLine. Reported here rather than read off the routines panel
        // because recording happens with no UI open at all: the panel is where a player reads them,
        // not where they are made.
        if (All("routines"))
            foreach (var r in _protagonist.RecordedRoutines)
                outp.Add(r.CliLine());

        if (All("wounds"))
            foreach (var w in actor.Wounds)
                outp.Add($"wound {w.WoundName} target={w.TargetId}");

        // The four humor queues by composition, newest first. This is the only assertable proof that
        // an emotion reached the spleen: the chip says "Voluptas × 4" and `expect` can scan for that,
        // but a chip proves what the player was TOLD — the queue is what actually moved. Reported as
        // counts per humor name rather than slot by slot, because a queue is 49 slots deep and a
        // script cares that N instances arrived, not where they landed.
        //
        // All four queues, not just the spleen, because the same line is what makes eating and the
        // travel drain assertable later, and a subject that answers only the question of the day
        // would need widening the first time anything else wanted it.
        if (All("humors"))
            foreach (var q in actor.HumorQueues.All)
            {
                var counts = q.Items.GroupBy(h => h.Name)
                                    .OrderByDescending(g => g.Count()).ThenBy(g => g.Key)
                                    .Select(g => $"{g.Key}={g.Count()}");
                outp.Add($"humors {q.OrganId} {string.Join(" ", counts)}");
            }

        // xp as well as level: practice is the commonest thing a verb does and the slowest to show,
        // since a level takes several successes. Without the raw count a script cannot tell a modus
        // mentis that was paid for its work from one that was not — which is how the Get-Up phase
        // came to award none at all without a single test noticing.
        // xp as well as level, and `effective` as well as `level`: a wound caps what a modus mentis
        // may reach without touching the level it stored, so the stored number alone cannot say
        // whether the thing can still be used. `broken=` is the flag every test in this area asserts
        // — it is the whole of the rule, and deriving it in a script from two numbers would be a
        // paraphrase of BrokenModusMentis rather than a reading of it.
        if (All("skills"))
            foreach (var m in actor.ModiMentis)
                outp.Add($"skill {m.ModusMentisId} level={m.Level} xp={m.CurrentXp}"
                       + $" effective={actor.GetEffectiveModusMentisLevel(m)}"
                       + $" max={actor.GetMaxLevelForModusMentis(m)}"
                       + $" broken={actor.IsModusMentisBroken(m)}");

        if (All("npcs") && _scene != null && _pov != null)
            foreach (var npc in _scene.Npcs)
            {
                string key   = _protagonist.AffinityKey;
                string where = _scene.GetAreaOf(npc, _pov.When)?.DisplayName ?? "-";
                var ent = npc.Entity as Cathedral.Game.Npc.NpcEntity;
                outp.Add($"npc {npc.Entity.Archetype.ArchetypeId} alive={npc.Entity.IsAlive}"
                       + (ent != null ? $" affinity={ent.AffinityTable.GetLevel(key)}"
                                      + $" enemy={ent.AffinityTable.IsEnemy(key)}"
                                      // What a blow does to them. Hit points are derived from the
                                      // wound count, so the pair is one fact read two ways — but a
                                      // test asserting the first blow landed needs the count, and
                                      // one asserting it was withheld from a dying man needs the
                                      // hit points. Nothing else surfaces either.
                                      + $" hp={ent.Combatant.CurrentHp}/{ent.Combatant.MaxHp}"
                                      + $" wounds={ent.Combatant.Wounds.Count}"
                                      // What a conversation's outcomes write. None of it is visible
                                      // any other way, and it is the whole state change of half the
                                      // catalogue: join_party, alms, the trade and job menus, the
                                      // goad into a fight, the introduction.
                                      + $" roused={npc.Roused}"
                                      + $" join={ent.JoinRequested}"
                                      + $" alms={ent.AlmsGiven}"
                                      + $" trade={ent.TradeRequest}"
                                      + $" job={(ent.JobRequest != null)}"
                                      + $" goaded={ent.FightRequestedByDialogue}"
                                      + $" introduced={(ent.IntroductionGranted != null)}"
                                      // What they sell, as the trade menu would list it — which
                                      // shelf holds what is otherwise only seen by trading.
                                      + (ent.BuyCatalog is { } shelf
                                          ? " sells=" + string.Join(",", shelf.Offers.Select(o => $"{o.Prototype.ItemId}:{o.UnitPrice}{o.Coin.ToString()[0]}"))
                                          : "") : "")
                       + $" area=\"{where}\"");
            }

        if (All("pois") && _pov != null)
            foreach (var poi in _pov.Where.PointsOfInterest)
                outp.Add($"poi \"{poi.DisplayName}\" lemma={poi.ReferenceLemma} items={poi.Items.Count}"
                       + (poi is Cathedral.Game.Scene.Building.DoorPointOfInterest d ? $" door={d.DoorState}" : ""));

        return outp;
    }

    private string? _cliLastExecutedVerbId;

    public string? CliClickKeyword(string keyword)
    {
        var regions = _ui.KeywordRegions;

        KeywordRegion? region;
        if (int.TryParse(keyword, out int index))
        {
            if (index < 0 || index >= regions.Count)
                return $"no keyword {index} on screen ({regions.Count} offered)";
            region = regions[index];
        }
        else
        {
            region = CliFindKeyword(regions, keyword);
            if (region == null) return $"no clickable keyword '{keyword}' on screen";
        }

        OnMouseMove(region.StartX, region.Y);
        OnMouseClick(region.StartX, region.Y);
        return null;
    }

    /// <summary>
    /// A keyword by its word — or, written <c>@name</c>, by the object it is linked to: the first
    /// keyword whose anchor's name contains <c>name</c>. The word is whatever the model wrote and the
    /// index is whatever order it wrote things in; the object is the scene's, so a script that must
    /// reach one thing whatever the prose says names it this way.
    /// </summary>
    private static KeywordRegion? CliFindKeyword(IReadOnlyList<KeywordRegion> regions, string keyword)
    {
        if (!keyword.StartsWith('@'))
            return regions.FirstOrDefault(r => r.Keyword.Equals(keyword, StringComparison.OrdinalIgnoreCase));
        string want = keyword[1..].Replace('_', ' ');
        return regions.FirstOrDefault(r => r.ResolvedAnchor is { } a && CliAnchorNames(a)
            .Any(n => n.Replace('_', ' ').Contains(want, StringComparison.OrdinalIgnoreCase)));
    }

    private static IEnumerable<string> CliAnchorNames(NarrativeAnchor anchor)
    {
        yield return anchor.DisplayName;
        if (anchor is ObservationObject obs)
        {
            yield return obs.NeutralName;
            yield return obs.ObservationId;
            foreach (var alias in obs.TargetingAliases) yield return alias;
        }
    }

    /// <summary>Click the action with the given global action index.</summary>
    public string? CliClickAction(int actionIndex)
    {
        var region = _ui.ActionRegions.FirstOrDefault(r => r.ActionIndex == actionIndex);
        if (region == null) return $"no action {actionIndex} on screen";
        OnMouseMove(region.StartX, region.StartY);
        OnMouseClick(region.StartX, region.StartY);
        return null;
    }

    // ── --record: where a player would click ─────────────────────────────────

    /// <summary>The narration's dice box, when a roll is on screen.</summary>
    public DiceRollComponent? CliDice => _narrationState.IsDiceRollActive ? _dice : null;
    // The recorder drives the game through the real pointer path, so instead of acting it needs to
    // know WHERE each thing is. Each lookup resolves its target exactly as the matching Cli* click
    // above does, and returns the cells (or, for popups, the pixel) a player would aim at.

    /// <summary>The cells a keyword occupies, resolved as <see cref="CliClickKeyword"/> resolves it.</summary>
    public (int X, int Y, int Width)? CliKeywordSpan(string keyword)
    {
        var regions = _ui.KeywordRegions;
        KeywordRegion? region = int.TryParse(keyword, out int index)
            ? (index >= 0 && index < regions.Count ? regions[index] : null)
            : CliFindKeyword(regions, keyword);
        return region == null ? null : (region.StartX, region.Y, Math.Max(1, region.EndX - region.StartX));
    }

    /// <summary>The first row of an action button, resolved as <see cref="CliClickAction"/> resolves it.</summary>
    public (int X, int Y, int Width)? CliActionSpan(int actionIndex)
    {
        var region = _ui.ActionRegions.FirstOrDefault(r => r.ActionIndex == actionIndex);
        return region == null ? null : (region.StartX, region.StartY, Math.Max(1, region.EndX - region.StartX));
    }

    /// <summary>
    /// A screen pixel that picks choice <paramref name="index"/> of the popup on screen, found by asking
    /// that popup's own hit-test — the same one a click is routed through, in the same priority order
    /// as <see cref="OnRawMouseClick"/>. Null when no popup is up or no pixel picks that choice.
    /// </summary>
    public OpenTK.Mathematics.Vector2? CliPopupChoicePixel(int index)
    {
        var bounds = _core.PopupTerminal?.GetScreenBounds(_core.ClientSize);
        if (bounds == null) return null;

        Func<float, float, OpenTK.Mathematics.Vector2i, float, int?>? hit =
              _choicePopup.IsVisible        ? _choicePopup.CliIndexAt
            : _itemSelectionPopup.IsVisible ? _itemSelectionPopup.CliIndexAt
            : _modusMentisPopup.IsVisible   ? _modusMentisPopup.CliIndexAt
            : null;
        if (hit == null) return null;

        float cell = _terminalInputHandler.GetLayoutInfo(_core.ClientSize).CellSize.X;
        var (left, top, right, bottom) = bounds.Value;
        float x = left + Math.Min(right - left, cell * 8f);
        for (float y = top; y <= bottom; y += cell * 0.25f)
            if (hit(x, y, _core.ClientSize, cell) == index)
            {
                // Aim at the middle of the row rather than its first hit pixel.
                float y2 = y;
                while (y2 + 1 <= bottom && hit(x, y2 + 1, _core.ClientSize, cell) == index) y2 += 1;
                return new OpenTK.Mathematics.Vector2(x, (y + y2) * 0.5f);
            }
        return null;
    }

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
            _narrationState.ActionPendingReconsider = null;
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
    public FightTriggerOutcome? PendingFightOutcome => _pendingFightOutcome;
    
    /// <summary>
    /// Check if a dialogue outcome is pending (NarrativeController wants to enter dialogue mode).
    /// </summary>
    public DialogueTriggerOutcome? PendingDialogueOutcome => _pendingDialogueOutcome;
    
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
    /// Whether a fight has been decided and is waiting on the CONTINUE press — read by the CLI's
    /// <c>state</c>, since a held fight is indistinguishable on screen from an ordinary resolved
    /// action and a script that does not press CONTINUE would sit out its timeout wondering why
    /// Fighting never arrived.
    /// </summary>
    public bool HasDeferredFight => _deferredFightOutcome != null;

    /// <summary>
    /// Lets the session's recorder learn its last routine — where the player is leaving from. Called
    /// by the game controller when the narration phase ends (returns to world travel).
    /// </summary>
    public void FinalizeRoutineRecording()
    {
        _recorder?.FinalizeAtNarrationEnd(_pov);
        _recorder = null;
    }

    /// <summary>
    /// Starts narration positioned at a specific area and time period — a Go to or Meet routine.
    /// Falls back to the opening area when the area cannot be resolved.
    /// </summary>
    /// <param name="focusNpcId">The <see cref="INpcEntity.PersistentId"/> of the person the first
    /// observation is of (a Meet routine), or null for the persona's own choice.</param>
    public void StartAtArea(string areaKey, TimePeriod time, string? focusNpcId = null)
    {
        // Only the area is set here — StartObservationPhase(time) below routes the period through
        // ApplyTimePeriod, the single writer of PoV.When + graph period.
        _approachFrom = _pov?.Where;
        MovePointOfViewToArea(areaKey);
        var focus = focusNpcId == null
            ? null
            : _scene?.Npcs.FirstOrDefault(n => n.IsAlive && n.Entity.PersistentId == focusNpcId);
        StartObservationPhase(time, focus);
        // Shown while the opening observation is written behind it: the walk is drawn ahead of the
        // preview box, which comes up once its Continue is pressed.
        WalkTheApproach(then: null);
    }

    /// <summary>Where the location is entered, kept by a routine's move to its own area for <see cref="WalkTheApproach"/>.</summary>
    private Area? _approachFrom;

    /// <summary>
    /// A routine opens in its own area — "you know the way" — and this shows the way: the party
    /// walking on the map from where the location is entered to the routine's area, by the ways the
    /// protagonist knows (<see cref="AreaVisits.MoveBack"/>, a backtrack's route; a jump, card to card,
    /// when the map holds none at that hour). Nothing is rolled and nothing new is learned; the walk is
    /// the routine's own, shown. <paramref name="then"/> runs on its Continue — at once when the
    /// routine's area is where the location is entered, and there is nothing to walk.
    /// </summary>
    public void WalkTheApproach(Action? then)
    {
        var from = _approachFrom;
        _approachFrom = null;
        if (from == null || _scene == null || _pov == null || from.Id == _pov.Where.Id) { then?.Invoke(); return; }

        ShowWalk(from, AreaVisits.MoveBack(_scene, from, _pov.Where, _pov.When), fresh: new());
        if (_walk == null) { then?.Invoke(); return; }
        _afterWalk = then;
    }

    /// <summary>
    /// Repositions the point of view (and the current node with it) onto the area named by
    /// <paramref name="areaKey"/>. No-op on null, or when nothing matches — a routine recorded
    /// before a factory changed should still open somewhere.
    ///
    /// <para>Matched by <b>display name</b> first, because that is the only per-location unique
    /// area identifier: <c>ReferenceLemma</c> is a generic noun, and <c>BuildingFactory</c> gives
    /// every building's roof the lemma "roof" while prefixing only the display name with the
    /// building. Resuming a village routine by lemma could therefore land on the wrong building's
    /// room. <c>--building-audit</c> is what guarantees the display names do not collide (it fails on
    /// two areas sharing a node-id slug). Lemma is kept as a fallback so a key recorded against an
    /// older build still finds something.</para>
    ///
    /// <para>By name rather than by instance because every caller is resuming into a <b>freshly
    /// rebuilt</b> scene: the <c>Area</c> the routine ended on belongs to a scene that has already
    /// been thrown away. Shared by the two resume paths (narration and the routine sub-phase) so
    /// they cannot disagree about how an area is found.</para>
    /// </summary>
    private void MovePointOfViewToArea(string? areaKey)
    {
        if (string.IsNullOrWhiteSpace(areaKey) || _scene == null || _pov == null) return;

        var area = _scene.AllAreas.FirstOrDefault(a =>
                       string.Equals(a.DisplayName, areaKey, StringComparison.OrdinalIgnoreCase))
                ?? _scene.AllAreas.FirstOrDefault(a =>
                       string.Equals(a.ReferenceLemma, areaKey, StringComparison.OrdinalIgnoreCase));
        if (area == null)
        {
            Console.Error.WriteLine($"NarrativeController: no area matching '{areaKey}' in the rebuilt "
                                  + $"scene — staying at '{_pov.Where.DisplayName}'");
            return;
        }

        _pov.Where = area;
        if (NodeForArea(area) is { } node)
            _currentNode = node;
        Console.WriteLine($"NarrativeController: point of view resumed at '{area.DisplayName}'");
    }

    /// <summary>
    /// Takes the player to the person a go-between has just agreed to present them to, and makes the
    /// phase that follows open on <b>that</b> person. Returns false when they are nowhere to be found
    /// at this hour — the reeve keeps his own times — in which case the standing still stands and the
    /// walk simply does not happen. Being vouched for does not conjure somebody into a room.
    ///
    /// <para>Three writes, and it was missing two of them. Moving <c>PoV.Where</c> alone is not
    /// moving: the observation phase runs against <c>_currentNode</c>, so the point of view said one
    /// area while everything the player could see and act on still came from the old one — the walk
    /// happened on paper and nowhere else. And the opening focus was still the go-between, set when
    /// the conversation was triggered, so the first observation after being introduced to the reeve
    /// was of the bondman who introduced you.</para>
    ///
    /// <para>Lives here rather than in the game controller because the PoV, the node and the opening
    /// focus are this class's invariants; reaching in to set one of the three from outside is what let
    /// them drift apart.</para>
    /// </summary>
    public bool WalkToIntroducedNpc(NpcEntity presented)
    {
        if (_scene == null || _pov == null) return false;

        var sceneNpc = _scene.Npcs.FirstOrDefault(n => ReferenceEquals(n.Entity, presented));
        if (sceneNpc == null) return false;

        var where = _scene.GetAreaOf(sceneNpc, _pov.When);
        if (where == null)
        {
            Console.WriteLine($"NarrativeController: {presented.DisplayName} is not about at {_pov.When} — "
                            + "introduction stands, but no walk");
            return false;
        }

        MoveOnMap(_pov.Where, where, by: null);
        _pov.Where = where;
        _pov.Focus = sceneNpc;
        if (NodeForArea(where) is { } node)
            _currentNode = node;

        // The phase that follows opens on the person just introduced, not on the go-between — that
        // is what makes the introduction read as an arrival, and puts the conversation it unlocked
        // one click away.
        _openingFocus = sceneNpc;

        Console.WriteLine($"NarrativeController: walked to {presented.DisplayName} in {where.DisplayName} "
                        + $"(node '{_currentNode.NodeId}') — next observation is of them");
        return true;
    }

    /// <summary>
    /// Takes the player to the premises of a trader who agreed to lead them there, at the first hour
    /// the trader keeps them (<see cref="TradeGate.NextOnPremises"/>) — now if they are already there
    /// by then, else later in the day, the time passing as it does when the player waits. The phase
    /// that follows opens on the trader, as an introduction's does on the person introduced.
    /// Returns false, and moves nobody, when the trader's day never brings them there.
    /// </summary>
    public bool WalkToPremises(NpcEntity trader)
    {
        if (_scene == null || _pov == null) return false;

        var sceneNpc = _scene.Npcs.FirstOrDefault(n => ReferenceEquals(n.Entity, trader));
        if (sceneNpc == null || TradeGate.NextOnPremises(_scene, sceneNpc, _pov.When) is not { } visit)
        {
            Console.WriteLine($"NarrativeController: {trader.DisplayName} keeps no premises at any hour — no walk");
            return false;
        }

        if (visit.When != _pov.When)
        {
            Console.WriteLine($"NarrativeController: waiting until {visit.When} for {trader.DisplayName}'s premises");
            TurnTheDayIfWrapped(_pov.When, visit.When);
            ShowTimePassage(_pov.When, visit.When);
            ApplyTimePeriod(visit.When);
        }

        MoveOnMap(_pov.Where, visit.Area, by: null);
        _pov.Where = visit.Area;
        _pov.Focus = sceneNpc;
        if (NodeForArea(visit.Area) is { } node)
            _currentNode = node;
        _openingFocus = sceneNpc;

        Console.WriteLine($"NarrativeController: led by {trader.DisplayName} to {visit.Area.DisplayName} "
                        + $"at {visit.When} — next observation is of them");
        return true;
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

        // An enemy in sight outranks a witness; a witness confronting you is one who came in here
        // after you, or anyone in sight on ground you have no business standing on.
        // ConfrontationSelector is the one place that is decided, so the exit, the rules of action
        // and the failure ladder cannot disagree about who is watching.
        return ConfrontationSelector.Current(_scene, _pov, _protagonist) switch
        {
            { Kind: ConfrontationKind.Enemy } c => (ExitButtonKind.RunawayEnemy, c.Npc),
            { } c                               => (ExitButtonKind.RunawayWitness, c.Npc),
            null                                => (ExitButtonKind.Leave, null),
        };
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
    /// <summary>Separator caption for the segment a resolved action closes.</summary>
    private static string? SegmentLabelFor(ActionExecutionResult result)
        => string.IsNullOrWhiteSpace(result.Action?.NeutralActionText)
            ? null
            : $"after trying to {result.Action.NeutralActionText}";

    private void HandleContinueClicked()
    {
        // A fight held back so its opening blow could be read (see _deferredFightOutcome). First,
        // because the segment it closes is the one the fight itself will grey into history.
        if (_deferredFightOutcome != null)
        {
            Console.WriteLine($"NarrativeController: CONTINUE — entering the held fight with {_deferredFightOutcome.Target.DisplayName}");
            _pendingFightOutcome  = _deferredFightOutcome;
            _deferredFightOutcome = null;
            _pendingSegmentLabel     = null;
            _pendingSegmentSucceeded = true;
            return;
        }

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
            // Carry the same caption the in-place branch below uses. This dropped it, so a segment
            // closed by an action that MOVED you — every move, climb, crossing and door — got the
            // unlabelled rule while one closed in place got "after trying to …". Two different
            // separators for the same event, decided by whether the action happened to relocate you.
            BeginNarrationSegment(_pendingSegmentLabel, refillNoetic: _pendingSegmentSucceeded);
            _pendingSegmentLabel     = null;
            _pendingSegmentSucceeded = true;
            return;
        }

        // A movement/leave action requested exit-on-continue (the LEAVE button is the primary exit
        // now, but honor an explicit movement-exit action too).
        if (_narrationState.ShouldExitOnContinue)
        {
            Console.WriteLine("NarrativeController: CONTINUE — the way lost (or a movement action), exiting to world view");
            _narrationState.RequestedExit = true;
            return;
        }

        // Nothing pending: the resolved action closes the segment — grey the text into history under
        // a labelled separator, refill noetic points, and open a fresh observation of the scene as it
        // now stands (the outcome may have changed it: item gone, state shifted). This makes CONTINUE
        // behave identically for in-place actions and transitions.
        Console.WriteLine("NarrativeController: CONTINUE — closing segment after action, restarting observations"
                        + (_pendingSegmentSucceeded ? "" : " (failed action — noetic points not refilled)"));
        BeginNarrationSegment(_pendingSegmentLabel, refillNoetic: _pendingSegmentSucceeded);
        _pendingSegmentLabel     = null;
        _pendingSegmentSucceeded = true;
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
        // The humors reach this roll as they reach every other: the dice are the protagonist's, so are
        // the queues. Without the member the overlay offered no humor buttons at all.
        NarrationDiceStart(diceCount, 1, humorMember: _protagonist, subtitle: "RUNAWAY CHECK — feet", difficultyVerb: "to flee");
        _narrationState.LoadingMessage = Config.LoadingMessages.RollingDice;

        // Fixed animation window (no async work backs this roll, unlike thinking checks).
        await Task.Delay(Config.Dice.AnimationDurationMs);

        var values = new int[diceCount];
        for (int i = 0; i < diceCount; i++) values[i] = _diceRandom.Next(1, 7);

        // --debug's preset strategy reaches this roll as it reaches the action and conversation rolls,
        // so a script can test both ends of a RUNAWAY rather than whichever one its seed lands on.
        if (DebugMode.IsActive && !DebugMode.IsAutoStrategy)
        {
            if (DebugMode.CurrentStrategy == DebugStrategy.FailDiceRoll)
                for (int i = 0; i < values.Length; i++) { if (values[i] == 6) values[i] = 5; }
            else if (DebugMode.CurrentStrategy == DebugStrategy.Succeed && values.Length > 0)
                values[0] = 6;
        }
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
            _pendingFightOutcome = new FightTriggerOutcome(target, $"failed to run away from {target.DisplayName}");
        }
        else
        {
            // They came because they heard a crime: they accuse you of that one. Otherwise they caught you
            // on private ground, and what they saw is trespass.
            var catchTree = ConfrontationSelector.TreeFor(_scene, target, null, _pov?.Where.IsPrivate ?? true);
            Console.WriteLine($"NarrativeController: RUNAWAY failed — witness '{target.DisplayName}' confronts ({catchTree.TreeId})");
            // A runaway confrontation has no observation→thinking→action origin — resample the narrator.
            SetPendingDialogue(new DialogueTriggerOutcome(target, tree: catchTree), null);
        }
    }
    
    /// <summary>
    /// Called by the game controller when returning from fight mode.
    /// Handles corpse spawning (victory), enemy affinity (runaway), and narration resumption.
    /// </summary>
    public void OnFightCompleted(
        Fight.FightAdapterResult result,
        NpcEntity npc,
        IReadOnlyList<NpcEntity>? allEnemyNpcs = null,
        IReadOnlyList<string>? combatLog = null)
    {
        Console.WriteLine($"NarrativeController: Fight completed with result {result} vs {npc.DisplayName}");

        var enemies = allEnemyNpcs ?? new List<NpcEntity> { npc };

        if (result == Fight.FightAdapterResult.Victory)
        {
            // A body — and, for a human, their belongings — for every enemy that fell. Each one
            // queues itself on the scene as it is added, and the narration phase that opens next
            // observes exactly those, in the order they were spawned here.
            Cathedral.Game.Scene.PointOfInterest? mainCorpse = null;
            foreach (var enemy in enemies)
            {
                if (enemy.IsAlive || _scene == null || _pov == null) continue;

                foreach (var remains in enemy.GenerateCorpse())
                {
                    _scene.AddPointOfInterestToArea(_pov.Where, remains);
                    if (enemy == npc) mainCorpse ??= remains;
                }

                // Out of play for good — the same door the slay verb and the two recruit routes use,
                // so a location does not stand its dead back up on the next visit. An enemy with no
                // SceneNpc (a --start-fight creature) was never in the scene and needs no removal.
                var fallen = _scene.Npcs.FirstOrDefault(n => ReferenceEquals(n.Entity, enemy));
                if (fallen != null) _scene.RemoveNpcFromPlay(fallen);

                Console.WriteLine($"NarrativeController: Corpse spawned for {enemy.DisplayName}");
            }

            // Focus on the main enemy's body so the debug view opens on what just happened.
            if (mainCorpse != null && _pov != null)
            {
                _pov.Focus = mainCorpse;
                SceneDebugManager.UpdatePoV(_pov);
            }

            // Drop the slain NPCs from their nodes immediately: re-placing for the current period
            // removes anyone no longer alive (Scene.GetNpcsAt filters the dead), so a defeated NPC
            // can't still be observed standing where their corpse now lies.
            _npcPlacement?.PlaceForPeriod(_graph.CurrentPeriod);
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

        // Archive the tail of the combat log so the fight leaves a scrollable trace. One block, not
        // one per line: the buffer re-wraps every block on each append, and WrapText already splits
        // on newlines. Only the closing exchange is kept — the rest was always ephemeral.
        if (combatLog is { Count: > 0 })
        {
            const int MaxCombatLogLines = 12;
            var tail = combatLog.Skip(Math.Max(0, combatLog.Count - MaxCombatLogLines));
            string logText = (combatLog.Count > MaxCombatLogLines ? "…\n" : "") + string.Join("\n", tail);
            _scrollBuffer.AddBlock(new NarrationBlock(
                Type: NarrationBlockType.Outcome,
                ModusMentis: null!,
                Text: logText,
                Keywords: null,
                Actions: null));
        }

        string outcomeText = result switch
        {
            Fight.FightAdapterResult.Victory => $"You defeated {npc.DisplayName}.",
            Fight.FightAdapterResult.Runaway => $"You fled from {npc.DisplayName}.",
            Fight.FightAdapterResult.Death => $"You were slain by {npc.DisplayName}.",
            _ => "The fight ended."
        };

        // Add outcome to scroll buffer. No modus mentis: this is a system note, not an MM's
        // narration — attaching one would print its skill header without any LLM involvement.
        var block = new NarrationBlock(
            Type: NarrationBlockType.Outcome,
            ModusMentis: null!,
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

        // A householder who found you waiting and let you stay: the house is yours to wait in for the
        // rest of the visit. Read before the release below forgets who found you.
        if (_scene != null && _pov != null && npc.ToleratesTrespass)
        {
            npc.ToleratesTrespass = false;   // consume the flag
            if (ConfrontationSelector.IsFoundWaitingBy(_scene, npc) && _scene.SectionOf(_pov.Where) is { } house)
            {
                _scene.ToleratedSections.Add(house);
                Console.WriteLine($"NarrativeController: {npc.DisplayName} lets the player stay — {house.DisplayName} tolerated for the visit");
            }
        }

        // A witness who came for you has had their say: the confrontation is over.
        if (_scene != null) ConfrontationSelector.Release(_scene, npc, _protagonist);
        
        // No modus mentis: a system note — an MM here would print its skill header (e.g.
        // "[DISCIPLINE ▪]") before the segment separator without any LLM involvement.
        var block = new NarrationBlock(
            Type: NarrationBlockType.Outcome,
            ModusMentis: null!,
            Text: $"You finished talking with {Cathedral.Game.Npc.NpcLabelResolver.ShortLabel(npc)}.",
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

    /// <summary>
    /// The tools that may be combined with an action. <see cref="ItemCategory.Tool"/> and
    /// <see cref="ItemCategory.Weapon"/> qualify — a weapon is a tool too, just a specialised and
    /// often clumsy one (see <c>WeaponItem.UsageLevel</c>). Garments, food and raw material are
    /// excluded: they are things you wear, eat or own, not things you work with.
    ///
    /// <para><b>Plus anything declaring <see cref="Item.MadeForVerbIds"/>, whatever its
    /// category.</b> An item made for an act is by definition something you work with, and the
    /// exception would otherwise be unreachable for everything that is not already a tool — which is
    /// most of what wants one. Reading lenses are a garment by category and are the whole reason the
    /// mechanism exists.</para>
    ///
    /// Location is deliberately not a factor — anything carried is usable, whether it is in hand
    /// or at the bottom of a pack. A container holding something is excluded, since combining it
    /// with an action would mean putting its contents down.
    /// </summary>
    private List<Item> GetCombinableItems()
    {
        return _activePartyMember.GetAllItems()
            .Where(i => i.Category is ItemCategory.Tool or ItemCategory.Weapon
                     || i.MadeForVerbIds.Count > 0)
            .Where(i => i is not IContainer c || c.Contents.Count == 0)
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
        // Nothing is attempted yet — the critic is deciding whether the item helps at all, and the
        // action it may reword is still waiting behind its own button.
        _narrationState.IsLoadingAction = true;
        _narrationState.LoadingMessage = Config.LoadingMessages.CombiningItem;

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

            // Lower-cased with an article, never the bare display name: the critic repeats whatever it is
            // shown, and its reason is read into the refusal ("I think Hairpin is too narrow").
            string itemContext = $"{item.WithArticle()} ({item.Description})";
            Console.WriteLine($"NarrativeController: Item combination — action='{action.DisplayText}', item='{itemContext}'");

            var gatedVerb    = action.PreselectedOutcome?.Verb;
            var proficiency  = ToolUsageProficiencyStat.Of(_activePartyMember);
            var gate         = ToolCombinationRules.Resolve(gatedVerb, item, proficiency);
            Console.WriteLine($"NarrativeController: Tool gate — proficiency={proficiency}, verb={gatedVerb?.VerbId ?? "none"} ({gatedVerb?.ToolUse.ToString() ?? "n/a"}), gate={gate}");

            // Three of the four gates settle it here, with no request made at all. The fourth is the
            // only one that was ever worth a critic call.
            //
            // How the refusal is worded is decided alongside it, because the four failures are four
            // different pieces of news and a single "it did not work" makes the acting modus mentis
            // invent which one it was.
            bool appropriatenessSuccess;
            ToolFailureKind failureKind = ToolFailureKind.None;
            string criticReason = "";

            if (gate == ToolCombinationGate.AskTheCritic)
            {
                var goalDescription = action.PreselectedOutcome?.ToNaturalLanguageString() ?? "";
                var criticContext = new CriticContext(_currentNode, _worldContext, _locationId, goalDescription);
                criticContext.CombinedItemContext = itemContext;

                // A required verb asks a different question: not "does this beat bare hands" (bare
                // hands are not an option there) but "can this do the work of the tool this needs".
                var appropriatenessTree = gatedVerb is { RequiresTool: true }
                    ? CriticTrees.BuildToolSubstitutionTree(
                          goalDescription, CriticTrees.ToolPhrase(gatedVerb.ReferenceToolIds),
                          NeutralNarration.ItemPhrase(item.DisplayName), criticContext)
                    : CriticTrees.BuildItemAppropriatenessTree(goalDescription, NeutralNarration.ItemPhrase(item.DisplayName), criticContext);
                var appropriatenessResult = await _actionExecutor.ItemUseCritic.EvaluateTreeAsync(appropriatenessTree);

                // The critic judges the implement; the body judges itself. A verdict it returns as
                // passing may still be out of this body's reach, and that is a different refusal from
                // "wrong tool" — the wording has to distinguish them or the rewrite blames the item.
                string verdict = appropriatenessResult.Trace.LastOrDefault()?.ChosenId ?? "";
                bool itemAcceptable = appropriatenessResult.OverallSuccess;
                appropriatenessSuccess = itemAcceptable
                                      && ToolCombinationRules.VerdictClears(verdict, proficiency);

                if (!appropriatenessSuccess)
                {
                    // The item passed and the hands did not: a different refusal from "wrong tool",
                    // and the one the player can do something about by taking a level.
                    failureKind  = itemAcceptable ? ToolFailureKind.BeyondSkill : ToolFailureKind.WrongTool;
                    criticReason = appropriatenessResult.CombinedFailureReason;
                    Console.WriteLine($"NarrativeController: Item verdict '{verdict}' needs "
                        + $"{ToolCombinationRules.RequiredFor(verdict)?.ToString() ?? "a band no body reaches"}"
                        + $", body has {proficiency} → {failureKind}");
                }
                else
                {
                    Console.WriteLine($"NarrativeController: Item verdict '{verdict}' cleared at {proficiency}.");
                }
            }
            else
            {
                appropriatenessSuccess = gate == ToolCombinationGate.MadeForIt;
                failureKind = gate switch
                {
                    ToolCombinationGate.NoProficiency => ToolFailureKind.NoProficiency,
                    ToolCombinationGate.NotItsPurpose => ToolFailureKind.NotItsPurpose,
                    ToolCombinationGate.ExcludedVerb  => ToolFailureKind.Senseless,
                    ToolCombinationGate.NotAWeapon    => ToolFailureKind.NotAWeapon,
                    _                                 => ToolFailureKind.None,
                };
                Console.WriteLine($"NarrativeController: Settled without a critic call — {gate}.");
            }

            // A failed combination costs a noetic point; an accepted one is free, being part of the
            // act already paid for. Charging for the accepted case is what made every required verb
            // impossible once the pool was cut to a third of the encephalon. Choosing an implement
            // that can actually serve is the player's side of that bargain.
            //
            // Exempt in childhood and get-up for the same reason every other spend is: those phases
            // do not run on the pool at all, and both of their verbs are Excluded — so without this
            // they would be the one place a combination could be charged for in a budget that is
            // not being kept. The counter is not saved here; only the Speak-About hand-off persists
            // it, because that is the only point at which the acting member changes.
            bool phaseKeepsBudget = _scene?.Phase != NarrationPhase.ChildhoodReminescence
                                 && _scene?.Phase != NarrationPhase.GetUp;
            if (!appropriatenessSuccess && phaseKeepsBudget && _narrationState.ThinkingAttemptsRemaining > 0)
                _narrationState.ThinkingAttemptsRemaining--;

            if (appropriatenessSuccess)
            {
                Console.WriteLine($"NarrativeController: Item '{item.DisplayName}' approved — generating reasoning then reformulating.");

                // Both the reasoning and the reformulated action stream into one preview box.
                _previewSession.Reset();
                var itemPart = _previewSession.BeginAccumulatingPart(PreviewTitles.For(actionModusMentis));

                // ── Step 1: reasoning (how does the item help?) ─────────────────
                string? reasoningText = await _thinkingExecutor.ExecuteItemReasoningAsync(
                    action, item, _currentNode, _protagonist, _worldContext, preview: itemPart?.NextSegment());
                if (string.IsNullOrWhiteSpace(reasoningText))
                    reasoningText = $"I could use {item.DisplayName} to help with this.";

                // ── Step 2: reformulation (rewrite the action incorporating the item) ──
                string? reformulatedText = await _thinkingExecutor.ExecuteItemReformulationAsync(
                    action, item, _currentNode, _protagonist, _worldContext, preview: itemPart?.NextSegment());
                if (string.IsNullOrWhiteSpace(reformulatedText))
                    reformulatedText = action.DisplayText;

                // ── Step 3: build the combined action ────────────────────────────
                // Chain leaf: a synthetic ModusMentis carrying item name + usage level so that:
                //   - the action button shows [ItemName ◼◼] instead of [ActionSkill ◼◼◼]
                //   - GetTotalModusMentisLevel() = obs.Level + thinking.Level + action.Level + usage
                // The level is the implement's own, whole. The hands had their say already, and it
                // was a harder one: the proficiency band decided whether this combination was allowed
                // to happen at all. Capping the dice on top of that taxed one organ twice for one act.
                var itemModusMentis = new SyntheticItemModusMentis(item.ItemId, item.DisplayName, item.UsageLevel);

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

                // Deferred commit: reveal the reasoning block (and its reformulated action button) on CONTINUE.
                void CommitItemReasoning()
                {
                    _scrollBuffer.AddBlock(reasoningBlock);
                    _narrationState.AddBlock(reasoningBlock);
                    _scrollBuffer.ScrollToBottom();
                    _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;
                }
                if (itemPart != null)
                {
                    itemPart.AttachCommit(CommitItemReasoning);
                    itemPart.MarkComplete();
                    _previewSession.EndProduction();
                }
                else CommitItemReasoning();
            }
            else
            {
                Console.WriteLine($"NarrativeController: Item '{item.ItemId}' rejected ({failureKind}) — narrating failure.");

                // The failure explanation streams into a preview box.
                _previewSession.Reset();
                var itemFailPart = _previewSession.BeginPart(PreviewTitles.For(actionModusMentis));
                string failureNarration = await _actionExecutor.OutcomeNarrator.NarrateItemCombinationFailureAsync(
                    action, item, actionModusMentis, criticReason, failureKind, preview: itemFailPart?.Sink);

                var failureBlock = new NarrationBlock(
                    Type: NarrationBlockType.Outcome,
                    ModusMentis: actionModusMentis,
                    Text: failureNarration,
                    Keywords: null,
                    Actions: null,
                    ChainOrigin: action.ChainOrigin
                );
                void CommitItemFailure()
                {
                    _scrollBuffer.AddBlock(failureBlock);
                    _narrationState.AddBlock(failureBlock);
                    _scrollBuffer.ScrollToBottom();
                    _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;
                }
                if (itemFailPart != null)
                {
                    itemFailPart.AttachCommit(CommitItemFailure);
                    itemFailPart.MarkComplete();
                    _previewSession.EndProduction();
                }
                else CommitItemFailure();
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"NarrativeController: Error during item combination: {ex.Message}");
            _previewSession.Reset();
        }
        finally
        {
            _narrationState.IsLoadingAction = false;
        }
    }
}
