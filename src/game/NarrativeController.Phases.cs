using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cathedral.Audio;
using Cathedral.Game.Dialogue.Tree.Trees;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Preview;
using Cathedral.Game.Narrative.Tree;
using Cathedral.Game.Npc;
using Cathedral.Game.Scene;

namespace Cathedral.Game;

// Where the narration hands over to something else and takes it back: a fight, a conversation, a
// crafting menu; the member played; and the two phases a run opens on.
public partial class NarrativeController
{
    // ── Handing over ─────────────────────────────────────────────────────────

    /// <summary>The tree id of the last conversation queued — <c>inspect dialogue</c>, for scripts.</summary>
    private string _lastDialogueTreeId = "-";

    /// <summary>Queues a conversation for the game controller's next tick. It opens at once: nothing waits on it.</summary>
    private void SetPendingDialogue(DialogueTriggerOutcome outcome)
    {
        _result = null;
        _pendingDialogueOutcome = outcome;
        _lastDialogueTreeId     = (outcome.Tree?.TreeId ?? outcome.TreeId ?? "-")
                                + (CaughtRedHandedTreeFactory.IsCameForward(outcome.Tree) ? " came_forward" : "");
    }

    /// <summary>
    /// What the narration wants to hand over to — a fight, a conversation, a crafting menu — read by
    /// the game controller every tick, and cleared by it once taken.
    /// </summary>
    public PhaseTransition? PendingPhaseTransition
    {
        get
        {
            if (_pendingFightOutcome != null)
                return new StartFightTransition(_pendingFightOutcome.Target, _pendingFightOutcome.CombatContext, _pendingFightOutcome.EnemyInitiative);
            if (_pendingDialogueOutcome != null)
                return new StartDialogueTransition(_pendingDialogueOutcome.Target, _pendingDialogueOutcome.TreeId, _pendingDialogueOutcome.Tree);
            if (_pendingCrafting != null)
                return new StartCraftingTransition(_pendingCrafting);
            return null;
        }
    }

    /// <summary>Clears whichever pending transition was just taken.</summary>
    public void ClearPendingPhaseTransition()
    {
        _pendingFightOutcome    = null;
        _pendingDialogueOutcome = null;
        _pendingCrafting        = null;
    }

    /// <summary>
    /// A conversation is about to be held: what earlier conversations of the visit wrote greys into
    /// history under a rule naming this one. Before the dialogue starts — its setup appends the first
    /// line when it completes, which must land live.
    /// </summary>
    public void BeginConversation(string label) => ScrollBuffer.ConvertToHistory(label);

    /// <summary>
    /// The narration takes the screen back from a fight, a conversation or a menu. Whoever stands
    /// where it happened comes back with a full pool, the people of the place are where the hour has
    /// them, and the trees are brought into line with whatever it changed.
    /// </summary>
    public void ResumeNarration()
    {
        foreach (var mind in _minds.Values.Where(m => m.Pov.Where.Id == _pov.Where.Id))
            mind.Refill();
        ApplyTimePeriod(Period);
    }

    /// <summary>
    /// Called by the game controller when a fight ends. A victory leaves a body — and, for a human,
    /// their belongings — where every enemy fell, and takes the dead out of play for good; a flight
    /// leaves every enemy still standing counting those who fled as enemies.
    /// </summary>
    public void OnFightCompleted(Fight.FightAdapterResult result, NpcEntity npc, IReadOnlyList<NpcEntity>? allEnemyNpcs = null)
    {
        Console.WriteLine($"NarrativeController: fight ended {result} against {npc.DisplayName}");
        var enemies = allEnemyNpcs ?? new List<NpcEntity> { npc };

        if (result == Fight.FightAdapterResult.Victory)
        {
            foreach (var enemy in enemies.Where(e => !e.IsAlive))
            {
                // Each body queues itself on the scene as it is added; the trees take them in when
                // the narration resumes (AdoptCorpses).
                foreach (var remains in enemy.GenerateCorpse())
                    _scene.AddPointOfInterestToArea(_pov.Where, remains);
                // Out of play for good — the same door the slay verb and the two recruit routes use,
                // so a place does not stand its dead back up on the next visit. An enemy with no
                // SceneNpc (a --start-fight creature) was never in the scene and needs no removal.
                if (_scene.Npcs.FirstOrDefault(n => ReferenceEquals(n.Entity, enemy)) is { } fallen)
                    _scene.RemoveNpcFromPlay(fallen);
            }
        }
        else if (result == Fight.FightAdapterResult.Runaway)
        {
            var fled = _minds.Values.Where(m => m.Pov.Where.Id == _pov.Where.Id).Select(m => m.Member.AffinityKey).ToList();
            foreach (var enemy in enemies.Where(e => e.IsAlive))
                foreach (var key in fled)
                    enemy.AffinityTable.SetEnemy(key);
        }
    }

