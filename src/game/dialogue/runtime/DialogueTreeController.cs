using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Preview;
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
/// A conversation rolls the dice <b>once</b>, at the branch end. The pool is the NPC affinity bonus,
/// the summed levels of every Modus Mentis that voiced a chosen reply along the branch, the speaker's
/// beauty, and what they are wearing as the NPC's own standing reads it; the difficulty is authored
/// on the <see cref="ResolutionNode"/>, clamped to the pool size.
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

    /// <summary>
    /// The narration session's shared text history. Spoken lines are appended here as they are
    /// produced; the panel renders from the same buffer, so scrolling up reaches the greyed-out
    /// narration that preceded the conversation.
    /// </summary>
    private readonly NarrationScrollBuffer    _buffer;

    // Unified dice-roll overlay (animation + humor modifiers + hit-testing).
    private readonly DiceRollComponent        _dice = new();

    // Streams the player replies being generated into a preview box, gated by CONTINUE.
    private readonly LlmPreviewSession        _preview = new();
    private string                            _lastPreviewText = ""; // last rendered preview text, to tick on change

    private NpcLineNode                       _currentNode;
    private readonly Random                   _rng = GameRng.Stream("dialogue-tree");

    // ── Branch accumulation (reset per conversation) ───────────────────────────
    /// <summary>Summed levels of the Modi Mentis that voiced each chosen reply so far.</summary>
    private int                               _accumulatedDice;
    /// <summary>The Modi Mentis that voiced chosen replies, awarded XP on a successful check.</summary>
    private readonly List<ModusMentis>        _chosenMMs = new();
    /// <summary>The resolution node awaiting the dice continue-click.</summary>
    private ResolutionNode?                   _pendingResolution;

    /// <summary>
    /// The buffer block holding the current node's selectable replies. Options live inside the
    /// shared scroll buffer (so they scroll with the conversation); on selection the block is marked
    /// with the chosen index and stays in the log — selected highlighted, the rest greyed.
    /// </summary>
    private NarrationBlock?                   _optionsBlock;

    // ── Last spoken line on each side (for rewrite-prompt grounding) ───────────
    // Holds the authored HEARD report (template-expanded, placeholder names) — "{npc} tells me that…" —
    // not the persona's spoken version the player read. That rendering is one persona's ornamented
    // take: "What oxen do ye tend to in this field? Is 't only those that till the soil…?" for a line
    // reporting "asks what I farm". Handing it on gives the next prompt a second persona's voice to
    // imitate and every flourish as a fact of the exchange.
    //
    // There is no player-side counterpart, because only one prompt is told what was just said: the one
    // grading which reply to offer. The rewrite that turns a description into speech is given nothing
    // but its own description (see PersonaRewriter.BuildDialoguePrompt).
    /// <summary>Report of the NPC's most recent line, from the player's side, or null before the first.</summary>
    private string?                           _lastNpcNeutralLine;

    /// <summary>
    /// The resolution dice, honouring <c>--debug</c>'s <c>strategy</c> preset.
    ///
    /// <para>This roll used to be a plain draw that consulted nothing, so <c>strategy succeed</c> and
    /// <c>strategy fail-dice</c> — which pin every other roll in the game, in narration and in a
    /// fight — did nothing to a conversation. A script could not choose whether it won or lost one,
    /// which made the failure branch of every dialogue tree untestable.</para>
    ///
    /// <para>A forced success fills exactly <paramref name="difficulty"/> sixes (capped at the pool,
    /// the same guard the narration roll uses so an impossible demand cannot spin); a forced failure
    /// fills none. Anything else draws as it always did.</para>
    /// </summary>
    private int[] RollResolution(int diceCount, int difficulty)
    {
        var values = Enumerable.Range(0, diceCount).Select(_ => _rng.Next(1, 7)).ToArray();
        if (!DebugMode.IsActive || DebugMode.IsAutoStrategy) return values;

        switch (DebugMode.CurrentStrategy)
        {
            case DebugStrategy.Succeed:
                int needed = Math.Min(difficulty, diceCount);
                for (int i = 0; i < values.Length; i++) values[i] = i < needed ? 6 : Math.Min(values[i], 5);
                Console.WriteLine($"DialogueTreeController: [debug] strategy Succeed — forcing {needed} six(es) of {diceCount}.");
                break;

            case DebugStrategy.FailDiceRoll:
                for (int i = 0; i < values.Length; i++) if (values[i] == 6) values[i] = 5;
                Console.WriteLine("DialogueTreeController: [debug] strategy FailDiceRoll — no sixes.");
                break;
        }
        return values;
    }

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
        NarrationScrollBuffer  buffer,
        Cathedral.Audio.AmbianceEngine? ambianceEngine = null)
    {
        _buffer        = buffer;
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
        _optionsBlock = null;
        _lastNpcNeutralLine = null;
        BeginNpcSpeakPhase();
    }

    public void Update()
    {
        if (_state.IsDiceRollActive) _dice.Advance();
        var preview = _preview.Snapshot();
        // Play the hover tick whenever new preview text streams in (typewriter feedback).
        if (preview.Active && preview.DisplayText != _lastPreviewText)
        {
            _ambianceEngine?.TriggerGameEvent(Cathedral.Audio.GameEventType.SmallInteraction);
            _lastPreviewText = preview.DisplayText;
        }
        else if (!preview.Active)
        {
            _lastPreviewText = "";
        }
        _ui.Render(_state, _dice, preview, _state.IsPreviewHovered);
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

        // Preview box CONTINUE hover (while replies are being previewed).
        var pv = _state.PreviewContinueRegion;
        bool overPreview = pv.Width > 0 && my == pv.Y && mx >= pv.X && mx < pv.X + pv.Width;
        if (overPreview != _state.IsPreviewHovered)
        {
            _state.IsPreviewHovered = overPreview;
            if (overPreview) _ambianceEngine?.TriggerGameEvent(Cathedral.Audio.GameEventType.SmallInteraction);
        }

        // Footer exit button (LEAVE / INTERRUPT) — region is default while hidden.
        var exit = _state.ExitButtonRegion;
        bool overExit = exit.Width > 0 && my == exit.Y && mx >= exit.X && mx < exit.X + exit.Width;
        if (overExit != _state.IsExitButtonHovered)
        {
            _state.IsExitButtonHovered = overExit;
            if (overExit) _ambianceEngine?.TriggerGameEvent(Cathedral.Audio.GameEventType.SmallInteraction);
        }

        if (!_state.IsLoadingOptions && !_preview.IsActive && !_state.IsDiceRollActive && !_state.ConversationEnded)
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

        // Preview box CONTINUE — commit the replies and make them selectable.
        var pv = _state.PreviewContinueRegion;
        if (pv.Width > 0 && my == pv.Y && mx >= pv.X && mx < pv.X + pv.Width)
        {
            _ambianceEngine?.TriggerGameEvent(Cathedral.Audio.GameEventType.StrongInteraction);
            _state.IsPreviewHovered = false;
            _preview.TryContinue();
            return;
        }

        if (_state.IsDiceRollActive && !_state.IsDiceRolling)
        {
            var r = _dice.ContinueButtonRegion;
            if (my == r.Y && mx >= r.X && mx < r.X + r.Width)
            {
                // Read the (possibly humor-modified) verdict BEFORE tearing the roll down, so the
                // committed outcome can never depend on what Hide() happens to leave behind.
                bool succeeded = _dice.IsCurrentlySuccess;
                _state.ClearDiceRoll();
                _dice.Hide();
                ResolveRoll(succeeded);
                return;
            }
            _dice.HandleHumorClick(mx, my);
            return;
        }
        if (_state.IsDiceRollActive || _state.IsLoadingNpcReplica
            || _state.IsLoadingOptions || _state.IsLoadingReaction || _preview.IsActive) return;

        int idx = _ui.GetOptionIndexAt(mx, my);
        if (idx >= 0 && idx < _state.Options.Count)
            OnOptionSelected(idx);
    }

    public void OnMouseWheel(float delta)
    {
        // The buffer owns and clamps the scroll position, so scrolling up runs off the top of the
        // conversation into the greyed narration history rather than into dead travel.
        if (delta > 0) _buffer.ScrollUp(3);
        else           _buffer.ScrollDown(3);
    }

    public void OnKeyPress(Keys key)
    {
        // ESC is deliberately NOT handled here: it opens the pause menu at the launcher level
        // (same as narration) and must never interrupt the conversation. Walking away is done
        // through the footer button (INTERRUPT while running, END once ended) — see OnMouseClick
        // and CliPressExitButton.
    }

    /// <summary>
    /// Early exit mid-conversation. Walking away is rude: the NPC still remembers the
    /// meeting (first contact), then affinity drops one step. Suspicious NPCs are left
    /// untouched — stepping them "down" the scale would paradoxically make them friends.
    /// </summary>
    private void InterruptConversation()
    {
        _npc.AffinityTable.MarkFirstContact(_partyMemberId);

        // Walking away costs standing, so it gets the same chip a resolved outcome would — the panel
        // closes immediately, but the buffer is shared and the player reads it back in narration.
        // One object does both: an ordinary outcome applies the step and words its own chip, and
        // stays silent when the step was a no-op. This used to adjust the table by hand and then
        // build a separate report describing what it had just done.
        var walkedOut = new AffinityIncrementOutcome(-1);
        walkedOut.ApplyTo(OutcomeContext.ForDialogue(_npc, _partyMemberId, _protagonist));
        if (walkedOut.ShowInUI) AppendOutcomeReports(new List<Outcome> { walkedOut });

        Console.WriteLine(
            $"DialogueTreeController: Conversation with {_npc.DisplayName} interrupted — " +
            $"affinity now {_npc.AffinityTable.GetLevel(_partyMemberId)}");
        _state.RequestedExit = true;
    }

    // ── CLI driving surface (--cli) ───────────────────────────────────────────

    /// <summary>Snapshot of the conversation state a --cli `state` command reports.</summary>
    public (bool Loading, bool DiceActive, bool DiceRolling, bool Ended, string? Error, int OptionCount)
        CliSnapshot() => (
            _state.IsLoadingNpcReplica || _state.IsLoadingOptions || _state.IsLoadingReaction,
            _state.IsDiceRollActive,
            _state.IsDiceRolling,
            _state.ConversationEnded,
            _state.ErrorMessage,
            _state.Options.Count);

    /// <summary>The selectable replies, as (index, skill, text).</summary>
    public IReadOnlyList<(int Index, string Skill, string Text)> CliOptions()
        => _state.Options.Select((o, i) => (i, o.Skill.DisplayName, o.ReplicaText)).ToList();

    /// <summary>Pick a reply by index. Returns an error string, or null on success.</summary>
    public string? CliSelectOption(int index)
    {
        if (_state.IsDiceRollActive)    return "a dice roll is in progress";
        if (_state.ConversationEnded)   return "the conversation has ended";
        if (index < 0 || index >= _state.Options.Count)
            return $"option {index} out of range (have {_state.Options.Count})";
        OnOptionSelected(index);
        return null;
    }

    /// <summary>Click the footer button: END after the conversation, INTERRUPT during it.</summary>
    public void CliPressExitButton()
    {
        if (_state.ConversationEnded || _state.ErrorMessage != null) _state.RequestedExit = true;
        else                                                        InterruptConversation();
    }

    /// <summary>Confirm the dice overlay's [ Continue ] button, if it is showing.</summary>
    public string? CliDiceContinue()
    {
        if (!_state.IsDiceRollActive) return "no dice roll is active";
        if (_state.IsDiceRolling)     return "dice are still rolling";
        bool succeeded = _dice.IsCurrentlySuccess;
        _state.ClearDiceRoll();
        _dice.Hide();
        ResolveRoll(succeeded);
        return null;
    }

    /// <summary>Whether the reply-generation preview box is up, its title, text and completeness.</summary>
    public (bool Active, string Title, string Text, bool Complete) CliPreview()
    {
        var s = _preview.Snapshot();
        return (s.Active, s.Title, s.DisplayText, s.Complete);
    }

    /// <summary>Press the preview box [ CONTINUE ] (commits the replies). Returns an error, or null.</summary>
    public string? CliPreviewContinue()
    {
        if (!_preview.IsActive)          return "no preview is active";
        if (!_preview.Snapshot().Complete) return "replies are still generating";
        _preview.TryContinue();
        return null;
    }

    // ── Shared-history writers ────────────────────────────────────────────────

    /// <summary>A line the NPC speaks. No modus mentis, so no header — renders as "Emma: …".</summary>
    private void AppendNpcLine(string text) => Append(new NarrationBlock(
        Type: NarrationBlockType.Speaking,
        ModusMentis: null!,
        Text: $"{_npc.DisplayName}: {text}",
        Keywords: null,
        Actions: null,
        SpeakerName: _npc.DisplayName));

    /// <summary>A bracketed system note (conversation end).</summary>
    private void AppendSystemLine(string text) => Append(new NarrationBlock(
        Type: NarrationBlockType.Outcome,
        ModusMentis: null!,
        Text: text,
        Keywords: null,
        Actions: null));

    /// <summary>
    /// What the conversation actually changed, as the very chips the narration panel uses for an
    /// action's outcome — same <see cref="Outcome"/> type, same severity colours, same centred
    /// band. A dialogue outcome and an action outcome should not look like different systems.
    /// </summary>
    private void AppendOutcomeReports(List<Outcome> reports) => Append(new NarrationBlock(
        Type: NarrationBlockType.Outcome,
        ModusMentis: null!,
        Text: "",
        Keywords: null,
        Actions: null,
        OutcomeReports: reports));

    private void Append(NarrationBlock block)
    {
        _buffer.AddBlock(block);
        _buffer.ScrollToBottom();   // follow the conversation unless the player scrolls back
    }

    // ── Phase: NPC speaks the current line ────────────────────────────────────

    private void BeginNpcSpeakPhase()
    {
        _state.Options.Clear();          // previous node's replies are no longer selectable
        _state.HoveredOptionIndex = -1;
        _state.IsLoadingNpcReplica = true;
        // No placeholder line: the wait is already shown as a greyed panel + "{npc} is thinking…".
        _ = Task.Run(NpcSpeakAsync);
    }

    private async Task NpcSpeakAsync()
    {
        _preview.Reset();
        // The NPC's line streams into a preview box titled with the NPC name; on CONTINUE it is added
        // to the log and the player-reply generation begins (which shows its own preview box). The
        // transform restores real names (the rewrite streams the simpler placeholder names).
        var part = _preview.BeginPart(_npc.DisplayName.ToUpper(), _context.Names.ToReal);
        try
        {
            string text = await _replicaWriter.WriteAsync(
                _npcSlotId, _currentNode.Replica, _context, addresseeRole: "you", subject: _tree.Description,
                preview: part.Sink);

            _state.IsLoadingNpcReplica = false;

            void CommitNpcLine()
            {
                // The heard report, not `text`: what this line sounds like from the player's side.
                _lastNpcNeutralLine = DialogueTemplate.Expand(_currentNode.ReplicaHeard, _context);
                AppendNpcLine(text);
                BeginOptionsPhase();
            }
            part.AttachCommit(CommitNpcLine);
            part.MarkComplete();
            _preview.EndProduction();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"DialogueTreeController: NPC speak failed: {ex.Message}");
            _preview.Reset();
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
        _preview.Reset();

        // Deferred commit: the replies become visible and selectable only when the player presses
        // CONTINUE on the preview box (once every reply has finished streaming).
        void CommitOptions(List<PlayerReplicaOption> options)
        {
            _state.Options = options;

            // Append the replies to the shared scroll buffer as clickable lines — they flow with
            // the conversation text (and scroll with it) instead of floating over the log.
            _optionsBlock = new NarrationBlock(
                Type: NarrationBlockType.DialogueOptions,
                ModusMentis: null!,
                Text: "",
                Keywords: null,
                Actions: null,
                DialogueOptions: options
                    .Select(o => new DialogueOptionItem(o.Skill.DisplayName, o.Skill.Level, o.ReplicaText))
                    .ToList());
            Append(_optionsBlock);
        }

        try
        {
            var options = await _optionGenerator.GenerateAsync(
                _currentNode, _protagonist, _context, _tree.Description,
                previousNpcReplica: _lastNpcNeutralLine,
                preview: _preview,
                commit: CommitOptions);

            _state.OptionsLoaded = options.Count;

            // If no MM had anything to say, there is nothing to choose — end the conversation.
            // (When options exist, they are committed on CONTINUE, not here.)
            if (options.Count == 0)
            {
                _preview.Reset();
                _state.IsLoadingOptions = false;
                EndConversation();
                return;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"DialogueTreeController: Option generation failed: {ex.Message}");
            _preview.Reset();
            _state.Options = new List<PlayerReplicaOption>();
        }

        _state.IsLoadingOptions = false;
    }

    // ── Phase: player selects a reply ─────────────────────────────────────────

    private void OnOptionSelected(int index)
    {
        var option = _state.Options[index];

        // The chosen reply stays visible inside the options group in the log: mark the block and
        // the renderer highlights the selected line (player colour) and greys out the others.
        // No separate "You: …" block — that would repeat the same text right below the group.
        if (_optionsBlock != null) _optionsBlock.SelectedDialogueOptionIndex = index;
        _optionsBlock = null;

        // This reply's Modus Mentis contributes its level to the branch dice pool.
        _accumulatedDice += option.Skill.Level;
        _chosenMMs.Add(option.Skill);
        _state.Options.Clear();

        switch (option.Option.Next)
        {
            case NpcLineNode npcNode:
                _currentNode = npcNode;
                // No separator: blocks already emit a trailing blank line, and segment rules are
                // owned by ConvertToHistory.
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

        // A forced node commits its result with no roll (e.g. the caught-red-handed "provoke" branch,
        // which always ends in a fight). No dice overlay, no accumulated-pool math.
        if (resolution.Mode != ResolutionMode.DiceCheck)
        {
            bool forcedSuccess = resolution.Mode == ResolutionMode.ForceSuccess;
            Console.WriteLine(
                $"DialogueTreeController: resolution '{resolution.NodeId}' forced → " +
                $"{(forcedSuccess ? "SUCCESS" : "FAILURE")} (no roll)");
            ResolveRoll(forcedSuccess);
            return;
        }

        int affinityBonus = _npc.AffinityTable.GetLevel(_partyMemberId).BonusDice();

        // What the protagonist is wearing, judged by this NPC's own social standing. A garment
        // only counts while actually worn, and only for the standing it flatters — so the same
        // coat that helps with a craftsman does nothing for a hermit.
        var social      = _npc.Archetype is NamedNpcArchetype named ? named.Social : null;
        int attireBonus = social is { } sc ? WearingDialogueBonus.For(_protagonist, sc) : 0;

        // A fair face helps with everyone, so unlike attire this one owes nothing to who is listening.
        int beautyBonus = _protagonist.BeautyDice;

        int diceCount     = Math.Clamp(affinityBonus + _accumulatedDice + attireBonus + beautyBonus, 1, 15);
        // An authored difficulty above the pool size is unreachable — DialogueSessionState already
        // clamps it, so clamp here too or the overlay would judge by a target the pool cannot meet.
        int difficulty    = Math.Clamp(resolution.Difficulty, 1, diceCount);

        string attireNote = social is { } s2 && attireBonus > 0
            ? $" + attire {attireBonus} for {s2} ({WearingDialogueBonus.Explain(_protagonist, s2)})"
            : "";
        Console.WriteLine(
            $"DialogueTreeController: resolution '{resolution.NodeId}' — {diceCount} dice " +
            $"(affinity {affinityBonus} + replica levels {_accumulatedDice} + beauty {beautyBonus}{attireNote}), " +
            $"difficulty {difficulty}" +
            (difficulty != resolution.Difficulty ? $" (authored {resolution.Difficulty}, clamped to the pool)" : ""));

        _state.StartDiceRoll(diceCount, difficulty);
        _dice.Start(diceCount, difficulty);
        int limit = HumorModifierLimit(_protagonist);
        if (limit > 0) _dice.EnableHumorModifiers(_protagonist.HumorQueues, limit);

        _ = Task.Run(async () =>
        {
            await Task.Delay(Config.Dice.AnimationDurationMs); // Let the animation play
            int[] values = RollResolution(diceCount, difficulty);
            _state.CompleteDiceRoll(values);
            _dice.Complete(values);
            // Same verdict cue as narration: a click so the reveal is heard even with no MIDI device,
            // then the success/failure sting. This roll previously settled in complete silence unless
            // a humor modifier was applied.
            _ambianceEngine?.TriggerGameEvent(Cathedral.Audio.GameEventType.StrongInteraction);
            _ambianceEngine?.TriggerGameEvent(_dice.IsCurrentlySuccess
                ? Cathedral.Audio.GameEventType.PositiveOutcome
                : Cathedral.Audio.GameEventType.NegativeOutcome);
        });
    }

    /// <param name="succeeded">
    /// The final verdict, already including any humor modifiers the player applied during the roll.
    /// Captured by the caller before the overlay is hidden.
    /// </param>
    private void ResolveRoll(bool succeeded)
    {
        var resolution = _pendingResolution;
        _pendingResolution = null;
        if (resolution == null) return;

        Console.WriteLine(
            $"DialogueTreeController: resolution '{resolution.NodeId}' → {(succeeded ? "SUCCESS" : "FAILURE")}");

        _state.IsLoadingReaction = true;

        _ = Task.Run(async () =>
        {
            _preview.Reset();
            // The NPC's closing line streams into a preview box titled with the NPC name, just like its
            // opening lines; XP, outcomes and the conversation end commit on the preview CONTINUE.
            var part = _preview.BeginPart(_npc.DisplayName.ToUpper(), _context.Names.ToReal);

            // The NPC's closing line — one of the node's two authored replicas.
            string neutral = succeeded ? resolution.SuccessReplica : resolution.FailureReplica;
            string reaction;
            try
            {
                reaction = await _replicaWriter.WriteAsync(
                    _npcSlotId, neutral, _context, addresseeRole: "you", subject: _tree.Description,
                    preview: part.Sink);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"DialogueTreeController: Resolution line failed: {ex.Message}");
                reaction = succeeded ? $"{_npc.DisplayName} nods." : $"{_npc.DisplayName} doesn't react.";
            }

            _state.IsLoadingReaction = false;

            void CommitResolution()
            {
                // Award +1 XP to every learned speaking MM that voiced a chosen reply on this branch.
                // Through a report rather than a bare award, so each one earns its own chip below —
                // the experience a conversation teaches used to accrue in complete silence.
                var practiceReports = new List<Outcome>();
                if (succeeded)
                {
                    foreach (var mm in _chosenMMs.DistinctBy(m => m.ModusMentisId))
                    {
                        var practice = ModusMentisPracticeOutcome.For(_protagonist, mm);
                        if (practice == null) continue;
                        practice.ApplyTo(OutcomeContext.For(_protagonist, null, null));
                        if (practice.ShowInUI) practiceReports.Add(practice);
                    }
                }

                AppendNpcLine(reaction);

                // Apply the tree's success or failure outcome set (shared by every branch).
                var reports = new List<Outcome>();
                foreach (var outcome in succeeded ? _tree.SuccessOutcomes : _tree.FailureOutcomes)
                {
                    Console.WriteLine($"DialogueTreeController: applying outcome — {outcome.DisplayName}");
                    // The outcome IS the chip now: applying it settles its own wording, and one
                    // that changed nothing never reports, so ShowInUI keeps it off the screen.
                    outcome.ApplyTo(OutcomeContext.ForDialogue(_npc, _partyMemberId, _protagonist));
                    reports.Add(outcome);
                }

                // The conversation's own lesson, on top of the experience the replies already earned.
                // Only on success: talking your way into a fight teaches nothing about talking.
                if (succeeded)
                {
                    // The tree's own lesson, plus any the branch decided — see
                    // DialogueTree.AdditionalGrantedModusMentisIds.
                    var taught = new List<string?> { _tree.GrantedModusMentisId };
                    taught.AddRange(_tree.AdditionalGrantedModusMentisIds(_npc, resolution));

                    foreach (var id in taught.Distinct())
                    {
                        var lesson = ModusMentisGrantOutcome.For(_protagonist, id);
                        if (lesson == null) continue;
                        lesson.ApplyTo(OutcomeContext.For(_protagonist, null, null));
                        if (lesson.ShowInUI) reports.Add(lesson);
                    }
                }

                _npc.AffinityTable.MarkFirstContact(_partyMemberId);

                // The replies' own practice reads as the coda to what the conversation came to, so
                // it goes after the relation outcomes and after the tree's lesson.
                reports.AddRange(practiceReports);

                // A branch can legitimately change nothing (a failure with no failure-outcome), and
                // silence there reads as a bug — say so plainly instead.
                if (reports.Count == 0)
                {
                    // Applied like any other, even though applying it does nothing: an outcome that
                    // reaches the player should always have gone through ApplyTo, which is what
                    // records it. This was the one that did not, so `expect-outcome` could not see
                    // the only outcome whose whole job is to be seen.
                    var nothing = new NoDialogueConsequenceOutcome(_npc.DisplayName);
                    nothing.ApplyTo(OutcomeContext.ForDialogue(_npc, _partyMemberId, _protagonist));
                    reports.Add(nothing);
                }
                AppendOutcomeReports(reports);

                EndConversation();
            }

            part.AttachCommit(CommitResolution);
            part.MarkComplete();
            _preview.EndProduction();
        });
    }

    private void EndConversation()
    {
        AppendSystemLine("[The conversation has ended.]");
        _state.ConversationEnded = true;
    }
}
