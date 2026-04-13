using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;
using Cathedral.LLM;
using Cathedral.Terminal;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Cathedral.Game.Dialogue.Runtime;

/// <summary>
/// Orchestrates a full dialogue tree session.
/// Loop: NPC speaks → MM samples options → player picks → dice roll → NPC reacts → advance or end.
/// </summary>
public class DialogueTreeController
{
    private readonly DialogueTree            _tree;
    private readonly NpcEntity               _npc;
    private readonly Protagonist             _protagonist;
    private readonly string                  _partyMemberId;
    private readonly int                     _npcSlotId;
    private readonly ModusMentisSlotManager  _slotManager;

    private readonly NpcNodeReplicaExecutor  _npcReplicaExec;
    private readonly MmBranchSelectorExecutor _branchSelExec;
    private readonly MmReplicaExecutor       _mmReplicaExec;
    private readonly NpcReactionExecutor     _reactionExec;

    private readonly DialogueSessionState    _state = new();
    private readonly DialogueTreeUI          _ui;

    private DialogueTreeNode                 _currentNode;
    private readonly Random                  _rng = new();

    // Pending succeeded result stored between dice animation and reaction generation
    private bool _pendingSucceeded;

    public bool HasRequestedExit => _state.RequestedExit;

    public DialogueTreeController(
        DialogueTree           tree,
        NpcEntity              npc,
        Protagonist            protagonist,
        int                    npcSlotId,
        LlamaServerManager     llmManager,
        ModusMentisSlotManager slotManager,
        TerminalHUD            terminal)
    {
        _tree          = tree;
        _npc           = npc;
        _protagonist   = protagonist;
        _partyMemberId = protagonist.DisplayName;
        _npcSlotId     = npcSlotId;
        _slotManager   = slotManager;

        _npcReplicaExec = new NpcNodeReplicaExecutor(llmManager);
        _branchSelExec  = new MmBranchSelectorExecutor(llmManager);
        _mmReplicaExec  = new MmReplicaExecutor(llmManager);
        _reactionExec   = new NpcReactionExecutor(llmManager);

        _currentNode = tree.EntryNode;
        _ui          = new DialogueTreeUI(terminal, npc, tree, _partyMemberId);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Start()
    {
        _state.Clear();
        BeginNpcSpeakPhase();
    }

    public void Update() => _ui.Render(_state);

    public void OnMouseMove(int mx, int my)
    {
        if (_state.IsDiceRollActive && !_state.IsDiceRolling)
        {
            _state.IsContinueHovered = _ui.IsMouseOverContinue(mx, my);
            return;
        }
        if (!_state.IsLoadingOptions && !_state.IsDiceRollActive && !_state.ConversationEnded)
            _state.HoveredOptionIndex = _ui.GetOptionIndexAt(mx, my);
    }

    public void OnMouseClick(int mx, int my)
    {
        if (_state.RequestedExit) return;

        if (_state.ConversationEnded) { _state.RequestedExit = true; return; }

        if (_state.IsDiceRollActive && !_state.IsDiceRolling)
        {
            if (_ui.IsMouseOverContinue(mx, my))
            {
                _state.ClearDiceRoll();
                BeginReactionPhase();
            }
            return;
        }
        if (_state.IsDiceRollActive || _state.IsLoadingNpcReplica
            || _state.IsLoadingOptions || _state.IsLoadingReaction) return;

        int idx = _ui.GetOptionIndexAt(mx, my);
        if (idx >= 0 && idx < _state.Options.Count)
            OnOptionSelected(_state.Options[idx]);
    }

    public void OnMouseWheel(float delta)
    {
        // Scroll up (delta > 0) → show older log entries (increase "lines back from bottom")
        // Scroll down (delta ≤ 0) → return toward latest content
        if (delta > 0) _state.ScrollOffset++;
        else           _state.ScrollOffset = Math.Max(0, _state.ScrollOffset - 1);
    }

    public void OnKeyPress(Keys key)
    {
        if (key == Keys.Escape) _state.RequestedExit = true;
    }

    // ── Phase: NPC speaks ─────────────────────────────────────────────────────

    private void BeginNpcSpeakPhase()
    {
        _state.IsLoadingNpcReplica = true;
        _state.Log.Add(new DialogueLogEntry(DialogueLogEntryType.SystemMessage, null, "..."));
        _ = Task.Run(NpcSpeakAsync);
    }

    private async Task NpcSpeakAsync()
    {
        try
        {
            string text = await _npcReplicaExec.ExecuteAsync(
                _npc, _npcSlotId, _currentNode, _tree.Description);

            _state.Log[^1] = new DialogueLogEntry(
                DialogueLogEntryType.NpcSpeaking, _npc.DisplayName, text);
            _state.IsLoadingNpcReplica = false;
            BeginOptionsPhase();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"DialogueTreeController: NPC speak failed: {ex.Message}");
            _state.IsLoadingNpcReplica = false;
            _state.ErrorMessage = ex.Message;
        }
    }

    // ── Phase: generate player options ────────────────────────────────────────

    private void BeginOptionsPhase()
    {
        // Terminal nodes still show options; they just have no branch to choose
        var speakingMMs = ModusMentisRegistry.Instance.GetSpeakingModiMentis();
        if (speakingMMs.Count == 0)
        {
            // No speaking skills — skip to end
            EndConversation();
            return;
        }

        int maxOptions = Math.Min(3, speakingMMs.Count);
        var sampled    = speakingMMs.OrderBy(_ => _rng.Next()).Take(maxOptions).ToList();

        _state.IsLoadingOptions = true;
        _state.OptionsLoaded    = 0;
        _state.OptionsTotal     = sampled.Count;
        _state.Options.Clear();

        _ = Task.Run(() => GenerateOptionsAsync(sampled));
    }

