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
    private DialogueTriggerOutcome? _pendingDialogueOutcome = null;

    // Continuity context captured when a dialogue becomes pending and consumed by the next observation
    // phase: the NPC talked to and the observation modus mentis that originated the dialogue's chain of
    // thought (null when the dialogue had no such origin). See SetPendingDialogue / GenerateObservationsAsync.
    private NpcEntity? _postDialogueNpc = null;
    private ModusMentis? _postDialogueObservationMM = null;

    // Records recordable successful verbs into a learned routine for this narration session.
    // Non-null only for scene-backed Exploration narration.
    private RoutineRecorder? _recorder = null;
    
    // Random for dice rolls — the run-long shared stream, not a per-controller generator: a new
    // NarrativeController is built for every narration phase, and a fresh Random on the same derived
    // seed made the first roll of each phase repeat the previous phase's first roll.
    private readonly Random _diceRandom = GameRng.Stream("dice");

    // Unified dice-roll overlay (animation + humor modifiers + hit-testing).
    private readonly DiceRollComponent _dice = new();

    // In-flight roll (main path): the evaluation to narrate and the current (humor-modified) result.
    // The actual outcome is generated only once the player presses the dice CONTINUE — see
    // OnDiceRollContinue → GenerateOutcomePreviewAsync — so no narration is produced during the roll.
    private ActionEvaluationResult? _pendingEval;
    private bool _pendingSucceeded;
    // Separator caption for the segment the post-action CONTINUE closes ("after trying to grab a
    // rope"). Set when an in-place outcome shows the CONTINUE button; consumed by HandleContinueClicked.
    private string? _pendingSegmentLabel;

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

    // ── LLM generation preview box ───────────────────────────────────────────────
    // Streams the text being generated over the greyed-out menu, gated by a CONTINUE button.
    // Active from the first BeginPart until the last part's CONTINUE commits its block(s).
    private readonly LlmPreviewSession _previewSession = new();
    private (int X, int Y, int Width) _previewContinueRegion; // last-rendered CONTINUE region (Width 0 ⇒ none)
    private bool _previewContinueHovered;
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
    public void CloseNarrationSegment(string? separatorLabel = null)
    {
        _scrollBuffer.ConvertToHistory(separatorLabel);
        _narrationState.ResetForNewNode();
        _observationLedger.Clear();
        _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;
    }

    /// <summary>
    /// <see cref="CloseNarrationSegment"/> followed by a fresh observation pass in the SAME node and
    /// scene. Call this when RETURNING to narration from another phase (or on a node transition,
    /// where the caller reassigns <c>_currentNode</c> first): the player gets full noetic points and
    /// narration that describes the scene as it now stands.
    /// </summary>
    public void BeginNarrationSegment(string? separatorLabel = null)
    {
        CloseNarrationSegment(separatorLabel);
        StartObservationPhaseWithHistory();
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
    public void StartObservationPhase(TimePeriod? forcedPeriod = null)
    {
        _narrationState.Clear();
        _scrollBuffer.Clear();
        _activePartyMember = _protagonist;
        _memberNoeticPoints.Clear();
        _observationLedger.Clear();
        // New session — drop any stale post-dialogue continuity context.
        _postDialogueNpc = null;
        _postDialogueObservationMM = null;

        // Place NPCs into nodes based on the supplied time period, or a random one when none is given.
        // --period pins it for the whole run: several rules (every entry door shutting at night) only
        // fire in one period, and a random draw reaches that one visit in six.
        var period = forcedPeriod ?? Config.Debug.ForcedPeriod ?? TimePeriodExtensions.Random(_diceRandom);
        ApplyTimePeriod(period);
        Console.WriteLine($"NarrativeController: Time period is {period}");

        // Begin recording a routine for scene-backed Exploration sessions (other phases opt out).
        if (_scene != null && _scene.Phase == NarrationPhase.Exploration)
            _recorder = new RoutineRecorder(_protagonist, _locationId, period);

        _narrationState.IsLoadingObservations = true;
        _narrationState.LoadingMessage = ObservationLoadingMessage();

        // Fire-and-forget async task
        _ = GenerateObservationsAsync();

        Console.WriteLine("NarrativeController: Started observation phase");
    }
    
    /// <summary>
    /// Initializes a scene-backed session enough to open a sub-phase (dialogue / trade / work)
    /// directly, WITHOUT running an observation pass. Used by the routine-replay bridge, where a
    /// recorded routine jumps straight into the dialogue (or its baked-in follow-on phase) it ended
    /// on. Places NPCs for the period so the target is present, and arms a recorder so anything the
    /// player does after the sub-phase is still recordable — exactly like a normal visit, minus the
    /// opening narration.
    /// </summary>
    public void PrepareForRoutineSubPhase(TimePeriod period)
    {
        _narrationState.Clear();
        _scrollBuffer.Clear();
        _activePartyMember = _protagonist;
        _memberNoeticPoints.Clear();
        _observationLedger.Clear();
        _postDialogueNpc = null;
        _postDialogueObservationMM = null;

        ApplyTimePeriod(period);
        Console.WriteLine($"NarrativeController: routine sub-phase prepared at period {period}");

        if (_scene != null && _scene.Phase == NarrationPhase.Exploration)
            _recorder = new RoutineRecorder(_protagonist, _locationId, period);
    }

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
    private void StartObservationPhaseWithHistory()
    {
        // The one case the hand-off cannot survive: a companion who has left the party since (dead of
        // old age, or dropped over the party cap). There is no one to narrate as, so the protagonist
        // takes over.
        if (_activePartyMember != _protagonist && !_protagonist.CompanionParty.Contains(_activePartyMember))
        {
            Console.WriteLine($"NarrativeController: active member '{_activePartyMember.DisplayName}' is no longer in the party — narration returns to the protagonist");
            _activePartyMember = _protagonist;
        }

        _memberNoeticPoints.Clear(); // New node — everyone starts with a fresh counter
        // ResetForNewNode refilled the counter already, but from whoever was active when the segment
        // closed; restate it from the member who will actually act, since their maxima differ.
        _narrationState.ThinkingAttemptsRemaining = _activePartyMember.MaxNoeticPoints;
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

            // If a dialogue just ended, open this phase with a single observation of that NPC, narrated by
            // the observation modus mentis that originated the dialogue (see GeneratePostDialogueObservationAsync).
            // The context is consumed once; if the NPC has left the scene we fall through to the normal phase.
            var postDialogueNpc = _postDialogueNpc;
            var postDialogueMM  = _postDialogueObservationMM;
            _postDialogueNpc = null;
            _postDialogueObservationMM = null;

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
            // This takes precedence over post-dialogue continuity.
            if (!handled)
                handled = await TryGenerateThreatObservationAsync(CommitObservation);

            // Otherwise, if a dialogue just ended, open with a single observation of that NPC.
            if (!handled)
                handled = postDialogueNpc != null
                    && await TryGeneratePostDialogueObservationAsync(postDialogueNpc, postDialogueMM, CommitObservation);

            if (!handled)
            {
                // Generate ONE overall observation (one sentence per sampled outcome), streamed into the box.
                await _observationController.ExecuteObservationPhaseAsync(
                    _currentNode,
                    _activePartyMember,
                    _protagonist.CurrentLocationId,
                    isReminescence: _scene?.Phase == NarrationPhase.ChildhoodReminescence,
                    ledger: _observationLedger,
                    preview: _previewSession,
                    commit: CommitObservation
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
            Console.Error.WriteLine($"NarrativeController: Error generating observations: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);

            _previewSession.Reset();
            _narrationState.IsLoadingObservations = false;
            _narrationState.ErrorMessage = $"Failed to generate observations: {ex.Message}";
        }
    }

    /// <summary>
    /// Runs the post-dialogue continuity observation when possible: resolves <paramref name="npc"/> to
    /// an observable object in the current node and, if it is still there, generates a single observation
    /// of it via <see cref="ObservationPhaseController.GeneratePostDialogueObservationAsync"/> (which reuses
    /// <paramref name="originMM"/> when still learned, else resamples). Returns false — so the caller runs
    /// the normal phase — when the NPC has left the scene or nothing was produced.
    /// </summary>
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

            var blocks = await _observationController.GenerateThreatObservationAsync(
                outcome, _protagonist.CurrentLocationId, _activePartyMember,
                ledger: _observationLedger, preview: _previewSession, commit: commit);
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

    private async Task<bool> TryGeneratePostDialogueObservationAsync(
        NpcEntity npc, ModusMentis? originMM, Action<List<NarrationBlock>> commit)
    {
        var npcOutcome = _currentNode.GetAllDirectConcreteOutcomes()
            .OfType<SyntheticNpcObservationObject>()
            .FirstOrDefault(o => ReferenceEquals(o.NpcEntity, npc));
        if (npcOutcome == null)
        {
            Console.WriteLine($"NarrativeController: Post-dialogue NPC '{npc.DisplayName}' left the scene — normal observation.");
            return false;
        }

        var blocks = await _observationController.GeneratePostDialogueObservationAsync(
            npcOutcome, originMM, _protagonist.CurrentLocationId, _activePartyMember,
            ledger: _observationLedger, preview: _previewSession, commit: commit);
        return blocks.Count > 0;
    }

    /// <summary>
    /// Records a pending dialogue and captures the continuity context for the observation phase that will
    /// follow it: the NPC being talked to, and the observation modus mentis that originated this chain of
    /// thought (traced back through <paramref name="chainOrigin"/>; null for dialogues that did not come
    /// from an observation→thinking→action chain, e.g. a caught-red-handed confrontation).
    /// </summary>
    private void SetPendingDialogue(DialogueTriggerOutcome outcome, ModusMentisChainElement? chainOrigin)
    {
        _pendingDialogueOutcome    = outcome;
        _postDialogueNpc           = outcome.Target;
        _postDialogueObservationMM = TraceObservationModusMentis(chainOrigin);
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
    /// Execute thinking phase with selected modusMentis and keyword (async).
    /// </summary>
    private async Task ExecuteThinkingPhaseAsync(ModusMentis thinkingModusMentis, string keyword)
    {
        // Get the source observation block from the hovered keyword (for modusMentis chain tracking)
        var sourceObservationBlock = _narrationState.HoveredKeyword?.SourceBlock;

        try
        {
            Console.WriteLine($"NarrativeController: Executing thinking with {thinkingModusMentis.DisplayName} on keyword '{keyword}'");

            _previewSession.Reset();

            // Resolve the outcome linked to the clicked keyword via KeywordOutcomeMap or LinkedOutcome
            NarrativeAnchor? targetOutcome = null;
            if (sourceObservationBlock?.KeywordOutcomeMap?.TryGetValue(keyword, out var kmo) == true)
                targetOutcome = kmo;
            else
                targetOutcome = sourceObservationBlock?.LinkedOutcome;

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

            // Persona-fit cancellation: the action skill refused (reluctant/opposed). Show the
            // first-person refusal as an outcome block; no action button is offered. The noetic
            // point is still consumed below via the normal thinking-complete decrement.
            NarrationBlock? refusalBlock = null;
            if (!hasActions && response.RefusalText != null && response.RefusalModusMentis != null)
            {
                refusalBlock = new NarrationBlock(
                    Type: NarrationBlockType.Outcome,
                    ModusMentis: response.RefusalModusMentis,
                    Text: response.RefusalText,
                    Keywords: null,
                    Actions: null);
                Console.WriteLine("NarrativeController: VerbAction refused by persona-fit — refusal narrated, no button.");
            }

            // Deferred commit: the thinking block (and refusal, and its action button) become visible
            // only when the player presses CONTINUE on the last preview part. Reveal sound + scroll ride
            // along at commit time so they match the button press.
            void CommitThinking()
            {
                _scrollBuffer.AddBlock(thinkingBlock);
                _narrationState.AddBlock(thinkingBlock);
                _ambianceEngine?.PlaySoundEffect(SoundEffectType.NarrativeReveal);
                if (refusalBlock != null)
                {
                    _scrollBuffer.AddBlock(refusalBlock);
                    _narrationState.AddBlock(refusalBlock);
                }
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
            Console.Error.WriteLine(ex.StackTrace);

            _previewSession.Reset();
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
                        action, refusalMm, ruleResult.ErrorMessage ?? "", _activePartyMember, CancellationToken.None, preview: refusalPart?.Sink);
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
                Console.WriteLine($"NarrativeController: VerbAction failed plausibility check");
                action.IsImpossible = true;

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
                    // No more noetic points - show continue button and grey out like a normal failure
                    _narrationState.ShowContinueButton = true;
                    return;
                }
            }
            
            // === PHASE 2: DICE ROLL (dice rolling animation) ===
            Console.WriteLine($"NarrativeController: VerbAction passed plausibility, starting dice roll phase");

            // Number of dice = total modusMentis level summed across the chain
            int numberOfDice = Math.Max(1, action.GetTotalModusMentisLevel());

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
            Console.Error.WriteLine(ex.StackTrace);

            _previewSession.Reset();
            _narrationState.IsLoadingAction = false;
            NarrationDiceClear();
            _narrationState.ErrorMessage = $"VerbAction execution failed: {ex.Message}";
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
        BeginNarrationSegment();
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
        int numberOfDice = Math.Max(1, action.GetTotalModusMentisLevel());
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
            Console.Error.WriteLine(ex.StackTrace);
            _previewSession.Reset();
            _narrationState.IsLoadingAction = false;
            NarrationDiceClear();
            _narrationState.ErrorMessage = $"GetUp outcome failed: {ex.Message}";
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

        // Collect and apply all verb reports (skills, items, history, transition).
        System.Collections.Generic.IReadOnlyList<Outcome> reminescenceReportList;
        try
        {
            reminescenceReportList = action.Verb.SuccessReports(_scene, _pov, _protagonist, target);
            foreach (var report in reminescenceReportList)
                report.Apply(OutcomeContext.For(_protagonist, _scene, _pov));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"NarrativeController: REMEMBER verb threw — {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            reminescenceReportList = System.Array.Empty<Outcome>();
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
            _narrationState.IsLoadingAction = true;
            _ = Task.Run(() => GenerateOutcomePreviewAsync(eval, succeeded));
            return;
        }

        Console.WriteLine("NarrativeController: No pending action result for dice roll continue");
        NarrationDiceClear();
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
            var verbReports = GatherVerbReports(eval.Action.PreselectedOutcome, succeeded);
            var result = await _actionExecutor.PrepareSingleOutcomeAsync(eval, succeeded, verbReports, part.Sink, CancellationToken.None);
            _narrationState.IsLoadingAction = false;
            part.AttachCommit(() => CommitOutcomeResult(result, deferredCommit: true));
            part.MarkComplete();
            _previewSession.EndProduction();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"NarrativeController: Outcome generation failed: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            _previewSession.Reset();
            _narrationState.IsLoadingAction = false;
            NarrationDiceClear();
            _narrationState.ErrorMessage = $"Outcome failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Gathers the verb-specific outcome reports for a resolved action. Success reports carry the
    /// chosen <see cref="VerbAction"/> (so verbs that expanded into several actions read their variant);
    /// failure reports use the target-only overload. Returns an empty list for non-verb outcomes.
    /// </summary>
    private System.Collections.Generic.IReadOnlyList<Outcome> GatherVerbReports(INarratable? outcome, bool succeeded)
    {
        if (outcome is VerbAction verbTarget && _scene != null && _pov != null && verbTarget.Target != null)
        {
            var verb = verbTarget.Verb;
            if (!succeeded)
                return verb.FailureReports(_scene, _pov, _activePartyMember, verbTarget.Target);

            var reports = new System.Collections.Generic.List<Outcome>(
                verb.SuccessReports(_scene, _pov, _activePartyMember, verbTarget.Target, verbTarget));

            // Doing a thing is how the thing is learned. Appended last so the lesson reads as the
            // consequence of whatever the verb actually did, not as its headline.
            var lesson = ModusMentisGrantOutcome.For(
                _activePartyMember, verb.ResolveGrantedModusMentisId(verbTarget.Target));
            if (lesson != null) reports.Add(lesson);

            return reports;
        }
        return System.Array.Empty<Outcome>();
    }

    /// <summary>
    /// Applies a resolved action outcome: side-effects (XP/item when <paramref name="deferredCommit"/>),
    /// outcome reports, the outcome narration block, and any fight/dialogue/transition it triggers.
    /// Called synchronously for Get-Up and via the outcome preview's CONTINUE for the main path.
    /// </summary>
    private void CommitOutcomeResult(ActionExecutionResult result, bool deferredCommit)
    {
        Console.WriteLine($"NarrativeController: committing {(result.Succeeded ? "SUCCESS" : "FAILURE")} outcome");

        // The dice chain's own lesson — one report per modus mentis that fed the roll (observation →
        // thinking → action), so each shows its own chip instead of the XP moving in silence. Built
        // here and applied with the rest below; a capped modus mentis reports nothing.
        var practiceReports = new System.Collections.Generic.List<Outcome>();

        if (deferredCommit)
        {
            // Keep only the chosen branch's narration in the narrator slot history (discard the
            // speculative other branch that was generated during the roll).
            _actionExecutor.OutcomeNarrator.CommitNarrationHistory(result.Succeeded);

            // Commit deferred side-effects for the FINAL (possibly humor-modified) outcome.
            if (result.Succeeded)
                foreach (var chainModusMentis in result.Action.GetModusMentisChain())
                {
                    var practice = ModusMentisPracticeOutcome.For(_activePartyMember, chainModusMentis);
                    if (practice != null) practiceReports.Add(practice);
                }
            if (result.ItemConsumed && result.Action.CombinedItem != null)
            {
                _activePartyMember.RemoveItem(result.Action.CombinedItem);
                Console.WriteLine($"NarrativeController: Item consumed — {result.Action.CombinedItem.ItemId}");
            }
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
            allReports.AddRange(GatherVerbReports(result.ActualOutcome, result.Succeeded));
            allReports.AddRange(result.LlmDecidedReports);
        }

        // The chain's practice chips read as the quiet coda to whatever the verb actually did, so
        // they go last — after the verb's own reports and after its lesson.
        allReports.AddRange(practiceReports);

        // Record this verb into the in-progress routine BEFORE applying reports, so the recorder
        // evaluates the verb against the pre-move PoV. The reports come along because they carry the
        // RoutineChainEffect the recorder decides on (skip vs stop, and what counts as movement).
        if (result.Succeeded && _recorder != null && _scene != null && _pov != null
            && result.ActualOutcome is VerbAction)
        {
            _recorder.OnVerbSucceeded(result.Action, _scene, _pov, _activePartyMember, result.ItemConsumed, allReports);
        }

        // Remember where and when we were before reports apply. The area drives continuing narration
        // at the destination node (any area-moving verb — move, follow path, stairs, climb, door — not
        // just MoveToArea); the period drives re-placing NPCs for the new time of day.
        var areaBefore   = _pov?.Where;
        var periodBefore = _pov?.When;

        // Apply every report's game-state change in order — to the acting member, so a companion's
        // loot, learned skills, and suffered wounds land on the companion, not the protagonist.
        foreach (var report in allReports)
            report.Apply(OutcomeContext.For(_activePartyMember, _scene, _pov));

        // Self-check for the routine recorder's one silent failure mode: a report that relocates the
        // player without declaring it. The recorder cannot see the move (it runs before Apply, by
        // design), so it would build routines on a stale prefix. Shout rather than record something
        // subtly wrong. Space and time are checked separately so the message names the right flag.
        if (!ReferenceEquals(areaBefore, _pov?.Where)
            && !allReports.Any(r => r.RoutineChainEffect.HasFlag(RoutineChainEffect.Movement)))
        {
            Console.Error.WriteLine(
                $"NarrativeController: '{result.Action.Verb?.VerbId}' moved the point of view but none of its " +
                "reports declared RoutineChainEffect.Movement — routine recording will mis-track position. " +
                "Declare it on the report that moves the PoV.");
        }

        if (periodBefore != _pov?.When
            && !allReports.Any(r => r.RoutineChainEffect.HasFlag(RoutineChainEffect.TimeShift)))
        {
            Console.Error.WriteLine(
                $"NarrativeController: '{result.Action.Verb?.VerbId}' changed the time of day but none of its " +
                "reports declared RoutineChainEffect.TimeShift — routine recording will mis-track it. " +
                "Declare it on the report that shifts the period.");
        }

        // A verb that shifted the period only wrote PoV.When; route it back through the single writer
        // so the graph's period, NPC placement and verb gating all follow it to the new time of day.
        if (_pov != null && periodBefore != null && periodBefore != _pov.When)
        {
            Console.WriteLine($"NarrativeController: time of day advanced {periodBefore} → {_pov.When}");
            ApplyTimePeriod(_pov.When);
        }

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
            var crimeType = DetermineCrimeType(result.Action.Verb, _pov.Where.IsPrivate);
            Console.WriteLine($"NarrativeController: Witness '{result.CaughtByWitness.DisplayName}' caught the failed illegal action (crime: {crimeType})");
            var catchTree = CaughtRedHandedTreeFactory.Create(crimeType);
            SetPendingDialogue(new Cathedral.Game.Scene.DialogueTriggerOutcome(result.CaughtByWitness, tree: catchTree), result.Action);
            return;
        }

        // Only heard, from a room away: they come to look. Nothing is confronted yet — the point of
        // this rung is that it leaves room to leave. What it costs is that they are now standing in
        // the room with you, which closes the free exit and makes the next slip a caught one.
        if (!result.Succeeded && result.NpcDrawnIn != null && _scene != null && _pov != null)
        {
            var arriving = _scene.Npcs.FirstOrDefault(n => ReferenceEquals(n.Entity, result.NpcDrawnIn));
            if (arriving != null)
                _scene.DrawNpcTo(arriving, _pov.Where);
            else
                Console.Error.WriteLine(
                    $"NarrativeController: '{result.NpcDrawnIn.DisplayName}' was drawn in but is not a scene NPC — nobody arrives.");
        }

        // Handle outcome based on type - show continue button for next step
        if (result.ActualOutcome is FightTriggerOutcome fightOutcome)
        {
            Console.WriteLine($"NarrativeController: Fight outcome with {fightOutcome.Target.DisplayName}, signaling fight mode");
            _pendingFightOutcome = fightOutcome;
            // Don't show continue button - the game controller will detect the pending fight and switch modes
        }
        else if (result.ActualOutcome is VerbAction verbOutcome && _scene != null && _pov != null)
        {
            Console.WriteLine($"NarrativeController: Verb outcome '{verbOutcome.Verb.VerbId}' on '{verbOutcome.Target?.DisplayName}', reports already applied");
            SceneDebugManager.UpdatePoV(_pov);

            // Check if the verb requested a dialogue session
            if (_scene.PendingDialogueRequest != null)
            {
                var req = _scene.PendingDialogueRequest;
                _scene.PendingDialogueRequest = null;
                SetPendingDialogue(new Cathedral.Game.Scene.DialogueTriggerOutcome(req.Npc, req.TreeId), result.Action);
                Console.WriteLine($"NarrativeController: Dialogue verb triggered tree '{req.TreeId}' with {req.Npc.DisplayName}");
                return;
            }

            // Check if the verb requested a fight (e.g. AttackVerb)
            if (_scene.PendingFightRequest != null)
            {
                var req = _scene.PendingFightRequest;
                _scene.PendingFightRequest = null;
                _pendingFightOutcome = new FightTriggerOutcome(req.Npc, $"attack on {req.Npc.DisplayName}");
                Console.WriteLine($"NarrativeController: Attack verb triggered fight with {req.Npc.DisplayName}");
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
            _pendingSegmentLabel = SegmentLabelFor(result);
        }
        else
        {
            Console.WriteLine("NarrativeController: Non-transition outcome, showing continue button");
            _narrationState.PendingTransitionNode = null;
            _narrationState.ShouldExitOnContinue = IsMovementAction(result.Action);
            _narrationState.ShowContinueButton = true;
            _pendingSegmentLabel = SegmentLabelFor(result);
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
        PartyMember companion,
        KeywordRegion keywordRegion)
    {
        string keyword = keywordRegion.Keyword;
        var sourceBlock = keywordRegion.SourceBlock;

        try
        {
            Console.WriteLine($"NarrativeController: Speaking phase — skill={speakingModusMentis.DisplayName}, companion={companion.DisplayName}, keyword='{keyword}'");

            // Resolve the outcome linked to this keyword
            NarrativeAnchor? linkedOutcome = null;
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

            _previewSession.Reset();

            // Deferred commit: when the player presses CONTINUE on the last spoken line, grey the old
            // content, add the speaking block, spend a noetic point and hand off to the companion.
            void CommitSpeaking(NarrationBlock speakingBlock)
            {
                _scrollBuffer.ConvertToHistory();
                _narrationState.ResetForPartyMemberChange();
                // The companion takes over the narration with their own attention: whatever the
                // speaker had already looked at does not constrain what draws them.
                _observationLedger.Clear();
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
            Console.Error.WriteLine(ex.StackTrace);
            _previewSession.Reset();
            _narrationState.IsLoadingSpeaking = false;
            _narrationState.ErrorMessage = $"Speaking failed: {ex.Message}";
        }
    }

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
                ? Config.LoadingMessages.RollingDice
                : (_dice.IsCurrentlySuccess ? "Success! Click Continue to see the outcome" : "Failed! Click Continue to see the outcome");
            _ui.RenderStatusBar(diceStatus);
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
            var region = _ui.RenderPreviewBox(snap, _previewContinueHovered);
            _previewContinueRegion = region.Present ? (region.X, region.Y, region.Width) : default;
            // No footer button while the box is up — clear its region so a stale one can't be clicked.
            _exitButtonRegion = default;
            _ui.RenderWaitingStatus(_narrationState.LoadingMessage);
            return;
        }
        _previewContinueRegion = default;
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
                Console.WriteLine($"NarrativeController: Selected modusMentis: {selectedModusMentis.DisplayName}");

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
                        _narrationState.LoadingMessage = ObservationLoadingMessage();
                        _narrationState.IsSelectingObservationModusMentis = false;

                        // Resolve focus outcome: prefer KeywordOutcomeMap, then LinkedOutcome, then keyword lookup
                        NarrativeAnchor? focusOutcome = null;
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

        // Preview box CONTINUE hover (shown while the generation-preview session is active).
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
            HandleFooterButtonClicked();
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
                _choicePopup.Show(screenPos, new List<string> { "Execute", "Use Tool" }, "Action", disabledIndices);
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
            _choicePopup.Show(screenPos, speakChoices, "Keyword VerbAction", speakDisabled);
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

        string keyword = _narrationState.HoveredKeyword.Keyword;
        var sourceBlock = _narrationState.HoveredKeyword.SourceBlock;

        // Check if we're selecting an observation modusMentis or thinking modusMentis
        if (_narrationState.IsSelectingObservationModusMentis)
        {
            // Focus observation phase
            _narrationState.IsLoadingFocusObservation = true;
            _narrationState.LoadingMessage = ObservationLoadingMessage();
            _narrationState.IsSelectingObservationModusMentis = false;

            // Resolve focus outcome: prefer KeywordOutcomeMap, then LinkedOutcome, then keyword lookup
            NarrativeAnchor? focusOutcome = null;
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
            // VerbAction choice: 0 = Execute, 1 = Use Tool
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
                    _itemSelectionPopup.Show(screenPos, candidateItems, "Combine Tool with VerbAction");
                }
                else
                {
                    Console.WriteLine("NarrativeController: No combinable items available.");
                }
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

    /// <summary>Whether the generation-preview box is up, its title, text and CONTINUE region (if clickable).</summary>
    public (bool Active, string Title, string Text, bool Complete) CliPreview()
    {
        var s = _previewSession.Snapshot();
        return (s.Active, s.Title, s.DisplayText, s.Complete);
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
            region = regions.FirstOrDefault(r => r.Keyword.Equals(keyword, StringComparison.OrdinalIgnoreCase));
            if (region == null) return $"no clickable keyword '{keyword}' on screen";
        }

        OnMouseMove(region.StartX, region.Y);
        OnMouseClick(region.StartX, region.Y);
        return null;
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
                // Only the area is set here — StartObservationPhase(time) below routes the period
                // through ApplyTimePeriod, the single writer of PoV.When + graph period.
                _pov.Where = area;
                if (NodeForArea(area) is { } node)
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

        // A bystander who can see you leave, and a reason for them to stop you: either you are
        // standing somewhere you have no business being, or they came in here after you.
        //
        // The second condition is what makes the approach cost something. Three crimes happen in the
        // open — pickpocket, stalk, attack — so a witness drawn to a public square by a botched one
        // would, on the privacy test alone, watch you stroll away.
        var witness = WitnessSelector.ComputeContext(_scene, _pov);
        if (witness.Type == WitnessType.Visual && witness.Witness != null)
        {
            var sceneNpc = _scene.Npcs.FirstOrDefault(n => ReferenceEquals(n.Entity, witness.Witness));
            bool cameForYou = sceneNpc != null && _scene.DisplacedNpcs.ContainsKey(sceneNpc.Id);
            if (_pov.Where.IsPrivate || cameForYou)
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
    /// <summary>Separator caption for the segment a resolved action closes.</summary>
    private static string? SegmentLabelFor(ActionExecutionResult result)
        => string.IsNullOrWhiteSpace(result.Action?.NeutralActionText)
            ? null
            : $"after trying to {result.Action.NeutralActionText}";

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
            BeginNarrationSegment();
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

        // Nothing pending: the resolved action closes the segment — grey the text into history under
        // a labelled separator, refill noetic points, and open a fresh observation of the scene as it
        // now stands (the outcome may have changed it: item gone, state shifted). This makes CONTINUE
        // behave identically for in-place actions and transitions.
        Console.WriteLine("NarrativeController: CONTINUE — closing segment after action, restarting observations");
        BeginNarrationSegment(_pendingSegmentLabel);
        _pendingSegmentLabel = null;
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
        _narrationState.LoadingMessage = Config.LoadingMessages.RollingDice;

        // Fixed animation window (no async work backs this roll, unlike thinking checks).
        await Task.Delay(Config.Dice.AnimationDurationMs);

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
            _pendingFightOutcome = new FightTriggerOutcome(target, $"failed to run away from {target.DisplayName}");
        }
        else
        {
            Console.WriteLine($"NarrativeController: RUNAWAY failed — witness '{target.DisplayName}' confronts trespass");
            var catchTree = CaughtRedHandedTreeFactory.Create(CriminalAffinityType.Intruder);
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
        
        // No modus mentis: a system note — an MM here would print its skill header (e.g.
        // "[DISCIPLINE ▪]") before the segment separator without any LLM involvement.
        var block = new NarrationBlock(
            Type: NarrationBlockType.Outcome,
            ModusMentis: null!,
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
    /// What the witness will say they saw, for a verb that was just caught. Only reached once the
    /// action is already established as a crime, so this names the kind rather than re-deciding it —
    /// see <see cref="Cathedral.Game.Scene.Verbs.Verb.IsIllegal"/> for the deciding.
    ///
    /// <para>The violent verbs all read as <c>Murderer</c>, including a bare <c>attack</c>: what the
    /// witness is reacting to is somebody setting about a person, and the accusation is worded from
    /// that. Anything unlisted done inside a private area is trespass, which is what makes an
    /// otherwise innocuous verb a crime there in the first place.</para>
    /// </summary>
    private static CriminalAffinityType DetermineCrimeType(Cathedral.Game.Scene.Verbs.Verb verb, bool areaIsPrivate)
    {
        return verb.VerbId switch
        {
            "steal"       => CriminalAffinityType.Thief,
            "pickpocket"  => CriminalAffinityType.Thief,
            "grab"        => areaIsPrivate ? CriminalAffinityType.Thief : CriminalAffinityType.None,
            "slay"        => CriminalAffinityType.Murderer,
            "murder"      => CriminalAffinityType.Murderer,
            "attack"      => CriminalAffinityType.Murderer,
            "unlock_door" => CriminalAffinityType.Intruder,
            "slip_into"   => CriminalAffinityType.Intruder,
            "stalk"       => CriminalAffinityType.Intruder,
            "break"       => CriminalAffinityType.Vandal,
            _             => areaIsPrivate ? CriminalAffinityType.Intruder : CriminalAffinityType.None,
        };
    }

    /// <summary>
    /// The tools that may be combined with an action. Only <see cref="ItemCategory.Tool"/> and
    /// <see cref="ItemCategory.Weapon"/> qualify — a weapon is a tool too, just a specialised and
    /// often clumsy one (see <c>WeaponItem.UsageLevel</c>). Garments, food and raw material are
    /// excluded: they are things you wear, eat or own, not things you work with.
    ///
    /// Location is deliberately not a factor — anything carried is usable, whether it is in hand
    /// or at the bottom of a pack. A container holding something is excluded, since combining it
    /// with an action would mean putting its contents down.
    /// </summary>
    private List<Item> GetCombinableItems()
    {
        return _activePartyMember.GetAllItems()
            .Where(i => i.Category is ItemCategory.Tool or ItemCategory.Weapon)
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

            string itemContext = $"{item.DisplayName} ({item.Description})";
            Console.WriteLine($"NarrativeController: Item combination — action='{action.DisplayText}', item='{itemContext}'");

            // Build critic context
            var goalDescription = action.PreselectedOutcome?.ToNaturalLanguageString() ?? "";
            var criticContext = new CriticContext(_currentNode, _worldContext, _locationId, goalDescription);
            criticContext.CombinedItemContext = itemContext;

            // === CRITIC: can the item help? (single pass, neutral goal-based phrasing) ===
            // A tool-gated verb asks a different question: not "does this beat bare hands" (bare hands
            // are not an option there) but "can this do the work of the tool this needs".
            var gatedVerb = action.PreselectedOutcome?.Verb;
            var appropriatenessTree = gatedVerb is { RequiresTool: true }
                ? CriticTrees.BuildToolSubstitutionTree(
                      goalDescription, CriticTrees.ToolPhrase(gatedVerb.ReferenceToolIds),
                      item.DisplayName, criticContext)
                : CriticTrees.BuildItemAppropriatenessTree(goalDescription, item.DisplayName, criticContext);
            var appropriatenessResult = await _actionExecutor.ItemUseCritic.EvaluateTreeAsync(appropriatenessTree);
            bool appropriatenessSuccess = appropriatenessResult.OverallSuccess;
            Console.WriteLine($"NarrativeController: Item appropriateness ({(gatedVerb is { RequiresTool: true } ? "tool substitution" : "neutral")}): {(appropriatenessSuccess ? "success" : "fail")}");

            // Item combination always costs one noetic point, regardless of outcome
            _narrationState.ThinkingAttemptsRemaining = Math.Max(0, _narrationState.ThinkingAttemptsRemaining - 1);
            Console.WriteLine($"NarrativeController: Item combination consumed 1 noetic point ({_narrationState.ThinkingAttemptsRemaining} remaining)");

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
                // Chain leaf: a synthetic ModusMentis carrying item name + effective usage level so that:
                //   - the action button shows [ItemName ◼◼] instead of [ActionSkill ◼◼◼]
                //   - GetTotalModusMentisLevel() = obs.Level + thinking.Level + action.Level + effectiveUsage (no repetition)
                // The item's UsageLevel is capped by the hands-derived "tool_usage_cap" stat so that
                // characters with stronger (or unwounded) hands extract more bonus from potent tools.
                int usageCap = _activePartyMember.DerivedStats
                    .First(s => s.Name == "tool_usage_cap").GetValue(_activePartyMember);
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
                Console.WriteLine($"NarrativeController: Item '{item.ItemId}' rejected — narrating failure.");

                // The failure explanation streams into a preview box.
                _previewSession.Reset();
                var itemFailPart = _previewSession.BeginPart(PreviewTitles.For(actionModusMentis));
                string failureNarration = await _actionExecutor.OutcomeNarrator.NarrateItemCombinationFailureAsync(
                    action, item, actionModusMentis, appropriatenessResult.CombinedFailureReason, preview: itemFailPart?.Sink);

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
