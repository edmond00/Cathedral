using System;
using System.Linq;
using System.Threading.Tasks;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;
using Cathedral.LLM;
using Cathedral.Terminal;

namespace Cathedral.Game.Dialogue;

/// <summary>
/// Bridges the narrative system to the dialogue system.
/// Creates and manages a <see cref="DialogueModeController"/> using the main window's terminal,
/// handling NPC slot acquisition and cleanup.
/// </summary>
public class DialogueModeAdapter
{
    private readonly NpcEntity _targetNpc;
    private readonly Protagonist _protagonist;
    private readonly LlamaServerManager _llmManager;
    private readonly ModusMentisSlotManager _slotManager;
    private readonly TerminalHUD _terminal;

    private DialogueModeController? _controller;
    private NpcInstance? _npcInstance;
    private bool _setupComplete;
    private bool _setupFailed;
    private string? _errorMessage;

    /// <summary>Whether dialogue setup is complete and the controller is running.</summary>
    public bool IsReady => _setupComplete && _controller != null;

    /// <summary>Whether dialogue has ended and the player wants to return to narration.</summary>
    public bool HasRequestedExit => _controller?.HasRequestedExit ?? _setupFailed;

    /// <summary>The NPC being talked to.</summary>
    public NpcEntity TargetNpc => _targetNpc;

    public DialogueModeAdapter(
        NpcEntity targetNpc,
        Protagonist protagonist,
        LlamaServerManager llmManager,
        ModusMentisSlotManager slotManager,
        TerminalHUD terminal)
    {
        _targetNpc = targetNpc;
        _protagonist = protagonist;
        _llmManager = llmManager;
        _slotManager = slotManager;
        _terminal = terminal;
    }

    /// <summary>
    /// Start the dialogue asynchronously. Acquires an LLM slot for the NPC and begins the greeting phase.
    /// </summary>
    public void Start()
    {
        _ = Task.Run(SetupAndStartAsync);
    }

    private async Task SetupAndStartAsync()
    {
        try
        {
            _npcInstance = _targetNpc.ToDialogueNpc();

            // Acquire LLM slot for NPC persona
            _npcInstance.LlmSlotId = await _llmManager.CreateInstanceAsync(
                _npcInstance.Persona.PersonaPrompt);

            int maxReplicas = GetTongueStat(_protagonist);

            _controller = new DialogueModeController(
                npc: _npcInstance,
                protagonist: _protagonist,
                maxReplicas: maxReplicas,
                llmManager: _llmManager,
                slotManager: _slotManager,
                terminal: _terminal);

            _setupComplete = true;
            _controller.Start();

            Console.WriteLine($"DialogueModeAdapter: Dialogue started with {_targetNpc.DisplayName}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"DialogueModeAdapter: Setup failed: {ex.Message}");
            _errorMessage = ex.Message;
            _setupFailed = true;
        }
    }

    /// <summary>Called every render frame.</summary>
    public void Update()
    {
        if (_controller != null)
        {
            _controller.Update();
        }
        else if (!_setupComplete && !_setupFailed)
        {
            // Show loading state
            _terminal.Clear();
            _terminal.CenteredText(
                Config.Terminal.MainHeight / 2,
                "Starting dialogue...",
                Config.Colors.LightGray,
                Config.Colors.Black);
        }
        else if (_setupFailed)
        {
            _terminal.Clear();
            _terminal.CenteredText(
                Config.Terminal.MainHeight / 2,
                $"Dialogue failed: {_errorMessage}",
                Config.Colors.Red,
                Config.Colors.Black);
        }
    }

    /// <summary>Route mouse move events to the dialogue controller.</summary>
    public void OnMouseMove(int mouseX, int mouseY)
    {
        _controller?.OnMouseMove(mouseX, mouseY);
    }

    /// <summary>Route click events to the dialogue controller.</summary>
    public void OnMouseClick(int mouseX, int mouseY)
    {
        if (_setupFailed)
        {
            // Click anywhere to dismiss the error and return
            _setupFailed = true; // HasRequestedExit will return true
            return;
        }
        _controller?.OnMouseClick(mouseX, mouseY);
    }

    /// <summary>Route mouse wheel events to the dialogue controller.</summary>
    public void OnMouseWheel(float delta)
    {
        _controller?.OnMouseWheel(delta);
    }

    /// <summary>Route key press events to the dialogue controller.</summary>
    public void OnKeyPress(OpenTK.Windowing.GraphicsLibraryFramework.Keys key)
    {
        _controller?.OnKeyPress(key);
    }

    private static int GetTongueStat(Protagonist p)
    {
        var stat = p.DerivedStats.FirstOrDefault(s => s.Name == "tongue");
        return stat?.GetValue(p) ?? 3;
    }
}
