using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Cathedral.Audio;
using Cathedral.Glyph;
using Cathedral.Glyph.Microworld;
using Cathedral.Glyph.Microworld.LocationSystem;
using Cathedral.Glyph.Microworld.LocationSystem.Generators;
using Cathedral.Glyph.Interaction;
using Cathedral.LLM;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Narrative.ModiMentis;
using Cathedral.Game.Narrative.Sanitizer;
using Cathedral.Game.Npc;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Plain;
using Cathedral.Game.Creation;
using Cathedral.Game.Management;
using Cathedral.Game.Dialogue;
using Cathedral.Game.Dialogue.Runtime;
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
    private readonly AmbianceEngine? _ambianceEngine;
    private string? _lastHoveredElementId; // fires hover sound only when element identity changes
    private int _mouseCellX = -1;
    private int _mouseCellY = -1;
    
    // Chain-of-Thought narrative system
    private NarrativeController? _narrativeController = null;
    private bool _isInNarrativeMode = false;
    private ModusMentisSlotManager? _modusMentisSlotManager = null;
    private ThinkingExecutor? _thinkingExecutor = null;
    
    // Embedded fight/dialogue adapters
    private FightModeAdapter? _fightAdapter = null;
    private DialogueTreeAdapter? _dialogueAdapter = null;
    private TradeMenuAdapter? _tradeAdapter = null;
    private WorkMenuAdapter?  _workAdapter  = null;
    
    // LLM loading screen
    private LLMLoadingRenderer? _llmLoadingRenderer;
    private volatile bool _llmBecameReady = false;
    private volatile float _llmLoadProgress = 0f;
    private string _llmLoadStatus = "Starting...";
    private readonly object _llmLoadLock = new object();

    // Main menu
    private MainMenuRenderer? _mainMenuRenderer;
    private SettingsMenuRenderer? _settingsMenuRenderer;
    private bool _hasGameStarted = false;

    // World selection — the sky a new run picks its world out of.
    private WorldSelectionRenderer? _worldSelectionRenderer;
    /// <summary>Ordinal of the moon the player has clicked and not yet confirmed, or -1.</summary>
    private int _selectedMoon = -1;
    /// <summary>Ordinal of the moon under the cursor, or -1. Purely for the box's preview line.</summary>
    private int _hoveredMoon = -1;

    // The four on-screen camera arrows, shared by the world map and the world-selection sky. One
    // instance: only one of those two modes is ever on screen, and the pad has no per-mode state.
    private CameraArrowPad? _cameraArrowPad;
    /// <summary>The arrow being held down with the mouse, turning the camera every frame.</summary>
    private CameraArrow _heldArrow = CameraArrow.None;
    
    // Protagonist creation
    private ProtagonistCreationRenderer? _protagonistCreationRenderer;
    private BodyArtData? _bodyArtData;
    private Protagonist? _protagonist;
    
    // Protagonist management
    private ManagementMenuRenderer? _managementMenuRenderer;

    // Companion-capacity gate shown at the start of WorldView (over max companions).
    private CompanionRemovalRenderer? _companionRemovalRenderer;

    // Old-age notice shown at the start of WorldView when companions have outlived their lifetime.
    private CompanionDeathBox? _companionDeathBox;

    // Routine replay: list box (travel UI), outcome box (after replay), engine and pending state.
    private TravelRoutinesBox? _travelRoutinesBox;
    private RoutineOutcomeBox? _routineOutcomeBox;
    private readonly Cathedral.Game.Narrative.Routines.RoutineReplayEngine _routineReplayEngine = new();
    private Cathedral.Game.Narrative.Routines.Routine? _pendingReplayRoutine;
    private Cathedral.Game.Narrative.PhaseTransition? _replayFinalTransition;

    // Game state
    private GameMode _currentMode;
    private LocationInstanceState? _currentLocationState;
    private int _currentLocationVertex = -1;

    /// <summary>
    /// The last location the player was actually inside, kept after they leave it (unlike
    /// <see cref="_currentLocationVertex"/>, which resets to -1). Exists for the CLI's
    /// <c>travel back</c>: a round trip is the only way to reach routine replay, and a script cannot
    /// name the vertex it started on — it is whatever the seed put under the avatar.
    /// </summary>
    private int _lastLocationVertex = -1;

    /// <summary>The location entered before <see cref="_lastLocationVertex"/>, for the same reason.</summary>
    private int _previousLocationVertex = -1;

    /// <summary>
    /// The vertex <c>travel back</c> plans a route to: the most recently entered location that is
    /// not the one under the avatar's feet. Two are kept because a round trip ends standing on the
    /// second, and "back" then has to mean the first. -1 before any location was entered.
    /// </summary>
    public int CliLastLocationVertex
        => _lastLocationVertex >= 0 && _lastLocationVertex != _interface.GetAvatarVertex()
            ? _lastLocationVertex
            : _previousLocationVertex;
    private int _destinationVertex = -1;

    // Travel planning (waypoints + bottom UI)
    private TravelPlanner? _travelPlanner;
    private TravelInfoRenderer? _travelInfoRenderer;
    private Cathedral.Pathfinding.AStar? _travelAStar;
    // Latest planner state cached for rendering
    private List<int>? _plannedPath;
    private TravelEstimate? _plannedEstimate;
    // In-game days for the in-flight trip, captured at travel start, applied to the clock on arrival.
    private float _committedTravelDays;
    // Tracks a cell that was flashed as "forbidden" so it can be cleared after a tick.
    private int _forbiddenFlashVertex = -1;
    private int _forbiddenFlashFramesLeft = 0;

    // Travel progress — vital heat consumption during Traveling mode
    private TravelProgressRenderer? _travelProgressRenderer;
    private readonly Random _travelRng = GameRng.For("travel");
    private float _tripVhRequired;
    private float _tripVhConsumedNet;
    private float _tripVhDebt;

    // Per-frame consumption state
    private bool   _consumptionActive    = false;
    private bool   _locationBatchNewFrame = false; // true on the first frame of a new batch — delays first consumption by one frame
    private string _consumptionBiome     = "unknown";
    private float  _locationVhConsumed   = 0f; // VH consumed within the current location batch (floor 0)
    private float  _locationVhRequired   = 1f; // VH debt at the moment the current batch started
    private int    _tripTotalCells       = 0;  // total path cells for the current trip
    private int    _tripCellsTraveled    = 0;  // cells stepped so far this trip

    // Travel encounter state
    private bool _inTravelEncounter = false;
    private EncounterPromptRenderer? _encounterPromptRenderer;
    private Cathedral.Game.Npc.NpcEntity? _pendingEncounterNpc;
    private string? _pendingEncounterCreatureName;

    /// <summary>
    /// The vertex an arrival was announced for while the mode had already left
    /// <see cref="GameMode.Traveling"/> — an encounter rolled on the journey's <em>final</em> step.
    /// -1 when there is none. Delivered by <see cref="OnFightCompleted"/> once the encounter is
    /// settled; see the comment in <see cref="OnProtagonistArrived"/> for why it cannot simply be
    /// re-fired by the interface.
    /// </summary>
    private int _pendingArrivalVertex = -1;

    // Maps BiomeTravelDatabase creature names to the archetype that spawns them.
    // Creature names without a fight-capable archetype (e.g. "blizzard") are absent and skipped.
    private static readonly Dictionary<string, Func<Cathedral.Game.Npc.NamedNpcArchetype>> TravelEncounterArchetypes = new()
    {
        ["wolf"]    = () => new Cathedral.Game.Npc.Archetypes.WolfArchetype(),
        ["bear"]    = () => new Cathedral.Game.Npc.Archetypes.BearArchetype(),
        ["bandit"]  = () => new Cathedral.Game.Npc.Archetypes.SavageArchetype(),
        ["brigand"] = () => new Cathedral.Game.Npc.Archetypes.SavageArchetype(),
    };

    // Death screen
    private DeathScreenRenderer? _deathScreenRenderer;
    private DeathCause _deathCause;
    
    // Location state storage (keyed by vertex index)
    private readonly Dictionary<int, LocationInstanceState> _locationStates = new();

    
    // Feature generators for different location types
    private readonly Dictionary<string, LocationFeatureGenerator> _generators = new();
    
    // Narration graph factories for different biomes
    private readonly Dictionary<string, NarrationGraphFactory> _narrationFactories = new();

    // Scene factories for biomes that use the Scene system directly (not graph-based). Constructors,
    // not instances: a factory carries working state while it builds and must not be reused.
    private readonly Dictionary<string, Func<SceneFactory>> _sceneFactories = new();
    
    // Local LLM server; when set, narrative generation is enabled. Null = no LLM (fallback narration).
    private LlamaServerManager? _llamaServer;
    private ItemUseCritic? _criticEvaluator;

    /// <summary>Where the Catalyst English models are stored — beside the executable.</summary>
    private static string CatalystModelPath
        => System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "catalyst-models");

    /// <summary>
    /// The POS tagger and the word vectors, started in the constructor because neither needs the
    /// LLM server. Awaited by the sanitization task in <see cref="SetLlamaServer"/> so the two do
    /// not enter Catalyst's initialisation concurrently. See the constructor for what starting it
    /// from <c>SetLlamaServer</c> instead used to cost.
    /// </summary>
    private readonly Task? _keywordInitTask;

    // Events
    public event Action<GameMode, GameMode>? ModeChanged;
    public event Action<LocationInstanceState>? LocationExited;
    public event System.Action? TravelStarted;
    public event System.Action? TravelCompleted;

    // Properties
    public GameMode CurrentMode => _currentMode;
    public LocationInstanceState? CurrentLocationState => _currentLocationState;
    public bool IsAtLocation => _currentMode == GameMode.LocationInteraction && _currentLocationState != null;
    public bool HasGameStarted => _hasGameStarted;
    /// <summary>
    /// Mode to resume when the main menu is dismissed. When a conversation is still alive underneath
    /// (the menu was opened as a pause overlay during dialogue), resume into it; when narration is
    /// alive, resume into that; otherwise fall back to the world. Derived from live state
    /// (<see cref="_dialogueAdapter"/> / <see cref="_isInNarrativeMode"/>) so intermediate menu
    /// navigation can't clobber it.
    /// </summary>
    public GameMode MenuReturnMode =>
        // A live fight outranks everything: it is running underneath the menu, and returning to
        // WorldView instead would strand it — the fight would still be there, unreachable.
        _fightAdapter != null    ? GameMode.Fighting
        : _dialogueAdapter != null ? GameMode.Dialogue
        : _workAdapter != null   ? GameMode.Working
        : _tradeAdapter != null  ? GameMode.Trading
        // Narration covers THREE phases — exploration, childhood and get-up — all of which run on
        // one _narrativeController with _isInNarrativeMode set. Returning a flat LocationInteraction
        // for all three is what made the pause menu unusable before the world opens: resuming from
        // childhood would have entered exploration's mode with childhood's controller underneath.
        : _isInNarrativeMode     ? _narrativePhase
        // Not narration, but still not the world: the creation screen and the encounter box are
        // both full-screen phases the player is in the middle of, each identified by the live
        // object that draws it.
        : _protagonistCreationRenderer != null ? GameMode.ProtagonistCreation
        : _pendingEncounterNpc != null         ? GameMode.EncounterPrompt
        :                          GameMode.WorldView;

    /// <summary>
    /// Which of the three narrative phases <see cref="_narrativeController"/> is currently running.
    /// Only meaningful while <see cref="_isInNarrativeMode"/> — the phases are otherwise
    /// indistinguishable, because they share one controller and differ only in the scene they were
    /// built from and in which "finished" flag ends them.
    /// </summary>
    private GameMode _narrativePhase = GameMode.LocationInteraction;

    /// <summary>
    /// ESC in a fight: cancel whatever the player has armed. Returns false when there was nothing
    /// to cancel, which is the launcher's cue to open the pause menu instead.
    /// </summary>
    public bool CliTryCancelFightSelection()
        => _currentMode == GameMode.Fighting && _fightAdapter?.TryCancelSelection() == true;
    
    /// <summary>
    /// Gets the terminal input handler for coordinate conversion (null if no terminal).
    /// </summary>
    public TerminalInputHandler? GetTerminalInputHandler() => _core.Terminal?.InputHandler;

    // ── CLI driving surface (--cli) ───────────────────────────────────────────
    // Accessors the command driver needs. Kept here rather than making the sub-controllers public
    // so the driver has a single seam onto the game.

    /// <summary>The terminal the CLI dumps as text.</summary>
    public TerminalHUD? CliTerminal => _core.Terminal;

    /// <summary>The live narration controller, or null outside a narration session.</summary>
    public NarrativeController? CliNarration => _narrativeController;

    /// <summary>The live dialogue adapter, or null when not in a conversation.</summary>
    public DialogueTreeAdapter? CliDialogue => _dialogueAdapter;

    /// <summary>The live fight adapter, or null when not fighting.</summary>
    public Fight.FightModeAdapter? CliFight => _fightAdapter;

    /// <summary>The world/travel interface, for world-state reporting and travel commands.</summary>
    public MicroworldInterface CliWorld => _interface;

    /// <summary>
    /// Press the encounter prompt's ENGAGE button. Returns an error string, or null on success.
    ///
    /// <para>ENGAGE is the encounter's only button, so without this a script that stages an
    /// encounter can never get past the prompt — and the fight it was staging is unreachable.</para>
    /// </summary>
    public string? CliEngageEncounter()
    {
        if (_currentMode != GameMode.EncounterPrompt) return "not at an encounter prompt";
        if (_encounterPromptRenderer?.EngageButtonCell is not { } cell) return "no engage button drawn";
        OnTerminalCellClicked(cell.X, cell.Y);
        return null;
    }

    /// <summary>
    /// True while the companion-death notice is up. It is modal over every mode, so a script that
    /// does not know it can appear will sit clicking at a screen that is answering nothing —
    /// `state` reports it for exactly that reason.
    /// </summary>
    public bool CliCompanionDeathShown => _companionDeathBox != null;

    /// <summary>
    /// Wound a party member outside a fight (<c>--cli</c> <c>wound</c>), from their own anatomy's
    /// catalogue, as a failed act's penalty does. <paramref name="who"/> is <c>protagonist</c>,
    /// <c>companions</c>, or a name matched by prefix; <paramref name="count"/> of 0 means "enough to
    /// kill". Returns an error string, or null on success.
    ///
    /// <para>The narration counterpart of <c>fight-wound</c>, and needed for the same reason at one
    /// remove: a wound is <em>sampled</em> from a verb's failure penalties, one slot in five, so
    /// whether a given failed act injures anybody is not something a script can ask for — and
    /// stacking three of them on one body to reach zero health is not something it can ask for
    /// three times. The <em>consequence</em> is what wants testing: a mortal wound taken in
    /// narration ends the run, and one taken by the companion who is narrating takes them out of the
    /// party mid-visit.</para>
    /// </summary>
    public string? CliWound(string who, int count)
    {
        if (_protagonist == null) return "no protagonist";

        var targets = who.ToLowerInvariant() switch
        {
            "protagonist" => new List<Cathedral.Game.Narrative.PartyMember> { _protagonist },
            "companions"  => _protagonist.CompanionParty.ToList(),
            _             => _protagonist.EveryMember
                                 .Where(m => m.DisplayName.StartsWith(who, StringComparison.OrdinalIgnoreCase))
                                 .ToList(),
        };
        if (targets.Count == 0) return $"no party member matching \"{who}\"";

        foreach (var member in targets)
        {
            var catalogue = Cathedral.Game.Narrative.WoundRegistry.ForAnatomy(member).ToList();
            if (catalogue.Count == 0) return $"{member.DisplayName}'s anatomy has no wounds to suffer";

            // Bounded even when asked for "enough to kill": health is MaxHp minus the wound count,
            // so this terminates — but a catalogue that ever stopped adding would hang the game
            // rather than fail a test.
            int wanted = count > 0 ? count : member.CurrentHp;
            for (int i = 0; i < wanted && i < member.MaxHp + 8; i++)
            {
                var wound = catalogue[GameRng.Stream("cli-wound").Next(catalogue.Count)];
                member.Wounds.Add(Cathedral.Game.Narrative.WoundInstance.Inflicted(wound));
            }
            Console.WriteLine($"[cli] wounded {member.DisplayName} — hp now {member.CurrentHp}/{member.MaxHp}");
        }
        return null;
    }

    /// <summary>
    /// Break a named modus mentis on a party member (<c>--cli</c> <c>cripple &lt;mm-id&gt; [who]</c>) by
    /// inflicting a High-handicap wound on every anatomy source it draws on — which drives its
    /// max-level contribution to −2 apiece and its effective level to 0 or below. See
    /// <c>BrokenModusMentis</c>.
    ///
    /// <para><b>Why this is a command and not a matter of calling <c>wound</c> a few times.</b> A
    /// wound is drawn at random from the anatomy's catalogue, so asking for one that lands on the
    /// eyes — and lands there at High rather than Medium — is a lottery a script cannot run. Breaking
    /// a <em>particular</em> modus mentis is the whole precondition of every test in this area, and
    /// every one of them would otherwise be a different flaky search through the wound table.</para>
    ///
    /// <para>The wound chosen per source is the first High-handicap one in that body's own catalogue
    /// that affects it, so a beast gets a beast's wound: <c>WoundRegistry.ForAnatomy</c> is what keeps
    /// an impossible wound off a body, and a debug command that bypassed it would stage a save the
    /// game cannot load (see "Wounds and healing"). A source with no such wound is reported and
    /// skipped rather than silently passed over.</para>
    /// </summary>
    public string? CliCripple(string modusMentisId, string who)
    {
        if (_protagonist == null) return "no protagonist";

        var targets = who.ToLowerInvariant() switch
        {
            "protagonist" => new List<Cathedral.Game.Narrative.PartyMember> { _protagonist },
            "companions"  => _protagonist.CompanionParty.ToList(),
            _             => _protagonist.EveryMember
                                 .Where(m => m.DisplayName.StartsWith(who, StringComparison.OrdinalIgnoreCase))
                                 .ToList(),
        };
        if (targets.Count == 0) return $"no party member matching \"{who}\"";

        foreach (var member in targets)
        {
            var mm = member.ModiMentis.FirstOrDefault(m =>
                m.ModusMentisId.Equals(modusMentisId, StringComparison.OrdinalIgnoreCase));
            if (mm == null) return $"{member.DisplayName} does not hold modus mentis \"{modusMentisId}\"";

            var catalogue = Cathedral.Game.Narrative.WoundRegistry.ForAnatomy(member).ToList();

            foreach (var sourceId in mm.Organs)
            {
                var organ     = member.GetOrganById(sourceId);
                var bodyPartId = organ != null
                    ? member.BodyParts.FirstOrDefault(bp => bp.Organs.Any(o => o.Id == sourceId))?.Id ?? ""
                    : sourceId;

                var wound = catalogue.FirstOrDefault(w =>
                    w.Handicap == Cathedral.Game.Narrative.WoundHandicap.High
                    && (organ != null ? w.AffectsOrgan(sourceId, bodyPartId)
                                      : w.AffectsBodyPart(sourceId)));

                if (wound == null)
                {
                    Console.WriteLine($"[cli] cripple: no High wound in {member.DisplayName}'s catalogue " +
                                      $"affects '{sourceId}' — skipped");
                    continue;
                }

                member.Wounds.Add(Cathedral.Game.Narrative.WoundInstance.Inflicted(wound));
                Console.WriteLine($"[cli] cripple: {wound.WoundName} on {member.DisplayName}'s {sourceId}");
            }

            Console.WriteLine($"[cli] {mm.DisplayName} on {member.DisplayName} — level {mm.Level}, " +
                              $"max now {member.GetMaxLevelForModusMentis(mm)}, " +
                              $"effective {member.GetEffectiveModusMentisLevel(mm)}" +
                              $"{(member.IsModusMentisBroken(mm) ? " — BROKEN" : "")}");
        }
        return null;
    }

    /// <summary>
    /// Sour a party member's humors to fully critical (<c>--cli</c> <c>starve</c>), the state a long
    /// bleed or a fight's worth of buffs arrives at. Same <paramref name="who"/> vocabulary as
    /// <see cref="CliWound"/>. Returns an error string, or null on success.
    ///
    /// <para>The humoral twin of <c>wound</c>, and the reason it is a command rather than a flag:
    /// <c>--black-bile</c> sours the queues at the moment the protagonist is accepted, which is
    /// before the world has been touched — so it can only ever stage a body that was <em>always</em>
    /// starving. What wants testing is a body that starves <em>during</em> a visit, after a save has
    /// been written and with a scene standing around it.</para>
    /// </summary>
    public string? CliStarve(string who)
    {
        if (_protagonist == null) return "no protagonist";

        var targets = who.ToLowerInvariant() switch
        {
            "protagonist" => new List<Cathedral.Game.Narrative.PartyMember> { _protagonist },
            "companions"  => _protagonist.CompanionParty.ToList(),
            _             => _protagonist.EveryMember
                                 .Where(m => m.DisplayName.StartsWith(who, StringComparison.OrdinalIgnoreCase))
                                 .ToList(),
        };
        if (targets.Count == 0) return $"no party member matching \"{who}\"";

        foreach (var member in targets)
        {
            foreach (var queue in member.HumorQueues.All)
                queue.FillWith(new Cathedral.Game.Narrative.BlackBileHumor());
            Console.WriteLine($"[cli] soured {member.DisplayName}'s humors — every queue critical");
        }
        return null;
    }

    /// <summary>
    /// Press the companion-death notice's CONTINUE. Returns an error string, or null on success.
    /// </summary>
    public string? CliDismissCompanionDeath()
    {
        if (_companionDeathBox?.ContinueButtonCell is not { } cell) return "no companion-death notice is up";
        OnTerminalCellClicked(cell.X, cell.Y);
        return null;
    }

    /// <summary>The vertex the protagonist currently occupies.</summary>
    public int CliAvatarVertex => _interface.GetAvatarVertex();

    /// <summary>Inject a click at a terminal cell, as if the player had clicked there.</summary>
    public void CliClickCell(int x, int y) => OnTerminalCellClicked(x, y);

    /// <summary>Inject a hover at a terminal cell (some UI only reacts once hovered).</summary>
    public void CliHoverCell(int x, int y) => OnTerminalCellHovered(x, y);

    /// <summary>Inject a world-map vertex click, bypassing 3D ray picking entirely.</summary>
    public void CliClickVertex(int vertexIndex)
        => OnVertexClicked(vertexIndex, ' ', new OpenTK.Mathematics.Vector4(1, 1, 1, 1), 0f);

    /// <summary>Close the game window (the CLI <c>quit</c> command).</summary>
    public void CliRequestClose() => _core.Close();

    /// <summary>Main-menu buttons with their cell positions, or null outside the menu.</summary>
    public IReadOnlyList<(string Label, bool Enabled, int X, int Y)>? CliMenuButtons()
        => _currentMode == GameMode.MainMenu ? _mainMenuRenderer?.CliButtons() : null;

    /// <summary>
    /// The world-selection screen's two buttons with their cells, or null when that screen is not up.
    /// Same shape as <see cref="CliMenuButtons"/>, so <c>regions</c> can print them the same way.
    /// </summary>
    public IReadOnlyList<(string Label, bool Enabled, int X, int Y)>? CliWorldSelectionButtons()
    {
        if (_currentMode != GameMode.WorldSelection || _worldSelectionRenderer == null) return null;

        var (cx, cy) = _worldSelectionRenderer.ConfirmCell();
        var (kx, ky) = _worldSelectionRenderer.CancelCell();
        return new List<(string, bool, int, int)>
        {
            ("Confirm", _worldSelectionRenderer.ConfirmEnabled, cx, cy),
            ("Cancel",  true,                                   kx, ky),
        };
    }

    /// <summary>How many moons the sky holds, and which one is selected. For <c>regions</c>.</summary>
    public (int Count, int Selected, int Hovered) CliMoonState()
        => (_core.MoonCount, _selectedMoon, _hoveredMoon);

    /// <summary>
    /// Picks a moon by ordinal from a script: turns the camera onto it, then hovers and clicks it
    /// through the real sky picking. Returns an error string, or null on success.
    ///
    /// <para>The camera move is not a shortcut around the picking — it is the arrow pad's job done in
    /// one step. A moon on the far side of the sky is genuinely not clickable until the sky has been
    /// turned, and a script that clicked one anyway would be testing a path no player has.</para>
    /// </summary>
    public string? CliSelectMoon(int ordinal)
    {
        if (_currentMode != GameMode.WorldSelection) return "not on the world-selection screen";
        if (ordinal < 0 || ordinal >= _core.MoonCount)
            return $"no moon with ordinal {ordinal} (the sky holds {_core.MoonCount})";
        if (ordinal == _core.HiddenMoon) return $"moon {ordinal} is the world you are already in";

        if (!_core.FaceMoon(ordinal)) return $"could not turn the sky towards moon {ordinal}";
        if (!_core.TryGetMoonScreenPosition(ordinal, out var screen))
            return $"moon {ordinal} is not on screen even after turning towards it";

        _core.CliPickSkyAt(screen, click: false);
        _core.CliPickSkyAt(screen, click: true);
        return _selectedMoon == ordinal ? null : $"the click landed on moon {_selectedMoon}, not {ordinal}";
    }

    /// <summary>
    /// Moves the cursor onto a moon without pressing, from a script. The hover and the choice are
    /// separate states with separate looks, and this is the only way to drive one without the other
    /// — which is what makes "a choice survives pointing at something else" assertable.
    /// </summary>
    public string? CliPointAtMoon(int ordinal)
    {
        if (_currentMode != GameMode.WorldSelection) return "not on the world-selection screen";
        if (ordinal < 0 || ordinal >= _core.MoonCount)
            return $"no moon with ordinal {ordinal} (the sky holds {_core.MoonCount})";

        if (!_core.FaceMoon(ordinal)) return $"could not turn the sky towards moon {ordinal}";
        if (!_core.TryGetMoonScreenPosition(ordinal, out var screen))
            return $"moon {ordinal} is not on screen even after turning towards it";

        _core.CliPickSkyAt(screen, click: false);
        return _hoveredMoon == ordinal ? null : $"the cursor landed on moon {_hoveredMoon}, not {ordinal}";
    }

    /// <summary>
    /// Presses a patch of empty sky, which releases the chosen moon without leaving the screen.
    /// Returns an error string, or null on success.
    /// </summary>
    public string? CliClickEmptySky()
    {
        if (_currentMode != GameMode.WorldSelection) return "not on the world-selection screen";
        if (!_core.TryFindEmptySkyPoint(out var screen)) return "found no empty patch of sky to press";

        _core.CliPickSkyAt(screen, click: true);
        return _selectedMoon < 0 ? null : $"moon {_selectedMoon} is still chosen";
    }

    /// <summary>
    /// Presses one of the four camera arrows from a script, through the same cell click a player
    /// makes. Returns an error string, or null on success.
    /// </summary>
    public string? CliPressCameraArrow(CameraArrow arrow)
    {
        if (!CameraPadActive) return "the camera arrows are not on screen in this mode";

        var (x, y) = _cameraArrowPad!.CellFor(arrow);
        if (x < 0) return $"no cell for the {arrow} arrow";

        CliHoverCell(x, y);
        CliClickCell(x, y);

        // A scripted press is a tap: nothing is still held down, so the standing rate the pad hands
        // the render loop has to be put back at once. Left standing it would turn the camera for the
        // rest of the run.
        _heldArrow = CameraArrow.None;
        _core.CameraTurnYawPerSecond   = 0f;
        _core.CameraTurnPitchPerSecond = 0f;
        return null;
    }

    /// <summary>The camera's current facing, so a script can prove an arrow actually turned it.</summary>
    public (float Yaw, float Pitch) CliCameraFacing() => (_core.Camera.Yaw, _core.Camera.Pitch);

    /// <summary>
    /// Opens the protagonist-management screen (anatomy / inventory / memory tabs) from wherever the
    /// game currently is, and closes it again on the second call.
    ///
    /// In the running game this screen is reached by clicking through the main-menu overlay, which
    /// a script cannot do from inside narration. Since the inventory is otherwise untestable —
    /// carrying weight, item categories and equipment all live there — the CLI gets a direct seam
    /// rather than a reconstruction of the click path.
    /// </summary>
    public bool CliToggleManagement()
    {
        if (_protagonist == null) return false;

        if (_currentMode == GameMode.ProtagonistManagement)
        {
            _core.SetNarrationMode(true);
            _managementMenuRenderer = null;
            SetMode(MenuReturnMode);
            return true;
        }

        SetMode(GameMode.ProtagonistManagement);
        return true;
    }

    /// <summary>
    /// Switches the management screen to a named tab. Null tab name lists what is available.
    /// Returns false when the screen is closed or the tab is unknown.
    /// </summary>
    public bool CliSelectManagementTab(string tabName) =>
        _currentMode == GameMode.ProtagonistManagement
        && _managementMenuRenderer?.CliSelectTab(tabName) == true;

    /// <summary>Tab labels available on the management screen, or empty when it is closed.</summary>
    public IReadOnlyList<string> CliManagementTabs =>
        _managementMenuRenderer?.CliTabNames ?? Array.Empty<string>();

    /// <summary>Selects a carried item by name so its info panel can be inspected from a script.</summary>
    public bool CliSelectItem(string itemName) =>
        _managementMenuRenderer?.CliSelectItem(itemName) == true;

    /// <summary>Everything carried, for <c>--cli</c> discovery of selectable item names.</summary>
    public IReadOnlyList<string> CliCarriedItemNames =>
        _managementMenuRenderer?.CliCarriedItemNames ?? Array.Empty<string>();

    /// <summary>
    /// The protagonist's carrying load and, when the party is grounded by it, the reason.
    /// Surfaced to <c>--cli state</c> because weight no longer blocks pickups — the only way a
    /// script can observe the constraint is here or by being refused travel.
    /// </summary>
    public (int Current, int Max, string? Blocker)? CliCarryLoad =>
        _protagonist == null
            ? null
            : (_protagonist.CurrentWeight, _protagonist.MaxCarryWeight, _protagonist.TravelWeightBlocker);

    /// <summary>
    /// Cell position of the protagonist-creation Continue button, or null outside that screen.
    /// Creation waits for this click, so an automated run has to press it to reach narration.
    /// </summary>
    public (int X, int Y)? CliCreationContinue()
        => _currentMode == GameMode.ProtagonistCreation
            ? _protagonistCreationRenderer?.CliContinueButton
            : null;

    /// <summary>
    /// True when the game is settled: no LLM generation in flight, no travel animation running and
    /// no dice mid-roll. The CLI <c>wait</c> command blocks on this.
    /// </summary>
    public bool CliIsIdle()
    {
        if (_currentMode == GameMode.Traveling) return false;

        // Fight mode has its own self-advancing phases (dice, movement, the vital-heat box). A
        // script that acted and dumped immediately would otherwise read a half-resolved turn.
        if (_currentMode == GameMode.Fighting && _fightAdapter != null)
            return !_fightAdapter.CliIsBusy;

        if (_currentMode == GameMode.Dialogue)
        {
            var dlg = _dialogueAdapter?.Controller;
            if (_dialogueAdapter != null && dlg == null) return false;   // still acquiring its LLM slot
            if (dlg != null)
            {
                var s = dlg.CliSnapshot();
                // IsDiceRolling is only meaningful while a roll is active — it rests at true.
                return !s.Loading && !(s.DiceActive && s.DiceRolling);
            }
            return true;
        }

        if (_narrativeController != null)
        {
            var s = _narrativeController.CliSnapshot();
            if (s.AnyLoading || (s.DiceActive && s.DiceRolling)) return false;
        }
        return true;
    }

    public LocationTravelGameController(GlyphSphereCore core, MicroworldInterface microworldInterface,
        AmbianceEngine? ambianceEngine = null)
    {
        _ambianceEngine = ambianceEngine;
        // Load persisted settings and apply them so saved levels take effect on launch.
        UserSettings.Load();
        _ambianceEngine?.SetMasterMusicVolume(UserSettings.MusicVolume01);
        _ambianceEngine?.SetMasterSfxVolume(UserSettings.SfxVolume01);
        _core = core ?? throw new ArgumentNullException(nameof(core));

        // Dither, unless --dither spoke for the layer on this run. Setting Enabled restores
        // whichever mode was last in use rather than a fixed one, so this turns the layer on or
        // off without overriding a mode chosen at the command line.
        if (!Config.PostProcess.DitherModeSetByFlag)
            _core.PostProcess.Enabled = UserSettings.DitherEnabled;

        // Glyph weight and size. The scale is a per-frame uniform, so writing it is the whole of
        // applying it. The weight is half rastered, so it needs the atlas re-built — and only when
        // it differs from the default the atlas was built with, which is the common case doing no
        // work at all. The null-conditional covers both orders this constructor can run in: before
        // the window loads there is no atlas yet, and the one built later is built with the value
        // set here, which is equally correct.
        Config.Terminal.GlyphScale = UserSettings.GlyphScale;
        if (UserSettings.GlyphWeight != Config.Terminal.GlyphWeightStep)
        {
            Config.Terminal.ApplyGlyphWeight(UserSettings.GlyphWeight);
            _core.Terminal?.Atlas.Rebuild();
        }

        // The same two for the sphere, and the same shape of rule: the scale is a per-frame
        // uniform, the weight is half rastered. The sphere atlas differs from the terminal's in
        // one respect that matters here — it always exists by now, being built during the core's
        // own construction, so there is no null case to cover and the rebuild is unconditional
        // once the step differs.
        Config.GlyphSphere.WorldGlyphScale = UserSettings.WorldGlyphScale;
        if (UserSettings.WorldGlyphWeight != Config.GlyphSphere.WorldGlyphWeightStep)
        {
            Config.GlyphSphere.ApplyWorldGlyphWeight(UserSettings.WorldGlyphWeight);
            _core.RebuildGlyphAtlasForWeight();
        }

        _interface = microworldInterface ?? throw new ArgumentNullException(nameof(microworldInterface));
        
        // Validate narrative world coherence at startup
        try
        {
            // The validation is load-bearing — it throws on a duplicate ItemId. The catalogue
            // listing beside it is a development aid: 282 lines naming every item type and its
            // namespace, at every single startup. It goes to log.txt in a development build and
            // is skipped entirely in a shipped one.
            Cathedral.Game.Narrative.NarrativeWorldValidator.ValidateWorldCoherence();
            if (Config.Debug.VerboseFileLogging)
                Cathedral.Game.Narrative.NarrativeWorldValidator.PrintWorldStructure();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FATAL ERROR: Narrative world validation failed: {ex.Message}");
            throw;
        }
        
        // The LLM server is attached later via SetLlamaServer() once it is ready.
        _llamaServer = null;

        // Catalyst and the word vectors are started HERE rather than from SetLlamaServer, because
        // neither of them needs the server — and being started from there meant they did not begin
        // until it was ready, which is the same instant the loading screen is dismissed and the
        // player is let in. So the minutes spent loading the model overlapped none of this work,
        // and the first ~10 seconds of PLAY ran without it: measured on a --playground run, the
        // first observation lands 2.3s after the server is attached and the vectors are ready at
        // 9.8s. A keyword chosen in that window is chosen by the fallbacks — rule-based candidates
        // instead of POS tagging, and longest-surface instead of the similarity ranking — so the
        // same scene gives a different keyword depending on how fast the player got there.
        //
        // A real session usually won that race (reaching an observation costs several real LLM
        // calls); a --playground one loses it every time, which is every script in the suite. So
        // the effect was invisible where it was measurable and unpredictable where it was not.
        _keywordInitTask = Task.Run(() =>
            Cathedral.Game.Narrative.KeywordExtractor.InitializeAsync(CatalystModelPath));

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

        // The sky's own hover and click, raised only while the core is in sky-selection mode.
        _core.MoonHovered += OnMoonHovered;
        _core.MoonClicked += OnMoonClicked;

        // Travel planning: install the land travel constraint (forbids sea/ocean) on
        // the microworld interface so pathfinding skips impassable cells, and stand up
        // the waypoint queue + bottom UI.
        _interface.SetTravelConstraint(
            TravelConstraints.Land(v => _interface.GetBiomeNameAt(v)));
        _interface.SetExternalTravelControl(true);
        _travelPlanner = new TravelPlanner(Config.GlyphSphere.MaxTravelWaypoints);
        _travelAStar = new Cathedral.Pathfinding.AStar();

        // Initialize terminal UI (if terminal is available)
        InitializeTerminalUI();
        if (_core.Terminal != null)
        {
            _travelInfoRenderer = new TravelInfoRenderer(_core.Terminal);
            _cameraArrowPad     = new CameraArrowPad(_core.Terminal);
        }
        
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

    public void Update(float deltaTime = 0f)
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

        // ── Companion deaths ──────────────────────────────────────────────────
        // Swept here rather than at each of the places a companion can be killed, because those are
        // not a closed set: a fight is the obvious one, but a failed action's wound penalty lands on
        // whoever is acting — which after a Speak-About hand-off is the companion — and the humors
        // drain on their own. Derived state answers the question at any moment
        // (PartyMember.CauseOfDeath), so asking every tick costs a few property reads and cannot be
        // forgotten by whatever kills somebody next.
        //
        // NOT during a fight: the fight holds Fighter wrappers over these members and pulling one out
        // of the party mid-swing would leave a fighter on the board belonging to nobody. It hands
        // back within a tick of the fight ending. Not on the death screen either — the run is over
        // and the party's losses are no longer news.
        if (_hasGameStarted && _companionDeathBox == null
            && _currentMode != GameMode.Fighting && _currentMode != GameMode.Death)
        {
            // The protagonist first: their death ends the run, and reporting a companion's loss to
            // somebody who is themselves dead is a box nobody will read.
            if (SettleProtagonistDeath()) return;
            SettleCompanionDeaths();
        }

        // The notice is modal over EVERY mode, not just the world view where old age used to raise
        // it: a companion now falls in a narration fight too, and the narration redraws itself every
        // frame, so a box gated below the mode dispatch would be painted over before it was read.
        if (_companionDeathBox != null)
        {
            _companionDeathBox.Render();
            UpdatePopupTerminal();
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

        // Progressive track activation during childhood reminescence:
        // each completed REMEMBER fragment unlocks one additional music track.
        if (_currentMode == GameMode.ChildhoodReminescence
            && _narrativeController != null
            && _ambianceEngine != null)
        {
            int trackCount = Math.Min(_narrativeController.ReminescenceCompletedCount, 4);
            _ambianceEngine.SetActiveTrackCount(trackCount);
        }

        // The camera arrows turn the camera for as long as one is held down, in either of the two
        // modes that draw them. Before the mode dispatch below, which returns early all over.
        UpdateCameraPad(deltaTime);

        // World selection: repaint the box every frame so the hover preview and the button
        // highlights follow the cursor, exactly as the travel box does on the world map.
        if (_currentMode == GameMode.WorldSelection)
        {
            if (_worldSelectionRenderer != null)
            {
                _worldSelectionRenderer.Draw(_selectedMoon, _hoveredMoon, _core.MoonCount);
                _cameraArrowPad?.Draw();
            }
            UpdatePopupTerminal();
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

        // Travel encounter fight update (outside narrative mode)
        if (_inTravelEncounter && _currentMode == GameMode.Fighting && _fightAdapter != null)
        {
            _fightAdapter.Update(deltaTime);
            if (_fightAdapter.IsOver)
                OnFightCompleted();
            return;
        }

        // Update Phase 6 controller if active
        if (_isInNarrativeMode && _narrativeController != null)
        {
            // Main menu / settings opened as a pause overlay over narration: the menu renderer owns
            // the screen and is event-driven (rendered on enter/hover/click). Skip the narration
            // Update entirely so it doesn't redraw over the menu every frame.
            if (_currentMode == GameMode.MainMenu || _currentMode == GameMode.Settings)
                return;

            // Check if fight/dialogue mode is active (sub-modes within narrative)
            if (_currentMode == GameMode.Fighting && _fightAdapter != null)
            {
                _fightAdapter.Update(deltaTime);

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

            if (_currentMode == GameMode.Trading && _tradeAdapter != null)
            {
                _tradeAdapter.Update();

                if (_tradeAdapter.HasRequestedExit)
                {
                    OnTradeCompleted();
                }
                return;
            }

            if (_currentMode == GameMode.Working && _workAdapter != null)
            {
                _workAdapter.Update();

                if (_workAdapter.HasRequestedExit)
                {
                    OnWorkCompleted();
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

            // Check if narrative controller wants to switch phase (fight/dialogue/…) — unified channel.
            var pendingTransition = _narrativeController.PendingPhaseTransition;
            if (pendingTransition != null)
            {
                _narrativeController.ClearPendingPhaseTransition();
                ApplyPhaseTransition(pendingTransition);
                return;
            }

            // Check if reminescence phase is complete — enter Get-Up scene.
            if (_currentMode == GameMode.ChildhoodReminescence
                && _narrativeController.ReminescencePhaseFinished)
            {
                Console.WriteLine("LocationTravelGameController: ChildhoodReminescence finished, entering GetUp");
                if (_protagonist != null)
                    SwapReminescenceForChildhoodMemory(_protagonist);
                if (FillMemoryMode.IsActive && _protagonist != null)
                    FillMemoryMode.FillEmptySlots(_protagonist);
                // After the fill modes, so an explicitly named modus mentis wins the last free slot.
                if (_protagonist != null)
                    GrantModiMentisMode.GrantIfActive(_protagonist);
                if (_protagonist != null)
                    FillPartyMode.FillIfActive(_protagonist);
                _isInNarrativeMode = false;
                _narrativeController = null;
                SetMode(GameMode.GetUp);
                return;
            }

            // Check if Get-Up phase is complete — enter WorldView.
            if (_currentMode == GameMode.GetUp
                && _narrativeController.GetUpPhaseFinished)
            {
                Console.WriteLine("LocationTravelGameController: GetUp finished, entering WorldView");
                _isInNarrativeMode = false;
                _narrativeController = null;
                SetMode(GameMode.WorldView);
                return;
            }

            // Check if player requested exit (clicked Continue button)
            if (_narrativeController.HasRequestedExit())
            {
                Console.WriteLine("LocationTravelGameController: Phase 6 exit requested");
                if (_currentMode == GameMode.ChildhoodReminescence
                    || _currentMode == GameMode.GetUp)
                {
                    // RequestedExit was set by the final phase transition — already handled
                    // above by ReminescencePhaseFinished / GetUpPhaseFinished, be defensive.
                    _isInNarrativeMode = false;
                    _narrativeController = null;
                    SetMode(GameMode.WorldView);
                }
                else
                {
                    ExitNarrativeMode();
                }
            }

            return;
        }

        // (The companion-death notice is handled at the top of Update, modal over every mode.)

        // Companion-removal overlay owns the screen until confirmed — redraw it and
        // skip the travel UI underneath.
        if (_currentMode == GameMode.WorldView && _companionRemovalRenderer != null)
        {
            _companionRemovalRenderer.Render();
            UpdatePopupTerminal();
            return;
        }

        // Routine outcome box (after a replay) and routine list box (from travel UI) are modal.
        if (_currentMode == GameMode.WorldView && _routineOutcomeBox != null)
        {
            _routineOutcomeBox.Render();
            UpdatePopupTerminal();
            return;
        }
        if (_currentMode == GameMode.WorldView && _travelRoutinesBox != null)
        {
            _travelRoutinesBox.Render();
            UpdatePopupTerminal();
            return;
        }

        // Travel UI: tick the brief "forbidden cell" flash and redraw the bottom box.
        if (_currentMode == GameMode.WorldView)
        {
            if (_forbiddenFlashFramesLeft > 0)
            {
                _forbiddenFlashFramesLeft--;
                if (_forbiddenFlashFramesLeft == 0 && _forbiddenFlashVertex >= 0)
                {
                    _interface.RestoreCellGlyph(_forbiddenFlashVertex);
                    _forbiddenFlashVertex = -1;
                }
            }
            RenderWorldViewUI();
        }

        // Render travel progress box and advance flash animation during Traveling.
        if (_currentMode == GameMode.Traveling && _travelProgressRenderer != null)
        {
            // Per-frame humor consumption: drain one humor per frame while debt is owed,
            // keeping the protagonist paused until the bill is fully paid.
            if (_consumptionActive && _protagonist != null)
            {
                if (_locationBatchNewFrame)
                {
                    // Hold for one frame so the VH bar renders empty before filling starts.
                    _locationBatchNewFrame = false;
                }
                else if (_tripVhDebt >= 1.0f)
                {
                    var humor = _protagonist.HumorQueues.ConsumeCycled(_protagonist, _travelRng);
                    if (humor == null)
                    {
                        // All queues critical — starvation death
                        _travelProgressRenderer.Erase();
                        _consumptionActive        = false;
                        _interface.MovementPaused = false;
                        TriggerDeath(DeathCause.Starvation);
                        return;
                    }

                    // A humor carrying no heat pays nothing and the debt does not move — but the queue
                    // still advances, and every secretion can add black bile, which Consume never
                    // removes. So a body that cannot pay silts up until all four queues are critical
                    // and ConsumeCycled returns null, which is the starvation branch above. The drain
                    // is therefore self-terminating, and needs no attempt limit.
                    if (humor.VitalHeat >= 0)
                    {
                        _tripVhDebt         -= humor.VitalHeat;
                        _tripVhConsumedNet  += humor.VitalHeat;
                        _locationVhConsumed += humor.VitalHeat;
                    }
                    else
                    {
                        // Negative VH: only subtracts from the current location's consumed VH
                        // (floor 0), returning that portion of debt unpaid. The overall trip
                        // consumed counter is not reduced.
                        float deduction = MathF.Min(_locationVhConsumed, -humor.VitalHeat);
                        _locationVhConsumed -= deduction;
                        _tripVhDebt         += deduction;
                    }
                    _travelProgressRenderer.RegisterConsumption(_consumptionBiome, humor, _tripVhConsumedNet);
                }
                else
                {
                    // Debt fully paid — resume protagonist movement
                    _consumptionActive        = false;
                    _interface.MovementPaused = false;
                }
            }

            _travelProgressRenderer.Update(deltaTime);
            _travelProgressRenderer.Draw(
                _tripCellsTraveled, _tripTotalCells,
                _locationVhConsumed, _locationVhRequired);
        }

        // Update popup terminal with location info
        UpdatePopupTerminal();
    }

    /// <summary>
    /// Draws the bottom travel info box every frame while in WorldView. The world
    /// sphere itself is drawn by the core renderer; this method only touches the
    /// terminal overlay cells (everything outside the box stays transparent so clicks
    /// fall through).
    /// </summary>
    private void RenderWorldViewUI()
    {
        if (_travelInfoRenderer == null || _travelPlanner == null) return;
        if (_core.Terminal == null) return;

        string? destinationName = null;
        bool routinesAvailable = false;
        if (_travelPlanner.HasWaypoints)
        {
            int destVertex = _travelPlanner.FinalDestination;
            destinationName = GetLocationNameAtVertex(destVertex);
            routinesAvailable = _protagonist != null
                && _protagonist.RecordedRoutines.Any(r => r.LocationId == destVertex);
        }

        // The arrow pad, drawn before the travel box so that if the two ever overlapped the box —
        // the one carrying the decision — would win.
        if (CameraPadActive) _cameraArrowPad!.Draw();

        _travelInfoRenderer.Erase();
        _travelInfoRenderer.Draw(
            waypointCount: _travelPlanner.Count,
            maxWaypoints: _travelPlanner.MaxWaypoints,
            estimate: _plannedEstimate,
            destinationName: destinationName,
            routinesAvailable: routinesAvailable,
            // An overloaded member grounds the whole party until something is put down.
            overloadWarning: _protagonist?.TravelWeightBlocker);
    }

    /// <summary>
    /// Attaches the local LLM server and spins up the narrative subsystems that depend on it
    /// (modusMentis slots, thinking executor, critic, text sanitization). When no server is
    /// attached the game runs with fallback narration.
    /// </summary>
    public void SetLlamaServer(LlamaServerManager llamaServer)
    {
        _llamaServer = llamaServer ?? throw new ArgumentNullException(nameof(llamaServer));

        // Per-modusMentis LLM slots and the thinking executor
        _modusMentisSlotManager = new ModusMentisSlotManager(llamaServer);
        var thinkingPromptConstructor = new ThinkingPromptConstructor();
        _thinkingExecutor = new ThinkingExecutor(
            llamaServer,
            thinkingPromptConstructor,
            _modusMentisSlotManager);
        Console.WriteLine("LocationTravelGameController: ModusMentisSlotManager and ThinkingExecutor initialized");

        // Critic evaluator
        _criticEvaluator = new ItemUseCritic(llamaServer);
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

        // Persona-match critic: the shared neutral instance that maps each persona's free-text want to
        // one of the offered options (see PersonaChoiceSelector). Own slot, initialised once.
        _ = Task.Run(async () =>
        {
            try
            {
                await Cathedral.Game.Narrative.PersonaMatchCritic.InitializeAsync(llamaServer);
                Console.WriteLine("LocationTravelGameController: PersonaMatchCritic initialized");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LocationTravelGameController: Failed to initialize PersonaMatchCritic - {ex.Message}");
            }
        });

        // Text sanitization pipeline (3-layer anachronism/entity filter). This one genuinely needs
        // the server — Layer 1 is a rewriter slot — so it stays here.
        _ = Task.Run(async () =>
        {
            // Two try blocks, not one, because these are two independent subsystems and either can
            // fail without the other being affected — the arrangement they replaced ran them in one
            // block, so a sanitizer failure skipped the keyword init entirely and the run continued
            // with no POS tagger and no vectors, reported under the sanitizer's name.
            try
            {
                // Await the keyword init started in the constructor rather than racing it. Both
                // paths call NounExtractor.InitializeAsync, whose _initialized guard is set only
                // after the await completes and is not thread-safe, so two concurrent callers would
                // both pass it and register English models into Catalyst's global Storage.Current
                // twice. Sequenced, the second call finds the guard set and returns at once — and
                // by now the constructor's task has had the whole model load to finish in, so this
                // await is normally free.
                if (_keywordInitTask != null) await _keywordInitTask;
                Console.WriteLine("LocationTravelGameController: KeywordExtractor initialized");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LocationTravelGameController: Failed to initialize KeywordExtractor - {ex.Message}");
            }

            try
            {
                await TextSanitizationPipeline.InitializeAsync(CatalystModelPath, llamaServer);
                Console.WriteLine("LocationTravelGameController: TextSanitizationPipeline initialized");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LocationTravelGameController: Failed to initialize TextSanitizationPipeline - {ex.Message}");
            }
        });

        Console.WriteLine("LocationTravelGameController: LLM server enabled");
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
        // The companion-death notice is modal over every mode and swallows every click until its
        // CONTINUE is pressed. Dismissing re-enters the current mode rather than the world view: the
        // box can now come up in the middle of a narration session, and OnEnterWorldView there would
        // repaint the map over a scene the player has not left.
        if (_companionDeathBox != null)
        {
            if (_companionDeathBox.OnMouseClick(x, y))
            {
                _ambianceEngine?.TriggerGameEvent(GameEventType.StrongInteraction);
                _companionDeathBox = null;
                _core.Terminal?.Clear();
                if (_currentMode == GameMode.WorldView) OnEnterWorldView();
                else                                    RedrawCurrentMode();
            }
            return;
        }

        // Companion-removal overlay is modal: it captures every click while shown.
        if (_currentMode == GameMode.WorldView && _companionRemovalRenderer != null)
        {
            var result = _companionRemovalRenderer.OnMouseClick(x, y);
            if (result != CompanionRemovalRenderer.ClickResult.None)
                _ambianceEngine?.TriggerGameEvent(GameEventType.StrongInteraction);
            if (result == CompanionRemovalRenderer.ClickResult.Confirmed)
                ConfirmCompanionRemoval();
            return;
        }

        // Routine outcome box is modal: CONTINUE applies the replay's final phase transition.
        if (_currentMode == GameMode.WorldView && _routineOutcomeBox != null)
        {
            if (_routineOutcomeBox.OnMouseClick(x, y))
            {
                _ambianceEngine?.TriggerGameEvent(GameEventType.StrongInteraction);
                var transition = _replayFinalTransition ?? Cathedral.Game.Narrative.ReturnToTravelTransition.Instance;
                _routineOutcomeBox    = null;
                _replayFinalTransition = null;
                _core.Terminal?.Clear(); // wipe the modal box before the next phase paints
                ApplyPhaseTransition(transition);
            }
            return;
        }

        // Routine list box is modal: select a replayable routine, or RETURN to the travel plan.
        if (_currentMode == GameMode.WorldView && _travelRoutinesBox != null)
        {
            var (kind, routine) = _travelRoutinesBox.OnMouseClick(x, y);
            if (kind == TravelRoutinesBox.ResultKind.Selected && routine != null)
            {
                _ambianceEngine?.TriggerGameEvent(GameEventType.StrongInteraction);
                _travelRoutinesBox    = null;
                _pendingReplayRoutine = routine;
                // Wipe the modal box while keeping the world visible for the travel animation — a
                // bare Clear would leave an opaque-black terminal over the whole trip.
                SetTransparentWorldOverlay(clickPassthrough: true);
                StartPlannedTravel();
            }
            else if (kind == TravelRoutinesBox.ResultKind.Return)
            {
                _ambianceEngine?.TriggerGameEvent(GameEventType.SmallInteraction);
                _travelRoutinesBox = null;
                _core.SetWorldInteractionsEnabled(true);
                _interface.SetWorldInteractionsEnabled(true);
                // Restore the transparent travel overlay (a bare Clear would leave opaque black) and
                // let clicks fall through to the world again.
                SetTransparentWorldOverlay(clickPassthrough: true);
            }
            return;
        }

        // Death screen: END RUN button
        if (_currentMode == GameMode.Death && _deathScreenRenderer != null)
        {
            if (_deathScreenRenderer.IsOverEndRunButton(x, y))
            {
                _ambianceEngine?.TriggerGameEvent(GameEventType.StrongInteraction);
                _hasGameStarted = false; // disable Continue
                SetMode(GameMode.MainMenu);
            }
            return;
        }

        // Encounter prompt: ENGAGE button starts the fight
        if (_currentMode == GameMode.EncounterPrompt && _encounterPromptRenderer != null)
        {
            if (_encounterPromptRenderer.IsOverEngageButton(x, y)
                && _pendingEncounterNpc != null && _core.Terminal != null && _protagonist != null)
            {
                _ambianceEngine?.TriggerGameEvent(GameEventType.StrongInteraction);
                // Pick the arena generator for the current biome (travel encounters are
                // wall-clock-random — fresh seed per fight).
                var biome = Cathedral.Glyph.Microworld.BiomeDatabase.Biomes[_consumptionBiome];
                var arena = biome.ArenaGeneratorFactory(Environment.TickCount);
                _fightAdapter = new FightModeAdapter(
                    _core.Terminal,
                    _core.PopupTerminal,
                    _pendingEncounterNpc,
                    _protagonist,
                    arena,
                    allies: new List<Cathedral.Game.Npc.NpcEntity>(),
                    sfxTrigger: e => _ambianceEngine?.TriggerGameEvent(e),
                    setMusicFilter: f => _ambianceEngine?.SetFilter(f));
                _pendingEncounterNpc = null;
                _pendingEncounterCreatureName = null;
                SetMode(GameMode.Fighting);
            }
            return;
        }

        // Travel encounter fight: route click to fight adapter
        if (_inTravelEncounter && _currentMode == GameMode.Fighting && _fightAdapter != null)
        {
            _ambianceEngine?.TriggerGameEvent(GameEventType.StrongInteraction);
            _fightAdapter.OnCellClicked(x, y);
            return;
        }

        // The camera arrows sit over both the world map and the selection sky, and are checked
        // before either screen's own buttons — they are drawn on top of both.
        if (TryPressCameraArrow(x, y)) return;

        // World selection: the confirmation box's two buttons. A press anywhere else on the screen
        // is either a moon (handled in the core, which sees the raw click) or nothing.
        if (_currentMode == GameMode.WorldSelection && _worldSelectionRenderer != null)
        {
            if (_worldSelectionRenderer.IsOverConfirmButton(x, y)) { ConfirmWorldSelection(); return; }
            if (_worldSelectionRenderer.IsOverCancelButton(x, y))  { CancelWorldSelection();  return; }
            return;
        }

        // WorldView: only the TRAVEL and CLEAR buttons on the bottom travel info box
        // are clickable (everything else is transparent and falls through to the world
        // sphere via TerminalHUD.TransparentClickPassthrough).
        if (_currentMode == GameMode.WorldView && _travelInfoRenderer != null)
        {
            if (_travelInfoRenderer.IsOverTravelButton(x, y))
            {
                _ambianceEngine?.TriggerGameEvent(GameEventType.StrongInteraction);
                StartPlannedTravel();
                return;
            }
            if (_travelInfoRenderer.IsOverClearButton(x, y))
            {
                _ambianceEngine?.TriggerGameEvent(GameEventType.SmallInteraction);
                ClearTravelPlan();
                return;
            }
            if (_travelInfoRenderer.IsOverRoutinesButton(x, y))
            {
                _ambianceEngine?.TriggerGameEvent(GameEventType.StrongInteraction);
                OpenRoutinesBox();
                return;
            }
            return;
        }

        // Main menu handles its own clicks
        if (_currentMode == GameMode.MainMenu && _mainMenuRenderer != null)
        {
            // Only on a button — the same predicate the hover tick uses, so the two agree about
            // what is clickable and a press on the empty backdrop is silent.
            if (_mainMenuRenderer.GetEnabledButtonAtPosition(x, y) >= 0)
                _ambianceEngine?.TriggerGameEvent(GameEventType.StrongInteraction);
            _mainMenuRenderer.OnMouseClick(x, y);
            return;
        }

        // Settings screen handles its own clicks
        if (_currentMode == GameMode.Settings && _settingsMenuRenderer != null)
        {
            if (_settingsMenuRenderer.GetHoveredControlId(x, y) != null)
                _ambianceEngine?.TriggerGameEvent(GameEventType.StrongInteraction);
            _settingsMenuRenderer.OnMouseClick(x, y);
            return;
        }
        
        // Protagonist creation handles its own clicks
        if (_currentMode == GameMode.ProtagonistCreation && _protagonistCreationRenderer != null)
        {
            if (_protagonistCreationRenderer.IsInteractiveCell(x, y))
                _ambianceEngine?.TriggerGameEvent(GameEventType.StrongInteraction);
            _protagonistCreationRenderer.OnMouseClick(x, y);
            return;
        }
        
        // Protagonist management handles its own clicks, and says whether the press landed on
        // anything — the screen is mostly art and read-only text, so firing the click sound first
        // made every press anywhere in it sound like a button.
        if (_currentMode == GameMode.ProtagonistManagement && _managementMenuRenderer != null)
        {
            if (_managementMenuRenderer.OnMouseClick(x, y))
                _ambianceEngine?.TriggerGameEvent(GameEventType.StrongInteraction);
            return;
        }
        
        // All location interactions now use Phase 6 narrative mode
        if (_isInNarrativeMode && _narrativeController != null)
        {
            // Route to fight adapter if in fight mode
            if (_currentMode == GameMode.Fighting && _fightAdapter != null)
            {
                _ambianceEngine?.TriggerGameEvent(GameEventType.StrongInteraction);
                _fightAdapter.OnCellClicked(x, y);
                return;
            }
            
            // Route to dialogue adapter if in dialogue mode
            if (_currentMode == GameMode.Dialogue && _dialogueAdapter != null)
            {
                _ambianceEngine?.TriggerGameEvent(GameEventType.StrongInteraction);
                _dialogueAdapter.OnMouseClick(x, y);
                return;
            }

            // Route to trade adapter if in trading mode
            if (_currentMode == GameMode.Trading && _tradeAdapter != null)
            {
                if (_tradeAdapter.GetHoveredControlId(x, y) != null)
                    _ambianceEngine?.TriggerGameEvent(GameEventType.StrongInteraction);
                _tradeAdapter.OnMouseClick(x, y);
                return;
            }

            // Route to work adapter if in working mode
            if (_currentMode == GameMode.Working && _workAdapter != null)
            {
                if (_workAdapter.GetHoveredControlId(x, y) != null)
                    _ambianceEngine?.TriggerGameEvent(GameEventType.StrongInteraction);
                _workAdapter.OnMouseClick(x, y);
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
        
        // WorldView idle click (no narrative open) — no sound; nothing meaningful here.
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
    /// Returns a stable identity string for the UI element under (x, y) in the current mode,
    /// or null if not over any interactive element. Narrative modes return null because
    /// NarrativeController fires its own hover sounds internally.
    /// </summary>
    private string? GetHoverElementId(int x, int y) => _currentMode switch
    {
        GameMode.LocationInteraction or
        GameMode.Fighting or
        GameMode.Dialogue or
        GameMode.ChildhoodReminescence => null, // handled inside NarrativeController
        // Trade and work are NOT narrative modes — they are their own renderers, and listing them
        // with the ones above is why neither ever ticked: every button on both screens highlighted
        // in silence, which reads as an unresponsive screen next to the menus either side of it.
        GameMode.Trading => _tradeAdapter?.GetHoveredControlId(x, y),
        GameMode.Working => _workAdapter?.GetHoveredControlId(x, y),
        GameMode.MainMenu => _mainMenuRenderer?.GetEnabledButtonAtPosition(x, y) is { } i and >= 0
            ? $"menu:{i}" : null,
        GameMode.Settings => _settingsMenuRenderer?.GetHoveredControlId(x, y),
        GameMode.ProtagonistCreation => _protagonistCreationRenderer?.GetHoveredElementId(x, y),
        // The travel box has three buttons, not two: ROUTINES was left out and so was the only one
        // of them that highlighted without a sound.
        GameMode.WorldView => _travelInfoRenderer == null ? null
            : _travelInfoRenderer.IsOverTravelButton(x, y)   ? "travel-button"
            : _travelInfoRenderer.IsOverClearButton(x, y)    ? "travel-clear-button"
            : _travelInfoRenderer.IsOverRoutinesButton(x, y) ? "travel-routines-button"
            : null,
        GameMode.WorldSelection => _worldSelectionRenderer == null ? null
            : _worldSelectionRenderer.IsOverConfirmButton(x, y) ? "world-confirm"
            : _worldSelectionRenderer.IsOverCancelButton(x, y)  ? "world-cancel"
            : null,
        GameMode.Death => _deathScreenRenderer?.IsOverEndRunButton(x, y) == true ? "death-end-run" : null,
        GameMode.EncounterPrompt => _encounterPromptRenderer?.IsOverEngageButton(x, y) == true ? "encounter-engage" : null,
        GameMode.ProtagonistManagement => null, // management menu fires its own tick via OnMouseMove return value
        _ => null,
    };

    /// <summary>
    /// Handles terminal cell hover for visual feedback.
    /// </summary>
    private void OnTerminalCellHovered(int x, int y)
    {
        // The companion-death notice is modal over every mode: route hover to it, tick on change.
        if (_companionDeathBox != null)
        {
            if (_companionDeathBox.OnMouseMove(x, y))
                _ambianceEngine?.TriggerGameEvent(GameEventType.SmallInteraction);
            return;
        }

        // Companion-removal overlay is modal: route hover to it and play a tick on change.
        if (_currentMode == GameMode.WorldView && _companionRemovalRenderer != null)
        {
            if (_companionRemovalRenderer.OnMouseMove(x, y))
                _ambianceEngine?.TriggerGameEvent(GameEventType.SmallInteraction);
            return;
        }

        // Routine boxes are modal: route hover to whichever is shown.
        if (_currentMode == GameMode.WorldView && _routineOutcomeBox != null)
        {
            if (_routineOutcomeBox.OnMouseMove(x, y))
                _ambianceEngine?.TriggerGameEvent(GameEventType.SmallInteraction);
            return;
        }
        if (_currentMode == GameMode.WorldView && _travelRoutinesBox != null)
        {
            if (_travelRoutinesBox.OnMouseMove(x, y))
                _ambianceEngine?.TriggerGameEvent(GameEventType.SmallInteraction);
            return;
        }

        // Track hover for the travel UI so the TRAVEL button highlights.
        _mouseCellX = x;
        _mouseCellY = y;
        _travelInfoRenderer?.SetHover(x, y);
        _worldSelectionRenderer?.SetHover(x, y);
        if (CameraPadActive && _cameraArrowPad!.SetHover(x, y)
            && _cameraArrowPad.Hovered != CameraArrow.None)
            _ambianceEngine?.TriggerGameEvent(GameEventType.SmallInteraction);
        _deathScreenRenderer?.SetHover(x, y);
        _encounterPromptRenderer?.SetHover(x, y);

        // Clear the sphere hover-path preview when the mouse enters the travel box.
        if (_currentMode == GameMode.WorldView
            && _travelInfoRenderer != null && _travelInfoRenderer.IsOverBox(x, y))
        {
            _interface.HandleVertexUnhovered();
        }

        // Only fire hover tick when entering a NEW interactive element.
        // Narrative modes (LocationInteraction, Fighting, Dialogue, ChildhoodReminescence) handle
        // their own hover sounds inside NarrativeController to get per-keyword/action accuracy.
        if (_currentMode != GameMode.Traveling && _currentMode != GameMode.LLMLoading)
        {
            string? elementId = GetHoverElementId(x, y);
            if (elementId != null && elementId != _lastHoveredElementId)
                _ambianceEngine?.TriggerGameEvent(GameEventType.SmallInteraction);
            _lastHoveredElementId = elementId;
        }
        // Main menu handles its own hover
        if (_currentMode == GameMode.MainMenu && _mainMenuRenderer != null)
        {
            _mainMenuRenderer.OnMouseMove(x, y);
            return;
        }

        // Settings screen handles its own hover
        if (_currentMode == GameMode.Settings && _settingsMenuRenderer != null)
        {
            _settingsMenuRenderer.OnMouseMove(x, y);
            return;
        }

        // Death screen: redraw so END RUN button highlight stays current
        if (_currentMode == GameMode.Death && _deathScreenRenderer != null)
        {
            _deathScreenRenderer.Draw(_deathCause);
            return;
        }

        // Encounter prompt: redraw so ENGAGE button highlight stays current
        if (_currentMode == GameMode.EncounterPrompt && _encounterPromptRenderer != null
            && _pendingEncounterNpc != null)
        {
            _encounterPromptRenderer.Draw(
                _pendingEncounterNpc.DisplayName,
                _pendingEncounterCreatureName ?? "",
                _consumptionBiome);
            return;
        }

        // Travel encounter fight: route hover to fight adapter
        if (_inTravelEncounter && _currentMode == GameMode.Fighting && _fightAdapter != null)
        {
            _fightAdapter.OnCellHovered(x, y);
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
            if (_managementMenuRenderer.OnMouseMove(x, y))
                _ambianceEngine?.TriggerGameEvent(GameEventType.SmallInteraction);
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

            // Route to trade adapter if in trading mode
            if (_currentMode == GameMode.Trading && _tradeAdapter != null)
            {
                _tradeAdapter.OnMouseMove(x, y);
                return;
            }

            // Route to work adapter if in working mode
            if (_currentMode == GameMode.Working && _workAdapter != null)
            {
                _workAdapter.OnMouseMove(x, y);
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
        // Travel encounter fight scroll
        if (_inTravelEncounter && _currentMode == GameMode.Fighting && _fightAdapter != null)
        {
            _fightAdapter.OnMouseWheel(delta);
            return;
        }

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

            if (_currentMode == GameMode.Trading && _tradeAdapter != null)
            {
                _tradeAdapter.OnMouseWheel(delta);
                return;
            }

            if (_currentMode == GameMode.Working && _workAdapter != null)
            {
                _workAdapter.OnMouseWheel(delta);
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
        _lastHoveredElementId = null; // reset so first hover in new mode always sounds

        // The transparent-click passthrough is only meaningful in WorldView; leaving
        // it on after a transition would silently swallow clicks elsewhere.
        if (_core.Terminal != null && newMode != GameMode.WorldView)
            _core.Terminal.TransparentClickPassthrough = false;

        // Discard any in-flight travel plan when leaving WorldView (e.g. entering a
        // location): the player is no longer planning a route.
        if (oldMode == GameMode.WorldView && newMode != GameMode.WorldView
            && newMode != GameMode.Traveling)
        {
            ClearTravelPlan();
        }

        // Reset cloud speed back to normal whenever travel ends.
        if (oldMode == GameMode.Traveling && newMode != GameMode.Traveling)
            _core.SetCloudSpeedMultiplier(1.0f);

        Console.WriteLine($"LocationTravelGameController: Mode changed: {oldMode} ↁE{newMode}");

        RunModeEnter(newMode);

        ModeChanged?.Invoke(oldMode, newMode);
    }

    /// <summary>
    /// Repaint the mode already running, without a transition.
    ///
    /// <para><see cref="SetMode"/> returns early when the mode is unchanged, which is right for a
    /// transition and useless for an overlay: a modal box drawn over a live phase leaves that phase's
    /// pixels to be restored when the box is dismissed, and the phase never changed. Every
    /// <c>OnEnter…</c> is written to be safe on re-entry already — that is what the pause menu's
    /// resume depends on — so this is the same call the menu makes, minus the mode change.</para>
    /// </summary>
    private void RedrawCurrentMode() => RunModeEnter(_currentMode);

    /// <summary>The mode-specific setup half of <see cref="SetMode"/>, shared with the repaint.</summary>
    private void RunModeEnter(GameMode newMode)
    {
        // Whether the world is drawn at all, decided in one place for every mode rather than in each
        // OnEnter. Two conditions, and both are easy to get wrong from inside a single mode: there
        // may be no world yet (nothing is generated until a run starts), and the world-selection
        // screen wants the sky unobstructed even when a previous run has left a world standing.
        _core.WorldRenderEnabled = _interface.IsWorldGenerated && newMode != GameMode.WorldSelection;

        // Handle mode-specific setup
        switch (newMode)
        {
            case GameMode.LLMLoading:
                OnEnterLLMLoading();
                break;

            case GameMode.MainMenu:
                OnEnterMainMenu();
                break;

            case GameMode.Settings:
                OnEnterSettings();
                break;

            case GameMode.WorldSelection:
                OnEnterWorldSelection();
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

            case GameMode.Trading:
                OnEnterTrading();
                break;

            case GameMode.Working:
                OnEnterWorking();
                break;

            case GameMode.ChildhoodReminescence:
                OnEnterChildhoodReminescence();
                break;

            case GameMode.GetUp:
                OnEnterGetUp();
                break;

            case GameMode.Death:
                OnEnterDeath();
                break;

            case GameMode.EncounterPrompt:
                OnEnterEncounterPrompt();
                break;
        }
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

            // A visit costs a journey. Arriving somewhere already opens it — OnProtagonistArrived
            // starts the interaction itself — so this click is only ever a way back into the place
            // just walked out of, and taking it means a player never has to go anywhere. Refused, so
            // every trip is to somewhere else. `--allow-reentry` lifts it for scripted runs, where
            // the spawn vertex is the only one a script can name from a cold start (`travel here`).
            if (!Config.Debug.AllowReentry)
            {
                Console.WriteLine("LocationTravelGameController: refusing to re-enter the current location — "
                                + "travel somewhere else (--allow-reentry lifts this)");
                _travelInfoRenderer?.ShowTransientMessage("You have been here — choose somewhere to travel to.");
                return;
            }

            // Any pending travel plan should be discarded when the player decides to
            // interact with the current cell.
            ClearTravelPlan();

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
            // Clicked on a different vertex - toggle it as a travel waypoint.
            ToggleWaypoint(vertexIndex);
        }
    }

    /// <summary>
    /// Adds the clicked vertex to the waypoint queue (or removes it if already queued),
    /// then recomputes the planned path and refreshes the bottom travel UI.
    /// </summary>
    private void ToggleWaypoint(int vertexIndex)
    {
        if (_travelPlanner == null) return;

        int protagonistVertex = _interface.GetAvatarVertex();
        var graph = _interface.GetTravelGraph();
        var astar = _travelAStar;

        // Reachability probe used by the planner to reject unreachable destinations
        // (e.g. an island surrounded by sea) instead of silently queueing them.
        bool Reachable(int from, int to)
        {
            if (graph == null || astar == null) return true;
            return astar.FindPath(graph, from, to) != null;
        }

        var result = _travelPlanner.Toggle(vertexIndex, protagonistVertex,
            v => _interface.IsVertexTraversable(v) && !_interface.IsOutOfTravelRange(v),
            Reachable);

        switch (result)
        {
            case WaypointToggleResult.Forbidden:
            case WaypointToggleResult.Unreachable:
                Console.WriteLine($"LocationTravelGameController: Vertex {vertexIndex} rejected ({result})");
                _ambianceEngine?.TriggerGameEvent(GameEventType.SmallInteraction);
                // Briefly mark the cell with the forbidden glyph; the next Update() tick
                // restores it. This gives the player a clear "no" feedback without
                // dirtying the waypoint queue.
                if (_forbiddenFlashVertex >= 0 && _forbiddenFlashVertex != vertexIndex)
                    _interface.RestoreCellGlyph(_forbiddenFlashVertex);
                _interface.FlashForbiddenCell(vertexIndex);
                _forbiddenFlashVertex = vertexIndex;
                _forbiddenFlashFramesLeft = 30;
                return;

            case WaypointToggleResult.IgnoredSelf:
                return;

            case WaypointToggleResult.Added:
            case WaypointToggleResult.AddedEvictingFirst:
            case WaypointToggleResult.Removed:
                _ambianceEngine?.TriggerGameEvent(GameEventType.StrongInteraction);
                RecomputeTravelPlan();
                break;
        }
    }

    /// <summary>
    /// Resolves the path through the current waypoint queue and re-renders the planned
    /// path on the world sphere plus the bottom travel info box.
    /// </summary>
    private void RecomputeTravelPlan()
    {
        if (_travelPlanner == null || _travelAStar == null) return;

        // Clear any prior planned-path overlay first.
        _interface.ClearPlannedPath();
        _plannedPath = null;
        _plannedEstimate = null;

        int protagonist = _interface.GetAvatarVertex();
        if (!_travelPlanner.HasWaypoints || protagonist < 0)
        {
            // No active plan — hover preview starts from the protagonist again.
            _interface.SetHoverPathOrigin(-1);
            return;
        }

        var graph = _interface.GetTravelGraph();
        if (graph == null) return;

        var path = _travelPlanner.ResolvePath(protagonist, graph, _travelAStar);
        if (path == null || path.Count <= 1)
        {
            // Destination unreachable — keep the waypoints (player can still remove them)
            // but show no planned-path overlay and let the UI display the error.
            _interface.SetHoverPathOrigin(_travelPlanner.FinalDestination);
            return;
        }

        _plannedPath = path;
        _plannedEstimate = TravelPlanner.EstimateForPath(path,
            v => _interface.GetBiomeNameAt(v),
            _protagonist);

        _interface.ShowPlannedPath(path, _travelPlanner.Waypoints);
        // Hover preview now extends from the last waypoint (the "tail" of the plan).
        _interface.SetHoverPathOrigin(_travelPlanner.FinalDestination);
    }

    /// <summary>Clears the waypoint queue, planned-path overlay, and bottom UI state.</summary>
    private void ClearTravelPlan()
    {
        _travelPlanner?.Clear();
        _interface.ClearPlannedPath();
        _interface.SetHoverPathOrigin(-1);
        _plannedPath = null;
        _plannedEstimate = null;

        // Cancel any pending forbidden-cell flash.
        if (_forbiddenFlashVertex >= 0)
        {
            _interface.RestoreCellGlyph(_forbiddenFlashVertex);
            _forbiddenFlashVertex = -1;
        }
        _forbiddenFlashFramesLeft = 0;
    }

    /// <summary>
    /// Commits the current waypoint queue and starts movement along the resolved path.
    /// Invoked by the TRAVEL button on the bottom UI.
    /// </summary>
    public void StartPlannedTravel()
    {
        if (_travelPlanner == null || _travelAStar == null) return;
        if (!_travelPlanner.HasWaypoints) return;

        // The TRAVEL button is already dead while anyone is overloaded, but this is the real gate:
        // it also covers the routines path and the --cli `travel` command, which never touch the
        // button. The party walks together, so one overloaded member grounds everyone.
        if (_protagonist?.TravelWeightBlocker is { } blocked)
        {
            Console.WriteLine($"LocationTravelGameController: Travel refused — {blocked}");
            return;
        }

        int protagonist = _interface.GetAvatarVertex();
        var graph = _interface.GetTravelGraph();
        if (protagonist < 0 || graph == null) return;

        var pathVertices = _travelPlanner.ResolvePath(protagonist, graph, _travelAStar);
        if (pathVertices == null || pathVertices.Count < 2)
        {
            Console.WriteLine("LocationTravelGameController: Cannot start travel — no resolvable path");
            return;
        }

        // Build a Cathedral.Pathfinding.Path from the resolved vertex list.
        var positions = new List<System.Numerics.Vector3>(pathVertices.Count);
        foreach (int v in pathVertices)
            positions.Add(graph.GetNodePosition(v));
        var path = new Cathedral.Pathfinding.Path(pathVertices, positions, 0f);

        _destinationVertex = pathVertices[^1];
        _interface.ClearPlannedPath();
        _travelPlanner.Clear();
        _plannedPath = null;

        // Initialise trip vital-heat and distance tracking
        float tripVhRequired = _plannedEstimate?.TotalVitalHeat ?? 1f;
        _tripVhRequired    = MathF.Max(1f, tripVhRequired);
        _tripVhConsumedNet = 0f;
        _tripVhDebt        = 0f;
        _tripTotalCells    = pathVertices.Count - 1;
        _tripCellsTraveled = 0;

        // Capture the trip's in-game duration before the estimate is cleared; added to the global
        // clock when the protagonist arrives (the estimate is gone by then).
        _committedTravelDays = _plannedEstimate?.TotalDurationDays ?? 0f;

        _plannedEstimate = null;

        // Create the travel progress renderer once and initialise the trip
        if (_travelProgressRenderer == null)
            _travelProgressRenderer = new TravelProgressRenderer(_core.Terminal);
        _travelProgressRenderer.StartTrip(_tripVhRequired);

        SetMode(GameMode.Traveling);
        TravelStarted?.Invoke();
        _interface.BeginTravelAlongPath(path);

        Console.WriteLine($"LocationTravelGameController: Starting planned travel "
            + $"to vertex {_destinationVertex} via {pathVertices.Count - 1} cells");
    }

    /// <summary>
    /// Called every time the protagonist steps into a new vertex during travel.
    /// Consumes vital heat from the humor queues based on the biome's travel cost.
    /// Triggers death by starvation if all humor queues become critical.
    /// </summary>
    public void OnProtagonistSteppedToVertex(int vertexIndex)
    {
        if (_currentMode != GameMode.Traveling) return;
        if (_protagonist == null) return;

        _tripCellsTraveled++;

        // Accumulate biome travel cost into the debt.
        string biomeName = _interface.GetBiomeNameAt(vertexIndex) ?? "unknown";
        var biomeInfo = Cathedral.Glyph.Microworld.BiomeTravelDatabase.GetFor(biomeName);
        _consumptionBiome  = biomeName;
        _tripVhDebt       += biomeInfo.VitalHeatPerCell;

        // If debt has reached a full unit, pause movement so Update() can drain it
        // one humor per frame before the protagonist takes the next step.
        if (_tripVhDebt >= 1.0f && !_consumptionActive)
        {
            _consumptionActive         = true;
            _locationBatchNewFrame     = true;
            _locationVhConsumed        = 0f;
            _locationVhRequired        = _tripVhDebt;
            _interface.MovementPaused  = true;
        }

        // --encounter-on-arrival: stage an encounter on the journey's LAST step, which is the one
        // arrangement a random roll almost never produces and the one that used to strand the run.
        // Ahead of --no-encounters on purpose, so a script can suppress the random rolls and still
        // ask for this one; fires once and clears itself.
        if (Config.Debug.EncounterOnArrival is { } staged && _tripCellsTraveled >= _tripTotalCells)
        {
            Config.Debug.EncounterOnArrival = null;
            if (!StartTravelEncounter(staged))
                DebugFlagAudit.Miss("--encounter-on-arrival", staged, "no encounter — the journey ended normally");
            return;
        }

        // Roll for encounters. First hit wins; remaining entries are skipped.
        // --no-encounters skips the roll entirely, for scripted runs that are testing something else.
        if (Config.Debug.NoEncounters) return;

        foreach (var enc in biomeInfo.Encounters)
        {
            if (_travelRng.NextDouble() < enc.ChancePerCell)
            {
                StartTravelEncounter(enc.CreatureName);
                break;
            }
        }
    }

    /// <summary>
    /// Triggers death with the given cause, transitioning to the Death game mode.
    /// </summary>
    public void TriggerDeath(DeathCause cause)
    {
        // Permadeath, and erased here rather than on the death screen's END RUN button: this is the
        // funnel for all three causes, and it fires the moment the run is over, so force-quitting at
        // the death screen cannot leave a save that outlives the character.
        Cathedral.Game.Save.SaveFile.Delete();

        // A death can now happen INSIDE a visit — a lost fight, or a wound taken on a failed act —
        // and a narration session left standing would go on drawing itself over the death screen
        // every frame, because Update's narrative branch is entered on _isInNarrativeMode and not on
        // the mode. Torn down here rather than at each call site: this is the funnel, and the next
        // cause of death added should not have to remember.
        if (_isInNarrativeMode) TearDownNarrativeSession();

        _deathCause = cause;
        _interface.MovementPaused = true;
        SetMode(GameMode.Death);
    }

    /// <summary>
    /// Called when protagonist arrives at a vertex.
    /// This should be called by MicroworldInterface when movement completes.
    /// </summary>
    public void OnProtagonistArrived(int vertexIndex)
    {
        if (_currentMode != GameMode.Traveling)
        {
            // An encounter rolled on the journey's LAST step is announced from inside the same
            // MicroworldInterface.UpdateMovement call that announces the arrival: the step event
            // switches the mode to EncounterPrompt, and the arrival event follows immediately —
            // with the interface's path already cleared between the two.
            //
            // Dropping the arrival here left nothing able to finish the journey. The fight ends,
            // OnFightCompleted resumes travel, and there is no path left to walk: the game sits in
            // Traveling for ever over a cleared terminal, which is a black screen and no way out
            // but the pause menu. So the arrival is kept and delivered once the encounter settles.
            if (_inTravelEncounter) _pendingArrivalVertex = vertexIndex;
            return;
        }

        Console.WriteLine($"LocationTravelGameController: Protagonist arrived at vertex {vertexIndex}");

        // Travel done — any leftover planning state should be wiped before the player
        // re-enters WorldView.
        ClearTravelPlan();
        _travelProgressRenderer?.Erase();
        TravelCompleted?.Invoke();

        // Advance the global in-game clock by the trip's duration before any scene is built (so
        // depletion timestamps and regen checks use the post-travel time).
        Cathedral.Game.Narrative.GameClock.Advance(_committedTravelDays);
        _committedTravelDays = 0f;

        // Settle any vital-heat debt the journey did not get a chance to drain.
        //
        // The per-frame drain in Update() only runs while the mode is Traveling, and the final step of
        // a path both accrues that step's cost AND fires the arrival — so on a ONE-CELL journey the
        // debt is raised and the mode changes before a single frame of draining happens. A body whose
        // queues are entirely black bile could therefore hop between adjacent vertices for ever
        // without paying anything, while the identical body died two cells into a longer trip.
        //
        // Whether a journey is affordable must not depend on how many frames it took, so the same
        // verdict is reached here.
        if (_tripVhDebt >= 1.0f && _protagonist != null)
        {
            if (_protagonist.HumorQueues.IsFullyCritical)
            {
                _consumptionActive        = false;
                _interface.MovementPaused = false;
                TriggerDeath(DeathCause.Starvation);
                return;
            }

            while (_tripVhDebt >= 1.0f)
            {
                var humor = _protagonist.HumorQueues.ConsumeCycled(_protagonist, _travelRng);
                if (humor == null)
                {
                    _consumptionActive        = false;
                    _interface.MovementPaused = false;
                    TriggerDeath(DeathCause.Starvation);
                    return;
                }
                if (humor.VitalHeat > 0) _tripVhDebt -= humor.VitalHeat;
                else break; // negative or zero heat pays nothing; stop rather than spin the queues dry
            }
        }
        _tripVhDebt        = 0f;
        _consumptionActive = false;
        // Movement is paused while the drain runs, and the drain's own "debt paid" branch is what
        // normally lifts it. Arriving mid-drain skips that branch, so releasing it here is what stops
        // the NEXT journey running its whole progress UI with the avatar standing still.
        _interface.MovementPaused = false;

        // Routine replay: if the player launched travel from the routine box and the routine belongs
        // to this destination, replay it instead of starting a fresh narration phase.
        if (_pendingReplayRoutine != null && _pendingReplayRoutine.LocationId == vertexIndex)
        {
            var routine = _pendingReplayRoutine;
            _pendingReplayRoutine  = null;
            _currentLocationVertex = vertexIndex;
            StartRoutineReplay(vertexIndex, routine);
            return;
        }
        _pendingReplayRoutine = null;

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
        // Sample a deterministic mood for this specific location and play full music
        _ambianceEngine?.SetMood(LocationMoodProfiles.SampleMood(locationType.Name, vertexIndex));
        _ambianceEngine?.SetActiveTrackCount(4);
        StartNarrativeInteraction(vertexIndex);
    }

    /// <summary>
    /// Starts biome interaction mode (when there's no specific location).
    /// Both biomes and named locations use the same Phase 6 narrative UI.
    /// </summary>
    private void StartBiomeInteraction(int vertexIndex, Cathedral.Glyph.Microworld.BiomeType biomeType)
    {
        Console.WriteLine($"LocationTravelGameController: Starting Phase 6 interaction for biome '{biomeType.Name}'");
        // Sample a deterministic mood for this biome tile and play full music
        _ambianceEngine?.SetMood(LocationMoodProfiles.SampleMood(biomeType.Name, vertexIndex));
        _ambianceEngine?.SetActiveTrackCount(4);
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

    // UpdateLocationState(LocationInstanceState) was removed here. It replaced the entry in
    // _locationStates with a different object while the built scene still held the OLD collections —
    // LocationInstanceState.AttachTo hands its dictionaries to the scene by reference, so a swap
    // detaches every later write from the state that gets saved. It had no callers, and the save
    // system depends on that reference-sharing invariant holding. Location state is mutated in
    // place, never replaced.

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
        _ambianceEngine?.SetFilter(MusicFilter.Loading);
        _core.SetNarrationMode(true);
        _core.SetWorldInteractionsEnabled(false);
        _interface.SetWorldInteractionsEnabled(false);

        if (_core.Terminal != null)
        {
            if (_llmLoadingRenderer == null)
                _llmLoadingRenderer = new LLMLoadingRenderer(_core.Terminal, "language model");

            float progress;
            string status;
            lock (_llmLoadLock)
            {
                progress = _llmLoadProgress;
                status   = _llmLoadStatus;
            }
            _llmLoadingRenderer.Update(progress, status);
        }

        // Released only now, with the screen composed: the hardware probe waits for this so its
        // benchmarks run in front of a player who can see what they are for. Last in the method, so
        // the frame is built before the probe starts competing for the CPU.
        _llamaServer?.NotifyLoadingScreenVisible();
    }

    private void OnEnterMainMenu()
    {
        Console.WriteLine("LocationTravelGameController: Entered MainMenu mode");
        _ambianceEngine?.SetFilter(MusicFilter.None);
        _ambianceEngine?.SetMood(MusicMoodState.Neutral);
        _ambianceEngine?.SetActiveTrackCount(0);
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
            
            // Continue does two jobs from one button: resume the session already in memory (the pause
            // menu's return path), or, on a cold start, load the save from disk. So it is enabled when
            // EITHER exists, and the callback picks between them.
            _mainMenuRenderer.HasGameStarted = _hasGameStarted;
            _mainMenuRenderer.CanContinue    = _hasGameStarted || HasReadableSave;
            _mainMenuRenderer.SetButtons(
                onNew: () =>
                {
                    // Which world? Normally the player answers by picking a moon out of the sky, and
                    // nothing is spent or built until they confirm one — so backing out of that
                    // screen leaves the save on disk exactly as it was.
                    //
                    // --seed answers it on the command line instead, and a screen that asked a
                    // question already answered would only be an extra keypress in front of every
                    // scripted run.
                    if (Config.Rng.SeedPinned && Config.Rng.Seed.HasValue)
                    {
                        StartNewRun(Config.Rng.Seed.Value);
                        SetMode(GameMode.ProtagonistCreation);
                        return;
                    }
                    SetMode(GameMode.WorldSelection);
                },
                onContinue: () =>
                {
                    if (_hasGameStarted)
                    {
                        // A live session: resume whatever was paused.
                        SetMode(MenuReturnMode);
                        return;
                    }

                    // A cold start. The old code called ResetGameState() here — a new game wearing
                    // Continue's clothes, which with a real save on disk would silently destroy it.
                    if (TryContinueSavedRun())
                    {
                        SetMode(GameMode.WorldView);
                        return;
                    }

                    // Unreadable after all (deleted or corrupted since the menu was drawn): grey the
                    // button out rather than starting a run the player did not ask for.
                    _mainMenuRenderer.CanContinue = false;
                    _mainMenuRenderer.Render();
                },
                onProtagonist: () =>
                {
                    SetMode(GameMode.ProtagonistManagement);
                },
                onSettings: () =>
                {
                    SetMode(GameMode.Settings);
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

    // ── World selection ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Opens the sky a new run picks its world out of: no world drawn, no world interactions, the
    /// star sphere alone, and the moons made clickable.
    /// </summary>
    private void OnEnterWorldSelection()
    {
        Console.WriteLine("LocationTravelGameController: Entered WorldSelection mode");
        _ambianceEngine?.SetFilter(MusicFilter.None);
        _ambianceEngine?.SetMood(MusicMoodState.Neutral);
        _ambianceEngine?.SetActiveTrackCount(1);

        // Not narration mode: that greys the world down, and there is no world here to grey. The
        // sky is the subject of the screen and is drawn at full strength.
        _core.SetNarrationMode(false);

        // The world's own hover and click are off and the sky's are on. Both matter: the sphere is
        // not drawn, but its vertices are still there to be hit, and a click that quietly travelled
        // somewhere in an ungenerated world is the kind of thing that surfaces three phases later.
        _core.SetWorldInteractionsEnabled(false);
        _interface.SetWorldInteractionsEnabled(false);
        _core.SkySelectionEnabled = true;

        // Far enough back that a good spread of sky is in frame at once, and still well inside the
        // star sphere. Same lever the world map uses for its own framing.
        _core.Camera.SetDistance(Config.GlyphSphere.CameraZoomWorldView);

        // Re-entrant, like every other OnEnter — a modal notice dismissed over this screen repaints
        // it through RedrawCurrentMode, and a repaint that quietly discarded the player's choice
        // would be indistinguishable from a click that never registered. The choice is cleared on
        // the way OUT instead, in LeaveWorldSelection, which both exits go through.
        _hoveredMoon = -1;
        _core.HighlightMoon(-1);
        _core.SelectMoon(_selectedMoon);

        // The whole sky is back. A moon is blanked only while its world is being lived in, and
        // standing here the player lives in none — without this, the world a previous run of this
        // process was played in would be the one world unchoosable.
        _core.HideMoon(-1);

        if (_core.Terminal != null)
        {
            _worldSelectionRenderer ??= new WorldSelectionRenderer(_core.Terminal);

            // Transparent with passthrough, exactly as the world map is: the box and the arrow pad
            // are the only opaque things on screen, and everything else has to let a click reach the
            // sky behind it.
            SetTransparentWorldOverlay(clickPassthrough: true);
            _worldSelectionRenderer.Draw(_selectedMoon, _hoveredMoon, _core.MoonCount);
            _cameraArrowPad?.Draw();
        }
    }

    /// <summary>
    /// Puts everything the selection screen switched on back, whichever way it is being left.
    /// Shared by CONFIRM and CANCEL so neither can forget half of it.
    /// </summary>
    private void LeaveWorldSelection()
    {
        _core.SkySelectionEnabled = false;
        _core.HighlightMoon(-1);
        _core.SelectMoon(-1);
        _selectedMoon = -1;
        _hoveredMoon  = -1;
        _heldArrow    = CameraArrow.None;
        _core.CameraTurnYawPerSecond   = 0f;
        _core.CameraTurnPitchPerSecond = 0f;
        _worldSelectionRenderer?.Erase();
        _cameraArrowPad?.Erase();
    }

    /// <summary>The cursor moved onto (or off) a moon. Fired by the core, only in this mode.</summary>
    private void OnMoonHovered(int ordinal)
    {
        if (_currentMode != GameMode.WorldSelection) return;
        if (ordinal == _hoveredMoon) return;

        _hoveredMoon = ordinal;

        // The highlight follows the cursor whatever else is going on, including over a choice already
        // made: the two are drawn differently (a chosen moon is larger and white, a pointed-at one
        // smaller and yellow), so both can be on screen at once and a player can weigh a second moon
        // against the one they have without giving it up first.
        _core.HighlightMoon(ordinal);

        if (ordinal >= 0)
            _ambianceEngine?.TriggerGameEvent(GameEventType.SmallInteraction);
    }

    /// <summary>
    /// A moon was clicked, or the empty sky was. Selects it; the world is not built until CONFIRM.
    ///
    /// <para>A press on bare sky unchooses, which is the ordinary meaning of clicking off a
    /// selection and saves the player hunting for a button to do it with. It is not the same as
    /// CANCEL: the screen stays open and another moon can be taken.</para>
    /// </summary>
    private void OnMoonClicked(int ordinal)
    {
        if (_currentMode != GameMode.WorldSelection) return;

        if (ordinal < 0)
        {
            if (_selectedMoon < 0) return; // nothing was chosen; a press on empty sky says nothing
            Console.WriteLine("WorldSelection: choice released (clicked empty sky)");
            _selectedMoon = -1;
            _core.SelectMoon(-1);
            _ambianceEngine?.TriggerGameEvent(GameEventType.SmallInteraction);
            return;
        }

        _selectedMoon = ordinal;
        _core.SelectMoon(ordinal);
        _ambianceEngine?.TriggerGameEvent(GameEventType.StrongInteraction);
        Console.WriteLine($"WorldSelection: moon {ordinal} ({Cathedral.Glyph.SkyMoons.Name(ordinal)}) "
                        + $"selected — seed {Cathedral.Glyph.SkyMoons.WorldSeed(ordinal)}");
    }

    /// <summary>
    /// Takes the chosen moon: builds its world, blanks it out of the sky, and hands the run on to
    /// protagonist creation. Nothing before this point has spent the save or touched the master seed.
    /// </summary>
    private void ConfirmWorldSelection()
    {
        if (_selectedMoon < 0) return;

        int ordinal = _selectedMoon;
        int seed    = Cathedral.Glyph.SkyMoons.WorldSeed(ordinal);
        Console.WriteLine($"WorldSelection: confirmed {Cathedral.Glyph.SkyMoons.Name(ordinal)} "
                        + $"(moon {ordinal}, seed {seed})");

        _ambianceEngine?.TriggerGameEvent(GameEventType.StrongInteraction);
        LeaveWorldSelection();

        // StartNewRun blanks the moon itself, from the seed — one rule covering this path and the
        // continued-save one alike, rather than two callers each remembering to do it.
        StartNewRun(seed);

        SetMode(GameMode.ProtagonistCreation);
    }

    /// <summary>Abandons the choice and goes back to the menu. Costs nothing: no save was spent.</summary>
    private void CancelWorldSelection()
    {
        Console.WriteLine("WorldSelection: cancelled");
        _ambianceEngine?.TriggerGameEvent(GameEventType.SmallInteraction);
        LeaveWorldSelection();
        SetMode(GameMode.MainMenu);
    }

    /// <summary>
    /// Blanks whichever moon stands for <paramref name="seed"/>. Does nothing when no moon does,
    /// which is every world reached by <c>--seed</c> rather than by picking one.
    /// </summary>
    private void HideMoonForSeed(int seed)
    {
        int ordinal = Cathedral.Glyph.SkyMoons.OrdinalForSeed(seed, _core.MoonCount);
        if (ordinal >= 0) _core.HideMoon(ordinal);
    }

    // ── The camera arrow pad ─────────────────────────────────────────────────────────────

    /// <summary>The two modes the arrow pad is drawn in — the only two with a camera to turn.</summary>
    private bool CameraPadActive
        => (_currentMode == GameMode.WorldSelection
            || (_currentMode == GameMode.WorldView && _companionRemovalRenderer == null
                                                   && _routineOutcomeBox == null
                                                   && _travelRoutinesBox == null))
           && _cameraArrowPad != null;

    /// <summary>
    /// Keeps the camera turning while an arrow is held down.
    ///
    /// <para>It does not turn the camera itself: it hands the core a standing rate in degrees per
    /// second, which the render loop applies at frame rate. This update runs at 10 Hz, and a camera
    /// turned from here would move in six-degree jerks under a sixty-frame-a-second sky. The press
    /// itself is handled in the click router, which turns it one fixed step at once so that a click
    /// too short to span a frame still does something.</para>
    /// </summary>
    private void UpdateCameraPad(float deltaTime)
    {
        bool held = _cameraArrowPad != null
                 && _core.Terminal != null
                 && CameraPadActive
                 && _core.Terminal.InputHandler.IsLeftMouseDown
                 && _heldArrow != CameraArrow.None
                 // Held means "still down, still over the arrow it went down on". Dragging off the
                 // pad releases it, which is what every other button in the game does.
                 && _cameraArrowPad.Hovered == _heldArrow;

        if (!held) _heldArrow = CameraArrow.None;

        float rate = held ? Config.CameraPad.RotationSpeed : 0f;
        _core.CameraTurnYawPerSecond = _heldArrow switch
        {
            CameraArrow.Left  => -rate,
            CameraArrow.Right => +rate,
            _                 => 0f,
        };
        _core.CameraTurnPitchPerSecond = _heldArrow switch
        {
            CameraArrow.Up   => +rate,
            CameraArrow.Down => -rate,
            _                => 0f,
        };
    }

    /// <summary>Applies one arrow's worth of rotation, in the same sense the arrow keys do.</summary>
    private void ApplyCameraArrow(CameraArrow arrow, float degrees)
    {
        switch (arrow)
        {
            case CameraArrow.Left:  _core.Camera.Rotate(-degrees, 0f); break;
            case CameraArrow.Right: _core.Camera.Rotate(+degrees, 0f); break;
            case CameraArrow.Up:    _core.Camera.Rotate(0f, +degrees); break;
            case CameraArrow.Down:  _core.Camera.Rotate(0f, -degrees); break;
        }
    }

    /// <summary>
    /// Routes a press to the arrow pad. Returns true when it landed on an arrow, so the caller stops
    /// looking for something else under the cursor.
    /// </summary>
    private bool TryPressCameraArrow(int x, int y)
    {
        if (!CameraPadActive) return false;

        var arrow = _cameraArrowPad!.ArrowAt(x, y);
        if (arrow == CameraArrow.None) return false;

        _heldArrow = arrow;
        ApplyCameraArrow(arrow, Config.CameraPad.ClickStep);
        _ambianceEngine?.TriggerGameEvent(GameEventType.SmallInteraction);
        return true;
    }

    private void OnEnterSettings()
    {
        Console.WriteLine("LocationTravelGameController: Entered Settings mode");
        // Darken the sphere and disable world interactions, like the main menu.
        _core.SetNarrationMode(true);
        _core.SetWorldInteractionsEnabled(false);
        _interface.SetWorldInteractionsEnabled(false);

        if (_core.Terminal != null)
        {
            if (_settingsMenuRenderer == null)
            {
                _settingsMenuRenderer = new SettingsMenuRenderer(_core.Terminal)
                {
                    OnMusicVolumeChanged = v =>
                    {
                        UserSettings.MusicVolume = v;
                        _ambianceEngine?.SetMasterMusicVolume(UserSettings.MusicVolume01);
                        UserSettings.Save();
                    },
                    OnSfxVolumeChanged = v =>
                    {
                        UserSettings.SfxVolume = v;
                        _ambianceEngine?.SetMasterSfxVolume(UserSettings.SfxVolume01);
                        UserSettings.Save();
                    },
                    OnDitherChanged = on =>
                    {
                        _core.PostProcess.Enabled = on;
                        UserSettings.DitherEnabled = on;
                        UserSettings.Save();
                    },
                    // Applied at once, unlike the model rows: the window is right there, and a
                    // fullscreen setting that took effect next launch would be a strange thing to
                    // offer. Same switch F11 flips (Cathedral.Glyph.WindowMode), same store.
                    OnFullscreenChanged = on =>
                    {
                        Cathedral.Glyph.WindowMode.Apply(_core, on);
                        UserSettings.Fullscreen = Cathedral.Glyph.WindowMode.IsFullscreen;
                        UserSettings.Save();
                    },

                    // Both glyph rows apply at once, and visibly — this screen is terminal text
                    // drawn through the very atlas and uniforms they change, so the labels under
                    // the player's cursor redraw at the new setting.
                    OnGlyphWeightChanged = step =>
                    {
                        Config.Terminal.ApplyGlyphWeight(step);
                        // Half of a weight step is the emboldening pen, which is baked into the
                        // raster rather than shaded. Without this the gamma half applies alone and
                        // the row does something subtler than it claims.
                        _core.Terminal?.Atlas.Rebuild();
                        UserSettings.GlyphWeight = Config.Terminal.GlyphWeightStep;
                        UserSettings.Save();
                    },
                    OnGlyphScaleChanged = scale =>
                    {
                        Config.Terminal.GlyphScale = scale;
                        UserSettings.GlyphScale = scale;
                        UserSettings.Save();
                    },

                    // The world rows apply at once too, but the player cannot see it from here —
                    // the sphere is behind this screen. Nothing is deferred all the same: a setting
                    // that waited for the next launch would be indistinguishable from one that did
                    // not work, since neither shows anything until the screen is closed.
                    OnWorldGlyphWeightChanged = step =>
                    {
                        Config.GlyphSphere.ApplyWorldGlyphWeight(step);
                        // Same trap as the terminal's weight: the pen half is rastered into the
                        // atlas, so without this only the cutoff half applies and the row does
                        // something subtler than it claims.
                        _core.RebuildGlyphAtlasForWeight();
                        UserSettings.WorldGlyphWeight = Config.GlyphSphere.WorldGlyphWeightStep;
                        UserSettings.Save();
                    },
                    OnWorldGlyphScaleChanged = scale =>
                    {
                        Config.GlyphSphere.WorldGlyphScale = scale;
                        UserSettings.WorldGlyphScale = scale;
                        UserSettings.Save();
                    },

                    // The three language-model rows only persist; nothing is applied here. The
                    // server has already loaded the model for this session, so these take effect
                    // at the next launch and the screen says so.
                    OnLlmDeviceChanged = d =>
                    {
                        UserSettings.LlmDevice = d;
                        UserSettings.Save();
                    },
                    OnLlmGpuLayersChanged = n =>
                    {
                        UserSettings.LlmGpuLayers = n;
                        UserSettings.Save();
                    },
                    OnLlmCpuThreadsChanged = n =>
                    {
                        UserSettings.LlmCpuThreads = n;
                        UserSettings.Save();
                    },

                    // Discarding the signature is the whole of "re-detect": LlamaProbe re-runs
                    // whenever it does not match the model file, which happens during the next
                    // launch's model load rather than freezing the game here.
                    OnLlmRedetect = () =>
                    {
                        UserSettings.LlmProbeSignature = "";
                        UserSettings.Save();
                    },

                    OnBack = () => SetMode(GameMode.MainMenu),
                };
            }

            SyncAndRenderSettingsScreen();
        }
    }

    /// <summary>
    /// Pulls every row of the Settings screen from its live source and redraws it.
    ///
    /// <para><b>Never render this screen without calling this first.</b> The renderer holds its own
    /// copy of each value, so a bare <c>Render()</c> redraws whatever it was last told — which is
    /// how F11 pressed with the screen open came to leave the SCREEN row reading WINDOW over a
    /// fullscreen window. The call was there; the sync was not, and a redraw that faithfully
    /// repaints a stale value looks exactly like a fix.</para>
    ///
    /// <para>Most rows are read from the thing that actually obeys them rather than from
    /// <c>UserSettings</c> — the renderer for dither, <c>WindowMode</c> for fullscreen,
    /// <c>Config.Terminal</c> for the glyph rows. All three can be moved by something that does not
    /// write the setting (a command-line flag, an F-key, the F11 key), and the screen's job is to
    /// show the player where the switch is, not where it was last saved. The volumes and the model
    /// rows have no such second route in, so they come from the store.</para>
    /// </summary>
    private void SyncAndRenderSettingsScreen()
    {
        if (_settingsMenuRenderer == null) return;

        _settingsMenuRenderer.MusicVolume   = UserSettings.MusicVolume;
        _settingsMenuRenderer.SfxVolume     = UserSettings.SfxVolume;
        _settingsMenuRenderer.LlmDevice     = UserSettings.LlmDevice;
        _settingsMenuRenderer.LlmGpuLayers  = UserSettings.LlmGpuLayers;
        _settingsMenuRenderer.LlmCpuThreads = UserSettings.LlmCpuThreads;
        _settingsMenuRenderer.DitherEnabled = _core.PostProcess.Enabled;
        _settingsMenuRenderer.Fullscreen    = Cathedral.Glyph.WindowMode.IsFullscreen;
        _settingsMenuRenderer.GlyphWeight   = Config.Terminal.GlyphWeightStep;
        _settingsMenuRenderer.GlyphScale    = Config.Terminal.GlyphScale;

        // Read from Config rather than UserSettings for the same reason as the two rows above: it
        // is what the sphere actually obeys, and the screen's job is to show where the switch is.
        _settingsMenuRenderer.WorldGlyphWeight = Config.GlyphSphere.WorldGlyphWeightStep;
        _settingsMenuRenderer.WorldGlyphScale  = Config.GlyphSphere.WorldGlyphScale;

        _settingsMenuRenderer.Render();
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
                Console.WriteLine("LocationTravelGameController: Protagonist creation complete, entering ChildhoodReminescence");
                // Re-initialize memory with the organ scores the player set during creation.
                // ResetGameState called InitializeMemory earlier with initial random scores;
                // now we rebuild modules to reflect the final configured values.
                protagonist.InitializeMemory();
                protagonist.ReinitializeHumorQueues();

                // --black-bile is applied HERE, not in ResetGameState. The queues are rebuilt from the
                // organ scores the player just settled, which overwrites anything put there earlier —
                // so souring them before this point leaves a perfectly healthy protagonist and the
                // starvation the flag exists to stage never happens.
                if (Config.Debug.BlackBile)
                {
                    foreach (var queue in protagonist.HumorQueues.All)
                        queue.FillWith(new Cathedral.Game.Narrative.BlackBileHumor());
                    Console.WriteLine("[debug] --black-bile: all four humor queues filled with black bile");
                }

                // Grant the only modus mentis the protagonist starts with.
                var rmm = ModusMentisRegistry.Instance.GetModusMentis("childhood_reminescence");
                if (rmm == null)
                    throw new InvalidOperationException("LocationTravelGameController: childhood_reminescence MM is not registered.");

                if (protagonist.LearnedModiMentis.Count > 0)
                    throw new InvalidOperationException("LocationTravelGameController: Protagonist must have no MM before childhood reminescence starts.");

                var instance = (ModusMentis)Activator.CreateInstance(rmm.GetType())!;
                instance.Level = 1;
                protagonist.AcquireModusMentis(instance);

                if (protagonist.LearnedModiMentis.Count == 0
                    || protagonist.LearnedModiMentis[0].ModusMentisId != "childhood_reminescence")
                {
                    throw new InvalidOperationException(
                        "LocationTravelGameController: The first MM before childhood reminescence must be ChildhoodReminescenceModusMentis.");
                }
                _protagonistCreationRenderer = null;
                SetMode(GameMode.ChildhoodReminescence);
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
                _core.SetNarrationMode(true); // restore management-mode world shading
                _managementMenuRenderer = null;
                SetMode(GameMode.MainMenu);
            };
            _managementMenuRenderer.OnItemConsumed = () =>
                _ambianceEngine?.TriggerGameEvent(GameEventType.StrongInteraction);

            // Routines tab: focus the world camera on the selected routine's location and switch the
            // world to full-colour shading so it shows through the tab's transparent minimap porthole.
            _managementMenuRenderer.OnRoutineLocationFocused = locId =>
            {
                if (locId < 0) return;
                _core.SetNarrationMode(false);
                // Use the destination-selection zoom so the porthole frames the world the same way
                // the world map does while the player is choosing where to travel next.
                _core.Camera.SetDistance(Config.GlyphSphere.CameraZoomWorldView);
                _core.CenterCameraOnGlyph(locId);
            };
            _managementMenuRenderer.OnRoutinesPortholeClosed = () =>
                _core.SetNarrationMode(true); // restore dark world shading for the other tabs
            
            _managementMenuRenderer.Render();
        }
    }
    
    private void OnEnterWorldView()
    {
        Console.WriteLine("LocationTravelGameController: Entered WorldView mode");
        _ambianceEngine?.SetMood(MusicMoodState.WorldView);
        _ambianceEngine?.SetFilter(MusicFilter.None);
        _ambianceEngine?.SetActiveTrackCount(4);
        // Set camera zoom for destination selection
        _core.Camera.SetDistance(Config.GlyphSphere.CameraZoomWorldView);
        // Disable narration mode (world is interactive and in focus)
        _core.SetNarrationMode(false);

        // --advance-days: a debug-only shove of the world clock, applied once on first arrival at
        // the world map and then cleared. Inert at its default of 0.
        if (Config.Debug.AdvanceDays > 0)
        {
            double days = Config.Debug.AdvanceDays;
            Config.Debug.AdvanceDays = 0;
            Cathedral.Game.Narrative.GameClock.Advance(days);
            Console.WriteLine($"[debug] --advance-days: clock pushed forward {days} d (now {Cathedral.Game.Narrative.GameClock.Days:F1} d)");
        }

        // Healing gate: the clock only advances on travel and work, so returning to the world map
        // is the moment wounds can have closed. Runs BEFORE the age check, because closing a wound
        // restores HP and lifetime is wound-aware — a heart wound that healed on the journey must
        // not still be counted against the person when their span is measured a line later.
        HealPartyWounds();

        // Age gate: the same moment anyone can have aged past their lifetime. Check before anything
        // else — a dead protagonist ends the run outright.
        if (CheckOldAgeDeaths())
        {
            _core.SetWorldInteractionsEnabled(false);
            _interface.SetWorldInteractionsEnabled(false);
            return;
        }

        // Companion-capacity gate: if the party exceeds what the heart can sustain, show the
        // dismissal overlay and keep the world non-interactive until the player confirms.
        if (TryShowCompanionRemoval())
        {
            _core.SetWorldInteractionsEnabled(false);
            _interface.SetWorldInteractionsEnabled(false);
            return;
        }

        // --start-fight: drop straight into a fight on the first arrival at the world map.
        // Inert unless the flag is passed.
        if (Config.Debug.StartFight != null)
        {
            string creature = Config.Debug.StartFight;
            Config.Debug.StartFight = null;
            if (StartDebugFight(creature)) return;
        }

        EnterWorldViewInteractive();
    }

    /// <summary>
    /// Debug-only: begin a fight against a freshly spawned <paramref name="creatureName"/>, with no
    /// travel encounter and no scene. Returns false when the name has no fight-capable archetype.
    ///
    /// <para>
    /// This is the only way a <c>--cli</c> script can reach fight mode at all. The two real routes
    /// in are a random travel encounter — which every script disables with <c>--no-encounters</c>,
    /// precisely because it fires unpredictably — and provoking a location NPC, which takes a
    /// conversation and a check. Neither is a reasonable prerequisite for testing the fight itself.
    /// It reuses the ENGAGE button's construction path, so what a script drives is what a player
    /// would get.
    /// </para>
    /// </summary>
    private bool StartDebugFight(string creatureName)
    {
        if (_core.Terminal == null || _protagonist == null) return false;
        if (!TravelEncounterArchetypes.TryGetValue(creatureName, out var archetypeFactory))
        {
            Console.Error.WriteLine(
                $"[debug] --start-fight: no fight-capable archetype named '{creatureName}'. " +
                $"Known: {string.Join(", ", TravelEncounterArchetypes.Keys)}");
            return false;
        }

        // _consumptionBiome is only set once a journey has been made, so on the very first arrival
        // at the world map it is still "unknown". Read the avatar's actual biome instead, and fall
        // back to plain if even that is unavailable — this must never throw.
        string biomeName = _interface.GetBiomeNameAt(_interface.GetAvatarVertex()) ?? "plain";
        if (!Cathedral.Glyph.Microworld.BiomeDatabase.Biomes.ContainsKey(biomeName))
            biomeName = "plain";

        var npc = archetypeFactory().Spawn(GameRng.Stream("debug-start-fight"), biomeName);
        npc.AffinityTable.SetEnemy(_protagonist.AffinityKey);

        // Seeded from the master RNG, not the wall clock, so the arena is reproducible under --seed.
        var biome = Cathedral.Glyph.Microworld.BiomeDatabase.Biomes[biomeName];
        var arena = biome.ArenaGeneratorFactory(GameRng.DerivedSeed("debug-start-fight-arena"));

        _interface.MovementPaused = true;
        _inTravelEncounter = true;
        _fightAdapter = new FightModeAdapter(
            _core.Terminal,
            _core.PopupTerminal,
            npc,
            _protagonist,
            arena,
            allies: new List<Cathedral.Game.Npc.NpcEntity>(),
            sfxTrigger: e => _ambianceEngine?.TriggerGameEvent(e),
            setMusicFilter: f => _ambianceEngine?.SetFilter(f));

        Console.WriteLine($"[debug] --start-fight: fighting {npc.DisplayName} ({creatureName})");
        SetMode(GameMode.Fighting);
        return true;
    }

    /// <summary>
    /// Closes any wound on the protagonist or a companion that has had time to mend, and reports
    /// each one. Only Low and Medium wounds ever heal, and only ones suffered during the run — see
    /// <see cref="PartyMember.HealWounds"/>.
    ///
    /// <para>
    /// Deliberately quiet: a line each, no modal. Healing takes hundreds of days, so it is a thing
    /// the player notices in the anatomy panel over a long run rather than an event to interrupt
    /// them for.
    /// </para>
    /// </summary>
    private int HealPartyWounds()
    {
        if (_protagonist == null) return 0;

        int closedCount = 0;
        foreach (var closed in _protagonist.HealWounds())
        {
            Console.WriteLine($"🩹 [HEALED] {_protagonist.DisplayName}: {closed.WoundName} has closed.");
            closedCount++;
        }

        foreach (var companion in _protagonist.CompanionParty)
            foreach (var closed in companion.HealWounds())
            {
                Console.WriteLine($"🩹 [HEALED] {companion.DisplayName}: {closed.WoundName} has closed.");
                closedCount++;
            }

        return closedCount;
    }

    /// <summary>Run the healing sweep on demand — the CLI <c>clock</c> command's other half.</summary>
    public int CliHealPartyWounds() => HealPartyWounds();

    // ── Routines (--cli) ──────────────────────────────────────────────────────
    //
    // The routine box is reached in play by planning a trip and pressing ROUTINES on the travel
    // panel, then clicking a row. A script cannot press either: both are hit-tested against a
    // rendered box whose rows move with how many routines exist. These three seams are the same
    // three actions, addressed by index instead of by pixel — the pattern `travel`/`travel-go`
    // already follows for the world sphere.

    /// <summary>
    /// The protagonist-scoped half of <c>inspect</c>, answerable with no narration in progress.
    ///
    /// <para>Routines live on the protagonist and are FINALISED when a narration session ends, so
    /// the only honest moment to read them is after leaving — by which time the narrative controller
    /// is gone and <c>CliNarration</c> is null. Asking before leaving would be asking before the
    /// thing under test has happened.</para>
    ///
    /// <para>Returns null for a subject this cannot answer, so the caller falls through to the
    /// narration-scoped inspect and its "not in narration" error stays meaningful.</para>
    /// </summary>
    public IReadOnlyList<string>? CliInspectGlobal(string subject)
    {
        // The save is a global fact, and — unlike everything the narration controller reports — it is
        // readable at the world map, which is the only place it is ever written. Answered here so the
        // ordinary expect-state assertion works on it; `save dump` only emits, and `expect` scans the
        // rendered screen, so neither can assert anything about a save.
        //   inspect save     — the LIVE run, as it would be written
        //   inspect savefile — what is actually ON DISK
        // They differ exactly when the autosave has not fired since the last change.
        if (subject == "save")     return CliSaveDump();
        if (subject == "savefile") return CliSaveRead();

        // Whether a menu button is enabled is state, not pixels: disabled buttons differ only by
        // colour on screen, so `dump` cannot show it and `expect` cannot assert it. `regions` prints
        // it but only to the CLI stream, which `expect` does not read either. This is the seam that
        // makes "Continue is greyed out after a death" assertable.
        if (subject == "menu")
            return CliMenuButtons()?
                       .Select(b => $"menu {b.Label} {(b.Enabled ? "enabled" : "disabled")}")
                       .ToList()
                   ?? new List<string> { "menu (not on the main menu)" };

        if (_protagonist == null) return null;
        if (subject is not ("routines" or "all")) return null;

        return _protagonist.RecordedRoutines
            .Select(r => $"routine location={r.LocationId} start={r.StartTime} steps={r.Steps.Count} "
                       + $"verbs=[{string.Join(",", r.Steps.Select(x => x.VerbId))}]")
            .ToList();
    }

    /// <summary>Opens the routine box for the planned destination. False when no trip is planned.</summary>
    public bool CliOpenRoutines()
    {
        if (_currentMode != GameMode.WorldView) return false;
        if (_travelPlanner is not { HasWaypoints: true }) return false;

        OpenRoutinesBox();
        return _travelRoutinesBox != null;
    }

    /// <summary>
    /// The rows the open routine box is offering: the routine's name, whether it can still be
    /// replayed, and why not when it cannot. Empty when the box is closed.
    /// </summary>
    public IReadOnlyList<(string Name, bool Replayable, string? Reason)> CliRoutineEntries
        => _travelRoutinesBox?.CliEntries ?? System.Array.Empty<(string, bool, string?)>();

    /// <summary>
    /// Picks row <paramref name="index"/> and sets out — the same commit the row click makes, so the
    /// replay runs on arrival exactly as it would in play. False when the box is closed, the index is
    /// out of range, or that routine is no longer replayable.
    /// </summary>
    public bool CliSelectRoutine(int index)
    {
        if (_travelRoutinesBox == null) return false;
        var entries = _travelRoutinesBox.CliEntries;
        if (index < 0 || index >= entries.Count || !entries[index].Replayable) return false;

        _pendingReplayRoutine = _travelRoutinesBox.CliRoutineAt(index);
        _travelRoutinesBox    = null;
        SetTransparentWorldOverlay(clickPassthrough: true);
        StartPlannedTravel();
        return true;
    }

    /// <summary>
    /// Presses CONTINUE on the post-replay outcome box, applying the phase the routine ended on.
    /// False when no such box is up. Mirrors the click path in <see cref="OnTerminalCellClicked"/>.
    /// </summary>
    public bool CliDismissRoutineOutcome()
    {
        if (_routineOutcomeBox == null) return false;

        var transition = _replayFinalTransition ?? Cathedral.Game.Narrative.ReturnToTravelTransition.Instance;
        _routineOutcomeBox     = null;
        _replayFinalTransition = null;
        _core.Terminal?.Clear();
        ApplyPhaseTransition(transition);
        return true;
    }

    /// <summary>True while the post-replay outcome box is on screen (for `wait`).</summary>
    public bool CliRoutineOutcomeShown => _routineOutcomeBox != null;

    /// <summary>
    /// Ages the party: kills the protagonist outright if they have outlived their lifetime, and
    /// otherwise drops any companion who has, announcing them in a modal box. Returns true when the
    /// caller should stop (the run ended, or a modal box is now up and owns the screen).
    ///
    /// <para>
    /// Lifetime is wound-aware, so this also catches the case where a heart wound — not the passage
    /// of time — is what pushed someone past their span.
    /// </para>
    /// </summary>
    private bool CheckOldAgeDeaths()
    {
        if (_protagonist == null || _core.Terminal == null) return false;

        // The protagonist's death ends the run; no point reporting companions.
        if (_protagonist.IsDeadOfOldAge())
        {
            Console.WriteLine("LocationTravelGameController: Protagonist died of old age");
            TriggerDeath(DeathCause.OldAge);
            return true;
        }

        // Companions are swept by cause rather than by old age alone — see SettleCompanionDeaths.
        return SettleCompanionDeaths();
    }

    /// <summary>
    /// Drops every companion who has died, leaves their body where the party is standing, and puts
    /// the news on screen. Returns true when anyone fell — the caller's cue that a modal box now owns
    /// the screen. Safe and free to call on any tick: it finds nobody once it has run.
    ///
    /// <para><b>One funnel for all three causes, because there was very nearly none.</b> Old age was
    /// the only death a companion could actually suffer: <c>CompanionParty.Remove</c> was reached by
    /// the age check and the heart-ceiling overflow and by nothing else, so a companion cut down in a
    /// fight walked out of it at zero hit points and stayed in the party for ever — and one bled dry
    /// was not even dead, since humor depletion lived on the discarded <c>Fighter</c> wrapper.
    /// <see cref="PartyMember.CauseOfDeath"/> now answers all three from derived state, so this asks
    /// one question instead of enumerating the ways it could have been asked.</para>
    ///
    /// <para><b>Never while a fight is running.</b> The fight holds its own <c>Fighter</c> wrappers
    /// over these members; pulling one out of the party mid-swing would leave a fighter on the board
    /// belonging to nobody. The sweep is gated on the mode in <see cref="Update"/> and runs the
    /// instant the fight hands back.</para>
    ///
    /// <para>The body is only left when there is a scene to leave it in. A travel encounter and the
    /// old-age check both happen on the world map, where there is no area and no observation graph —
    /// there the box is the whole of the news, which is what it always was.</para>
    /// </summary>
    /// <summary>
    /// Ends the run if the protagonist is dead of <b>wounds</b> or of <b>spent humors</b>. Returns
    /// true when it did.
    ///
    /// <para>Both are dealt at arbitrary moments of a visit and neither had a check that covered
    /// those moments. A wound arrives from a blow or from the penalty a failed act samples — there
    /// was no check for it at all, so a mortal one left the protagonist playing on at zero health.
    /// The humors are spent by bleeding and by every buff paid for in a fight, and the three checks
    /// that existed were all on the travel path — so a body whose queues went critical indoors stayed
    /// upright until the next journey drew heat it did not have. Worse, a protagonist who bled out in
    /// a fight that a companion then <em>won</em> walked out of it alive and untouched, because
    /// depletion is recorded on the discarded <c>Fighter</c> wrapper.</para>
    ///
    /// <para><b>Age is the exception, and stays on the world map.</b> Sweeping it here would end the
    /// run mid-visit the moment the clock crossed the term — and since a beast outlives a person, it
    /// would also make a companion's death by age unreachable, the protagonist always falling first.
    /// Companions are swept on all three (<see cref="SettleCompanionDeaths"/>) because their death
    /// ends nothing.</para>
    ///
    /// <para>The travel-path starvation checks are left in place and still fire first while a journey
    /// is being walked: heat is drawn leg by leg there, and the drain notices the queues empty in the
    /// same breath as it empties them.</para>
    /// </summary>
    private bool SettleProtagonistDeath()
    {
        var cause = _protagonist?.CauseOfDeath;
        if (cause is null or DeathCause.OldAge) return false;

        Console.WriteLine($"LocationTravelGameController: Protagonist died — {cause}");
        TriggerDeath(cause.Value);
        return true;
    }

    private bool SettleCompanionDeaths()
    {
        if (_protagonist == null || _core.Terminal == null) return false;

        var fallen = _protagonist.CompanionParty.Where(c => c.IsDead).ToList();
        if (fallen.Count == 0) return false;

        var scene = _narrativeController?.Scene;
        var area  = _narrativeController?.CurrentPoV?.Where;

        var lines = new List<string>();
        foreach (var companion in fallen)
        {
            var cause = companion.CauseOfDeath;
            _protagonist.CompanionParty.Remove(companion);

            lines.Add(cause switch
            {
                DeathCause.OldAge     => $"{companion.DisplayName} — died at {(int)Math.Round(companion.GetAgeDays())} days",
                DeathCause.Starvation => $"{companion.DisplayName} — bled out, humors spent",
                _                     => $"{companion.DisplayName} — died of their wounds",
            });
            Console.WriteLine($"LocationTravelGameController: Companion '{companion.DisplayName}' died — {cause}");

            // The same door an enemy's body comes through, so the corpse is observable, cuttable and
            // queued to open the next narration phase without any of that knowing whose it is.
            if (scene != null && area != null)
                foreach (var remains in Cathedral.Game.Npc.Corpse.CorpseRegistry.CreateForCompanion(companion))
                    scene.AddPointOfInterestToArea(area, remains);
        }

        // Modal overlay: transparent backdrop (world visible behind) but clicks are captured.
        _core.Terminal.Visible = true;
        _core.Terminal.TransparentClickPassthrough = false;
        _core.Terminal.Clear();
        _companionDeathBox = new CompanionDeathBox(_core.Terminal, lines);
        _companionDeathBox.Render();
        _ambianceEngine?.TriggerGameEvent(GameEventType.NegativeOutcome);
        return true;
    }

    /// <summary>
    /// Shows the companion-removal overlay when the party holds more companions than the
    /// protagonist's heart can sustain (<see cref="MaxCompanionsStat"/>). Returns true when
    /// the overlay was shown (and the travel phase should wait for confirmation).
    /// </summary>
    private bool TryShowCompanionRemoval()
    {
        if (_protagonist == null || _core.Terminal == null) return false;

        int max = new MaxCompanionsStat().GetValue(_protagonist);
        if (_protagonist.CompanionParty.Count <= max) return false;

        var companions = _protagonist.CompanionParty.ToList();
        _companionRemovalRenderer = new CompanionRemovalRenderer(_core.Terminal, companions, max);

        // Modal overlay: transparent backdrop (world visible behind) but clicks are captured.
        _core.Terminal.Visible = true;
        _core.Terminal.TransparentClickPassthrough = false;
        _core.Terminal.Clear();
        for (int yy = 0; yy < _core.Terminal.Height; yy++)
            for (int xx = 0; xx < _core.Terminal.Width; xx++)
                _core.Terminal.SetCell(xx, yy, ' ',
                    Cathedral.Terminal.Utils.Colors.Transparent,
                    Cathedral.Terminal.Utils.Colors.Transparent);
        _companionRemovalRenderer.Render();
        return true;
    }

    /// <summary>
    /// Dismisses the ticked companions, closes the overlay, and proceeds into the normal
    /// interactive WorldView phase.
    /// </summary>
    private void ConfirmCompanionRemoval()
    {
        if (_companionRemovalRenderer == null || _protagonist == null) return;

        foreach (var companion in _companionRemovalRenderer.SelectedForRemoval)
            _protagonist.CompanionParty.Remove(companion);

        _companionRemovalRenderer = null;
        EnterWorldViewInteractive();
    }

    /// <summary>
    /// Enables world interactions and draws the transparent travel overlay — the normal
    /// WorldView state once any companion-capacity gate has been cleared.
    /// </summary>
    private void EnterWorldViewInteractive()
    {
        // Ensure no companion-capacity overlay lingers over the interactive view.
        _companionRemovalRenderer = null;

        // Re-enable world interactions
        _core.SetWorldInteractionsEnabled(true);
        _interface.SetWorldInteractionsEnabled(true);

        // Apply travel-range darkening based on feet stat.
        if (_protagonist != null)
        {
            var rangeStat = new MaxTravelDistanceStat();
            _interface.SetTravelRange(_interface.GetAvatarVertex(), rangeStat.GetRadius(_protagonist));
        }

        // Re-assert the protagonist glyph and re-center the camera on it.
        // SetTravelRange / path cleanup can occasionally overwrite the protagonist cell;
        // doing this last guarantees the '@' is always visible when WorldView opens.
        int avatarVertex = _interface.GetAvatarVertex();
        if (avatarVertex >= 0)
        {
            _interface.RefreshProtagonistGlyph();
            _core.CenterCameraOnGlyph(avatarVertex);
        }

        // Show the terminal as a UI overlay for the travel info box, but let clicks
        // on transparent cells fall through to the 3D world.
        SetTransparentWorldOverlay(clickPassthrough: true);

        // The one save point. This method is the single funnel for "on the world map, interactive,
        // choosing where to go", which is past the old-age, companion-capacity and debug-fight gates
        // in OnEnterWorldView, and is reached again after a companion dismissal. Loading routes
        // through here too, so a continued run immediately rewrites an identical file — harmless, and
        // simpler than a flag to suppress it.
        Autosave();
    }

    /// <summary>
    /// Resets the terminal to a full sheet of fully-transparent cells so the 3D world shows through,
    /// and sets whether clicks on those transparent cells fall through to the sphere. Because
    /// <see cref="TerminalHUD.Clear"/> paints opaque black, every path that wants the world visible
    /// behind it — the interactive WorldView and the modal travel overlays alike — must re-assert
    /// transparency this way rather than relying on a bare Clear.
    /// </summary>
    private void SetTransparentWorldOverlay(bool clickPassthrough)
    {
        if (_core.Terminal == null) return;
        _core.Terminal.Visible = true;
        _core.Terminal.TransparentClickPassthrough = clickPassthrough;
        _core.Terminal.Clear();
        for (int y = 0; y < _core.Terminal.Height; y++)
            for (int x = 0; x < _core.Terminal.Width; x++)
                _core.Terminal.SetCell(x, y, ' ',
                    Cathedral.Terminal.Utils.Colors.Transparent,
                    Cathedral.Terminal.Utils.Colors.Transparent);
    }

    private void OnEnterTraveling()
    {
        Console.WriteLine("LocationTravelGameController: Entered Traveling mode");
        _ambianceEngine?.SetFilter(MusicFilter.Traveling);
        _core.SetCloudSpeedMultiplier(50.0f);
        // Keep current location mood but thin out to drone + noise only during travel
        _ambianceEngine?.SetActiveTrackCount(1);
        // Set camera zoom for travel animation
        _core.Camera.SetDistance(Config.GlyphSphere.CameraZoomTraveling);
        // Disable narration mode (world is visible during travel)
        _core.SetNarrationMode(false);
        // Could show travel info in terminal
    }

    private void OnEnterDeath()
    {
        Console.WriteLine($"LocationTravelGameController: Protagonist died — {_deathCause}");
        _ambianceEngine?.TriggerGameEvent(GameEventType.NegativeOutcome);
        _ambianceEngine?.SetFilter(MusicFilter.None);
        _ambianceEngine?.SetActiveTrackCount(0);
        _core.SetNarrationMode(true);

        if (_deathScreenRenderer == null)
            _deathScreenRenderer = new DeathScreenRenderer(_core.Terminal);

        _deathScreenRenderer.Draw(_deathCause);
    }

    private void OnEnterEncounterPrompt()
    {
        Console.WriteLine("LocationTravelGameController: Entered EncounterPrompt mode");
        _ambianceEngine?.TriggerGameEvent(GameEventType.NegativeOutcome);
        _core.SetNarrationMode(true);
        _core.SetWorldInteractionsEnabled(false);
        _interface.SetWorldInteractionsEnabled(false);

        if (_core.Terminal == null || _pendingEncounterNpc == null) return;
        _core.Terminal.Visible = true;

        if (_encounterPromptRenderer == null)
            _encounterPromptRenderer = new EncounterPromptRenderer(_core.Terminal);

        _encounterPromptRenderer.SetHover(-1, -1);
        _encounterPromptRenderer.Draw(
            _pendingEncounterNpc.DisplayName,
            _pendingEncounterCreatureName ?? "",
            _consumptionBiome);
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
    /// Registers a scene factory for a specific biome (Scene system, not graph-based).
    /// Takes precedence over the default PlainSceneFactory fallback.
    ///
    /// <para>Takes a <b>constructor</b>, not an instance: <see cref="BuildSceneForLocation"/> calls it
    /// once per build so no factory's working state can leak into the next scene. See the note
    /// there.</para>
    /// </summary>
    /// <summary>How many biomes have a scene factory. Reported once after registration.</summary>
    public int SceneFactoryCount => _sceneFactories.Count;

    public void RegisterSceneFactory(string biomeName, Func<SceneFactory> factory)
    {
        _sceneFactories[biomeName.ToLowerInvariant()] = factory;
        // One line per biome was ten lines of the startup log saying only that a registry has
        // entries in it. The count is reported once when registration finishes.
        Cathedral.GameLog.WriteToFileOnly($"LocationTravelGameController: Registered scene factory for biome '{biomeName}'");
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

        // Hide popup when mouse is over the travel box to avoid overlapping UI
        if (_travelInfoRenderer != null && _travelInfoRenderer.IsOverBox(_mouseCellX, _mouseCellY))
            return;
        
        // Get hovered vertex from core
        int hoveredVertex = _core.HoveredVertexIndex;
        
        // If no vertex is hovered or hovering over invalid vertex, leave popup empty
        if (hoveredVertex < 0)
            return;

        // Suppress popup for cells outside the travel radius.
        if (_interface.IsOutOfTravelRange(hoveredVertex))
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
    /// Builds and runs the childhood reminescence narrative session immediately after
    /// protagonist creation. Reuses the same NarrativeController pipeline as exploration but
    /// with a Reminescence-phase scene.
    /// </summary>
    /// <summary>
    /// True when this phase is already running and the mode change is a <b>resume</b> from the pause
    /// menu rather than a start. Restores the display and leaves the live controller alone.
    ///
    /// <para>The phases whose <c>OnEnter</c> builds a scene are not idempotent — a second build
    /// throws the first away, and with it everything the player did in the phase. Exploration
    /// carries the same guard inline (<see cref="OnEnterLocationInteraction"/>).</para>
    /// </summary>
    private bool ResumeLiveNarrativePhase()
    {
        if (!_isInNarrativeMode || _narrativeController == null) return false;

        Console.WriteLine($"LocationTravelGameController: resuming live {_currentMode} phase");
        _core.SetNarrationMode(true);
        _core.SetWorldInteractionsEnabled(false);
        _interface.SetWorldInteractionsEnabled(false);
        if (_core.Terminal != null) _core.Terminal.Visible = true;
        return true;
    }

    private void OnEnterChildhoodReminescence()
    {
        Console.WriteLine("LocationTravelGameController: Entered ChildhoodReminescence mode");

        // Resuming from the pause menu, not starting. Building a second scene here would restart
        // childhood from its first reminescence, discarding whatever the player had already
        // recovered — which is what the whole phase is for. Exploration has always had this guard
        // (OnEnterLocationInteraction returns early on _isInNarrativeMode); childhood needed one
        // the moment Escape could reach the menu from it.
        if (ResumeLiveNarrativePhase()) return;

        // --skip-childhood: random-walk the reminescence catalog, apply outcomes
        // directly to the protagonist, then jump straight to WorldView (bypassing
        // both ChildhoodReminescence and GetUp narrative scenes).
        if (SkipChildhoodMode.IsActive && _protagonist != null)
        {
            SkipChildhoodMode.SimulateAndApply(_protagonist);
            SwapReminescenceForChildhoodMemory(_protagonist);
            if (FillMemoryMode.IsActive)
                FillMemoryMode.FillEmptySlots(_protagonist);
            // After the fill modes, so an explicitly named modus mentis wins the last free slot.
            GrantModiMentisMode.GrantIfActive(_protagonist);
            FillPartyMode.FillIfActive(_protagonist);
            SetMode(GameMode.WorldView);
            return;
        }

        // Start childhood with noise only; tracks are added progressively as REMEMBERs complete
        _ambianceEngine?.SetFilter(MusicFilter.None);
        _ambianceEngine?.SetMood(MusicMoodState.Childhood);
        _ambianceEngine?.SetActiveTrackCount(0);
        _core.SetNarrationMode(true);
        _core.SetWorldInteractionsEnabled(false);
        _interface.SetWorldInteractionsEnabled(false);
        if (_core.Terminal != null) _core.Terminal.Visible = true;

        if (_core.Terminal == null || _core.PopupTerminal == null
            || _llamaServer == null || _modusMentisSlotManager == null
            || _thinkingExecutor == null || _criticEvaluator == null
            || _protagonist == null)
        {
            Console.Error.WriteLine("ChildhoodReminescence: missing dependencies — falling back to WorldView");
            SetMode(GameMode.WorldView);
            return;
        }

        var inputHandler = GetTerminalInputHandler();
        if (inputHandler == null)
        {
            Console.Error.WriteLine("ChildhoodReminescence: no terminal input handler — falling back to WorldView");
            SetMode(GameMode.WorldView);
            return;
        }

        try
        {
            var entry = Cathedral.Game.Narrative.Reminescence.ReminescenceRegistry.GetEntry();
            var sceneFactory = new Cathedral.Game.Scene.Reminescence.ReminescenceSceneFactory(entry);
            var scene = sceneFactory.Build(0);

            var worldContext = new Cathedral.Game.Narrative.PlainBiomeContext();
            var outcomeNarrator = new OutcomeNarrator(
                _llamaServer,
                _modusMentisSlotManager);
            var actionExecutor = new ActionExecutionController(
                outcomeNarrator,
                _protagonist,
                _criticEvaluator,
                worldContext,
                locationId: 0);

            _narrativeController = new NarrativeController(
                _core.Terminal,
                _core.PopupTerminal,
                _core,
                _llamaServer,
                _modusMentisSlotManager,
                inputHandler,
                _thinkingExecutor,
                actionExecutor,
                scene,
                locationId: 0,
                worldContext,
                _protagonist,
                _ambianceEngine);

            _isInNarrativeMode = true;
            _narrativePhase    = GameMode.ChildhoodReminescence;
            _interface.SetWorldInteractionsEnabled(false);
            _core.SetWorldInteractionsEnabled(false);
            _narrativeController.StartObservationPhase();

            Console.WriteLine("LocationTravelGameController: Childhood reminescence started");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ChildhoodReminescence: failed to start — {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            _isInNarrativeMode = false;
            _narrativeController = null;
            SetMode(GameMode.WorldView);
        }
    }

    /// <summary>
    /// Builds and runs the Get-Up narrative scene immediately after childhood reminescence.
    /// The protagonist sits exhausted under a tree; the only action is GET UP.
    /// Reuses the same NarrativeController pipeline as exploration but with a GetUp-phase scene.
    /// </summary>
    private void OnEnterGetUp()
    {
        Console.WriteLine("LocationTravelGameController: Entered GetUp mode");

        // Resuming from the pause menu — see OnEnterChildhoodReminescence.
        if (ResumeLiveNarrativePhase()) return;

        _core.SetNarrationMode(true);
        _core.SetWorldInteractionsEnabled(false);
        _interface.SetWorldInteractionsEnabled(false);
        if (_core.Terminal != null) _core.Terminal.Visible = true;

        if (_core.Terminal == null || _core.PopupTerminal == null
            || _llamaServer == null || _modusMentisSlotManager == null
            || _thinkingExecutor == null || _criticEvaluator == null
            || _protagonist == null)
        {
            Console.Error.WriteLine("GetUp: missing dependencies — falling back to WorldView");
            SetMode(GameMode.WorldView);
            return;
        }

        var inputHandler = GetTerminalInputHandler();
        if (inputHandler == null)
        {
            Console.Error.WriteLine("GetUp: no terminal input handler — falling back to WorldView");
            SetMode(GameMode.WorldView);
            return;
        }

        try
        {
            var sceneFactory = new Cathedral.Game.Scene.GetUp.GetUpSceneFactory();
            var scene = sceneFactory.Build(0);

            var worldContext = new Cathedral.Game.Narrative.PlainBiomeContext();
            var outcomeNarrator = new OutcomeNarrator(
                _llamaServer,
                _modusMentisSlotManager);
            var actionExecutor = new ActionExecutionController(
                outcomeNarrator,
                _protagonist,
                _criticEvaluator,
                worldContext,
                locationId: 0);

            _narrativeController = new NarrativeController(
                _core.Terminal,
                _core.PopupTerminal,
                _core,
                _llamaServer,
                _modusMentisSlotManager,
                inputHandler,
                _thinkingExecutor,
                actionExecutor,
                scene,
                locationId: 0,
                worldContext,
                _protagonist,
                _ambianceEngine);

            _isInNarrativeMode = true;
            _narrativePhase    = GameMode.GetUp;
            _interface.SetWorldInteractionsEnabled(false);
            _core.SetWorldInteractionsEnabled(false);
            _narrativeController.StartObservationPhase();

            Console.WriteLine("LocationTravelGameController: Get-Up scene started");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"GetUp: failed to start — {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            _isInNarrativeMode = false;
            _narrativeController = null;
            SetMode(GameMode.WorldView);
        }
    }

    /// <summary>
    /// Starts Phase 6 Chain-of-Thought narrative interaction.
    /// </summary>
    private void StartNarrativeInteraction(int vertexIndex, string? startAreaKey = null, Cathedral.Game.Narrative.TimePeriod? startTime = null)
    {
        if (!EstablishNarrativeContext(vertexIndex)) return;

        // Start observation phase (async). When continuing after a routine replay, position the
        // session at the area the routine ended in, at its recorded time period.
        if (startAreaKey != null && startTime != null)
            _narrativeController!.StartAtArea(startAreaKey, startTime.Value);
        else
            _narrativeController!.StartObservationPhase();

        Console.WriteLine("LocationTravelGameController: Phase 6 narrative interaction started");
    }

    /// <summary>
    /// Builds the scene + <see cref="NarrativeController"/> for a location and switches to
    /// <see cref="GameMode.LocationInteraction"/>, WITHOUT starting the observation phase. Returns
    /// false (and resets narrative state) on failure. Shared by normal narration entry
    /// (<see cref="StartNarrativeInteraction"/>) and the routine-replay sub-phase bridge.
    /// </summary>
    private bool EstablishNarrativeContext(int vertexIndex)
    {
        if (_core.Terminal == null || _core.PopupTerminal == null || _llamaServer == null || _modusMentisSlotManager == null)
        {
            Console.Error.WriteLine("NarrativeController: Cannot start - missing dependencies");
            return false;
        }

        // Recorded here because this is the one door every way into a location comes through —
        // arriving, clicking your own vertex, and the routine-replay sub-phase all land on it.
        // Re-entering the same place is not a new step in the history, or one round trip would push
        // the vertex a test wants to go back to out of it.
        if (vertexIndex != _lastLocationVertex)
        {
            _previousLocationVertex = _lastLocationVertex;
            _lastLocationVertex     = vertexIndex;
        }
        
        try
        {
            // Get terminal input handler for coordinate conversion
            var inputHandler = GetTerminalInputHandler();
            if (inputHandler is null)
            {
                Console.WriteLine("LocationTravelGameController: Cannot enter Phase 6 mode - no terminal input handler");
                return false;
            }

            // Ensure ThinkingExecutor is initialized
            if (_thinkingExecutor is null)
            {
                Console.WriteLine("LocationTravelGameController: Cannot enter Phase 6 mode - ThinkingExecutor not initialized");
                return false;
            }

            if (_criticEvaluator == null)
            {
                Console.WriteLine("LocationTravelGameController: Cannot enter Phase 6 mode - Critic not initialized");
                return false;
            }

            // Create VerbAction Execution Controller dependencies
            var outcomeNarrator = new OutcomeNarrator(
                _llamaServer,
                _modusMentisSlotManager
            );
            
            // Use the protagonist from game state (created in ResetGameState, configured in
            // ProtagonistCreation, then enriched during the childhood reminescence phase).
            if (_protagonist == null)
            {
                _protagonist = new Protagonist();
                _protagonist.InitializeMemory();
                WeaponsMode.ApplyIfActive(_protagonist);
        GrantItemMode.ApplyIfActive(_protagonist);
            }
            var protagonist = _protagonist;
            
            // Get the appropriate narration factory for this biome/location
            var biomeInfo = _interface.GetDetailedBiomeInfoAt(vertexIndex);
            var biomeName    = biomeInfo.biome.Name.ToLowerInvariant();
            var locationName = biomeInfo.location?.Name.ToLowerInvariant();
            var worldContext = Narrative.WorldContext.From(biomeInfo.biome, biomeInfo.location);

            var actionExecutor = new ActionExecutionController(
                outcomeNarrator,
                protagonist,
                _criticEvaluator,
                worldContext,
                vertexIndex
            );

            // Lookup key: prefer location type name (e.g. "farm") over biome name (e.g. "field")
            var lookupKey = locationName ?? biomeName;

            if (!_narrationFactories.TryGetValue(lookupKey, out var graphFactory) &&
                !_narrationFactories.TryGetValue(biomeName, out graphFactory))
            {
                // Scene system path: build via the shared helper (factory selection + per-location
                // state get-or-create + item-depletion application).
                var scene = BuildSceneForLocation(vertexIndex, out var locState);
                if (scene == null)
                {
                    Console.Error.WriteLine("LocationTravelGameController: scene build failed - aborting Phase 6");
                    _isInNarrativeMode = false;
                    _narrativeController = null;
                    return false;
                }
                _currentLocationState = locState;

                _narrativeController = new NarrativeController(
                    _core.Terminal,
                    _core.PopupTerminal,
                    _core,
                    _llamaServer,
                    _modusMentisSlotManager,
                    inputHandler,
                    _thinkingExecutor,
                    actionExecutor,
                    scene,
                    vertexIndex,
                    worldContext,
                    _protagonist,
                    _ambianceEngine
                );
            }
            else
            {
                // Create Phase 6 controller with graph factory and vertex as location ID
                _narrativeController = new NarrativeController(
                    _core.Terminal,
                    _core.PopupTerminal,
                    _core,
                    _llamaServer,
                    _modusMentisSlotManager,
                    inputHandler,
                    _thinkingExecutor,
                    actionExecutor,
                    graphFactory,
                    vertexIndex,               // Use vertex index as location ID seed
                    worldContext,              // Typed world context for flavor and display
                    _protagonist,              // run-owned protagonist
                    _ambianceEngine
                );
            }
            
            // Mark as active
            _isInNarrativeMode = true;
            _narrativePhase    = GameMode.LocationInteraction;
            _currentLocationVertex = vertexIndex;
            
            // Disable world map interactions while narration UI is active
            _interface.SetWorldInteractionsEnabled(false);
            _core.SetWorldInteractionsEnabled(false);
            
            // Set mode to LocationInteraction
            SetMode(GameMode.LocationInteraction);
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LocationTravelGameController: Failed to start Phase 6: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);

            // Fallback to normal interaction
            _isInNarrativeMode = false;
            _narrativeController = null;
            return false;
        }
    }
    
    // ── Save / load ───────────────────────────────────────────────────────────

    /// <summary>True when a save exists that this build can read — what gates Continue on a cold start.</summary>
    public bool HasReadableSave => Cathedral.Game.Save.SaveFile.Exists();

    // ── --cli seams ───────────────────────────────────────────────────────────

    /// <summary>Round-trips the live run in memory. Null when it survived; otherwise the divergence.</summary>
    public string? CliSaveRoundTrip()
    {
        var save = CaptureSave();
        return save == null ? "no run in progress" : Cathedral.Game.Save.SaveRoundTrip.Check(save);
    }

    /// <summary>A fixed summary of the live run, for `expect` to assert on.</summary>
    public IReadOnlyList<string> CliSaveDump()
    {
        var save = CaptureSave();
        return save == null
            ? new List<string> { "save none" }
            : Cathedral.Game.Save.SaveRoundTrip.Describe(save);
    }

    /// <summary>Forces a write of the live run, bypassing the autosave point. False when it did not write.</summary>
    public bool CliSaveWrite()
    {
        var save = CaptureSave();
        return save != null && Cathedral.Game.Save.SaveFile.Write(save);
    }

    /// <summary>A summary of what is ON DISK, or "save none" — distinct from <see cref="CliSaveDump"/>.</summary>
    public IReadOnlyList<string> CliSaveRead()
    {
        var save = Cathedral.Game.Save.SaveFile.Read();
        return save == null
            ? new List<string> { "save none" }
            : Cathedral.Game.Save.SaveRoundTrip.Describe(save);
    }

    /// <summary>Deletes the save from disk, so a script can assert what happens without one.</summary>
    public void CliSaveErase() => Cathedral.Game.Save.SaveFile.Delete();

    /// <summary>
    /// Everything Escape does, for every mode, in one place. The launcher's key hook calls this and
    /// so does the CLI's <c>pause</c>.
    ///
    /// <para><b>Why it lives here rather than in the key hook.</b> It used to be a chain of
    /// <c>else if</c>s in <c>LocationTravelModeLauncher</c>, with the CLI keeping a second,
    /// simplified copy of the rule ("anything that is not the menu opens the menu"). Two copies of a
    /// mode list is a test that cannot fail: <c>pause</c> opened the menu during childhood
    /// reminescence, so a script saw a working pause menu in a phase where a player pressing Escape
    /// got nothing at all. One method, called by both, is what makes <c>pause</c> a test of Escape
    /// rather than a test of a paraphrase of Escape.</para>
    ///
    /// <para><b>Every mode is listed, including the ones that do nothing.</b> The gap this replaced
    /// was a mode nobody had thought about, not a mode somebody had decided against — so silence is
    /// not allowed to mean "no". A phase that should not reach the menu says so and says why.</para>
    ///
    /// <para>Returns whether Escape did anything, which is what <c>pause</c> reports.</para>
    /// </summary>
    public bool HandleEscape()
    {
        switch (_currentMode)
        {
            // ── Escape backs out of something first, and only then pauses ──────────────────

            // A fight backs out of whatever is armed — a selected skill, a move target. With
            // nothing armed it pauses like everything else; Fighting used to be the one mode with
            // no way to the menu at all.
            case GameMode.Fighting:
                if (CliTryCancelFightSelection())
                {
                    Console.WriteLine("ESC pressed - cancelled fight selection");
                    return true;
                }
                return OpenPauseMenu("fight");

            // The three narrative phases. A popup closes first; otherwise the menu opens as an
            // OVERLAY, without tearing narration down — leaving a scene is the footer LEAVE
            // button's job, and childhood and get-up have no way out at all until they end.
            case GameMode.LocationInteraction:
            case GameMode.ChildhoodReminescence:
            case GameMode.GetUp:
                if (CloseNarrativePopup())
                {
                    Console.WriteLine("ESC pressed - closed thinking modusMentis popup");
                    return true;
                }
                return OpenPauseMenu("narration");

            // ── Escape pauses, leaving the phase running underneath ───────────────────────

            // None of these are cancelled by pausing: walking out of a conversation is the footer
            // INTERRUPT button, closing a work or trade session is that menu's LEAVE button, and
            // the encounter is still waiting to be engaged when the menu is dismissed.
            case GameMode.Dialogue:        return OpenPauseMenu("dialogue");
            case GameMode.Working:         return OpenPauseMenu("work menu");
            case GameMode.Trading:         return OpenPauseMenu("trade menu");
            case GameMode.EncounterPrompt: return OpenPauseMenu("encounter");
            case GameMode.WorldView:       return OpenPauseMenu("world");

            // Creation is not a running phase, but it is a screen a player can be stuck on: the
            // menu is the only place with an Exit button, so without this there is no way to quit
            // the game from it. Nothing is lost by pausing — the allocation lives on the
            // protagonist, not in the renderer, so resuming rebuilds the same screen.
            case GameMode.ProtagonistCreation: return OpenPauseMenu("protagonist creation");

            // ── Escape goes back one screen ───────────────────────────────────────────────

            // Choosing a world is not a running phase and nothing has been spent yet, so Escape is
            // the CANCEL button — the same call, so the two cannot drift.
            case GameMode.WorldSelection:
                Console.WriteLine("ESC pressed - abandoning world selection");
                CancelWorldSelection();
                return true;

            // Both are opened FROM the main menu and both have a Back button; Escape is the same
            // press. Settings had no Escape at all, which on a screen whose only exit is one small
            // button reads as a freeze.
            case GameMode.ProtagonistManagement:
            case GameMode.Settings:
                Console.WriteLine($"ESC pressed - closing {_currentMode}");
                SetMode(GameMode.MainMenu);
                return true;

            // ── Escape closes the menu again ──────────────────────────────────────────────

            case GameMode.MainMenu:
                // Resume whatever the menu was opened over. Before a run has started there is
                // nothing to resume into, so Escape is inert rather than dropping into an empty
                // world.
                if (!_hasGameStarted) return false;
                Console.WriteLine($"ESC pressed - closing main menu, resuming {MenuReturnMode}");
                SetMode(MenuReturnMode);
                return true;

            // ── Deliberately nothing, and why ─────────────────────────────────────────────

            // Nothing to pause and nowhere to go: the menu is not built yet and the model still
            // has to load whatever the player would do next.
            case GameMode.LLMLoading:
                return false;

            // A few seconds of avatar animation that ends by itself. Pausing it would have to
            // either freeze the journey or abandon it, and neither is worth a mode.
            case GameMode.Traveling:
                return false;

            // Death funnels through END RUN on purpose: that button is what clears HasGameStarted,
            // and reaching the menu around it would leave Continue lit over a dead character. The
            // save is already erased by TriggerDeath, so nothing is at stake in the extra click.
            case GameMode.Death:
                return false;

            // The standalone --dialogue demo launcher, which has no menu behind it.
            case GameMode.DialogueDemo:
                return false;
        }

        return false;
    }

    /// <summary>Opens the menu over a still-running phase, naming what it was opened over.</summary>
    private bool OpenPauseMenu(string over)
    {
        Console.WriteLine($"ESC pressed - opening pause menu over {over}");
        SetMode(GameMode.MainMenu);
        return _currentMode == GameMode.MainMenu;
    }

    /// <summary>
    /// The CLI's <c>pause</c>. Escape is handled in the launcher's own key hook rather than in
    /// <c>OnKeyDown</c>, so <c>key escape</c> from a script never reaches it — this is the same
    /// call the hook makes, which is what lets a script assert the pause menu is reachable from a
    /// phase at all.
    /// </summary>
    public bool CliOpenPauseMenu() => _currentMode == GameMode.MainMenu || HandleEscape();

    /// <summary>
    /// Presses the death screen's END RUN button. The same two writes the click handler makes, so a
    /// script reaches the main menu exactly as a player does — which is what lets it assert that
    /// Continue is greyed out after a death.
    /// </summary>
    public bool CliDeathEndRun()
    {
        if (_currentMode != GameMode.Death) return false;
        _hasGameStarted = false;
        SetMode(GameMode.MainMenu);
        return true;
    }

    /// <summary>
    /// Snapshots the run. Called only from <see cref="EnterWorldViewInteractive"/>, which is the one
    /// moment the player is choosing a destination and, crucially, <b>no scene is live</b> — so the
    /// location states being serialised cannot be mutated underneath by a scene that still holds their
    /// collections by reference.
    /// </summary>
    /// <summary>Snapshots the run into a save record. Null when there is no run to snapshot.</summary>
    public Cathedral.Game.Save.SaveGame? CaptureSave()
    {
        if (_protagonist == null) return null;
        return new Cathedral.Game.Save.SaveGame
        {
            Seed         = GameRng.MasterSeed,
            Days         = Cathedral.Game.Narrative.GameClock.Days,
            AvatarVertex = _interface.GetAvatarVertex(),
            Party        = Cathedral.Game.Save.PartyState.Capture(_protagonist),
            Locations    = new Dictionary<int, LocationInstanceState>(_locationStates),
        };
    }

    private void Autosave()
    {
        if (!_hasGameStarted || _protagonist == null) return;
        if (Cathedral.Game.Save.SaveFile.Disabled) return;

        var save = CaptureSave();
        if (save == null) return;

        if (Cathedral.Game.Save.SaveFile.Write(save))
            Console.WriteLine($"Autosave: day {save.Days:F1}, vertex {save.AvatarVertex}, "
                            + $"{save.Locations.Count} location(s) remembered");
    }

    /// <summary>
    /// Restores the saved run and puts the player on the world map. Returns false when there is no
    /// readable save, or when the save cannot be rebuilt.
    ///
    /// <para><b>Nothing is installed until the whole rebuild has succeeded.</b> The protagonist is
    /// built to one side first, so a save that names content this build no longer has leaves the menu
    /// exactly as it was rather than dropping the player into a half-restored run.</para>
    ///
    /// <para>No world work happens here: the process booted on this save's seed (see the seed block in
    /// <c>Program.cs</c>), so the world under the avatar is already the right one.</para>
    /// </summary>
    public bool TryContinueSavedRun()
    {
        var save = Cathedral.Game.Save.SaveFile.Read();
        if (save == null) return false;

        if (save.Seed != GameRng.MasterSeed)
        {
            // Only reachable under --seed, which pins the process to one world. Refused rather than
            // loaded into the wrong world.
            Console.Error.WriteLine(
                $"Continue: the save was played on seed {save.Seed}, this process is on {GameRng.MasterSeed} — refusing.");
            return false;
        }

        var protagonist = save.Party.Rebuild();
        if (protagonist == null)
        {
            Console.Error.WriteLine("Continue: the save could not be rebuilt; leaving the run untouched.");
            return false;
        }

        // The world is built here rather than at startup. The process already booted on this save's
        // seed (see the seed block in Program.cs) and the check above proved it, so generating now
        // reproduces exactly the world this run was played in. Done AFTER the protagonist has been
        // rebuilt, so a save this build can no longer read costs nothing and changes nothing.
        if (!_interface.IsWorldGenerated)
        {
            _interface.GenerateWorld();
            _core.WorldRenderEnabled = true;
            Console.WriteLine($"Continue: world rebuilt from seed {save.Seed}");
        }

        if (_isInNarrativeMode) ExitNarrativeMode();

        _protagonist = protagonist;
        Cathedral.Game.Narrative.GameClock.Restore(save.Days);

        _locationStates.Clear();
        foreach (var (vertex, state) in save.Locations) _locationStates[vertex] = state;

        _currentLocationState  = null;
        _currentLocationVertex = -1;
        _destinationVertex     = -1;
        _lastLocationVertex     = -1;
        _previousLocationVertex = -1;
        ClearTravelPlan();

        NameFaking.Current = null;
        SceneSetting.SetPlace(null);

        if (!_interface.PlaceAvatarAt(save.AvatarVertex))
        {
            Console.Error.WriteLine("Continue: the saved vertex is not in this world; refusing.");
            return false;
        }

        // Same rule as a new run's: the world under your feet is not above your head.
        HideMoonForSeed(save.Seed);

        _hasGameStarted = true;
        Console.WriteLine($"Continue: restored day {save.Days:F1} at vertex {save.AvatarVertex}, "
                        + $"{save.Locations.Count} location(s) remembered");
        return true;
    }


    /// <summary>
    /// Begins a new run on <paramref name="worldSeed"/>: spends the save, builds that world, and
    /// clears any previous one out.
    ///
    /// <para><b>The seed is per run, not per process.</b> The world — terrain, where each location
    /// sits, and the people inside it — is a pure function of the master seed. Nothing is generated
    /// at startup any more, so every New generates: either the world of the moon just confirmed, or,
    /// under <c>--seed</c>, the world that flag names.</para>
    ///
    /// <para>This used to claim the world the process booted with for the first run of a process and
    /// regenerate only from the second onwards. That distinction is gone with the boot-time
    /// generation that created it, and with it the class of bug where the first run and every
    /// subsequent one took different paths through the same code.</para>
    /// </summary>
    public void StartNewRun(int worldSeed)
    {
        // Erased FIRST: a crash part-way through setup must not leave a save pointing at a world that
        // no longer exists.
        Cathedral.Game.Save.SaveFile.Delete();

        GameRng.Reseed(worldSeed);
        _core.RebuildForNewSeed();
        _interface.RegenerateWorld();
        _core.WorldRenderEnabled = true;

        // The world you are standing in is not one of the moons in your sky. Does nothing when the
        // seed belongs to no moon, which is every run pinned with --seed.
        HideMoonForSeed(worldSeed);

        ResetGameState();
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

        // The "where was I before" pair drives `travel back`. Left standing, a new run inherits the
        // previous run's return target — a vertex whose location state has just been cleared.
        _lastLocationVertex     = -1;
        _previousLocationVertex = -1;

        // Two statics carry the last scene's identity into whatever opens next: the real-to-false
        // name map the LLM is told to use, and the place clause in its prompts. Neither is cleared
        // when a scene closes, so without this a new run's first narration is told it is standing in
        // the last run's village.
        NameFaking.Current = null;
        SceneSetting.SetPlace(null);

        // Discard any pending travel plan and range darkening from the previous run.
        ClearTravelPlan();
        _interface.ClearTravelRange();

        // Reset travel encounter state.
        _inTravelEncounter = false;
        _pendingEncounterNpc = null;
        _pendingEncounterCreatureName = null;
        _pendingArrivalVertex = -1;

        // Discard any modal overlay left over from the previous run.
        _companionDeathBox = null;
        _companionRemovalRenderer = null;

        // Reset travel consumption state from the previous run.
        _consumptionActive  = false;
        _locationVhConsumed = 0f;
        _locationVhRequired = 1f;
        _tripTotalCells     = 0;
        _tripCellsTraveled  = 0;
        _tripVhRequired     = 0f;
        _tripVhConsumedNet  = 0f;
        _tripVhDebt         = 0f;

        // Reset protagonist to a new random starting position
        _interface.ResetProtagonistPosition();

        // Rewind the global clock before the protagonist is built, so their birth time (and hence
        // their starting age) is measured against day zero of the new run.
        Cathedral.Game.Narrative.GameClock.Reset();

        // Create a fresh protagonist for the new game.
        // No starter modus mentis, no starter items, no companions: the run starts in the
        // childhood reminescence phase, and the player acquires their first MM and items
        // via REMEMBER.
        _protagonist = new Protagonist();
        _protagonist.InitializeMemory();
        WeaponsMode.ApplyIfActive(_protagonist);
        GrantItemMode.ApplyIfActive(_protagonist);

        // (--black-bile is applied at the end of protagonist creation, not here: the humor queues are
        // rebuilt from the final organ scores at that point and would overwrite anything set now.)

        _hasGameStarted = true;
        Console.WriteLine("LocationTravelGameController: Game state reset complete");
    }
    
    // ── Fight/Dialogue mode entry methods ──────────────────────────────
    
    private void OnEnterFighting()
    {
        Console.WriteLine("LocationTravelGameController: Entered Fighting mode");
        _ambianceEngine?.TriggerGameEvent(GameEventType.NegativeOutcome);
        _ambianceEngine?.SetFilter(MusicFilter.Fighting);
        // Keep narration mode visuals (darkened sphere, terminal visible)
        _core.SetNarrationMode(true);
        _core.SetWorldInteractionsEnabled(false);
        _interface.SetWorldInteractionsEnabled(false);

        if (_core.Terminal != null)
            _core.Terminal.Visible = true;

        // Repaint at once. This is re-entered when the pause menu is dismissed as well as on first
        // entry, and without it the menu's pixels stay on screen until the next update tick.
        _fightAdapter?.Redraw();
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

    private void OnEnterTrading()
    {
        Console.WriteLine("LocationTravelGameController: Entered Trading mode");
        _core.SetNarrationMode(true);
        _core.SetWorldInteractionsEnabled(false);
        _interface.SetWorldInteractionsEnabled(false);

        if (_core.Terminal != null)
            _core.Terminal.Visible = true;
    }

    private void OnEnterWorking()
    {
        Console.WriteLine("LocationTravelGameController: Entered Working mode");
        _core.SetNarrationMode(true);
        _core.SetWorldInteractionsEnabled(false);
        _interface.SetWorldInteractionsEnabled(false);

        if (_core.Terminal != null)
            _core.Terminal.Visible = true;
    }
    
    /// <summary>
    /// Transitions from narrative mode into embedded fight mode.
    /// Computes allies (brave NPCs in the same scene section as the player),
    /// sets enemy affinity for all fighters, and passes allies to FightModeAdapter.
    /// </summary>
    /// <summary>
    /// Triggers a fight encounter mid-travel. Does not require an active narrative session.
    /// Movement stays paused; on fight end the travel resumes or death is triggered.
    /// Creature names with no fight-capable archetype are silently ignored.
    /// </summary>
    /// <returns>True when the prompt was opened; false when nothing could be staged.</returns>
    private bool StartTravelEncounter(string creatureName)
    {
        if (_core.Terminal == null || _protagonist == null) return false;
        if (!TravelEncounterArchetypes.TryGetValue(creatureName, out var archetypeFactory)) return false;

        var archetype = archetypeFactory();
        var npc = archetype.Spawn(_travelRng, _consumptionBiome);
        npc.AffinityTable.SetEnemy(_protagonist.AffinityKey);

        _interface.MovementPaused = true;
        _inTravelEncounter = true;
        _pendingEncounterNpc = npc;
        _pendingEncounterCreatureName = creatureName;

        Console.WriteLine($"LocationTravelGameController: Travel encounter — {npc.DisplayName} ({creatureName}) in {_consumptionBiome}");
        SetMode(GameMode.EncounterPrompt);
        return true;
    }

    /// <summary>
    /// The single place that switches game mode in response to a <see cref="PhaseTransition"/>.
    /// Both the narration flow and routine replay produce transitions; future phase kinds add a
    /// subclass and one arm here.
    /// </summary>
    public void ApplyPhaseTransition(PhaseTransition transition)
    {
        switch (transition)
        {
            case StartFightTransition f:
                if (_narrativeController != null)
                    StartFightMode(new FightTriggerOutcome(f.Enemy, f.Reason) { EnemyInitiative = f.EnemyInitiative });
                else
                {
                    Console.Error.WriteLine("ApplyPhaseTransition: fight requested with no narrative context — returning to travel");
                    ReturnToWorldView();
                }
                break;

            case StartDialogueTransition d:
                if (_narrativeController != null)
                    StartDialogueMode(new DialogueTriggerOutcome(d.Npc, d.TreeId, d.Tree));
                else
                {
                    Console.Error.WriteLine("ApplyPhaseTransition: dialogue requested with no narrative context — returning to travel");
                    ReturnToWorldView();
                }
                break;

            case StartRoutineDialogueTransition rd:
                StartRoutineSubPhase(rd.Vertex, rd.NpcKey, rd.Time, rd.StartArea?.DisplayName,
                    npc => StartDialogueMode(new DialogueTriggerOutcome(npc, rd.TreeId)));
                break;

            case StartRoutineTradeTransition rt:
                StartRoutineSubPhase(rt.Vertex, rt.NpcKey, rt.Time, rt.StartArea?.DisplayName,
                    npc => StartTradeMode(npc, rt.Mode));
                break;

            case StartRoutineWorkTransition rw:
                StartRoutineSubPhase(rw.Vertex, rw.NpcKey, rw.Time, rw.StartArea?.DisplayName, npc =>
                {
                    var job = Cathedral.Game.Narrative.Work.JobRegistry.Instance.GetById(rw.JobId);
                    if (job == null)
                    {
                        Console.Error.WriteLine($"ApplyPhaseTransition: recorded job '{rw.JobId}' no longer exists — returning to travel");
                        ExitNarrativeMode();
                        return;
                    }
                    StartWorkMode(npc, job);
                });
                break;

            case StartNarrationTransition n:
                StartNarrativeInteraction(n.Vertex, n.StartArea?.DisplayName, n.Time);
                break;

            case ReturnToTravelTransition:
            default:
                ReturnToWorldView();
                break;
        }
    }

    /// <summary>
    /// Bridges a headless routine replay into a location sub-phase: rebuilds narrative context at the
    /// vertex (without an observation pass), re-resolves the recorded NPC in the fresh scene, and then
    /// runs <paramref name="open"/> to enter the dialogue / trade / work phase. Because a real
    /// <see cref="NarrativeController"/> now exists, the normal completion handlers
    /// (OnDialogueCompleted / OnTradeCompleted / OnWorkCompleted) return the player to narration or
    /// the world map exactly as after a live visit.
    /// </summary>
    private void StartRoutineSubPhase(int vertex, string npcKey,
        Cathedral.Game.Narrative.TimePeriod time, string? startAreaKey,
        Action<Cathedral.Game.Npc.NpcEntity> open)
    {
        if (!EstablishNarrativeContext(vertex))
        {
            Console.Error.WriteLine("StartRoutineSubPhase: could not establish narrative context — returning to travel");
            ReturnToWorldView();
            return;
        }

        // Where AND when the replay ended. The area matters as much as the period: the sub-phase
        // rebuilds the scene at its default opening area, so without it the player walks out of the
        // trade menu into the square rather than the forge the routine had walked into.
        _narrativeController!.PrepareForRoutineSubPhase(time, startAreaKey);

        var npc = _narrativeController.Scene?.Npcs
            .FirstOrDefault(n => n.IsAlive && string.Equals(n.DisplayName, npcKey, StringComparison.OrdinalIgnoreCase))
            ?.Entity as Cathedral.Game.Npc.NpcEntity;

        if (npc == null)
        {
            Console.Error.WriteLine($"StartRoutineSubPhase: NPC '{npcKey}' not found in the rebuilt scene — exiting to travel");
            ExitNarrativeMode();
            return;
        }

        open(npc);
    }

    /// <summary>Tears down any narrative context and returns to the world-travel view.</summary>
    private void ReturnToWorldView()
    {
        _isInNarrativeMode     = false;
        _narrativeController   = null;
        _currentLocationVertex = -1;
        _interface.SetWorldInteractionsEnabled(true);
        _core.SetWorldInteractionsEnabled(true);
        SetMode(GameMode.WorldView);
    }

    /// <summary>
    /// Opens the routine list box for the current travel destination. Each routine is virtually
    /// replayed to determine whether it can still be replayed (greyed out otherwise).
    /// </summary>
    private void OpenRoutinesBox()
    {
        if (_core.Terminal == null || _protagonist == null || _travelPlanner == null) return;
        if (!_travelPlanner.HasWaypoints) return;

        int destVertex = _travelPlanner.FinalDestination;
        var routines = _protagonist.RecordedRoutines.Where(r => r.LocationId == destVertex).ToList();

        var entries = new List<TravelRoutinesBox.Entry>();
        foreach (var r in routines)
        {
            var vr = _routineReplayEngine.VirtualReplay(r, _protagonist, () => BuildSceneForVertexOrThrow(destVertex));
            entries.Add(new TravelRoutinesBox.Entry
            {
                Routine    = r,
                Replayable = vr.Replayable,
                Reason     = vr.Replayable ? null : vr.FailReason,
            });
        }

        // Keep the world non-interactive while the modal box is shown.
        _core.SetWorldInteractionsEnabled(false);
        _interface.SetWorldInteractionsEnabled(false);

        // Modal overlay: the world stays visible behind the box, but the terminal must be reset to
        // transparent first — the travel overlay leaves stale opaque cells that would otherwise show
        // as a black backdrop. Capture clicks (no passthrough) so they can't fall through the box.
        SetTransparentWorldOverlay(clickPassthrough: false);

        _travelRoutinesBox = new TravelRoutinesBox(_core.Terminal, entries);
        _travelRoutinesBox.Render();
    }

    /// <summary>
    /// Fully replays a routine on arrival and shows the outcome box. The final phase transition is
    /// applied when the player clicks CONTINUE.
    /// </summary>
    private void StartRoutineReplay(int vertexIndex, Cathedral.Game.Narrative.Routines.Routine routine)
    {
        if (_core.Terminal == null || _protagonist == null) { ReturnToWorldView(); return; }

        Console.WriteLine($"LocationTravelGameController: replaying routine '{routine.Name}' at vertex {vertexIndex}");

        var result = _routineReplayEngine.FullReplay(routine, _protagonist,
            () => BuildSceneForVertexOrThrow(vertexIndex));

        if (!result.Replayable)
        {
            Console.Error.WriteLine($"LocationTravelGameController: routine no longer replayable on arrival — {result.FailReason}");
            ReturnToWorldView();
            return;
        }

        var lines = new List<string>();
        foreach (var o in result.Outcomes) lines.Add(o.Text);
        lines.AddRange(result.ExtraLines);
        lines.Add(PhaseNote(result.FinalTransition));

        _replayFinalTransition = result.FinalTransition;

        // Enter WorldView first (OnEnterWorldView re-enables interactions), THEN show the modal box
        // and disable interactions so world clicks can't fall through underneath it.
        SetMode(GameMode.WorldView);
        _routineOutcomeBox = new RoutineOutcomeBox(_core.Terminal, routine.Name, lines);
        _core.SetWorldInteractionsEnabled(false);
        _interface.SetWorldInteractionsEnabled(false);
        // Modal overlay over the world: reset to transparent rather than a bare Clear, which paints
        // opaque black and hid the sphere behind the box. Capture clicks so none reach the world.
        SetTransparentWorldOverlay(clickPassthrough: false);
        _routineOutcomeBox.Render();
    }

    private static string PhaseNote(Cathedral.Game.Narrative.PhaseTransition t) => t switch
    {
        Cathedral.Game.Narrative.StartNarrationTransition n     => $"You explore {n.StartArea?.DisplayName ?? "the area"}.",
        Cathedral.Game.Narrative.StartFightTransition f         => $"A fight breaks out with {f.Enemy.DisplayName}!",
        Cathedral.Game.Narrative.StartDialogueTransition d      => $"You begin speaking with {d.Npc.DisplayName}.",
        Cathedral.Game.Narrative.StartRoutineDialogueTransition rd => $"You begin speaking with {rd.NpcKey}.",
        Cathedral.Game.Narrative.StartRoutineTradeTransition rt => $"You sit down to trade with {rt.NpcKey}.",
        Cathedral.Game.Narrative.StartRoutineWorkTransition rw  => $"You set to work for {rw.NpcKey}.",
        _ => "You return to your journey.",
    };

    private Cathedral.Game.Scene.Scene BuildSceneForVertexOrThrow(int vertexIndex)
    {
        var scene = BuildSceneForVertex(vertexIndex);
        if (scene == null) throw new InvalidOperationException("Scene factory is unavailable for routine replay.");
        return scene;
    }

    /// <summary>Builds a fresh scene for routine replay (same as a narration start).</summary>
    private Cathedral.Game.Scene.Scene? BuildSceneForVertex(int vertexIndex)
        => BuildSceneForLocation(vertexIndex, out _);

    /// <summary>
    /// Builds a fresh scene for a vertex exactly as a narration start would: scene-factory selection,
    /// per-location state (NPC affinity + item depletion) get-or-create, default-enemy flags, and the
    /// shared depletion store + current-depletion application. Returns null when LLM deps aren't ready.
    /// </summary>
    private Cathedral.Game.Scene.Scene? BuildSceneForLocation(int vertexIndex, out LocationInstanceState? lis)
    {
        lis = null;
        if (_llamaServer == null || _protagonist == null) return null;

        var biomeInfo    = _interface.GetDetailedBiomeInfoAt(vertexIndex);
        var biomeName    = biomeInfo.biome.Name.ToLowerInvariant();
        var locationName = biomeInfo.location?.Name.ToLowerInvariant();
        var sessionPath  = _llamaServer.SessionLogDir;

        Func<SceneFactory>? newFactory;

        // --location-type overrides the biome entirely, so a test can build a forest in a world that
        // has none. Checked first, and loud when it names something unregistered: falling back to
        // whatever is underfoot is how a forest test came to run in a plain.
        if (Config.Debug.LocationType is { } forcedType)
        {
            if (_sceneFactories.TryGetValue(forcedType.ToLowerInvariant(), out newFactory))
            {
                Console.WriteLine($"[debug] --location-type: building a '{forcedType}' here.");
            }
            else
            {
                DebugFlagAudit.Miss("--location-type", forcedType,
                    $"the biome underfoot ({locationName ?? biomeName}). Registered: {string.Join(", ", _sceneFactories.Keys)}");
                newFactory = null;
            }
        }
        else newFactory = null;

        if (newFactory == null
            && (locationName == null || !_sceneFactories.TryGetValue(locationName, out newFactory))
            && !_sceneFactories.TryGetValue(biomeName, out newFactory))
        {
            newFactory = () => new PlainSceneFactory(sessionPath);
        }

        // A factory per build, never a shared one. Every factory keeps working state while it builds
        // (the area list it wires paths through, a village's workshops and houses), and none of it is
        // meant to outlive the scene — what survives a visit lives in LocationInstanceState. Reusing
        // one instance let that working state pile up: the second build of a plain ran its path
        // wiring and its beast placement over eight areas, four of them belonging to a scene that no
        // longer existed. The audits always built one factory per location, which is why they never
        // saw it; this makes the game do what they do.
        var sceneFactory = newFactory();

        // Get-or-create the persistent per-location state, then build the scene from it.
        if (!_locationStates.TryGetValue(vertexIndex, out var state))
        {
            state = LocationInstanceState.ForScene(vertexIndex, locationName ?? biomeName);
            _locationStates[vertexIndex] = state;
        }
        lis = state;

        // --location-id pins WHICH scene gets built, independently of where the avatar is standing.
        // The build is a pure function of this number, so it is the difference between a test aimed
        // at "a village" and one aimed at the exact village --verb-probe reported.
        int buildId = Config.Debug.LocationId ?? vertexIndex;
        if (Config.Debug.LocationId is int forced)
            Console.WriteLine($"[debug] --location-id: building vertex {vertexIndex} as location {forced}.");
        var scene = sceneFactory.Build(buildId, state);

        // Debug: --spawn-beast puts one where the script opens. Before the first-contact pass below,
        // so it is flagged an enemy exactly like a beast the factory rolled.
        Cathedral.Game.Scene.DebugBeastSpawn.Apply(scene, vertexIndex);

        // Seed default-enemy archetypes (wolves, bears, boars, …) as enemies of the protagonist,
        // but only on first contact: a persistent NPC the player has already met (e.g. reconciled)
        // keeps its recorded relationship instead of being re-flagged hostile on every revisit.
        // Non-persistent beasts spawn fresh as strangers each visit, so they are always re-flagged.
        foreach (var sceneNpc in scene.Npcs)
            if (sceneNpc.Entity is Cathedral.Game.Npc.NpcEntity npcEnt && npcEnt.Archetype.DefaultEnemy
                && npcEnt.AffinityTable.IsStranger(_protagonist.AffinityKey))
                npcEnt.AffinityTable.SetEnemy(_protagonist.AffinityKey);

        // The persistent stores are already shared with the scene (LocationInstanceState.AttachTo,
        // called from SceneFactory.Build); apply the depletion that has regenerated since.
        ApplyDepletion(scene, Cathedral.Game.Narrative.GameClock.Days);

        // Scene-wide false names: map the protagonist, party companions and every named (human) NPC to
        // simple, sanitizer-safe placeholder names the LLM sees in prompts; real names are restored on
        // output (see NameFaking). Non-human NPCs (beasts) are referenced by role clause, not a proper
        // name, so they are excluded.
        var namedCharacters = new List<(string Real, bool Male)>
        {
            (_protagonist.DisplayName, Cathedral.Game.Npc.NpcLabelResolver.GenderIsMale(_protagonist)),
        };
        foreach (var companion in _protagonist.CompanionParty)
            namedCharacters.Add((companion.DisplayName, Cathedral.Game.Npc.NpcLabelResolver.GenderIsMale(companion)));
        foreach (var sceneNpc in scene.Npcs)
            if (sceneNpc.Entity is Cathedral.Game.Npc.NpcEntity human
                && human.Combatant.AnatomyType == Cathedral.Game.Narrative.AnatomyType.Human)
                namedCharacters.Add((human.DisplayName, Cathedral.Game.Npc.NpcLabelResolver.GenderIsMale(human.Combatant)));

        var nameRegistry = new Cathedral.Game.Narrative.NameFakingRegistry();
        nameRegistry.Build(namedCharacters);
        Cathedral.Game.Narrative.NameFaking.Current = nameRegistry;
        Cathedral.Game.Narrative.Sanitizer.TextSanitizationPipeline.SetAllowedNames(nameRegistry.FalseNames);

        return scene;
    }

    /// <summary>
    /// Removes still-depleted items from the freshly built scene. An item slot is depleted while
    /// <c>now − lastPicked &lt; poi.RegenDays</c>; once that elapses it is simply present again
    /// (regeneration requires no state write). Lazily prunes entries whose regen has elapsed.
    /// </summary>
    private static void ApplyDepletion(Cathedral.Game.Scene.Scene scene, double nowDays)
    {
        foreach (var area in scene.AllAreas)
            ApplyToPois(area.PointsOfInterest);

        void ApplyToPois(List<Cathedral.Game.Scene.PointOfInterest> pois)
        {
            foreach (var poi in pois)
            {
                for (int i = poi.Items.Count - 1; i >= 0; i--)
                {
                    var key = poi.Items[i].DepletionKey;
                    if (!scene.ItemDepletions.TryGetValue(key, out var pickedAt)) continue;

                    if (nowDays - pickedAt < poi.RegenDays)
                        poi.Items.RemoveAt(i);          // still depleted
                    else
                        scene.ItemDepletions.Remove(key); // regenerated — prune the stale entry
                }
            }
        }
    }

    /// <param name="soloEnemy">
    /// True for a fight the enemy was goaded into personally, which nobody else joins. Every other
    /// fight recruits the brave NPCs of the section, which is what makes picking one in a village
    /// square a bad idea.
    /// </param>
    private void StartFightMode(FightTriggerOutcome fightOutcome, bool soloEnemy = false)
    {
        if (_core.Terminal == null || _narrativeController == null)
            return;

        var mainEnemy  = fightOutcome.Target;
        var protagonist = _narrativeController.Protagonist;
        var scene      = _narrativeController.Scene;
        var pov        = _narrativeController.CurrentPoV;

        Console.WriteLine($"LocationTravelGameController: Starting fight with {mainEnemy.DisplayName}");

        // Mark main enemy and all allies as enemies of the protagonist
        mainEnemy.AffinityTable.SetEnemy(protagonist.AffinityKey);

        // Compute allies: brave NPCs in the same section as the player (excluding main enemy).
        // A provoked fight skips this entirely — being goaded into swinging at one person is not a
        // call for help, and getting somebody on their own is the whole point of provoking them.
        var allies = new List<Cathedral.Game.Npc.NpcEntity>();
        if (soloEnemy) Console.WriteLine("LocationTravelGameController: personal fight — no allies join");
        if (!soloEnemy && scene != null && pov != null)
        {
            var section = scene.Sections.FirstOrDefault(s => s.Areas.Contains(pov.Where));
            if (section != null)
            {
                // Who wades in: the people who answer for this place. Authority stands in for the
                // former per-archetype bravery flag, which was carried by the same masters and
                // owners this reads — a bystander with no stake watches.
                allies = section.Areas
                    .SelectMany(a => scene.GetNpcsAt(a, pov.When))
                    .Where(n => n.IsAlive
                        && n.Entity is Cathedral.Game.Npc.NpcEntity ne
                        && ne.AuthorityLevel > 0
                        && ne != mainEnemy)
                    .Select(n => (Cathedral.Game.Npc.NpcEntity)n.Entity)
                    .Distinct()
                    .ToList();

                foreach (var ally in allies)
                    ally.AffinityTable.SetEnemy(protagonist.AffinityKey);

                Console.WriteLine($"LocationTravelGameController: {allies.Count} ally(ies) joining the fight");
            }
        }

        // Narration fights: the section the PoV stands in supplies the generator,
        // and the current area's Id is the seed so the same area always rolls the same arena.
        var fightSection = scene?.Sections.FirstOrDefault(s => s.Areas.Contains(pov!.Where));
        if (fightSection == null)
        {
            Console.Error.WriteLine("LocationTravelGameController: cannot start fight — no section found for current area");
            return;
        }
        int areaSeed = pov!.Where.Id.GetHashCode();
        var arena2 = fightSection.ArenaGeneratorFactory(areaSeed);

        _fightAdapter = new FightModeAdapter(
            _core.Terminal,
            _core.PopupTerminal,
            mainEnemy,
            protagonist,
            arena2,
            allies,
            sfxTrigger: e => _ambianceEngine?.TriggerGameEvent(e),
            setMusicFilter: f => _ambianceEngine?.SetFilter(f),
            enemyInitiative: fightOutcome.EnemyInitiative);

        // Grey the narration so far into history under a labelled rule. Deliberately after every
        // early-return guard above — bailing out with the panel already greyed would strand the
        // player on dead text.
        _narrativeController.CloseNarrationSegment($"fight with {mainEnemy.DisplayName}");

        SetMode(GameMode.Fighting);
    }
    
    /// <summary>
    /// Transitions from narrative mode into embedded dialogue mode.
    /// </summary>
    private void StartDialogueMode(DialogueTriggerOutcome dialogueOutcome)
    {
        if (_core.Terminal == null || _narrativeController == null ||
            _llamaServer == null || _modusMentisSlotManager == null)
            return;
        
        // --auto-dialogue settles the conversation where it stands and never enters Dialogue mode, so
        // a verb test asserts about its verb rather than about somebody else's dialogue tree. The
        // trees themselves are covered by cli/_systems/dialogue_*.cli.
        if (Config.Debug.AutoDialogue
            && Cathedral.Game.Dialogue.Runtime.DialogueAutoResolve.TryResolve(
                   dialogueOutcome.Target, _narrativeController.Protagonist,
                   dialogueOutcome.TreeId, dialogueOutcome.Tree))
        {
            // The flags a won conversation leaves on the NPC are consumed by OnDialogueCompleted,
            // which this path never reaches — so the one that repositions the player is consumed
            // here. Without it --auto-dialogue applied the introduction's standing and skipped the
            // walk, which is precisely the half that was broken in the real game and could not be
            // caught by a test.
            if (dialogueOutcome.Target.IntroductionGranted is { } presented)
            {
                dialogueOutcome.Target.IntroductionGranted = null;
                WalkToIntroduction(presented);
            }
            _narrativeController.OnDialogueCompleted(dialogueOutcome.Target);
            return;
        }

        Console.WriteLine($"LocationTravelGameController: Starting dialogue with {dialogueOutcome.Target.DisplayName}");
        
        _dialogueAdapter = new DialogueTreeAdapter(
            npc:          dialogueOutcome.Target,
            protagonist:  _narrativeController.Protagonist,
            treeId:       dialogueOutcome.TreeId,
            llmManager:   _llamaServer,
            slotManager:  _modusMentisSlotManager,
            terminal:     _core.Terminal,
            scrollBuffer: _narrativeController.ScrollBuffer,
            world:        _narrativeController.WorldContext,
            locationId:   _narrativeController.LocationId,
            prebuiltTree: dialogueOutcome.Tree,
            ambianceEngine: _ambianceEngine);

        // Grey the narration into history BEFORE the adapter starts: setup is async and appends the
        // NPC's first line when it completes, which must land live rather than be swept into history.
        _narrativeController.CloseNarrationSegment(
            $"conversation with {dialogueOutcome.Target.DisplayName}");

        _dialogueAdapter.Start();
        SetMode(GameMode.Dialogue);
    }
    
    /// <summary>
    /// Called when fight adapter reports completion. Returns to narrative mode.
    /// </summary>
    private void OnFightCompleted()
    {
        if (_fightAdapter == null) return;

        _ambianceEngine?.SetFilter(MusicFilter.None);

        var result       = _fightAdapter.Result;
        var npc          = _fightAdapter.TargetNpc;
        var allEnemyNpcs = _fightAdapter.AllEnemyNpcs;

        Console.WriteLine($"LocationTravelGameController: Fight completed - {result}");

        // ── Travel encounter path (no narrative session) ──────────────────────
        if (_inTravelEncounter)
        {
            _inTravelEncounter = false;
            _fightAdapter = null;
            _core.Terminal?.Clear();

            if (result == FightAdapterResult.Death)
            {
                _pendingArrivalVertex = -1;
                TriggerDeath(DeathCause.Wounds);
                return;
            }

            if (result == FightAdapterResult.Runaway)
            {
                // Abandon the in-flight travel and let the player pick a new destination. Any
                // arrival held over the fight goes with it — fleeing ends the journey, and the
                // avatar is standing on the vertex either way.
                _pendingArrivalVertex = -1;
                _interface.MovementPaused = true;
                _interface.CancelTravel();
                ClearTravelPlan();
                SetMode(GameMode.WorldView);
                return;
            }

            // Victory, and the encounter interrupted the journey's final step: the arrival was
            // announced while the mode had already left Traveling and was held rather than lost.
            // Deliver it now — this is the branch that enters the location that was travelled to.
            if (_pendingArrivalVertex >= 0)
            {
                int arrival = _pendingArrivalVertex;
                _pendingArrivalVertex = -1;
                // OnProtagonistArrived only answers while the mode is Traveling, which is also what
                // the ordinary path arrives from, so the journey ends exactly as it would have.
                SetMode(GameMode.Traveling);
                _interface.MovementPaused = false;
                OnProtagonistArrived(arrival);
                return;
            }

            // Victory with no path left to walk — a --start-fight drop-in, or travel cancelled
            // under the fight. Traveling renders nothing and would never end, so go back to the
            // world view rather than resuming a journey that is not happening.
            if (!_interface.IsAvatarMoving())
            {
                _interface.MovementPaused = false;
                ClearTravelPlan();
                SetMode(GameMode.WorldView);
                return;
            }

            // Victory mid-journey: resume travel. If VH consumption is still pending,
            // Update() will keep movement paused until the debt is cleared.
            SetMode(GameMode.Traveling);
            if (!_consumptionActive)
                _interface.MovementPaused = false;
            return;
        }

        // ── Narrative-session fight path ──────────────────────────────────────
        if (_narrativeController == null) return;

        // Snapshot the combat log before the adapter is dropped — it is the fight's only text trace.
        var combatLog = _fightAdapter.ActionLog.Select(e => e.Text).ToList();

        _narrativeController.OnFightCompleted(result, npc, allEnemyNpcs, combatLog);
        _fightAdapter = null;
        _core.Terminal?.Clear();

        if (result == FightAdapterResult.Death)
        {
            // A fight lost is a run lost, wherever it was fought. This used to exit to the world view
            // instead — so the one lethal fight in the game was the travel encounter, and a fight
            // picked inside a location could be lost at no cost at all, the protagonist walking out
            // of it at zero hit points and playing on. TriggerDeath tears the session down itself.
            Console.WriteLine("LocationTravelGameController: Player was killed in a narration fight");
            TriggerDeath(DeathCause.Wounds);
            return;
        }

        if (result == FightAdapterResult.Runaway)
        {
            Console.WriteLine("LocationTravelGameController: Player ran away, exiting to world view");
            ExitNarrativeMode();
            return;
        }

        // Victory: re-enter narration first so the fresh observation task isn't racing
        // OnEnterLocationInteraction's redraw, then open a new segment. The scene is untouched, so
        // the corpses spawned by OnFightCompleted are described by the new observation pass.
        SetMode(GameMode.LocationInteraction);
        _narrativeController.BeginNarrationSegment($"after the fight with {npc.DisplayName}");
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

        // If the NPC demanded a fight during the dialogue (caught-red-handed provoke/rejection),
        // transition directly into fight mode instead of returning to narrative.
        if (npc.FightRequestedByDialogue)
        {
            bool provoked = npc.FightIsPersonal;
            npc.FightRequestedByDialogue = false;   // consume the flag
            npc.FightIsPersonal          = false;
            _dialogueAdapter = null;
            Console.WriteLine($"LocationTravelGameController: NPC {npc.DisplayName} demanded fight — entering fight mode"
                            + (provoked ? " (personal — no allies)" : ""));
            var fightOutcome = new FightTriggerOutcome(npc, $"confrontation with {npc.DisplayName}");
            StartFightMode(fightOutcome, soloEnemy: provoked);
            return;
        }

        // A successful beg pays out here: an Outcome can reach the NPC and nothing else, so
        // the wallet is out of its reach the same way the trade menu is.
        if (npc.AlmsGiven > 0 && _protagonist != null)
        {
            _protagonist.Party.Add(Cathedral.Game.Narrative.CoinType.Copper, npc.AlmsGiven);
            Console.WriteLine($"LocationTravelGameController: {npc.DisplayName} gave {npc.AlmsGiven} copper");
            npc.AlmsGiven = 0;   // consume the flag
        }

        // If an introduction succeeded, walk the player over to whoever was named and put them in
        // focus, so the next observation is of that person. Standing with them is already set by the
        // dialogue outcome; this is the other half — being taken there.
        if (npc.IntroductionGranted is { } presented)
        {
            npc.IntroductionGranted = null;   // consume the flag
            WalkToIntroduction(presented);
        }

        // If a propose-to-join dialogue succeeded, take them into the party and out of the scene.
        // Deferred to here for the same reason trade and work are: a dialogue outcome can reach the
        // NPC and nothing else, so the flag is the only thing it can set.
        if (npc.JoinRequested)
        {
            npc.JoinRequested = false;   // consume the flag
            RecruitFromDialogue(npc);
        }

        // If a propose-to-buy/sell dialogue succeeded, open the trade menu instead of returning.
        if (npc.TradeRequest != Cathedral.Game.Npc.Trade.TradeMode.None)
        {
            var tradeMode = npc.TradeRequest;
            npc.TradeRequest = Cathedral.Game.Npc.Trade.TradeMode.None;   // consume the flag
            _dialogueAdapter = null;
            Console.WriteLine($"LocationTravelGameController: {npc.DisplayName} agreed to trade ({tradeMode}) — opening trade menu");
            StartTradeMode(npc, tradeMode);
            return;
        }

        // If a request-job dialogue succeeded, open the work menu instead of returning.
        if (npc.JobRequest is { } job)
        {
            npc.JobRequest       = null;   // consume the flag
            npc.PendingJobOffer  = null;
            _dialogueAdapter     = null;
            Console.WriteLine($"LocationTravelGameController: {npc.DisplayName} agreed to hire ({job.Id}) — opening work menu");
            StartWorkMode(npc, job);
            return;
        }

        _dialogueAdapter = null;

        // Return to narrative mode. The Clear is required: the dialogue panel's option rows and
        // footer would otherwise bleed through into the narration frame.
        _core.Terminal?.Clear();
        SetMode(GameMode.LocationInteraction);
        _narrativeController.BeginNarrationSegment($"after talking with {npc.DisplayName}");
    }

    /// <summary>
    /// Transitions from narrative mode into the embedded buy/sell trade menu.
    /// </summary>
    /// <summary>
    /// Moves the player to wherever the newly-introduced person is standing, and focuses them.
    ///
    /// <para>Both halves matter. Moving is what saves the player hunting a village for somebody they
    /// have never seen; focusing is what makes the next thing that happens an observation <i>of that
    /// person</i>, which is how the introduction reads as an arrival rather than a teleport.</para>
    ///
    /// <para>If they are nowhere at this hour — the reeve keeps his own times — the standing still
    /// stands and the walk simply does not happen. Being vouched for does not conjure somebody into
    /// a room.</para>
    /// </summary>
    private void WalkToIntroduction(Cathedral.Game.Npc.NpcEntity presented)
        => _narrativeController?.WalkToIntroducedNpc(presented);

    /// <summary>
    /// Moves an NPC who agreed to travel with the player out of the scene and into the party.
    ///
    /// <para>Their existing body joins unchanged — an <c>NpcEntity</c> wraps an
    /// <c>EnemyCombatant</c>, which is a <c>PartyMember</c> — so nothing is copied and nothing can
    /// drift. The cap is re-checked here because it may have been reached between the ask and the
    /// answer, and quietly exceeding it would be worse than a refusal the player can see.</para>
    /// </summary>
    private void RecruitFromDialogue(Cathedral.Game.Npc.NpcEntity npc)
    {
        var scene = _narrativeController?.Scene;
        if (_protagonist == null || scene == null) return;

        int max = Cathedral.Game.Scene.Verbs.TameVerb.MaxCompanions(_protagonist);
        if (_protagonist.CompanionParty.Count >= max)
        {
            Console.WriteLine($"LocationTravelGameController: {npc.DisplayName} agreed to join, but the party is full ({max})");
            return;
        }

        _protagonist.CompanionParty.Add(npc.Combatant);

        // Same door as taming and killing: not alive, out of the scene, and recorded as departed so
        // the next build of this location does not leave them standing where they were.
        var sceneNpc = scene.Npcs.FirstOrDefault(n => ReferenceEquals(n.Entity, npc));
        if (sceneNpc != null) scene.RemoveNpcFromPlay(sceneNpc);
        else                  npc.IsAlive = false;

        Console.WriteLine($"LocationTravelGameController: {npc.DisplayName} joined the party ({_protagonist.CompanionParty.Count}/{max})");
    }

    private void StartTradeMode(Cathedral.Game.Npc.NpcEntity npc, Cathedral.Game.Npc.Trade.TradeMode mode)
    {
        if (_core.Terminal == null || _narrativeController == null)
            return;

        _tradeAdapter = new TradeMenuAdapter(
            protagonist: _narrativeController.Protagonist,
            npc:         npc,
            mode:        mode,
            terminal:    _core.Terminal);

        _narrativeController.CloseNarrationSegment($"trading with {npc.DisplayName}");
        _tradeAdapter.Start();
        SetMode(GameMode.Trading);
    }

    /// <summary>
    /// Called when the trade menu reports completion. Begins a fresh narration segment.
    /// </summary>
    private void OnTradeCompleted()
    {
        if (_tradeAdapter == null) return;

        var npc = _tradeAdapter.TargetNpc;
        Console.WriteLine($"LocationTravelGameController: Trade completed with {npc.DisplayName}");
        _tradeAdapter = null;
        _core.Terminal?.Clear();
        SetMode(GameMode.LocationInteraction);

        // The trade menu draws no narration text of its own, so leave a one-line trace before the
        // segment closes, then re-observe the scene.
        _narrativeController?.AppendPhaseNote($"You finished trading with {npc.DisplayName}.");
        _narrativeController?.BeginNarrationSegment($"after trading with {npc.DisplayName}");
    }

    /// <summary>
    /// Transitions from narrative mode into the embedded work menu.
    /// </summary>
    private void StartWorkMode(Cathedral.Game.Npc.NpcEntity npc, Cathedral.Game.Narrative.Work.Job job)
    {
        if (_core.Terminal == null || _narrativeController == null)
            return;

        _workAdapter = new WorkMenuAdapter(
            protagonist: _narrativeController.Protagonist,
            npc:         npc,
            job:         job,
            terminal:    _core.Terminal);

        _narrativeController.CloseNarrationSegment($"working for {npc.DisplayName}");
        _workAdapter.Start();
        SetMode(GameMode.Working);
    }

    /// <summary>
    /// Called when the work menu reports completion. Unlike the other phases this does NOT return to
    /// narration: a work stint advances <see cref="Cathedral.Game.Narrative.GameClock"/> by up to
    /// several years, so the scene the player left is long stale. Exit to the world map instead and
    /// let them travel somewhere with a freshly built scene.
    /// </summary>
    private void OnWorkCompleted()
    {
        if (_workAdapter == null) return;

        Console.WriteLine($"LocationTravelGameController: Work completed with {_workAdapter.TargetNpc.DisplayName} — months have passed, returning to world view");
        _workAdapter = null;
        _core.Terminal?.Clear();
        ExitNarrativeMode();
    }
    
    /// <summary>
    /// Exits Phase 6 mode and returns to world view.
    /// </summary>
    public void ExitNarrativeMode()
    {
        if (!_isInNarrativeMode)
            return;

        TearDownNarrativeSession();
        SetMode(GameMode.WorldView);
    }

    /// <summary>
    /// Ends the narration session and hands the world back, without deciding where the player goes
    /// next. <see cref="ExitNarrativeMode"/> follows it with the world view; <see cref="TriggerDeath"/>
    /// follows it with the death screen.
    ///
    /// <para>Split out because a death inside a visit needs every one of these writes and none of the
    /// mode change: <c>Update</c> enters its narration branch on <see cref="_isInNarrativeMode"/>
    /// rather than on the mode, so a session left standing repaints itself over whatever replaced
    /// it.</para>
    /// </summary>
    private void TearDownNarrativeSession()
    {
        Console.WriteLine("LocationTravelGameController: Exiting Phase 6 mode");

        // Save any routine recorded during this narration session before tearing it down.
        _narrativeController?.FinalizeRoutineRecording();

        // Re-enable world map and 3D interactions
        _interface.SetWorldInteractionsEnabled(true);
        _core.SetWorldInteractionsEnabled(true);

        _isInNarrativeMode = false;
        _narrativeController = null;
        _currentLocationVertex = -1;
    }

    /// <summary>
    /// Once the childhood reminescence phase ends, replaces the protagonist's
    /// <c>childhood_reminescence</c> MM (a "recollect a fuzzy memory" persona that no longer fits
    /// ordinary exploration) with a <see cref="ChildhoodMemoryModusMentis"/> whose prompt is built
    /// from the childhood life-experiences just recorded — a "reuse your childhood experience"
    /// persona. No-op if the reminescence MM is absent. Called on both the normal reminescence
    /// finish and the --skip-childhood path (which simulates the history first).
    /// </summary>
    private static void SwapReminescenceForChildhoodMemory(Protagonist protagonist)
    {
        var oldMm = protagonist.GetModusMentisById("childhood_reminescence");
        if (oldMm == null)
            return;

        var experiences = protagonist.ChildhoodHistory.ToExperienceSummary();
        var newMm = new ChildhoodMemoryModusMentis(experiences) { Level = oldMm.Level };

        protagonist.RemoveModusMentis(oldMm);
        protagonist.AcquireModusMentis(newMm);
        Console.WriteLine("LocationTravelGameController: swapped childhood_reminescence MM → childhood_memory (reuse-experience persona)");
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
    /// <summary>
    /// Flips the window between windowed and borderless fullscreen and persists the choice. The one
    /// implementation behind all three ways in: the F11 key, the Settings row, and the CLI's
    /// <c>key F11</c>. Returns the new state.
    /// </summary>
    public bool ToggleFullscreen()
    {
        bool now = Cathedral.Glyph.WindowMode.Toggle(_core);
        UserSettings.Fullscreen = now;
        UserSettings.Save();

        // Only when that screen is the one on display, and only through the sync. Both halves of
        // this were wrong before and each was its own bug:
        //
        //   - a bare Render() redrew the renderer's own stale Fullscreen field, so the row went on
        //     reading WINDOW over a fullscreen window until the player left the screen and came
        //     back. A faithful repaint of the wrong value is the hardest kind of no-op to see;
        //   - it was unguarded by mode, and _settingsMenuRenderer is built once and kept. So F11
        //     pressed anywhere after that screen had been opened even once repainted the whole
        //     settings screen — Fill() and all — over the live mode, and it stayed there until
        //     something else happened to redraw.
        if (_currentMode == GameMode.Settings)
            SyncAndRenderSettingsScreen();

        return now;
    }

    public void OnKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys key)
    {
        // Mode-independent, and handled before the per-mode dispatch: fullscreen belongs to the
        // window, not to whatever the game is showing inside it. This is also the CLI's route in
        // (`key F11`) — the real keyboard reaches it through the launcher's KeyDown handler, which
        // only forwards to this method in fight and dialogue modes.
        if (key == OpenTK.Windowing.GraphicsLibraryFramework.Keys.F11)
        {
            ToggleFullscreen();
            return;
        }

        if (_currentMode == GameMode.Fighting && _fightAdapter != null)
        {
            _fightAdapter.OnKeyPress(key);
        }
        else if (_currentMode == GameMode.Dialogue && _dialogueAdapter != null)
        {
            _dialogueAdapter.OnKeyPress(key);
        }
        else if (_currentMode == GameMode.Trading && _tradeAdapter != null)
        {
            _tradeAdapter.OnKeyPress(key);
        }
        else if (_currentMode == GameMode.Working && _workAdapter != null)
        {
            _workAdapter.OnKeyPress(key);
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