    /// <summary>
    /// Called by the game controller when a conversation ends: what it settled that only the scene
    /// can keep — leave to stay the night, leave to use a workshop — and the end of the confrontation
    /// it may have been.
    /// </summary>
    public void OnDialogueCompleted(NpcEntity npc)
    {
        Console.WriteLine($"NarrativeController: conversation ended with {npc.DisplayName}");

        // A householder who found the member waiting and let them stay: the house is theirs to wait
        // in for the rest of the visit. Read before the release below forgets who found them.
        if (npc.ToleratesTrespass)
        {
            npc.ToleratesTrespass = false;
            if (ConfrontationSelector.IsFoundWaitingBy(_scene, npc) && _scene.SectionOf(_pov.Where) is { } house)
            {
                _scene.ToleratedSections.Add(house);
                Console.WriteLine($"NarrativeController: {npc.DisplayName} lets the player stay — {house.DisplayName} tolerated for the visit");
            }
        }

        // A master who let the member use their workshop: the leave goes into the scene's authorized
        // set, which is the place's saved state by reference, so it holds on every later visit too.
        if (npc.GrantedWorkshop is { } workshop)
        {
            npc.GrantedWorkshop = null;
            _scene.AuthorizedWorkshops.Add(workshop);
            ActionPathPlanner.Forget();
            Console.WriteLine($"NarrativeController: {npc.DisplayName} gives the use of {workshop} — authorized for good");
        }
        npc.PendingWorkshop = null;

        // A witness who came for the member has had their say: the confrontation is over.
        ConfrontationSelector.Release(_scene, npc, _active);
    }

    // ── The member played ────────────────────────────────────────────────────

    /// <summary>
    /// Plays <paramref name="member"/> from now on: their trees are the ones shown, they observe,
    /// think and act. Free, and nothing is said — which is the whole difference from speaking to them.
    /// </summary>
    private void Switch(PartyMember member)
    {
        if (ReferenceEquals(member, _active) || !_minds.ContainsKey(member)) return;
        _active = member;
        _hovered = null;
        _map.Recentre();
        Console.WriteLine($"NarrativeController: now playing {member.DisplayName} in '{_pov.Where.DisplayName}'");
        // Every verb is gated for the body that would perform it.
        ReconcileTrees();
    }

    // ── The childhood reminescence ───────────────────────────────────────────

    /// <summary>
    /// True once the last memory of the childhood has been remembered. Polled by the game controller
    /// to leave <c>GameMode.ChildhoodReminescence</c>.
    /// </summary>
    public bool ReminescencePhaseFinished { get; private set; }

    /// <summary>
    /// How many memories have been remembered so far. Used by the game controller to unlock music
    /// tracks one by one (0 = noise only, 1 = +drone, 2 = +melody, …, 4 = full).
    /// </summary>
    public int ReminescenceCompletedCount { get; private set; }

    /// <summary>
    /// REMEMBER: never fails, never costs, never asks the critic. Like any other act its account is
    /// written by the acting modus mentis — the reminescence's own — so the memory is told in the
    /// character's voice.
    /// </summary>
    private async Task RememberAsync(ThoughtNode actNode)
    {
        var action = actNode.Action!;
        action.DifficultyLevel = 0;
        if (action.PreselectedOutcome.Target is not { } target)
        {
            Console.Error.WriteLine("NarrativeController: REMEMBER has no target — aborting");
            return;
        }

        // Every verb report (skills, items, history, the transition), then the chain's own lesson —
        // REMEMBER rolls nothing, but the modi mentis that observed the fragment, chose to reach for
        // it and voiced the memory did the work all the same. Appended after the fragment's own
        // grants, so a skill the fragment has just taught is already known when its practice report
        // is built.
        //
        // Deliberately uncaught. Every throw on this path today happens while the list is being
        // built, so nothing has been applied and the world is consistent — but that ordering is an
        // accident nothing enforces. Letting it propagate reaches ActAsync's handler, which writes a
        // crash report and stops the narration; stopping is what keeps a half-applied character off
        // disk, since autosave fires on entry to the world map and a phase that cannot finish never
        // gets there.
        var reports = new List<Outcome>(action.Verb.SuccessReports(_scene, _pov, _protagonist, target));
        foreach (var report in reports)
            report.ApplyTo(OutcomeContext.For(_protagonist, _scene, _pov));
        foreach (var chained in action.GetModusMentisChain())
        {
            if (ModusMentisPracticeOutcome.For(_protagonist, chained) is not { } practice) continue;
            practice.ApplyTo(OutcomeContext.For(_protagonist, _scene, _pov));
            reports.Add(practice);
        }

        var actionMind = action.ActionModusMentis ?? action.ChainModusMentis;

        // The neutral outcome is built from the fragment itself — a plain "I tried to remember …, and
        // succeeded" plus the memory recovered — and handed to the narrator as an override, so the
        // rewrite styles the memory rather than the thinking's label for it.
        var fragment = (target as Cathedral.Game.Scene.Reminescence.FragmentPointOfInterest)?.Fragment;
        var neutral = fragment != null ? NeutralNarration.ReminescenceOutcome(fragment.Name, fragment.OutcomeText) : null;
        INarratable forPrompt = fragment != null
            ? new InlineNarratable(fragment.Name, $"remember: {fragment.OutcomeText}")
            : new InlineNarratable("memory", "remember this childhood moment");

        _loading = Config.LoadingMessages.Remembering;
        _previewSession.Reset();
        var part = _previewSession.BeginPart(PreviewTitles.For(actionMind));

        string text;
        try
        {
            text = await _actionExecutor.OutcomeNarrator.NarrateOutcomeAsync(
                action, actionMind, forPrompt, succeeded: true, difficulty: 0.0, _protagonist,
                CancellationToken.None, neutralOverride: neutral, preview: part.Sink);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"NarrativeController: outcome narration failed — {ex.Message}");
            // The sink never completed — the raw memory goes in the box so CONTINUE still appears.
            text = fragment?.OutcomeText ?? "You remember.";
            part.Sink.OnComplete(text);
        }
        _loading = null;

