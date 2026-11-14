using System;
using System.Linq;
using System.Collections.Generic;
using Cathedral.Audio;
using Cathedral.Debug;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Preview;
using Cathedral.Game.Narrative.Routines;
using Cathedral.Game.Narrative.Tree;
using Cathedral.Game.Npc;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Verbs;
using Cathedral.LLM;
using Cathedral.Terminal;
using Cathedral.Glyph;

namespace Cathedral.Game;

/// <summary>
/// The narration of a place, played on its map.
///
/// <para><b>The map is the screen.</b> Every area the party knows is a card
/// (<see cref="LocationMap"/>). On a card stand the party's tokens and one symbol for each thing not
/// yet observed there, by kind; under it hang the thought trees of the member being played
/// (<see cref="MapTreeOverlay"/>). A click on a symbol is the whole of the player's part: what kind of
/// thing to observe, which observed thing to think about, which act to carry out, whom to play. Which
/// modus mentis does it is then asked in a popup, and what the mind writes is read in the preview box
/// before it becomes a symbol on the tree, where the pointer brings the text back.</para>
///
/// <para><b>A tree is the modus mentis chain.</b> Observation, thinking, action: the root is an
/// observed element, a thought hangs from it, an act from the thought, an outcome from the act
/// (<see cref="ThoughtNode"/>). The dice read the same chain. Trees are private to a member
/// (<see cref="MemberMind"/>), last the visit, and are kept coherent with the scene by
/// <see cref="ReconcileTrees"/> after anything that changes it.</para>
///
/// <para><b>Presence.</b> Each member stands in an area of their own. Thinking needs no presence — a
/// member may think about anything they have observed, wherever it now is — while observing and
/// acting do: the member walks there first, by ways the party knows, for nothing
/// (<see cref="WalkTo"/>), and is stopped where somebody on the way would confront them.</para>
///
/// <para><b>The budget.</b> An observation, a thinking, an act refused by a rule and a refused
/// implement each cost the member a noetic point; the pool refills when one of their acts succeeds,
/// and when they come back from a fight or a conversation. Nothing else closes anything: acts
/// proposed stay on their trees until carried out or no longer possible.</para>
///
/// <para>The class is split by concern: this file holds the session and the scene's upkeep;
/// <c>.Trees</c> the trees and their reconciling; <c>.Observation</c>, <c>.Thinking</c> and
/// <c>.Acting</c> the three steps of an act; <c>.Movement</c> walking and the way out; <c>.Screen</c>
/// what is drawn and what a click does; <c>.Phases</c> the hand-over to fights, conversations and
/// the opening phases; <c>.Cli</c> the script driver's seams.</para>
/// </summary>
public partial class NarrativeController
{
    // ── The screen ───────────────────────────────────────────────────────────
    private readonly NarrativeUI _ui;
    private readonly LocationMap _map = new() { Narration = true };
    private readonly MapTreeOverlay _overlay = new();

    /// <summary>
    /// The mouse-following popup, shared with the modal popups below: the map's pointer label goes up
    /// in it while none of them is open (<see cref="UpdateMapLabel"/>).
    /// </summary>
    private readonly PopupTerminalHUD _popup;
    private readonly TerminalThinkingModusMentisPopup _modusMentisPopup;
    private readonly TerminalItemSelectionPopup _itemSelectionPopup;
    private readonly TerminalSimpleChoicePopup _choicePopup;
    private readonly GlyphSphereCore _core;
    private readonly TerminalInputHandler _terminalInputHandler;

    // ── The place ────────────────────────────────────────────────────────────
    private readonly WorldContext _worldContext;
    private readonly int _locationId;

    // The scene and its graph are swapped in place by the reminescence, one memory after another.
    private Scene.Scene _scene;
    private NarrationGraph _graph;
    private SceneNpcPlacement _npcPlacement;

    // ── The party ────────────────────────────────────────────────────────────
    private readonly Protagonist _protagonist;

    /// <summary>What each member carries through the visit, by member.</summary>
    private readonly Dictionary<PartyMember, MemberMind> _minds = new(ReferenceEqualityComparer.Instance);

    /// <summary>The member being played: whose trees are shown, who observes, thinks and acts.</summary>
    private PartyMember _active;

    private MemberMind Mind => _minds[_active];

    /// <summary>Where the played member stands, and the hour.</summary>
    private PoV _pov => Mind.Pov;

