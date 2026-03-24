using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cathedral.Game.Narrative;
using Cathedral.Game.Dialogue.Executors;
using Cathedral.Game.Dialogue.Phases;
using Cathedral.LLM;
using Cathedral.Terminal;

namespace Cathedral.Game.Dialogue;

/// <summary>
/// Orchestrates the full dialogue loop: Greeting → Replicas → Selection → Dice Roll → NPC Response → loop.
/// Follows the same fire-and-forget async + Update() render-frame pattern as NarrativeController.
/// </summary>
public class DialogueModeController
{
    private readonly NpcInstance _npc;
    private readonly Protagonist _protagonist;
    private readonly int _maxReplicas;

    private readonly NpcGreetingPhaseController   _greetingPhase;
    private readonly PlayerReplicaPhaseController _replicaPhase;
    private readonly NpcResponsePhaseController   _responsePhase;
    private readonly DialogueUI                   _ui;
    private readonly DialogueScrollBuffer         _scrollBuffer;

    private readonly DialogueState _state = new();

    // Pending result between dice roll display and response generation
    private ExchangeResult? _pendingResult;

    public DialogueModeController(
        NpcInstance npc,
        Protagonist protagonist,
        int maxReplicas,
        LlamaServerManager llmManager,
        ModusMentisSlotManager slotManager,
        TerminalHUD terminal)
    {
        _npc          = npc;
        _protagonist  = protagonist;
        _maxReplicas  = maxReplicas;

        var greetingExecutor = new NpcGreetingExecutor(llmManager);
        var replicaExecutor  = new PlayerReplicaExecutor(llmManager, slotManager);
        var responseExecutor = new NpcResponseExecutor(llmManager);

        _greetingPhase = new NpcGreetingPhaseController(greetingExecutor);
        _replicaPhase  = new PlayerReplicaPhaseController(replicaExecutor, ModusMentisRegistry.Instance);
        _responsePhase = new NpcResponsePhaseController(responseExecutor);

        _ui           = new DialogueUI(terminal, _npc);
        _scrollBuffer = _ui.Buffer;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public bool HasRequestedExit => _state.RequestedExit;

    /// <summary>
    /// Call once after LLM slot is assigned to the NPC. Starts the greeting phase.
    /// </summary>
    public void Start()
    {
        _state.Clear();
        _scrollBuffer.Clear();
        BeginGreetingPhase();
    }

    /// <summary>Called every render frame by the game loop.</summary>
    public void Update()
    {
        _ui.Render(_state);
    }

    /// <summary>Mouse move — update hovered replica index or dice-roll button hover.</summary>
    public void OnMouseMove(int mouseX, int mouseY)
    {
        if (_state.IsDiceRollActive && !_state.IsDiceRolling)
        {
            _state.IsDiceRollButtonHovered = _ui.IsMouseOverDiceRollButton(mouseX, mouseY);
            return;
        }

        if (_state.IsLoadingGreeting || _state.IsLoadingReplicas || _state.IsLoadingResponse
            || _state.IsDiceRollActive || _state.ConversationEnded)
            return;

        _state.HoveredReplicaIndex = _ui.GetReplicaIndexAt(mouseX, mouseY);
    }

    /// <summary>Mouse click — select a replica or acknowledge end of conversation.</summary>
    public void OnMouseClick(int mouseX, int mouseY)
    {
        if (_state.RequestedExit) return;

        if (_state.ConversationEnded)
        {
            _state.RequestedExit = true;
            return;
        }

        if (_state.IsLoadingGreeting || _state.IsLoadingReplicas || _state.IsLoadingResponse) return;

        // After dice roll is shown, click the Continue button to proceed
        if (_state.IsDiceRollActive && !_state.IsDiceRolling)
        {
            if (_ui.IsMouseOverDiceRollButton(mouseX, mouseY))
            {
                _state.ClearDiceRoll();
                BeginResponsePhase();
            }
            return;
        }

        if (_state.IsDiceRollActive) return;

        // Select a replica
        int idx = _ui.GetReplicaIndexAt(mouseX, mouseY);
        if (idx >= 0 && idx < _state.Replicas.Count)
        {
            OnReplicaSelected(_state.Replicas[idx]);
        }
    }

    public void OnMouseWheel(float delta)
    {
        if (delta > 0) _scrollBuffer.ScrollUp();
        else           _scrollBuffer.ScrollDown();
    }

    public void OnKeyPress(OpenTK.Windowing.GraphicsLibraryFramework.Keys key)
    {
        if (key == OpenTK.Windowing.GraphicsLibraryFramework.Keys.Escape)
            _state.RequestedExit = true;
    }

    // ── Phase orchestration ───────────────────────────────────────────────────

    private void BeginGreetingPhase()
    {
        _state.IsLoadingGreeting = true;
        _scrollBuffer.AddBlock(new DialogueBlock(DialogueBlockType.SystemMessage, null, "..."));
        _ = Task.Run(GenerateGreetingAsync);
    }

    private async Task GenerateGreetingAsync()
    {
        try
        {
            string greeting = await _greetingPhase.ExecuteAsync(_npc);
            _state.NpcGreetingText   = greeting;
            _state.IsLoadingGreeting = false;

            // Replace loading placeholder with actual greeting
            _scrollBuffer.ReplaceLastBlock(
                new DialogueBlock(DialogueBlockType.NpcSpeaking, _npc.DisplayName, greeting));
            _scrollBuffer.ScrollToBottom();

            BeginReplicaPhase();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"DialogueModeController: Greeting failed: {ex.Message}");
            _state.ErrorMessage      = $"Failed to generate greeting: {ex.Message}";
            _state.IsLoadingGreeting = false;
        }
    }