    private async Task GenerateOptionsAsync(List<ModusMentis> skills)
    {
        var results = new List<PlayerReplicaOption>();

        foreach (var mm in skills)
        {
            try
            {
                int slotId = await _slotManager.GetOrCreateSlotForModusMentisAsync(mm);

                // Choose branch (only if >1 branch; terminal nodes skip this)
                DialogueTreeNode targetNode = _currentNode;
                if (_currentNode.Branches.Count > 1)
                {
                    int branchIdx = await _branchSelExec.ExecuteAsync(
                        mm, slotId, _currentNode, _tree.Description);
                    targetNode = _currentNode.Branches[branchIdx].TargetNode;
                }
                else if (_currentNode.Branches.Count == 1)
                {
                    targetNode = _currentNode.Branches[0].TargetNode;
                }
                // else: terminal node, targetNode stays as _currentNode

                string replica = await _mmReplicaExec.ExecuteAsync(
                    mm, slotId, targetNode, _npc, _partyMemberId, _tree.Description);

                results.Add(new PlayerReplicaOption(mm, targetNode, replica));
                _state.OptionsLoaded++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"DialogueTreeController: Option gen failed for {mm.DisplayName}: {ex.Message}");
                _state.OptionsLoaded++;
            }
        }

        _state.Options          = results;
        _state.IsLoadingOptions = false;
    }

    // ── Phase: player selects an option → dice roll ───────────────────────────

    private void OnOptionSelected(PlayerReplicaOption option)
    {
        _state.Log.Add(new DialogueLogEntry(
            DialogueLogEntryType.PlayerReplica, "You", $"\"{option.ReplicaText}\""));

        // Compute dice: MM level + affinity bonus
        int affinityBonus = (int)_npc.AffinityTable.GetLevel(_partyMemberId);
        int diceCount     = Math.Clamp(option.Skill.Level + affinityBonus, 1, 15);
        int difficulty    = Math.Max(1, (int)Math.Ceiling(diceCount * 0.4)); // ~40% base difficulty

        _state.StartDiceRoll(diceCount, difficulty);

        _ = Task.Run(async () =>
        {
            await Task.Delay(700); // Let animation play
            int[] values    = Enumerable.Range(0, diceCount).Select(_ => _rng.Next(1, 7)).ToArray();
            bool  succeeded = values.Count(v => v == 6) >= difficulty;
            _pendingSucceeded = succeeded;

            // Pre-generate NPC reaction in parallel with dice animation
            try
            {
                string reaction = await _reactionExec.ExecuteAsync(
                    _npc, _npcSlotId, option.ReplicaText, succeeded, option.TargetNode);
                _state.PendingNpcReaction = reaction;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"DialogueTreeController: Reaction gen failed: {ex.Message}");
                _state.PendingNpcReaction = succeeded
                    ? $"{_npc.DisplayName} nods."
                    : $"{_npc.DisplayName} doesn't react.";
            }

            _state.CompleteDiceRoll(values);
            _selectedOption = option;
        });
    }

    private PlayerReplicaOption? _selectedOption;

    // ── Phase: show reaction + apply outcome ──────────────────────────────────

    private void BeginReactionPhase()
    {
        if (_selectedOption == null || _state.PendingNpcReaction == null) return;

        _state.IsLoadingReaction = true;
        var option    = _selectedOption!;
        var reaction  = _state.PendingNpcReaction!;
        bool succeeded = _pendingSucceeded;
        _selectedOption = null;

        _ = Task.Run(() =>
        {
            _state.Log.Add(new DialogueLogEntry(
                DialogueLogEntryType.NpcSpeaking, _npc.DisplayName, reaction));

            _state.IsLoadingReaction = false;

            if (_currentNode.IsTerminal)
            {
                // Apply matching outcomes
                foreach (var oc in _currentNode.Outcomes)
                {
                    bool fires = oc.Condition == BranchCondition.Either
                        || (oc.Condition == BranchCondition.Success && succeeded)
                        || (oc.Condition == BranchCondition.Failure && !succeeded);

                    if (fires) oc.Outcome.Apply(_npc, _partyMemberId);
                }

                // Mark first contact if still stranger
                _npc.AffinityTable.MarkFirstContact(_partyMemberId);

                // Show final affinity
                var finalLevel = _npc.AffinityTable.GetLevel(_partyMemberId);
                _state.Log.Add(new DialogueLogEntry(
                    DialogueLogEntryType.SystemMessage, null,
                    $"[{finalLevel.ToDisplayName(_npc.DisplayName)}]"));

                EndConversation();
            }
            else
            {
                // Advance to chosen branch node whose condition matches
                var branchesToFollow = option.TargetNode.Branches
                    .Where(b => b.Condition == BranchCondition.Either
                        || (b.Condition == BranchCondition.Success && succeeded)
                        || (b.Condition == BranchCondition.Failure && !succeeded))
                    .ToList();

                if (branchesToFollow.Count == 0)
                {
                    // No valid branch — go to the target node directly
                    _currentNode = option.TargetNode;
                }
                else
                {
                    _currentNode = branchesToFollow[0].TargetNode;
                }

                _state.Log.Add(new DialogueLogEntry(
                    DialogueLogEntryType.Separator, null, string.Empty));
                BeginNpcSpeakPhase();
            }
        });
    }

    private void EndConversation()
    {
        _state.Log.Add(new DialogueLogEntry(
            DialogueLogEntryType.SystemMessage, null, "[The conversation has ended.]"));
        _state.ConversationEnded = true;
    }
}