    // ── The steps of an act ──────────────────────────────────────────────────
    private readonly ObservationController _observationController;
    private readonly ThinkingExecutor _thinkingExecutor;
    private readonly ActionExecutionController _actionExecutor;

    // Learns routines from this visit. Non-null only in exploration.
    private RoutineRecorder? _recorder;

    // Random for dice rolls — the run-long shared stream, not a per-controller generator: a new
    // NarrativeController is built for every visit, and a fresh Random on the same derived seed made
    // the first roll of each visit repeat the previous visit's first roll.
    private readonly Random _diceRandom = GameRng.Stream("dice");

    // ── What is in flight ────────────────────────────────────────────────────

    /// <summary>The footer message while a mind is at work with no text to show yet; null when none is.</summary>
    private string? _loading;

    /// <summary>What went wrong, when a step could not finish. The narration stays dead on it.</summary>
    private string? _error;

    /// <summary>The visit is over: read by the game controller, which leaves for the world.</summary>
    private bool _requestedExit;

    // Streams the text being generated over the greyed-out map, gated by a CONTINUE button. Active
    // from the first BeginPart until the last part's CONTINUE commits its node.
    private readonly LlmPreviewSession _previewSession = new();

    // Ambient music engine (optional — null when no audio device is available).
    private readonly AmbianceEngine? _ambianceEngine;

    private void PlayClickSound() => _ambianceEngine?.TriggerGameEvent(GameEventType.StrongInteraction);
    private void PlayHoverSound() => _ambianceEngine?.TriggerGameEvent(GameEventType.SmallInteraction);

