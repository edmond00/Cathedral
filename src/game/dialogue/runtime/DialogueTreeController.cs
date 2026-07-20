using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;
using Cathedral.LLM;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Cathedral.Game.Dialogue.Runtime;

/// <summary>
/// Orchestrates a full dialogue tree session.
/// Flow: NPC speaks a line → sampled Modi Mentis grade + voice the player replies → player picks →
/// (advance to the next NPC line, or, at a <see cref="ResolutionNode"/>, roll the single accumulated
/// dice check) → the NPC speaks its success/failure line and outcomes fire.
///
/// <para>
/// A conversation rolls the dice <b>once</b>, at the branch end. The pool is the NPC affinity bonus
/// plus the summed levels of every Modus Mentis that voiced a chosen reply along the branch; the
/// difficulty is authored on the <see cref="ResolutionNode"/>.
/// </para>
/// </summary>
public class DialogueTreeController
{
    private readonly DialogueTree             _tree;
    private readonly NpcEntity                _npc;
    private readonly Protagonist              _protagonist;
    private readonly string                   _partyMemberId;
    private readonly int                      _npcSlotId;
    private readonly DialogueContext          _context;

    private readonly DialogueReplicaWriter    _replicaWriter;
    private readonly DialogueOptionGenerator  _optionGenerator;

    private readonly DialogueSessionState     _state = new();
    private readonly DialogueTreeUI           _ui;

    // Unified dice-roll overlay (animation + humor modifiers + hit-testing).
    private readonly DiceRollComponent        _dice = new();

    private NpcLineNode                       _currentNode;
    private readonly Random                   _rng = new();

    // ── Branch accumulation (reset per conversation) ───────────────────────────
    /// <summary>Summed levels of the Modi Mentis that voiced each chosen reply so far.</summary>
    private int                               _accumulatedDice;
    /// <summary>The Modi Mentis that voiced chosen replies, awarded XP on a successful check.</summary>
    private readonly List<ModusMentis>        _chosenMMs = new();
    /// <summary>The resolution node awaiting the dice continue-click.</summary>
    private ResolutionNode?                   _pendingResolution;

    /// <summary>Per-roll humor modifier budget from the viscera <c>humor_modifier_limit</c> stat.</summary>
    private static int HumorModifierLimit(PartyMember member)
        => member.DerivedStats.First(s => s.Name == "humor_modifier_limit").GetValue(member);

    public bool HasRequestedExit => _state.RequestedExit;

    private readonly Cathedral.Audio.AmbianceEngine? _ambianceEngine;