        var chips = reports.Where(r => r.ShowInUI).ToList();
        var block = new NarrationBlock(NarrationBlockType.Outcome, actionMind, text, Actions: null,
                                       ChainOrigin: action, OutcomeReports: chips.Count > 0 ? chips : null);
        var memory = _scene.PendingReminescenceTransition;
        CommitOnContinue(part, () =>
        {
            Chronicle(block);
            actNode.State = ActState.Done;
            actNode.Add(new ThoughtNode(ThoughtNodeKind.Success) { Block = block });
            Mind.Proposals.Withdraw(action.PreselectedOutcome);
            _ambianceEngine?.TriggerGameEvent(GameEventType.PositiveOutcome);
            ShowResult(succeeded: true, OutcomeNarrator.ActionDisplay(action), chips,
                       then: memory == null ? null : () => NextReminescence(memory));
        });
    }

    /// <summary>
    /// The memory remembered leads on: to the next memory, whose scene takes this one's place, or —
    /// at the last — out of the childhood.
    /// </summary>
    private void NextReminescence(ReminescenceTransitionRequest request)
    {
        Console.WriteLine($"NarrativeController: reminescence '{request.FromReminescenceId}' → '{request.NextReminescenceId}'");
        _scene.PendingReminescenceTransition = null;
        ReminescenceCompletedCount++;   // each memory unlocks one more music track

        var registry = Cathedral.Game.Narrative.Reminescence.ReminescenceRegistry.Get(request.NextReminescenceId);
        if (Cathedral.Game.Narrative.Reminescence.ReminescenceRegistry.IsEnd(request.NextReminescenceId) || registry == null)
        {
            if (registry == null && !Cathedral.Game.Narrative.Reminescence.ReminescenceRegistry.IsEnd(request.NextReminescenceId))
                Console.Error.WriteLine($"NarrativeController: unknown reminescence id '{request.NextReminescenceId}', ending phase.");
            ReminescencePhaseFinished = true;
            _requestedExit = true;
            return;
        }

        // The next memory's scene, in the same controller: the childhood is one sitting, and what was
        // remembered so far stays on the map.
        ReplaceMemory(new Cathedral.Game.Scene.Reminescence.ReminescenceSceneFactory(registry).Build(_locationId));
    }

    /// <summary>
    /// Puts the next memory's scene in place of the one just remembered. What hung under "myself"
    /// is carried over to the new scene's: each memory adds its branch, so by the end the tree is the
    /// whole childhood. A fragment looked at and not reached for is kept as it was read, with
    /// nothing left to do about it — its memory has gone by.
    /// </summary>
    private void ReplaceMemory(Scene.Scene next)
    {
        var remembered = Mind.Roots.FirstOrDefault(r => r.Element is Scene.GetUp.SelfPointOfInterest);
        AdoptScene(next);
        if (remembered != null && Mind.Roots.FirstOrDefault(r => r.Element is Scene.GetUp.SelfPointOfInterest) is { } myself)
            foreach (var branch in remembered.Children.ToList())
            {
                remembered.Remove(branch);
                foreach (var act in branch.SelfAndDescendants().Where(n => n.IsWaitingAct))
                    act.State = ActState.Lapsed;
                myself.Add(branch);
            }
        ApplyTimePeriod(TimePeriod.Morning);
        Console.WriteLine($"NarrativeController: scene replaced with reminescence '{next.CurrentReminescenceId}'");
    }

    // ── The get-up ───────────────────────────────────────────────────────────

    /// <summary>
    /// True once GET UP has succeeded. Polled by the game controller to leave <c>GameMode.GetUp</c>
    /// for the world.
    /// </summary>
    public bool GetUpPhaseFinished { get; private set; }

    /// <summary>
    /// GET UP: rolled at a forced difficulty of 1, with no critic and no cost. A failure has no
    /// penalty and the act may be thought of again; a success ends the phase.
    /// </summary>
    private async Task GetUpAsync(ThoughtNode actNode)
    {
        var action = actNode.Action!;
        const int difficulty = 1;
        action.DifficultyLevel = difficulty;

        int dice = Math.Max(1, action.GetTotalModusMentisLevel(_active));
        // Humor modifiers are offered here exactly as in any roll: rising from the ground exhausted is
        // precisely the check a spent humor should be able to swing.
        NarrationDiceStart(dice, difficulty, _active);

        var values = new int[dice];
        for (int i = 0; i < dice; i++) values[i] = _diceRandom.Next(1, 7);
        bool succeeded = values.Count(v => v == 6) >= difficulty;
        Console.WriteLine($"NarrativeController: GetUp dice — {values.Count(v => v == 6)}/{dice} sixes → {(succeeded ? "SUCCESS" : "FAILURE")}");

        // The rolled result is provisional: a humor modifier may still flip it, so the closure reads
        // the field on the dice CONTINUE, not now.
        var actionMind = action.ActionModusMentis ?? action.ChainModusMentis;
        _pendingSucceeded    = succeeded;
        _pendingGetUpOutcome = () => GenerateGetUpOutcomeAsync(actNode, actionMind, _pendingSucceeded, difficulty);

        await Task.Delay(Config.Dice.AnimationDurationMs);
        NarrationDiceComplete(values);
    }

    /// <summary>
    /// Generates the get-up's account after the dice CONTINUE and commits it on the preview's, as
    /// the main path does. <paramref name="succeeded"/> is final, humors applied.
    /// </summary>
    private async Task GenerateGetUpOutcomeAsync(ThoughtNode actNode, ModusMentis actionMind, bool succeeded, int difficulty)
    {
        var action = actNode.Action!;
        try
        {
            INarratable forPrompt = succeeded
                ? new InlineNarratable("getting up", "with great effort you push yourself to your feet and continue your travel")
                : new InlineNarratable("the effort", "your exhausted body refuses to rise — you slump back against the tree");

            _previewSession.Reset();
            var part = _previewSession.BeginPart(PreviewTitles.For(actionMind));

            string narration;
            try
            {
                narration = await _actionExecutor.OutcomeNarrator.NarrateOutcomeAsync(
                    action, actionMind, forPrompt, succeeded,
                    difficulty: CriticTrees.DifficultyLevelToScore(difficulty),
                    _protagonist, CancellationToken.None, preview: part.Sink);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"NarrativeController: GetUp narration failed — {ex.Message}");
                narration = succeeded
                    ? "With great effort, you force yourself to your feet."
                    : "Your body refuses to cooperate. You slump back against the tree.";
                // The sink never completed — the fallback goes in the box so CONTINUE still appears.
                part.Sink.OnComplete(narration);
            }

            // The actual outcome is the act itself, so the verb's own reports fire.
            var result = new ActionExecutionResult
            {
                Action              = action,
                ActionModusMentis   = actionMind,
                ThinkingModusMentis = action.ThinkingModusMentis ?? actionMind,
                Difficulty          = CriticTrees.DifficultyLevelToScore(difficulty),
                DifficultyLevel     = difficulty,
                Succeeded           = succeeded,
                ActualOutcome       = action.PreselectedOutcome,
                Narration           = narration,
            };

            _loading = null;
            part.AttachCommit(() => CommitOutcomeResult(actNode, result, deferredCommit: false));
            part.MarkComplete();
            _previewSession.EndProduction();
        }
        catch (Exception ex)
        {
            Fail("resolving the get-up outcome", ex);
        }
    }

    /// <summary>The body has risen: the phase is over, and the world begins.</summary>
    private void FinishGetUp()
    {
        Console.WriteLine("NarrativeController: Get-Up complete — the protagonist has risen");
        _scene.PendingGetUpTransition = false;
        GetUpPhaseFinished = true;
        _requestedExit = true;
    }
}