    private void BeginReplicaPhase()
    {
        _state.IsLoadingReplicas  = true;
        _state.ReplicasLoaded     = 0;
        _state.ReplicasTotal      = _maxReplicas;
        _state.Replicas.Clear();

        _ = Task.Run(GenerateReplicasAsync);
    }

    private async Task GenerateReplicasAsync()
    {
        try
        {
            var options = await _replicaPhase.GenerateReplicasAsync(
                _npc,
                _maxReplicas,
                onProgress: (loaded, total) =>
                {
                    _state.ReplicasLoaded = loaded;
                    _state.ReplicasTotal  = total;
                });

            _state.Replicas          = options;
            _state.IsLoadingReplicas = false;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"DialogueModeController: Replica generation failed: {ex.Message}");
            _state.ErrorMessage      = $"Failed to generate replies: {ex.Message}";
            _state.IsLoadingReplicas = false;
        }
    }

    private void OnReplicaSelected(ReplicaOption selected)
    {
        _scrollBuffer.AddBlock(
            new DialogueBlock(DialogueBlockType.PlayerReplica, "You", $"\"{selected.ReplicaText}\""));

        // Pre-compute dice params so the animation starts immediately
        var (diceCount, difficulty) = NpcResponsePhaseController.ComputeDiceParams(_npc, selected);
        _state.StartDiceRoll(diceCount, difficulty);
        _pendingResult = null;

        _ = Task.Run(async () =>
        {
            try
            {
                // Let the animation run for at least 700 ms before resolving
                await Task.Delay(700);
                var result = await _responsePhase.ExecuteAsync(_npc, selected, diceCount, difficulty);
                _pendingResult = result;
                _state.CompleteDiceRoll(result.FinalDiceValues);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"DialogueModeController: Dice roll failed: {ex.Message}");
                _state.ClearDiceRoll();
                _state.ErrorMessage = $"Exchange failed: {ex.Message}";
            }
        });
    }

    private void BeginResponsePhase()
    {
        if (_pendingResult == null) return;

        _state.IsLoadingResponse = true;
        var result = _pendingResult!;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.CompletedTask; // Response already generated in OnReplicaSelected

                _scrollBuffer.AddBlock(new DialogueBlock(
                    DialogueBlockType.NpcSpeaking,
                    _npc.DisplayName,
                    result.NpcResponse));

                string affinityMsg = result.AffinityDelta >= 0
                    ? $"[Affinity +{result.AffinityDelta:F0}]"
                    : $"[Affinity {result.AffinityDelta:F0}]";
                _scrollBuffer.AddBlock(new DialogueBlock(DialogueBlockType.SystemMessage, null, affinityMsg));
                _scrollBuffer.ScrollToBottom();

                _state.IsLoadingResponse = false;
                _state.NpcResponseText   = result.NpcResponse;

                // Handle node transition
                if (result.NodeTransition != null)
                {
                    // Already applied in NpcResponsePhaseController; generate new greeting for new node
                    _scrollBuffer.AddSeparator();
                    _state.Replicas.Clear();
                    BeginGreetingPhase();
                }
                else
                {
                    // Check if conversation has bottomed out
                    if (_npc.CurrentSubjectNode.PossiblePositiveOutcomes.Count == 0
                        && _npc.CurrentSubjectNode.Transitions.Count == 0)
                    {
                        _scrollBuffer.AddBlock(new DialogueBlock(
                            DialogueBlockType.SystemMessage, null,
                            "[The conversation has run its course.]"));
                        _state.ConversationEnded = true;
                    }
                    else
                    {
                        BeginReplicaPhase();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"DialogueModeController: Response phase failed: {ex.Message}");
                _state.IsLoadingResponse = false;
                _state.ErrorMessage      = $"Response phase failed: {ex.Message}";
            }
        });
    }
}