    /// <summary>
    /// Builds the narration of <paramref name="scene"/>. Every member of the party stands where the
    /// place is entered until <see cref="StartVisit"/> or a routine says otherwise.
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
        Scene.Scene scene,
        int locationId,
        WorldContext worldContext,
        Protagonist protagonist,
        AmbianceEngine? ambianceEngine = null)
    {
        _ambianceEngine       = ambianceEngine;
        _ui                   = new NarrativeUI(terminal ?? throw new ArgumentNullException(nameof(terminal)));
        _popup                = popup ?? throw new ArgumentNullException(nameof(popup));
        _modusMentisPopup     = new TerminalThinkingModusMentisPopup(popup);
        _itemSelectionPopup   = new TerminalItemSelectionPopup(popup);
        _choicePopup          = new TerminalSimpleChoicePopup(popup);
        _core                 = core ?? throw new ArgumentNullException(nameof(core));
        _terminalInputHandler = terminalInputHandler ?? throw new ArgumentNullException(nameof(terminalInputHandler));
        _thinkingExecutor     = thinkingExecutor ?? throw new ArgumentNullException(nameof(thinkingExecutor));
        _actionExecutor       = actionExecutor ?? throw new ArgumentNullException(nameof(actionExecutor));
        _worldContext         = worldContext ?? throw new ArgumentNullException(nameof(worldContext));
        _protagonist          = protagonist ?? throw new ArgumentNullException(nameof(protagonist));
        _locationId           = locationId;
        _active               = protagonist;

        _dice.OnDiceTick      = () => _ambianceEngine?.TriggerGameEvent(GameEventType.SmallInteraction);
        _dice.OnButtonHover   = PlayHoverSound;
        _dice.OnButtonClick   = PlayClickSound;
        _dice.OnResultChanged = OnDiceOutcomeFlipped;
        _timeBox.OnTick       = () => _ambianceEngine?.TriggerGameEvent(GameEventType.SmallInteraction);

        // Publish where this scene is, so every prompt's closing reminder can name it (a forest must
        // not be furnished with town streets). Every visit builds a controller, so the ambient value
        // cannot go stale behind a move.
        //
        // The childhood reminescence is narrated from the foot of a tree on a plain, but everything it
        // narrates is a memory from somewhere else — so the place clause ("You are in a vast plain —
        // write only what belongs there") had the memories dissolve "into the vastness of the plain":
        // 5 of 15 recorded rewrites, 0 of 60 without the clause (tools/prompts/setting_leak.py). The
        // get-up phase that follows really is on the plain, and its own scene sets it again.
        SceneSetting.SetPlace(scene.Phase == NarrationPhase.ChildhoodReminescence
            ? null : _worldContext.GenerateContextDescription(locationId));

        _observationController = new ObservationController(llamaServer, slotManager, _worldContext)
        {
            MustCommit = () => InOpeningPhase,
        };

        (_scene, _graph, _npcPlacement) = (null!, null!, null!);
        AdoptScene(scene);

        LlmMonitorDebugManager.Show();
        SceneDebugManager.Show(scene, _pov, locationId);
        Console.WriteLine($"NarrativeController: {scene.AllAreas.Count} area(s), party of {_minds.Count}, "
                        + $"protagonist holds {_protagonist.ModiMentis.Count} modi mentis");
    }

    /// <summary>
    /// Takes <paramref name="scene"/> as the place narrated: its graph, its people's placement, and
    /// every member standing where it is entered with nothing yet observed.
    /// </summary>
    private void AdoptScene(Scene.Scene scene)
    {
        _scene        = scene;
        _graph        = new SceneSyntheticGraphFactory(scene, _locationId, _protagonist).GenerateGraph(_locationId);
        _npcPlacement = new SceneNpcPlacement(scene, _graph.AllNodes.Values);

        // The area the graph opened on — the first the factory built, or the one --start-area names.
        var entry = SceneSyntheticGraphFactory.ResolveEntryArea(scene)
                    ?? throw new InvalidOperationException("Scene has no areas — nothing to narrate");
        _minds.Clear();
        _minds[_protagonist] = new MemberMind(_protagonist, new PoV(entry, TimePeriod.Morning));
        // Companions take part in a real place only: the opening phases are the protagonist's alone.
        if (scene.Phase == NarrationPhase.Exploration)
            foreach (var companion in _protagonist.CompanionParty)
                _minds[companion] = new MemberMind(companion, new PoV(entry, TimePeriod.Morning));
        _active = _protagonist;

        StandMyself();
    }

    /// <summary>
    /// The opening phases are played through the one who lives them: "myself" is on the map from
    /// the start, observed by nobody, and holds what the phase is about. Nothing in a real place.
    /// </summary>
    private void StandMyself()
    {
        var here = _pov.Where;
        if (Mind.Roots.Any(r => r.Element is Scene.GetUp.SelfPointOfInterest)) return;
        if (NodeForArea(here).PossibleOutcomes.OfType<SyntheticObservationObject>()
                .FirstOrDefault(o => o.PointOfInterest is Scene.GetUp.SelfPointOfInterest) is { } myself)
            Root(Mind, myself, here, text: null, note: ((Scene.GetUp.SelfPointOfInterest)myself.PointOfInterest).Note);
    }

    /// <summary>
    /// Brings the members played here into line with the party: somebody recruited during the visit
    /// stands where the member who won them over stands, with nothing observed yet; somebody dead or
    /// sent away is gone with their trees, and the protagonist is played in their place. True when
    /// anything changed. The opening phases are the protagonist's alone.
    /// </summary>
    private bool SyncRoster()
    {
        if (_scene.Phase != NarrationPhase.Exploration) return false;
        bool changed = false;
        foreach (var companion in _protagonist.CompanionParty)
            if (!_minds.ContainsKey(companion))
            {
                _minds[companion] = new MemberMind(companion, new PoV(_pov.Where, Period));
                changed = true;
            }
        foreach (var gone in _minds.Keys.Where(m => !ReferenceEquals(m, _protagonist)
                                                 && !_protagonist.CompanionParty.Contains(m)).ToList())
        {
            if (ReferenceEquals(gone, _active)) _active = _protagonist;
            _minds.Remove(gone);
            changed = true;
        }
        return changed;
    }

    // ── What the game controller reads ───────────────────────────────────────

    /// <summary>The members standing in <paramref name="area"/>, the one played first when they are there.</summary>
    public IReadOnlyList<PartyMember> MembersAt(Area area)
        => _minds.Values.Where(m => m.Pov.Where.Id == area.Id).Select(m => m.Member)
                 .OrderBy(m => ReferenceEquals(m, _active) ? 0 : 1).ToList();

    /// <summary>Where <paramref name="member"/> stands in the place, or null for somebody not played here.</summary>
    public Area? AreaOf(PartyMember member) => _minds.TryGetValue(member, out var mind) ? mind.Pov.Where : null;

    /// <summary>The scene narrated.</summary>
    public Scene.Scene Scene => _scene;

    /// <summary>Where the played member stands, and the hour.</summary>
    public PoV CurrentPoV => _pov;

    /// <summary>The protagonist of the run.</summary>
    public Protagonist Protagonist => _protagonist;

    /// <summary>The world context of the place (for dialogue template fields).</summary>
    public WorldContext WorldContext => _worldContext;

    /// <summary>The place's vertex id.</summary>
    public int LocationId => _locationId;

    /// <summary>The member being played.</summary>
    public PartyMember ActingMember => _active;

    /// <summary>Whether the visit is over and the party is to leave for the world.</summary>
    public bool HasRequestedExit() => _requestedExit;

    /// <summary>Whether a modal popup (modus mentis / item / choice) is open over the map.</summary>
    public bool IsPopupVisible => _modusMentisPopup.IsVisible || _itemSelectionPopup.IsVisible || _choicePopup.IsVisible;

    /// <summary>
    /// The text history of the conversations held this visit: each writes its lines into it, so a
    /// second conversation scrolls back over the first. The narration itself keeps no text — its
    /// texts are on the trees.
    /// </summary>
    public NarrationScrollBuffer ScrollBuffer => _dialogueBuffer ??= NewDialogueBuffer();
    private NarrationScrollBuffer? _dialogueBuffer;

    private NarrationScrollBuffer NewDialogueBuffer()
    {
        var terminal = _core.Terminal!;
        var layout = new NarrativeLayout(terminal.Width, terminal.Height,
            Config.NarrativeUI.TopPadding, Config.NarrativeUI.BottomPadding,
            Config.NarrativeUI.LeftPadding, Config.NarrativeUI.RightPadding);
        var buffer = new NarrationScrollBuffer(maxWidth: layout.CONTENT_WIDTH - 1, layout: layout);
        buffer.BlockAdded += block => JournalRecorder.Narration(block, _active);
        return buffer;
    }

    /// <summary>
    /// The two phases a run opens on — the childhood reminescence and the get-up — which keep no
    /// budget, offer no way out, and whose one act is the only way onward.
    /// </summary>
    private bool InOpeningPhase => _scene.Phase is NarrationPhase.ChildhoodReminescence or NarrationPhase.GetUp;

    private bool Exploring => _scene.Phase == NarrationPhase.Exploration;

    /// <summary>
    /// Whether the played member is standing somewhere they have no business being — what the header
    /// shows, and the condition under which nothing they do is lawful and no routine is learned.
    /// </summary>
    public bool IsTrespassing => Exploring && PrivacyModel.IsTrespassing(_scene, _pov.Where);

    // ── The visit ────────────────────────────────────────────────────────────

    /// <summary>
    /// Opens the visit: everybody where the place is entered, at <paramref name="forcedPeriod"/>, else
    /// <c>--period</c>, else the place's own hour (<paramref name="arrivalPeriod"/>,
    /// <see cref="Scene.ArrivalPeriod"/>), else a drawn one. The draw is made even when the arrival
    /// hour wins and its result discarded, so every later roll of a seeded run is the roll it was
    /// before arrival hours existed.
    /// </summary>
    public void StartVisit(TimePeriod? forcedPeriod = null, TimePeriod? arrivalPeriod = null)
    {
        ResetSession();
        var drawn = TimePeriodExtensions.Random(_diceRandom);
        var period = forcedPeriod ?? Config.Debug.ForcedPeriod ?? arrivalPeriod ?? drawn;
        ApplyTimePeriod(period);
        Console.WriteLine($"NarrativeController: visit opens at {period} in '{_pov.Where.DisplayName}'");

        ArmRoutineRecorder();
        // An arrival is shown coming in by the place's way in.
        if (arrivalPeriod != null && Exploring) ShowEntryWalk();
    }

    /// <summary>
    /// Opens the visit in a routine's own area and hour — "you know the way" — and shows the way
    /// walked. <paramref name="then"/> runs once the party has arrived: a Go to routine passes none,
    /// a trade or a work routine opens its menu there.
    /// </summary>
    public void StartAtArea(string? areaKey, TimePeriod period, Action? then = null)
    {
        ResetSession();
        var entry = _pov.Where;
        if (FindArea(areaKey) is { } area)
            foreach (var mind in _minds.Values) mind.Pov.Where = area;
        ApplyTimePeriod(period);
        Console.WriteLine($"NarrativeController: routine opens at {period} in '{_pov.Where.DisplayName}'");

        ArmRoutineRecorder();
        WalkTheApproach(entry, then);
    }

    private void ResetSession()
    {
        _error = null;
        _loading = null;
        _requestedExit = false;
        _pending = null;
        _result = null;
        ActionPathPlanner.Forget();
        foreach (var mind in _minds.Values)
        {
            mind.Roots.Clear();
            mind.Strays.Clear();
            mind.Refill();
        }
        _active = _protagonist;
        StandMyself();
    }

    /// <summary>
    /// The area a routine names: by display name first, the only per-location unique identifier
    /// (<c>--building-audit</c> holds the names apart), then by lemma, so a key recorded against an
    /// older build still finds something. By name rather than by instance because the scene the
    /// routine was learned in has been rebuilt since. Null when nothing matches.
    /// </summary>
    private Area? FindArea(string? areaKey)
    {
        if (string.IsNullOrWhiteSpace(areaKey)) return null;
        var area = _scene.AllAreas.FirstOrDefault(a => string.Equals(a.DisplayName, areaKey, StringComparison.OrdinalIgnoreCase))
                ?? _scene.AllAreas.FirstOrDefault(a => string.Equals(a.ReferenceLemma, areaKey, StringComparison.OrdinalIgnoreCase));
        if (area == null)
            Console.Error.WriteLine($"NarrativeController: no area matching '{areaKey}' in the rebuilt scene");
        return area;
    }

    /// <summary>Starts learning routines for this visit, from where it opened. Exploration only.</summary>
    private void ArmRoutineRecorder()
    {
        if (Exploring)
            _recorder = new RoutineRecorder(_protagonist, _locationId, _worldContext.DisplayName, _pov.Where);
    }

    // ── Routine moments the game controller sees first ─────────────────────────
    // A trade and a hire are settled outside this class (the dialogue runs in its own mode, and
    // --auto-dialogue settles it without one), so the controller reports them here; the point of
    // view they were agreed at is this class's.

    /// <summary>A merchant agreed to trade.</summary>
    public void RecordTradeRoutine(NpcEntity npc, Cathedral.Game.Npc.Trade.TradeMode mode) => _recorder?.OnTrade(_pov, npc, mode);

    /// <summary>A master agreed to take the player on.</summary>
    public void RecordWorkRoutine(NpcEntity npc, Cathedral.Game.Narrative.Work.Job job) => _recorder?.OnWork(_pov, npc, job);

    /// <summary>Learns the Craft routine for the station a successful USE opened, where the player may stand.</summary>
    public void RecordCraftRoutine(Cathedral.Game.Narrative.Crafting.CraftingRequest request)
        => _recorder?.OnCraftingOpened(request, _scene, _pov, _active);

    /// <summary>
    /// Lets the visit's recorder learn its last routine — where the player is leaving from. Called by
    /// the game controller when the visit ends.
    /// </summary>
    public void FinalizeRoutineRecording()
    {
        _recorder?.FinalizeAtNarrationEnd(_pov);
        _recorder = null;
    }

    // ── The hour, and the scene's upkeep ─────────────────────────────────────

    /// <summary>
    /// Applies a time period: records it on the graph <b>and on every member's point of view</b>,
    /// puts the scene's people where their day has them at that hour (<see cref="SceneNpcPlacement"/>),
    /// re-expands every element's verbs against the world as it now stands, and brings the trees into
    /// line with it.
    ///
    /// <para>This is the <b>single writer</b> of the period. <c>NarrationGraph.CurrentPeriod</c> and
    /// <c>PoV.When</c> are views of one fact — placement reads the first, every verb's
    /// <c>IsPossible</c> the second — so nothing else may set either. A verb that shifts time returns
    /// a <c>TimeShiftOutcome</c>, which writes the acting member's point of view;
    /// <see cref="CommitOutcomeResult"/> notices and routes it back through here.</para>
    /// </summary>
    private void ApplyTimePeriod(TimePeriod period)
    {
        _graph.SetCurrentPeriod(period);
        foreach (var mind in _minds.Values) mind.Pov.When = period;
        // A place at night plays its night music; the opening phases are not a place.
        _ambianceEngine?.SetNight(Exploring && period == TimePeriod.Night);
        if (Exploring)
            foreach (var mind in _minds.Values) AreaVisits.Arrive(_scene, mind.Pov.Where);
        ReconcileTrees();
    }

    /// <summary>The hour.</summary>
    private TimePeriod Period => _graph.CurrentPeriod;

    /// <summary>
    /// The graph node standing for <paramref name="area"/>, matched on area identity. Node ids are
    /// display-name slugs, readable in logs and not unique: every building has its own hall and
    /// bedrooms, so routing goes by identity.
    /// </summary>
    private SyntheticNarrationNode NodeForArea(Area area)
        => (SyntheticNarrationNode)_graph.AllNodes.Values.First(
            n => n is SyntheticNarrationNode { Area: { } a } && a.Id == area.Id);

    /// <summary>
    /// Re-expands the verbs of every element of the scene against the world as it now stands, at the
    /// hour: each area's from that area's own point of view, for the member being played. Called on
    /// every change of hour and of member, after every outcome, and before each thinking, so the acts
    /// on offer are always the ones possible — and the people the ones present — now.
    /// </summary>
    private void RefreshSceneVerbs()
    {
        foreach (var node in _graph.AllNodes.Values.OfType<SyntheticNarrationNode>())
        {
            if (node.Area is not { } area) continue;
            SyncSpawnedObservations(node, area);
            var pov = new PoV(area, Period);
            foreach (var outcome in node.PossibleOutcomes)
            {
                // Stamp before refreshing: an element whose text depends on the hour (a door saying
                // whether it looks locked) must describe the same period its verbs were gated at, or
                // the player reads "seems open" and is offered UNLOCK.
                (outcome as IPeriodStampable)?.StampPeriod(Period);
                // Gated against the member who will act: a beast must not be offered the verbs its
                // body cannot perform (every dialogue verb, everything needing hands).
                (outcome as IVerbRefreshable)?.RefreshVerbs(_scene, pov, _active);
            }
        }
    }

    /// <summary>
    /// Brings a node's point-of-interest observations into line with the points of interest its area
    /// actually holds: one for each that has none, none for one that has left.
    ///
    /// <para>The graph is built once, from the areas as the factory left them — so what the game
    /// spawns during play (a corpse) has no observation object until this gives it one, and what it
    /// swaps (a bed for its sleeper) keeps a stale one until this takes it away.</para>
    /// </summary>
    private static void SyncSpawnedObservations(SyntheticNarrationNode node, Area area)
    {
        node.PossibleOutcomes.RemoveAll(
            o => o is SyntheticObservationObject obs && !area.PointsOfInterest.Contains(obs.PointOfInterest));

        var present = node.PossibleOutcomes.OfType<SyntheticObservationObject>()
                          .Select(o => o.PointOfInterest).ToHashSet();
        foreach (var poi in area.PointsOfInterest)
        {
            if (present.Contains(poi)) continue;
            // Empty verb lists: the refresh that follows expands the real ones, for the point of
            // interest and for every item in it, at the hour in force.
            var entry = new SceneViewEntry(poi, new List<VerbAction>());
            var items = poi.Items.Select(ie => new SceneViewEntry(ie, new List<VerbAction>())).ToList();
            node.PossibleOutcomes.Add(new SyntheticObservationObject(poi, entry, items, area));
        }
    }

    // ── Failure ──────────────────────────────────────────────────────────────

    /// <summary>
    /// The one way a step reports that it could not finish: writes a full crash report, preserves the
    /// run's log under a name the next launch will not overwrite, and returns the text to show the
    /// player.
    ///
    /// <para><b>The narration stays dead on purpose.</b> There is no retry and no way to continue —
    /// the screen draws this message and nothing else, leaving Escape (the pause menu, and so the way
    /// out) as the only input. A run that could be resumed is a run the player will resume, and then
    /// <c>log.txt</c> grows for another hour, the failure ends up buried in the middle of it, and the
    /// next launch truncates it entirely. Stopping is what produces a report worth reading.</para>
    ///
    /// <para>The message names the preserved file, because a bug report without it costs a round
    /// trip and usually the log as well.</para>
    /// </summary>
    private static string ReportPhaseFailure(string what, Exception ex)
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

    /// <summary>A step failed: the preview is taken down and the narration stops on the report.</summary>
    private void Fail(string what, Exception ex)
    {
        Console.Error.WriteLine($"NarrativeController: error while {what}: {ex.Message}");
        _previewSession.Reset();
        _loading = null;
        NarrationDiceClear();
        _error = ReportPhaseFailure(what, ex);
    }

    // ── Spending ─────────────────────────────────────────────────────────────

    /// <summary>Takes a noetic point from the played member, where the phase keeps a budget.</summary>
    private void SpendPoint()
    {
        if (!InOpeningPhase) Mind.Noetic = Math.Max(0, Mind.Noetic - 1);
    }

    /// <summary>Whether the played member has a point to spend, or is in a phase that asks none.</summary>
    private bool HasPoint => InOpeningPhase || Mind.Noetic > 0;

    /// <summary>
    /// Puts a text a mind wrote in the journal, credited to the member who was played when it was
    /// written. The trees are the screen's; the journal is what is kept.
    /// </summary>
    private void Chronicle(NarrationBlock block) => JournalRecorder.Narration(block, _active);
}
