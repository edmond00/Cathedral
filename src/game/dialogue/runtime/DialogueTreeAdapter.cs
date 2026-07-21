using System;
using System.Linq;
using System.Threading.Tasks;
using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;
using Cathedral.LLM;
using Cathedral.Terminal;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Cathedral.Game.Dialogue.Runtime;

/// <summary>
/// Bridges <see cref="LocationTravelGameController"/> to the new dialogue-tree system.
/// Resolves the dialogue tree, acquires an LLM slot for the NPC, and owns the
/// <see cref="DialogueTreeController"/> for the duration of a single conversation.
///
/// Mirrors the public API of the old <c>DialogueModeAdapter</c> so the game controller
/// needs only a one-line change.
/// </summary>
public class DialogueTreeAdapter
{
    private readonly NpcEntity              _npc;
    private readonly Protagonist            _protagonist;
    private readonly string?                _treeId;
    private readonly DialogueTree?          _prebuiltTree;
    private readonly LlamaServerManager     _llmManager;
    private readonly ModusMentisSlotManager _slotManager;
    private readonly WorldContext?          _world;
    private readonly int                    _locationId;
    private readonly NarrationScrollBuffer  _buffer;
    private readonly Cathedral.Audio.AmbianceEngine? _ambianceEngine;

    private readonly DialogueTreeUI  _ui;
    private DialogueTreeController? _controller;
    private int                     _npcSlotId  = -1;
    private bool                    _ready;
    private bool                    _failed;
    private string?                 _errorMessage;

    /// <summary>The NPC being talked to (used by the game controller after dialogue ends).</summary>
    public NpcEntity TargetNpc => _npc;

    /// <summary>True once the LLM slot is acquired and the controller has been started.</summary>
    public bool IsReady => _ready;

    /// <summary>True when dialogue has ended and the session should be torn down.</summary>
    public bool HasRequestedExit => _controller?.HasRequestedExit ?? _failed;

    /// <summary>
    /// The live conversation controller, or null while the LLM slot is still being acquired.
    /// Exposed for --cli driving; the driver reports "still starting" when this is null.
    /// </summary>
    public DialogueTreeController? Controller => _controller;

    /// <param name="scrollBuffer">
    /// The narration session's shared text history. The conversation appends its lines to it live and
    /// the dialogue panel renders from it, so the player can scroll back into the greyed narration
    /// that preceded the conversation.
    /// </param>
    public DialogueTreeAdapter(
        NpcEntity              npc,
        Protagonist            protagonist,
        string?                treeId,
        LlamaServerManager     llmManager,
        ModusMentisSlotManager slotManager,
        TerminalHUD            terminal,
        NarrationScrollBuffer  scrollBuffer,
        WorldContext?          world          = null,
        int                    locationId     = 0,
        DialogueTree?          prebuiltTree   = null,
        Cathedral.Audio.AmbianceEngine? ambianceEngine = null)
    {
        _npc          = npc;
        _protagonist  = protagonist;
        _treeId       = treeId;
        _prebuiltTree = prebuiltTree;
        _llmManager   = llmManager;
        _slotManager  = slotManager;
        _world        = world;
        _locationId   = locationId;
        _buffer       = scrollBuffer;
        _ambianceEngine = ambianceEngine;

        // The UI is created up front so the setup phase can already render the standard
        // bordered panel — avoids a black flash when transitioning from narration.
        _ui = new DialogueTreeUI(terminal, npc, protagonist.AffinityKey, scrollBuffer);
    }

    // ── Public API ──────────────────────────────────────────────────────────────

    /// <summary>Kicks off async setup (slot acquisition + controller start).</summary>
    public void Start() => _ = Task.Run(SetupAndStartAsync);

    /// <summary>Called every render frame.</summary>
    public void Update()
    {
        if (_controller != null)
        {
            _controller.Update();
            return;
        }

        // While setting up, keep the standard panel frame with a generating message
        // at the bottom — a smooth transition from the narration panel.
        _ui.RenderSetupFrame(
            $"Starting dialogue with {_npc.DisplayName}…",
            _failed ? $"Dialogue failed: {_errorMessage}" : null);
    }

    public void OnMouseMove(int mx, int my)  => _controller?.OnMouseMove(mx, my);
    public void OnMouseClick(int mx, int my) => _controller?.OnMouseClick(mx, my);
    public void OnMouseWheel(float delta)    => _controller?.OnMouseWheel(delta);
    public void OnKeyPress(Keys key)         => _controller?.OnKeyPress(key);

    // ── Setup ────────────────────────────────────────────────────────────────────

    private async Task SetupAndStartAsync()
    {
        try
        {
            // Resolve tree
            DialogueTree tree = ResolveTree();

            // Placeholder names: the LLM never sees real names. Build the mapping once, then use it
            // both to rewrite the NPC persona prompt (real → placeholder) and, later, to restore real
            // names into every generated replica.
            var names   = DialogueNameTable.Build(_protagonist, _npc);
            var context = new DialogueContext(_protagonist, _npc, _world, _locationId, names);

            // Acquire NPC slot (system prompt = way-to-speak description, with the NPC's real name
            // swapped for its placeholder).
            string rawPrompt    = _npc.WayToSpeakDescription ?? $"You are {names.Placeholder("npc")}.";
            string systemPrompt = names.ToPlaceholder(rawPrompt);
            _npcSlotId = await _llmManager.CreateInstanceAsync(systemPrompt);

            _controller = new DialogueTreeController(
                tree:        tree,
                npc:         _npc,
                protagonist: _protagonist,
                npcSlotId:   _npcSlotId,
                llmManager:  _llmManager,
                slotManager: _slotManager,
                ui:          _ui,
                context:     context,
                buffer:      _buffer,
                ambianceEngine: _ambianceEngine);

            _ready = true;
            _controller.Start();

            Console.WriteLine(
                $"DialogueTreeAdapter: Started '{tree.TreeId}' with {_npc.DisplayName}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"DialogueTreeAdapter: Setup failed: {ex.Message}");
            _errorMessage = ex.Message;
            _failed = true;
        }
    }

    private DialogueTree ResolveTree()
    {
        // Pre-built tree (e.g. caught-red-handed) takes precedence over registry lookup.
        if (_prebuiltTree != null) return _prebuiltTree;

        string partyMemberId = _protagonist.AffinityKey;

        if (_treeId != null)
        {
            var tree = DialogueTreeRegistry.Instance.TryGet(_treeId);
            if (tree != null) return tree;
            throw new InvalidOperationException($"Dialogue tree '{_treeId}' not found in registry.");
        }

        // Auto-resolve: pick the first available tree for this NPC/protagonist pair
        var available = DialogueTreeRegistry.Instance.All
            .Where(t => t.IsAvailable(_npc, partyMemberId))
            .ToList();

        if (available.Count == 0)
            throw new InvalidOperationException(
                $"No dialogue tree is available for {_npc.DisplayName} and {partyMemberId}.");

        return available[0];
    }
}