    public DialogueTreeController(
        DialogueTree           tree,
        NpcEntity              npc,
        Protagonist            protagonist,
        int                    npcSlotId,
        LlamaServerManager     llmManager,
        ModusMentisSlotManager slotManager,
        DialogueTreeUI         ui,
        DialogueContext        context,
        Cathedral.Audio.AmbianceEngine? ambianceEngine = null)
    {
        _tree          = tree;
        _npc           = npc;
        _protagonist   = protagonist;
        _partyMemberId = protagonist.AffinityKey;
        _npcSlotId     = npcSlotId;
        _context       = context;
        _ambianceEngine = ambianceEngine;

        _replicaWriter   = new DialogueReplicaWriter(llmManager);
        _optionGenerator = new DialogueOptionGenerator(llmManager, slotManager);

        _currentNode = tree.EntryNode;
        _ui          = ui;

        _dice.OnDiceTick      = () => _ambianceEngine?.TriggerGameEvent(Cathedral.Audio.GameEventType.SmallInteraction);
        _dice.OnButtonHover   = () => _ambianceEngine?.TriggerGameEvent(Cathedral.Audio.GameEventType.SmallInteraction);
        _dice.OnButtonClick   = () => _ambianceEngine?.TriggerGameEvent(Cathedral.Audio.GameEventType.StrongInteraction);
        _dice.OnResultChanged = success => _ambianceEngine?.TriggerGameEvent(
            success ? Cathedral.Audio.GameEventType.PositiveOutcome : Cathedral.Audio.GameEventType.NegativeOutcome);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Start()
    {
        _state.Clear();
        _accumulatedDice = 0;
        _chosenMMs.Clear();
        _pendingResolution = null;
        BeginNpcSpeakPhase();
    }

    public void Update()
    {
        if (_state.IsDiceRollActive) _dice.Advance();
        _ui.Render(_state, _dice);
    }

    public void OnMouseMove(int mx, int my)
    {
        if (_state.IsDiceRollActive && !_state.IsDiceRolling)
        {
            var r = _dice.ContinueButtonRegion;
            _state.IsContinueHovered = my == r.Y && mx >= r.X && mx < r.X + r.Width;
            _dice.HandleHumorHover(mx, my);
            return;
        }

        // Footer exit button (LEAVE / INTERRUPT) — region is default while hidden.
        var exit = _state.ExitButtonRegion;
        bool overExit = exit.Width > 0 && my == exit.Y && mx >= exit.X && mx < exit.X + exit.Width;
        if (overExit != _state.IsExitButtonHovered)
        {
            _state.IsExitButtonHovered = overExit;
            if (overExit) _ambianceEngine?.TriggerGameEvent(Cathedral.Audio.GameEventType.SmallInteraction);
        }

        if (!_state.IsLoadingOptions && !_state.IsDiceRollActive && !_state.ConversationEnded)
            _state.HoveredOptionIndex = _ui.GetOptionIndexAt(mx, my);
    }

    public void OnMouseClick(int mx, int my)
    {
        if (_state.RequestedExit) return;

        // Footer exit button: LEAVE (normal exit after the tree ends, or on error)
        // or INTERRUPT (early exit mid-conversation, with consequences).
        var exit = _state.ExitButtonRegion;
        if (exit.Width > 0 && my == exit.Y && mx >= exit.X && mx < exit.X + exit.Width)
        {
            _ambianceEngine?.TriggerGameEvent(Cathedral.Audio.GameEventType.StrongInteraction);
            if (_state.ConversationEnded || _state.ErrorMessage != null)
                _state.RequestedExit = true;
            else
                InterruptConversation();
            return;
        }

        if (_state.ConversationEnded) return;

        if (_state.IsDiceRollActive && !_state.IsDiceRolling)
        {
            var r = _dice.ContinueButtonRegion;
            if (my == r.Y && mx >= r.X && mx < r.X + r.Width)
            {
                _state.ClearDiceRoll();
                _dice.Hide();
                ResolveRoll();
                return;
            }
            _dice.HandleHumorClick(mx, my);
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
        if (key != Keys.Escape) return;

        // ESC mirrors the footer button: a plain exit once the conversation has ended
        // (or errored), an interruption with consequences while it is still running.
        if (_state.ConversationEnded || _state.ErrorMessage != null)
            _state.RequestedExit = true;
        else
            InterruptConversation();
    }

    /// <summary>
    /// Early exit mid-conversation. Walking away is rude: the NPC still remembers the
    /// meeting (first contact), then affinity drops one step. Suspicious NPCs are left
    /// untouched — stepping them "down" the scale would paradoxically make them friends.
    /// </summary>
    private void InterruptConversation()
    {
        _npc.AffinityTable.MarkFirstContact(_partyMemberId);
        if (_npc.AffinityTable.GetLevel(_partyMemberId) != AffinityLevel.Suspicious)
            _npc.AffinityTable.Adjust(_partyMemberId, -1);

        Console.WriteLine(
            $"DialogueTreeController: Conversation with {_npc.DisplayName} interrupted — " +
            $"affinity now {_npc.AffinityTable.GetLevel(_partyMemberId)}");
        _state.RequestedExit = true;
    }

    // ── Phase: NPC speaks the current line ────────────────────────────────────

    private void BeginNpcSpeakPhase()
    {
        _state.Options.Clear();          // previous node's replies are no longer selectable
        _state.HoveredOptionIndex = -1;
        _state.IsLoadingNpcReplica = true;
        _state.Log.Add(new DialogueLogEntry(DialogueLogEntryType.SystemMessage, null, "..."));
        _ = Task.Run(NpcSpeakAsync);
    }

    private async Task NpcSpeakAsync()
    {
        try
        {
            string text = await _replicaWriter.WriteAsync(
                _npcSlotId, _currentNode.Replica, _context, addresseeRole: "you", subject: _tree.Description);

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
        if (_currentNode.Options.Count == 0)
        {
            // Malformed node (no replies) — nothing the player can say; end gracefully.
            EndConversation();
            return;
        }

        int expected = Math.Min(
            DialogueOptionGenerator.SpeechFluency(_protagonist),
            _protagonist.GetSpeakingModiMentis().Count);

        _state.IsLoadingOptions = true;
        _state.OptionsLoaded    = 0;
        _state.OptionsTotal     = Math.Max(1, expected);
        _state.Options.Clear();

        _ = Task.Run(GenerateOptionsAsync);
    }

    private async Task GenerateOptionsAsync()
    {
        try
        {
            var options = await _optionGenerator.GenerateAsync(
                _currentNode, _protagonist, _context, _tree.Description);

            _state.Options       = options;
            _state.OptionsLoaded = options.Count;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"DialogueTreeController: Option generation failed: {ex.Message}");
            _state.Options = new List<PlayerReplicaOption>();
        }
        finally
        {
            _state.IsLoadingOptions = false;
        }

        // If no MM had anything to say, there is nothing to choose — end the conversation.
        if (_state.Options.Count == 0) EndConversation();
    }

    // ── Phase: player selects a reply ─────────────────────────────────────────

    private void OnOptionSelected(PlayerReplicaOption option)
    {
        _state.Log.Add(new DialogueLogEntry(
            DialogueLogEntryType.PlayerReplica, "You", $"\"{option.ReplicaText}\""));

        // This reply's Modus Mentis contributes its level to the branch dice pool.
        _accumulatedDice += option.Skill.Level;
        _chosenMMs.Add(option.Skill);
        _state.Options.Clear();

        switch (option.Option.Next)
        {
            case NpcLineNode npcNode:
                _currentNode = npcNode;
                _state.Log.Add(new DialogueLogEntry(DialogueLogEntryType.Separator, null, string.Empty));
                BeginNpcSpeakPhase();
                break;

            case ResolutionNode resolution:
                BeginResolution(resolution);
                break;

            default:
                Console.Error.WriteLine("DialogueTreeController: option leads to an unknown node type.");
                EndConversation();
                break;
        }
    }

    // ── Phase: the single dice check at a branch end ──────────────────────────

    private void BeginResolution(ResolutionNode resolution)
    {
        _pendingResolution = resolution;

        int affinityBonus = _npc.AffinityTable.GetLevel(_partyMemberId).BonusDice();
        int diceCount     = Math.Clamp(affinityBonus + _accumulatedDice, 1, 15);
        int difficulty    = resolution.Difficulty;

        _state.StartDiceRoll(diceCount, difficulty);
        _dice.Start(diceCount, difficulty);
        int limit = HumorModifierLimit(_protagonist);
        if (limit > 0) _dice.EnableHumorModifiers(_protagonist.HumorQueues, limit);

        _ = Task.Run(async () =>
        {
            await Task.Delay(700); // Let the animation play
            int[] values = Enumerable.Range(0, diceCount).Select(_ => _rng.Next(1, 7)).ToArray();
            _state.CompleteDiceRoll(values);
            _dice.Complete(values);
        });
    }

    private void ResolveRoll()
    {
        var resolution = _pendingResolution;
        _pendingResolution = null;
        if (resolution == null) return;

        // Final outcome reflects any humor modifiers the player applied during the roll.
        bool succeeded = _dice.IsCurrentlySuccess;

        _state.IsLoadingReaction = true;

        _ = Task.Run(async () =>
        {
            // The NPC's closing line — one of the node's two authored replicas.
            string neutral = succeeded ? resolution.SuccessReplica : resolution.FailureReplica;
            string reaction;
            try
            {
                reaction = await _replicaWriter.WriteAsync(
                    _npcSlotId, neutral, _context, addresseeRole: "you", subject: _tree.Description);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"DialogueTreeController: Resolution line failed: {ex.Message}");
                reaction = succeeded ? $"{_npc.DisplayName} nods." : $"{_npc.DisplayName} doesn't react.";
            }

            // Award +1 XP to every learned speaking MM that voiced a chosen reply on this branch.
            if (succeeded)
            {
                foreach (var mm in _chosenMMs.DistinctBy(m => m.ModusMentisId))
                {
                    var learned = _protagonist.GetModusMentisById(mm.ModusMentisId);
                    if (learned != null) _protagonist.AwardModusMentisXp(learned);
                }
            }

            _state.Log.Add(new DialogueLogEntry(
                DialogueLogEntryType.NpcSpeaking, _npc.DisplayName, reaction));
            _state.IsLoadingReaction = false;

            // Apply the outcomes gated by success/failure.
            foreach (var oc in resolution.Outcomes)
            {
                bool fires = oc.Condition == BranchCondition.Either
                    || (oc.Condition == BranchCondition.Success && succeeded)
                    || (oc.Condition == BranchCondition.Failure && !succeeded);
                if (fires) oc.Outcome.Apply(_npc, _partyMemberId);
            }

            _npc.AffinityTable.MarkFirstContact(_partyMemberId);

            var finalLevel = _npc.AffinityTable.GetLevel(_partyMemberId);
            _state.Log.Add(new DialogueLogEntry(
                DialogueLogEntryType.SystemMessage, null,
                $"[{finalLevel.ToDisplayName(_npc.DisplayName)}]"));

            EndConversation();
        });
    }

    private void EndConversation()
    {
        _state.Log.Add(new DialogueLogEntry(
            DialogueLogEntryType.SystemMessage, null, "[The conversation has ended.]"));
        _state.ConversationEnded = true;
    }
}
