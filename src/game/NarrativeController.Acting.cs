using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cathedral.Audio;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Preview;
using Cathedral.Game.Narrative.Rules;
using Cathedral.Game.Narrative.Tree;
using Cathedral.Game.Npc;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game;

// Carrying an act out: the coded rules, the roll, the outcome and what it stirred, and what the
// outcome then sets going.
public partial class NarrativeController
{
    // Unified dice-roll overlay (animation + humor modifiers + hit-testing).
    private readonly DiceRollComponent _dice = new();

    /// <summary>
    /// The box that shows the hours passing (and, for sitting and waiting, asks how many). Modal over
    /// the map like the dice box; <see cref="_afterTimeBox"/> is what its Continue resumes.
    /// </summary>
    private readonly TimePassageBox _timeBox = new();
    private Action? _afterTimeBox;

    /// <summary>
    /// Set when the passage of the act being committed was already shown — a chosen wait plays it
    /// before its narration — so the commit does not play it a second time.
    /// </summary>
    private bool _passagePlayed;

    // The roll in flight: the act, the evaluation to narrate and the current (humor-modified) result.
    // The outcome is generated only once the player presses the dice CONTINUE, so no narration is
    // produced during the roll.
    private ThoughtNode? _rolledAct;
    private ActionEvaluationResult? _pendingEval;
    private bool _pendingSucceeded;

    // Get-Up defers its outcome narration to the dice CONTINUE (like the main path), so the dice
    // animation runs for the fixed Config.Dice duration instead of blocking on generation.
    private Func<Task>? _pendingGetUpOutcome;

    // What the next tick hands to the game controller: a fight, a conversation, a crafting menu.
    private FightTriggerOutcome? _pendingFightOutcome;
    private DialogueTriggerOutcome? _pendingDialogueOutcome;
    private Cathedral.Game.Narrative.Crafting.CraftingRequest? _pendingCrafting;

    /// <summary>
    /// What an act came to, shown once its account has been read: whether it worked, and the chips —
    /// what was gained, lost, learned, suffered. Its CONTINUE is where whatever the act set going
    /// starts (<see cref="ActResult.Then"/>): a fight the player began, a crafting menu, the way out.
    /// Those wait for it because the chips are the only account of what the blow or the work did, and
    /// the screen that follows paints over the map the frame it opens.
    /// </summary>
    private sealed class ActResult
    {
        public required string Title { get; init; }
        public required bool Succeeded { get; init; }
        public List<Outcome> Chips { get; } = new();
        public Action? Then { get; init; }
    }
    private ActResult? _result;

    // ── Dice-roll lifecycle — the box, and its cues. ─────────────────────────
    // Open: neutral sound. Resolve: positive or negative. Close: neutral.

    private void NarrationDiceStart(int numberOfDice, int difficulty, PartyMember? humorMember = null,
        string? subtitle = null, string difficultyVerb = "to hit")
    {
        _dice.Start(numberOfDice, difficulty, subtitle, difficultyVerb);
        if (humorMember != null)
        {
            int limit = HumorModifierLimit(humorMember);
            if (limit > 0) _dice.EnableHumorModifiers(humorMember.HumorQueues, limit);
        }
        _ambianceEngine?.TriggerGameEvent(GameEventType.NeutralOutcome);
    }

    /// <summary>
    /// Settles the roll: the PCM click, then the success or failure sting. The click is what
    /// guarantees the reveal is heard at all — the stings are MIDI and go silent without an open
    /// device.
    /// </summary>
    private void NarrationDiceComplete(int[] finalValues)
    {
        _dice.Complete(finalValues);
        PlayClickSound();
        _ambianceEngine?.TriggerGameEvent(_dice.IsCurrentlySuccess
            ? GameEventType.PositiveOutcome : GameEventType.NegativeOutcome);
    }

    private void NarrationDiceClear()
    {
        bool wasActive = _dice.IsVisible;
        _dice.Hide();
        if (wasActive) _ambianceEngine?.TriggerGameEvent(GameEventType.NeutralOutcome);
    }

    /// <summary>Per-roll humor modifier budget from the viscera <c>humor_modifier_limit</c> stat.</summary>
    private static int HumorModifierLimit(PartyMember member)
        => member.DerivedStats.First(s => s.Name == "humor_modifier_limit").GetValue(member);

    /// <summary>
    /// Fired by the dice component when a humor modifier flips success and failure. Records the new
    /// result (the outcome is generated later, on the dice CONTINUE) and replays the cue.
    /// </summary>
    private void OnDiceOutcomeFlipped(bool nowSuccess)
    {
        _pendingSucceeded = nowSuccess;
        _ambianceEngine?.TriggerGameEvent(nowSuccess
            ? GameEventType.PositiveOutcome : GameEventType.NegativeOutcome);
    }

    // ── The coded rules ──────────────────────────────────────────────────────

    /// <summary>
    /// The context the coded action rules judge <paramref name="action"/> in
    /// (<c>ActionRulesChecker.Check</c>), as if the member stood in <paramref name="where"/>: witnesses
    /// (when the act is illegal), threat, and a confronting witness. Reads the scene and draws nothing.
    /// </summary>
    private ActionRuleContext BuildActionRuleContext(ParsedNarrativeAction action, Area where)
    {
        var pov = new PoV(where, Period);
        // Contextual: the verb, its target and who counts the actor an enemy all speak to it.
        bool illegal = action.Verb.IsIllegal(_scene, pov, action.PreselectedOutcome?.Target, _active);
        // Witnesses (visual = same area, audio = within earshot) matter to an illegal act only.
        var witnesses = illegal ? WitnessSelector.ComputeContext(_scene, pov) : WitnessContext.None;
        var threat = ThreatSelector.ComputeContext(_scene, pov, _active);
        // A witness confronting the member: one who came for them, or anyone in sight on private
        // ground. An enemy in the room outranks them, as everywhere else.
        var confrontation = ConfrontationSelector.Current(_scene, pov, _active);
        return new ActionRuleContext(action, _active, _scene, pov, witnesses, threat)
        {
            Confronter = confrontation is { IsWitness: true } ? confrontation.Npc : null,
        };
    }

    /// <summary>
    /// The action rules asked ahead of the click, for Reconsider: the refusal the click would give, or
    /// null when the act would pass. The rules that read a combined item are not asked
    /// (<c>ActionRulesChecker.Probe</c>).
    /// </summary>
    private string? ProbeActionRules(ParsedNarrativeAction action, Area where)
    {
        var result = ActionRulesChecker.Probe(BuildActionRuleContext(action, where));
        return result.Passed ? null : (result.ErrorMessage ?? "");
    }

    /// <summary>
    /// A coded refusal shown without re-voicing — no modus mentis to voice it, or the rewrite came
    /// back empty. A missing tool keeps its longer sentence, which is the part worth reading.
    /// </summary>
    private static string RawRefusal(ParsedNarrativeAction action, ActionRuleResult result)
        => result.NeededTool != null
            ? $"[IMPOSSIBLE] {OutcomeNarrator.RefusalNeutral(action, result.ErrorMessage ?? "", result.NeededTool)}"
            : $"[IMPOSSIBLE] {result.ErrorMessage}";

    // ── Carrying an act out ──────────────────────────────────────────────────

    /// <summary>
    /// Carries out the act of <paramref name="actNode"/>, the member standing where it is to be done:
    /// the coded rules, then the roll. A refusal by rule or as implausible is a node under the act,
    /// costs a point and leaves the act standing; the roll's outcome is generated on the dice
    /// CONTINUE (<see cref="OnDiceRollContinue"/>).
    /// </summary>
    private async Task ActAsync(ThoughtNode actNode)
    {
        var action = actNode.Action!;
        try
        {
            Console.WriteLine($"NarrativeController: carrying out '{action.ActionText}'");
            _cliLastExecutedVerbId = action.PreselectedOutcome?.Verb.VerbId;

            // The played member performs the act: their skills, organ scores, experience, wounds and
            // what it consumes are theirs.
            _actionExecutor.ActingMember = _active;

            // In the childhood reminescence REMEMBER succeeds outright: no critic, no dice, no cost.
            if (_scene.Phase == NarrationPhase.ChildhoodReminescence)
            {
                await RememberAsync(actNode);
                return;
            }
            // GET UP skips the critic and is rolled at difficulty 1.
            if (_scene.Phase == NarrationPhase.GetUp)
            {
                await GetUpAsync(actNode);
                return;
            }

            if (DebugMode.IsActive)
                DebugMode.PromptActionStrategy(action.ActionText,
                    $"{action.PreselectedOutcome.GetType().Name} → {action.PreselectedOutcome.DisplayName}");

            // === The coded rules: fast, deterministic, absolute ===
            var ruleCtx = BuildActionRuleContext(action, _pov.Where);
            var ruleResult = ActionRulesChecker.Check(ruleCtx);
            if (!ruleResult.Passed)
            {
                Console.WriteLine($"NarrativeController: coded rule blocked the act — {ruleResult.ErrorMessage}");
                // A refused act stays lit: Reconsider may still give it to a means the rule lets
                // through (a discreet one past a witness), and a missing tool leaves Use Tool as the
                // way on.
                if (ruleResult.NeededTool != null) action.NeedsTool = true;

                // Voiced by the acting modus mentis when one is resolvable; the raw rule otherwise.
                var refusalMind = _active.ModiMentis.FirstOrDefault(m => m.ModusMentisId == action.ActionModusMentisId)
                                  ?? action.ActionModusMentis;
                _previewSession.Reset();
                var part = refusalMind != null ? _previewSession.BeginPart(PreviewTitles.For(refusalMind)) : null;
                string text = refusalMind == null ? "" : await _actionExecutor.OutcomeNarrator.NarrateRefusalAsync(
                    action, refusalMind, ruleResult.ErrorMessage ?? "", _active, CancellationToken.None,
                    preview: part?.Sink, neededTool: ruleResult.NeededTool);
                if (string.IsNullOrWhiteSpace(text)) text = RawRefusal(action, ruleResult);

                Refuse(actNode, part, new NarrationBlock(NarrationBlockType.Outcome, refusalMind ?? action.ThinkingModusMentis, text, Actions: null)
                    { Attempt = new ActionAttempt(OutcomeNarrator.ActionDisplay(action), Succeeded: false) });
                return;
            }

            // === Evaluation ===
            _loading = Config.LoadingMessages.EvaluatingAction;
            var eval = await _actionExecutor.EvaluateActionAsync(
                action, NodeForArea(_pov.Where), action.ThinkingModusMentis,
                ruleCtx.WitnessContext, ruleCtx.ThreatContext, CancellationToken.None);
            eval.Confronter = ruleCtx.Confronter;

            if (!eval.IsPlausible)
            {
                Console.WriteLine("NarrativeController: the act failed the plausibility check");
                _previewSession.Reset();
                var part = eval.ActionModusMentis != null ? _previewSession.BeginPart(PreviewTitles.For(eval.ActionModusMentis)) : null;
                var implausible = await _actionExecutor.GeneratePlausibilityFailureNarrationAsync(
                    eval, CancellationToken.None, preview: part?.Sink);
                Refuse(actNode, part, new NarrationBlock(NarrationBlockType.Outcome,
                        implausible.ActionModusMentis ?? throw new InvalidOperationException("the act has no acting modus mentis"),
                        $"[IMPOSSIBLE] {implausible.Narration}", Actions: null)
                    { Attempt = new ActionAttempt(OutcomeNarrator.ActionDisplay(action), Succeeded: false) });
                return;
            }

            // What a failure penalty will need to take hold: the pool as the thinking left it (losing
            // focus needs a point to lose), the hour (wasting it needs one after), and a way out to
            // wander off by — which only a real place has.
            eval.Situation = new FailureSituation(_active, _pov, Mind.Noetic, CanLoseTheWay: Exploring);
            _rolledAct   = actNode;
            _pendingEval = eval;

            // === Difficulty 0: no roll ===
            // An act that cannot fail (a plain verb, or a difficulty-1 one an eager means took on)
            // succeeds outright: no dice, no humor layer, no failure penalty, and no strategy
            // override — `fail-dice` cannot fail what is not rolled. Everything after the roll is the
            // ordinary success path.
            if (eval.DifficultyLevel == 0)
            {
                Console.WriteLine($"NarrativeController: difficulty 0 — '{action.ActionText}' succeeds with no roll");
                _pendingSucceeded = true;
                _loading = null;
                OnDiceRollContinue();
                return;
            }

            // === The roll ===
            // As many dice as the chain's levels: the mind that observed, the one that thought, the
            // one that acts, and an implement combined.
            int dice = Math.Max(1, action.GetTotalModusMentisLevel(_active));
            int difficulty = eval.DifficultyLevel;

            // The forced-outcome branch runs before the animation starts because a forced success may
            // have to lower the difficulty, and the animation is drawn against it.
            int[] values;
            bool succeeded;
            if (DebugMode.IsActive && !DebugMode.IsAutoStrategy)
            {
                succeeded = DebugMode.GetDiceRollOverride(action.ActionText, dice, difficulty);
                // A forced success has to be a roll the pool can show. A verb harder than the chain
                // that reached it needs more sixes than there are dice, which no arrangement
                // satisfies: cap the demand at the pool.
                if (succeeded && difficulty > dice)
                {
                    Console.WriteLine($"NarrativeController: forced success needs {difficulty} sixes from {dice} dice — capped at {dice}");
                    difficulty = dice;
                }
                values = GenerateDiceValuesForResult(dice, difficulty, succeeded);
            }
            else
            {
                values = new int[dice];
                for (int i = 0; i < dice; i++) values[i] = _diceRandom.Next(1, 7);
                succeeded = values.Count(v => v == 6) >= difficulty;
            }

            NarrationDiceStart(dice, difficulty, _active);
            _loading = null;
            _pendingSucceeded = succeeded;
            Console.WriteLine($"NarrativeController: rolled {values.Count(v => v == 6)} sixes of {dice} dice (need {difficulty}) → "
                            + $"{(succeeded ? "SUCCESS" : "FAILURE")} (humor may change this)");

            // No narration backs this roll, so pause as the runaway and fight rolls do to let it play.
            await Task.Delay(Config.Dice.AnimationDurationMs);
            NarrationDiceComplete(values);
        }
        catch (Exception ex)
        {
            Fail("executing an action", ex);
        }
    }

    /// <summary>
    /// An act refused before any roll — by a coded rule, or as implausible: the refusal hangs under
    /// the act, which stays where it was, and the attempt costs a point.
    /// </summary>
    private void Refuse(ThoughtNode actNode, PreviewPart? part, NarrationBlock refusal)
    {
        _loading = null;
        CommitOnContinue(part, () =>
        {
            Chronicle(refusal);
            actNode.Add(new ThoughtNode(ThoughtNodeKind.Refusal) { Block = refusal });
        });
        SpendPoint();
    }

    /// <summary>Dice values that show the given result.</summary>
    private int[] GenerateDiceValuesForResult(int numberOfDice, int difficulty, bool succeeded)
    {
        int[] values = new int[numberOfDice];

        if (succeeded)
        {
            // At least `difficulty` sixes — but never more than there are dice. The loop below places
            // one six a pass and has nowhere to put a surplus, so an unclamped demand spins for ever.
            // Callers clamp too; this is the backstop, because a hang here freezes the whole game
            // with nothing on screen to say why.
            int sixesNeeded = Math.Min(difficulty, numberOfDice);
            int sixesPlaced = 0;
            for (int i = 0; i < numberOfDice; i++)
            {
                values[i] = _diceRandom.Next(1, 7);
                if (values[i] == 6) sixesPlaced++;
            }
            while (sixesPlaced < sixesNeeded)
            {
                int idx = _diceRandom.Next(numberOfDice);
                if (values[idx] == 6) continue;
                values[idx] = 6;
                sixesPlaced++;
            }
        }
        else
        {
            // Fewer than `difficulty` sixes.
            int sixesLeft = difficulty - 1;
            for (int i = 0; i < numberOfDice; i++)
            {
                values[i] = _diceRandom.Next(1, 7);
                if (values[i] == 6 && sixesLeft-- <= 0) values[i] = _diceRandom.Next(1, 6);
            }
        }

        // Shuffle for a natural appearance.
        for (int i = values.Length - 1; i > 0; i--)
        {
            int j = _diceRandom.Next(i + 1);
            (values[i], values[j]) = (values[j], values[i]);
        }
        return values;
    }

    // ── After the roll ───────────────────────────────────────────────────────

    /// <summary>
    /// The dice box's CONTINUE (and where an act of difficulty 0 goes straight to): the outcome is
    /// generated now, streamed into a preview box, and committed on that box's CONTINUE.
    /// </summary>
    private void OnDiceRollContinue()
    {
        // Get-Up: the closure reads the final _pendingSucceeded, so a humor modifier applied during
        // the roll is honoured.
        if (_pendingGetUpOutcome != null)
        {
            var generate = _pendingGetUpOutcome;
            _pendingGetUpOutcome = null;
            NarrationDiceClear();
            _loading = Config.LoadingMessages.NarratingOutcome;
            _ = Task.Run(generate);
            return;
        }

        if (_pendingEval == null || _rolledAct == null)
        {
            NarrationDiceClear();
            return;
        }

        var eval = _pendingEval;
        var act = _rolledAct;
        bool succeeded = _pendingSucceeded;
        _pendingEval = null;
        _rolledAct = null;
        NarrationDiceClear();

        // A wait that succeeded asks how long first: the hour waited until is in the narration.
        if (succeeded && eval.Action.Verb is SitAndWaitVerb)
        {
            AskWaitUntil(act, eval);
            return;
        }

        _loading = Config.LoadingMessages.NarratingOutcome;
        _ = Task.Run(() => GenerateOutcomePreviewAsync(act, eval, succeeded));
    }

    /// <summary>
    /// Asks which period a successful wait lasts until, then plays the passage to it; the outcome is
    /// narrated once the player presses Continue. The choice is <see cref="Scene.WaitUntil"/>, which
    /// <see cref="SitAndWaitVerb"/> reads for its time shift.
    /// </summary>
    private void AskWaitUntil(ThoughtNode act, ActionEvaluationResult eval)
    {
        var from = _pov.When;
        // A will's wait was settled by its path: the hour it needs, with nothing to ask.
        if (eval.Action.PreselectedOutcome is { Variant: TimePeriod settled })
        {
            Console.WriteLine($"NarrativeController: a will's wait — until {settled}, not asked");
            WaitChosen(settled);
            return;
        }
        _timeBox.OnChosen = WaitChosen;
        _timeBox.ShowChoice(from, Enumerable.Range(1, 5).Select(i => from.Advance(i)));

        void WaitChosen(TimePeriod chosen)
        {
            var (until, note) = WaitStopsAt(from, chosen);
            _scene.WaitUntil = until;
            _passagePlayed = true;
            PlayClickSound();
            Console.WriteLine($"NarrativeController: waiting until {until}" + (until != chosen ? $" (chose {chosen})" : ""));
            ShowTimePassage(from, until, note, then: () =>
            {
                _loading = Config.LoadingMessages.NarratingOutcome;
                _ = Task.Run(() => GenerateOutcomePreviewAsync(act, eval, succeeded: true));
            });
        }
    }

    /// <summary>
    /// Where a wait chosen to last until <paramref name="chosen"/> actually ends. A wait through the
    /// night, somewhere the member is trespassing in a house that has somebody to come home to it,
    /// ends at night: that is when the householder comes in and finds them.
    /// </summary>
    private (TimePeriod Until, string? Note) WaitStopsAt(TimePeriod from, TimePeriod chosen)
    {
        int steps   = TimePassageBox.Steps(from, chosen);
        int toNight = TimePassageBox.Steps(from, TimePeriod.Night);
        if (chosen != TimePeriod.Night && toNight > 0 && toNight < steps && Exploring
            && PrivacyModel.IsTrespassing(_scene, _pov.Where)
            && ConfrontationSelector.Householder(_scene, _pov.Where, _active.AffinityKey) != null)
            return (TimePeriod.Night, "Somebody comes in before the night is out.");
        return (chosen, null);
    }

    /// <summary>Plays a passage of the hours; <paramref name="then"/> runs when its Continue is pressed.</summary>
    private void ShowTimePassage(TimePeriod from, TimePeriod to, string? note = null, Action? then = null)
    {
        _timeBox.ShowPassage(from, to, note);
        _afterTimeBox = then;
    }

    private void OnTimePassageContinue()
    {
        PlayClickSound();
        _timeBox.Hide();
        var then = _afterTimeBox;
        _afterTimeBox = null;
        then?.Invoke();
    }

    /// <summary>
    /// Moves the calendar on when the period has wrapped from night into dawn: a day has turned. The
    /// visit's periods and the world's days are otherwise separate clocks (<see cref="GameClock"/>),
    /// and a passage never spans a full day, so one wrap is one day.
    /// </summary>
    private static void TurnTheDayIfWrapped(TimePeriod before, TimePeriod after)
    {
        if ((int)after >= (int)before) return;
        GameClock.Advance(1);
        Console.WriteLine($"NarrativeController: night turned to dawn — day {(int)GameClock.Days}");
    }

    /// <summary>
    /// Generates the single true outcome after the dice, streaming it into the preview box; the node
    /// and every consequence come on the preview's CONTINUE.
    /// </summary>
    private async Task GenerateOutcomePreviewAsync(ThoughtNode act, ActionEvaluationResult eval, bool succeeded)
    {
        try
        {
            _previewSession.Reset();
            var part = _previewSession.BeginPart(eval.ActionModusMentis != null ? PreviewTitles.For(eval.ActionModusMentis) : "OUTCOME");
            // The verb's reports are gathered up front so their verbatims can be woven into the
            // narration; the same instances are applied at commit.
            var verbReports = GatherVerbReports(eval.Action.PreselectedOutcome, succeeded, eval.Action.CombinedItem);
            var result = await _actionExecutor.PrepareSingleOutcomeAsync(eval, succeeded, verbReports, part.Sink, CancellationToken.None);
            _loading = null;
            part.AttachCommit(() => CommitOutcomeResult(act, result, deferredCommit: true));
            part.MarkComplete();

            // The emotional coda. Resolved from the reports that are about to be applied rather than
            // from applied ones: a commit happens on the player's press, and the emotion's own
            // generation is supposed to be running ahead of it. A second part rather than a longer
            // first one: it is another mind in another slot, and the box gates one part a CONTINUE.
            await GenerateEmotionPreviewAsync(act, result.OutcomeReports ?? verbReports, result.LlmDecidedReports);

            _previewSession.EndProduction();
        }
        catch (Exception ex)
        {
            Fail("resolving an outcome", ex);
        }
    }

    /// <summary>
    /// Streams the emotional coda into a preview part of its own, when what the act produced stirred
    /// anything in the body that produced it.
    ///
    /// <para><b>Silent by default and by design.</b> Most acts move nothing a disposition cares
    /// about, and <see cref="EmotionResolver.Resolve"/> returns null for those — no part is begun, no
    /// request is made, and the player presses CONTINUE once as they always did.</para>
    ///
    /// <para>The commit does the two things in the order the player sees them: the humors reach the
    /// spleen (<see cref="EmotionOutcome"/>'s own Apply) and the node lands carrying its chip.</para>
    ///
    /// <para>A failure here costs the coda and nothing else. An emotion is a garnish on an act that
    /// has already resolved, and taking the narration down over one would turn a decorative request
    /// into a third way for a visit to end — so this path swallows its exception.</para>
    /// </summary>
    private async Task GenerateEmotionPreviewAsync(ThoughtNode act,
        IReadOnlyList<Outcome>? verbReports, IReadOnlyList<Outcome>? llmDecidedReports)
    {
        var all = new List<Outcome>();
        if (verbReports       != null) all.AddRange(verbReports);
        if (llmDecidedReports != null) all.AddRange(llmDecidedReports);

        var felt = EmotionResolver.Resolve(_active, all);
        if (felt == null) return;
        var emotion = felt.Value;

        try
        {
            _loading = Config.LoadingMessages.NarratingOutcome;
            var part = _previewSession.BeginPart(PreviewTitles.For(emotion.ModusMentis));
            string text = await _actionExecutor.OutcomeNarrator.NarrateEmotionAsync(emotion, CancellationToken.None, part.Sink);
            _loading = null;

            var chip = emotion.ToOutcome();
            part.AttachCommit(() =>
            {
                chip.ApplyTo(OutcomeContext.For(_active, _scene, _pov));
                var block = new NarrationBlock(NarrationBlockType.Emotion, emotion.ModusMentis, text, Actions: null,
                                               OutcomeReports: new List<Outcome> { chip });
                Chronicle(block);
                // Under the outcome it answers, and on the result that outcome is being read from.
                (act.Children.LastOrDefault(c => c.Kind is ThoughtNodeKind.Success or ThoughtNodeKind.Failure) ?? act)
                    .Add(new ThoughtNode(ThoughtNodeKind.Emotion) { Block = block });
                _result?.Chips.Add(chip);
                Console.WriteLine($"NarrativeController: emotion — {emotion.ModusMentis.ModusMentisId} felt {chip.Humor.Name} x{emotion.Count} (spleen)");
            });
            part.MarkComplete();
        }
        catch (Exception ex)
        {
            _loading = null;
            Console.Error.WriteLine($"NarrativeController: emotion narration failed, skipping the coda: {ex.Message}");
        }
    }

    /// <summary>
    /// Gathers the verb's own reports for a resolved act. Success reports carry the chosen
    /// <see cref="VerbAction"/> (so verbs that expanded into several acts read their variant) and the
    /// implement combined, if any — which only <c>attack</c> reads. Failure reports use the
    /// target-only overload.
    /// </summary>
    private IReadOnlyList<Outcome> GatherVerbReports(INarratable? outcome, bool succeeded, Item? tool = null)
    {
        if (outcome is not VerbAction { Target: not null } goal) return Array.Empty<Outcome>();
        var verb = goal.Verb;
        if (!succeeded) return verb.FailureReports(_scene, _pov, _active, goal.Target);

        var reports = new List<Outcome>(verb.SuccessReports(_scene, _pov, _active, goal.Target, goal, tool));
        // Doing a thing is how the thing is learned. Appended last so the lesson reads as the
        // consequence of whatever the verb did, not as its headline.
        var lesson = ModusMentisGrantOutcome.For(_active, ChooseLesson(verb, goal.Target, goal)?.ModusMentisId);
        if (lesson != null) reports.Add(lesson);
        return reports;
    }

    /// <summary>
    /// The one lesson this act teaches. The whole decision belongs to the verb — see
    /// <see cref="Verb.ResolveLesson"/>, which asks the verb's own contextual answer first, then the
    /// target's declaration, then the verb's defaults, skipping whatever this body cannot hold.
    /// </summary>
    private ModusMentis? ChooseLesson(Verb verb, Element target, VerbAction? action = null)
        => verb.ResolveLesson(new LessonContext(_scene, _pov, _active, target)
        {
            Hostile = ThreatSelector.ComputeContext(_scene, _pov, _active).Level,
            Action  = action,
        });

    /// <summary>
    /// Applies a resolved act: the chain's practice, the outcome's reports, the outcome node, the
    /// hour and the walk the act moved, and whatever it set going — a fight, a conversation, a
    /// crafting menu, the next memory. <paramref name="deferredCommit"/> also settles what only the
    /// rolled path produced, the speculative branch's narration history.
    /// </summary>
    private void CommitOutcomeResult(ThoughtNode actNode, ActionExecutionResult result, bool deferredCommit)
    {
        Console.WriteLine($"NarrativeController: committing {(result.Succeeded ? "SUCCESS" : "FAILURE")}");
        var owner = Mind;
        var action = result.Action;

        // Collect every report: the verb's own and the decided ones (a wound). Reports gathered up
        // front for the narration are reused — gathering again would run an item factory twice.
        List<Outcome> reports;
        if (result.OutcomeReports != null)
            reports = new List<Outcome>(result.OutcomeReports);
        else
        {
            reports = new List<Outcome>(GatherVerbReports(result.ActualOutcome, result.Succeeded, action?.CombinedItem));
            reports.AddRange(result.LlmDecidedReports);
        }

        // The dice chain's own lesson, on every success: one report per modus mentis that fed the
        // roll, each with its own chip. Last, as the quiet coda to whatever the verb did.
        if (result.Succeeded && action != null)
            foreach (var chained in action.GetModusMentisChain())
                if (ModusMentisPracticeOutcome.For(_active, chained) is { } practice)
                    reports.Add(practice);

        // Only the chosen branch's narration stays in the narrator's history.
        if (deferredCommit) _actionExecutor.OutcomeNarrator.CommitNarrationHistory(result.Succeeded);

        // The recorder sees a successful verb before its reports apply: a gathering routine is
        // learned off the source while the item is still on it, from where the member stood to take it.
        if (result.Succeeded && action != null && result.ActualOutcome is VerbAction)
            _recorder?.OnVerbSucceeded(action, _scene, _pov);

        // Where and when the member was before the reports apply: an act may move either.
        var areaBefore   = _pov.Where;
        var periodBefore = _pov.When;
        // Whether the way about to be taken leads somewhere the party has already stood by a way it
        // has not: the lesson of finding one's way back (Verb.ResolveLesson reads the visits).
        foreach (var report in reports)
            report.ApplyTo(OutcomeContext.For(_active, _scene, _pov));

        // The two failure penalties the scene cannot carry out itself, because what they change is
        // the narration's: the noetic pool, and whether the visit goes on.
        if (_scene.PendingFocusLoss > 0)
        {
            owner.Noetic = Math.Max(0, owner.Noetic - _scene.PendingFocusLoss);
            _scene.PendingFocusLoss = 0;
            Console.WriteLine($"NarrativeController: focus lost — {owner.Noetic} noetic point(s) left");
        }
        bool lostWay = _scene.PendingLostWay;
        _scene.PendingLostWay = false;

        // A verb that shifted the period only wrote the member's point of view; route it back through
        // the single writer so placement, verb gating and every member follow.
        if (periodBefore != _pov.When)
        {
            var now = _pov.When;
            Console.WriteLine($"NarrativeController: time of day advanced {periodBefore} → {now}");
            TurnTheDayIfWrapped(periodBefore, now);
            if (!_passagePlayed) ShowTimePassage(periodBefore, now);
            ApplyTimePeriod(now);

            // Night came round on somebody waiting where they had no business to: the householder
            // comes to bed and finds them. Sitting out the hours and an hour wasted both bring it on;
            // hiding does not, since the whole point of hiding is not being found.
            if (now == TimePeriod.Night && Exploring && action?.Verb is not HideAndWaitVerb)
                foreach (var mind in _minds.Values)
                    ConfrontationSelector.SummonHouseholder(_scene, mind.Pov, mind.Member.AffinityKey);
        }
        _passagePlayed = false;
        _scene.WaitUntil = null;

        // An act that moved the member: the area is on the map from now on, with the way it was
        // reached, and is a place the protagonist may come back to.
        if (areaBefore.Id != _pov.Where.Id && Exploring)
        {
            _recorder?.OnAreaEntered(_pov);
            MoveOnMap(areaBefore, _pov.Where, action?.PreselectedOutcome?.Target);
        }

        // The outcome, under its act.
        var chips = reports.Where(r => r.ShowInUI).ToList();
        var outcomeBlock = new NarrationBlock(
            NarrationBlockType.Outcome,
            result.ActionModusMentis ?? throw new InvalidOperationException("the act has no acting modus mentis"),
            $"[{(result.Succeeded ? "SUCCESS" : "FAILURE")}] {result.Narration}",
            Actions: null,
            OutcomeReports: chips.Count > 0 ? chips : null)
        { Attempt = action == null ? null : new ActionAttempt(OutcomeNarrator.ActionDisplay(action), result.Succeeded) };
        Chronicle(outcomeBlock);
        actNode.State = ActState.Done;
        actNode.Add(new ThoughtNode(result.Succeeded ? ThoughtNodeKind.Success : ThoughtNodeKind.Failure) { Block = outcomeBlock });
        if (action != null) owner.Proposals.Withdraw(action.PreselectedOutcome);
        _ambianceEngine?.TriggerGameEvent(result.Succeeded ? GameEventType.PositiveOutcome : GameEventType.NegativeOutcome);
        NarrationDiceClear();
        _ambianceEngine?.SetFilter(MusicFilter.None);

        // A success refills the pool; a failure costs the point it spent and leaves the member to
        // think again with what is left, which is what makes a point worth spending at all.
        if (result.Succeeded && !InOpeningPhase) owner.Refill();

        // The scene has changed: the trees follow it before anything else reads them.
        var fightRequest    = _scene.PendingFightRequest;
        var craftingRequest = _scene.PendingCraftingRequest;
        var dialogueRequest = _scene.PendingDialogueRequest;
        _scene.PendingFightRequest    = null;
        _scene.PendingCraftingRequest = null;
        _scene.PendingDialogueRequest = null;
        ReconcileTrees();

        // === A fight asked for by the act itself ===
        // Read off the scene rather than off the outcome, because a report is what asks for it and
        // reports are applied for a success and a failure alike. A fight the player started outranks
        // the one their failure invited: swing at somebody and miss in front of a witness, and the
        // brawl is the story, not the conversation the miss would otherwise have opened. It waits for
        // the result's CONTINUE — the chips are the only account of what the blow did.
        Action? then = null;
        if (fightRequest != null)
        {
            var fight = new FightTriggerOutcome(fightRequest.Npc, $"attack on {fightRequest.Npc.DisplayName}")
            {
                EnemyInitiative = !result.Succeeded,
            };
            Console.WriteLine($"NarrativeController: fight with {fightRequest.Npc.DisplayName} held for CONTINUE"
                            + (result.Succeeded ? "" : " (missed — enemy initiative)"));
            then = () => _pendingFightOutcome = fight;
        }
        // === The failure's social consequence ===
        // Three rungs of one ladder, decided by the executor from effective proximity. At most one is
        // set. Seen by an enemy: they attack, with the initiative, and at once.
        else if (!result.Succeeded && result.FightWithEnemy != null)
        {
            Console.WriteLine($"NarrativeController: enemy '{result.FightWithEnemy.DisplayName}' attacks after a failed act in plain sight");
            _pendingFightOutcome = new FightTriggerOutcome(result.FightWithEnemy, $"opportunity attack by {result.FightWithEnemy.DisplayName}")
            {
                EnemyInitiative = true,
            };
            return;
        }
        // Seen by a witness: they confront the member, and the tree decides what that becomes.
        else if (!result.Succeeded && result.CaughtByWitness != null)
        {
            var catchTree = ConfrontationSelector.TreeFor(_scene, result.CaughtByWitness, action!.Verb, _pov.Where.IsPrivate);
            Console.WriteLine($"NarrativeController: witness '{result.CaughtByWitness.DisplayName}' caught the failed act ({catchTree.TreeId})");
            SetPendingDialogue(new DialogueTriggerOutcome(result.CaughtByWitness, tree: catchTree));
            return;
        }
        // Only heard, from a room away: they come to look. That rung is NpcDrawnInOutcome, applied
        // with the other reports above, and the trees have already taken them in.
        else if (craftingRequest != null)
        {
            Console.WriteLine($"NarrativeController: USE opens the crafting menu at '{craftingRequest.Station.DisplayName}' — held for CONTINUE");
            then = () => _pendingCrafting = craftingRequest;
        }
        else if (dialogueRequest != null)
        {
            Console.WriteLine($"NarrativeController: a conversation with {dialogueRequest.Npc.DisplayName} ('{dialogueRequest.Tree?.TreeId ?? dialogueRequest.TreeId}')");
            SetPendingDialogue(new DialogueTriggerOutcome(dialogueRequest.Npc, dialogueRequest.TreeId, dialogueRequest.Tree));
            return;
        }
        else if (_scene.PendingGetUpTransition)
            then = FinishGetUp;
        else if (_scene.PendingReminescenceTransition is { } memory)
            then = () => NextReminescence(memory);
        else if (lostWay)
        {
            Console.WriteLine("NarrativeController: the way is lost — the visit ends");
            then = () => _requestedExit = true;
        }

        ShowResult(result.Succeeded, action == null ? "" : OutcomeNarrator.ActionDisplay(action), chips, then);
    }

    /// <summary>
    /// Puts the result of an act up: what it came to and its chips, with what follows on its
    /// CONTINUE. With no chip to read the box is not shown and what follows starts at once.
    /// </summary>
    private void ShowResult(bool succeeded, string act, IReadOnlyList<Outcome> chips, Action? then)
    {
        if (chips.Count == 0)
        {
            _result = null;
            then?.Invoke();
            return;
        }
        _result = new ActResult
        {
            Title     = $"{(succeeded ? "SUCCESS" : "FAILURE")}{(act.Length > 0 ? $" — {act}" : "")}",
            Succeeded = succeeded,
            Then      = then,
        };
        _result.Chips.AddRange(chips);
    }

    private void OnResultContinue()
    {
        PlayClickSound();
        var result = _result;
        _result = null;
        result?.Then?.Invoke();
    }

    // ── Implements ───────────────────────────────────────────────────────────

    /// <summary>
    /// The tools that may be combined with an act. <see cref="ItemCategory.Tool"/> and
    /// <see cref="ItemCategory.Weapon"/> qualify — a weapon is a tool too, just a specialised and
    /// often clumsy one — plus anything declaring <see cref="Item.MadeForVerbIds"/>, whatever its
    /// category: an item made for an act is by definition something worked with (reading lenses are
    /// a garment by category and the whole reason the mechanism exists).
    ///
    /// <para>Where it is carried is not a factor. A container holding something is excluded, since
    /// combining it with an act would mean putting its contents down.</para>
    /// </summary>
    private List<Item> GetCombinableItems()
        => _active.GetAllItems().Where(ToolCombinationRules.IsCombinable).ToList();

    /// <summary>
    /// Combines <paramref name="item"/> with the act of <paramref name="actNode"/>: the gates decide
    /// whether it may serve (three of the four with no request made at all); accepted, the acting
    /// modus mentis reasons how and rewords the act, and both hang under the old act — the reasoning,
    /// and the new act with the implement as its last link; refused, the refusal hangs there and
    /// costs a point. An accepted implement costs nothing: it is part of an act already thought of.
    /// </summary>
    private async Task CombineItemAsync(ThoughtNode actNode, Item item)
    {
        var action = actNode.Action!;
        // Nothing is attempted yet — the critic is deciding whether the item helps at all.
        _loading = Config.LoadingMessages.CombiningItem;

        try
        {
            var actionMind = action.ActionModusMentis
                ?? _active.ModiMentis.FirstOrDefault(m => m.ModusMentisId == action.ActionModusMentisId);
            if (actionMind == null)
            {
                Console.Error.WriteLine("NarrativeController: cannot combine an item — the acting modus mentis is not resolved.");
                return;
            }
            var where = Locate(actNode.ElementNode)?.Where ?? _pov.Where;
            var node = NodeForArea(where);

            // Lower-cased with an article, never the bare display name, which reads to the critic as a
            // proper name ("Hairpin").
            string itemContext = $"{item.WithArticle()} ({item.Description})";
            var gatedVerb   = action.PreselectedOutcome?.Verb;
            var proficiency = ToolUsageProficiencyStat.Of(_active);
            var gate        = ToolCombinationRules.Resolve(gatedVerb, item, proficiency,
                                  wieldable: _active.CanWieldInSomeHand(item), view: action.PreselectedOutcome);
            Console.WriteLine($"NarrativeController: tool gate — '{action.DisplayText}' with '{itemContext}': proficiency={proficiency}, "
                            + $"verb={gatedVerb?.VerbId ?? "none"} ({gatedVerb?.ToolUse.ToString() ?? "n/a"}), gate={gate}");

            // How the refusal is worded is decided alongside the gate, because the failures are
            // different pieces of news and a single "it did not work" makes the acting modus mentis
            // invent which one it was.
            bool accepted;
            var failure = ToolFailureKind.None;
            string criticReason = "";

            if (gate == ToolCombinationGate.AskTheCritic)
            {
                var goal = action.PreselectedOutcome?.ToNaturalLanguageString() ?? "";
                var context = new CriticContext(node, _worldContext, _locationId, goal)
                {
                    CombinedItemContext = itemContext,
                    TargetDescription   = CriticTarget.Describe(_scene, action.PreselectedOutcome?.Target),
                };
                // A required verb asks a different question: not "does this beat bare hands" (bare
                // hands are not an option there) but "can this do the work of the tool this needs".
                var tree = gatedVerb is { RequiresTool: true }
                    ? CriticTrees.BuildToolSubstitutionTree(goal,
                          CriticTrees.ToolPhrase(gatedVerb.ReferenceToolIdsFor(action.PreselectedOutcome)),
                          NeutralNarration.ItemPhrase(item.DisplayName), context)
                    : CriticTrees.BuildItemAppropriatenessTree(goal, NeutralNarration.ItemPhrase(item.DisplayName), context);
                var verdictResult = await _actionExecutor.ItemUseCritic.EvaluateTreeAsync(tree);

                // The critic judges the implement; the body judges itself. A verdict that passes may
                // still be out of this body's reach, and that is a different refusal from "wrong
                // tool" — and the one the player can do something about by taking a level.
                string verdict = verdictResult.Trace.LastOrDefault()?.ChosenId ?? "";
                bool itemAcceptable = verdictResult.OverallSuccess;
                accepted = itemAcceptable && ToolCombinationRules.VerdictClears(verdict, proficiency);
                if (!accepted)
                {
                    failure      = itemAcceptable ? ToolFailureKind.BeyondSkill : ToolFailureKind.WrongTool;
                    criticReason = verdictResult.CombinedFailureReason;
                }
                Console.WriteLine($"NarrativeController: item verdict '{verdict}' at {proficiency} → {(accepted ? "cleared" : failure.ToString())}");
            }
            else
            {
                accepted = gate == ToolCombinationGate.MadeForIt;
                failure = gate switch
                {
                    ToolCombinationGate.NoProficiency => ToolFailureKind.NoProficiency,
                    ToolCombinationGate.NotItsPurpose => ToolFailureKind.NotItsPurpose,
                    ToolCombinationGate.ExcludedVerb  => ToolFailureKind.Senseless,
                    ToolCombinationGate.NotAWeapon    => ToolFailureKind.NotAWeapon,
                    ToolCombinationGate.TooHeavy      => ToolFailureKind.TooHeavy,
                    _                                 => ToolFailureKind.None,
                };
                Console.WriteLine($"NarrativeController: settled without a critic call — {gate}.");
            }

            _previewSession.Reset();
            if (!accepted)
            {
                // A refused combination costs a noetic point; an accepted one is free. Charging for
                // the accepted case made every tool-gated verb impossible once the pool was cut.
                // Choosing an implement that can serve is the player's side of that bargain.
                SpendPoint();
                var failPart = _previewSession.BeginPart(PreviewTitles.For(actionMind));
                string failText = await _actionExecutor.OutcomeNarrator.NarrateItemCombinationFailureAsync(
                    action, item, actionMind, criticReason, failure, preview: failPart.Sink);
                var failBlock = new NarrationBlock(NarrationBlockType.Outcome, actionMind, failText, Actions: null, ChainOrigin: action.ChainOrigin);
                CommitOnContinue(failPart, () =>
                {
                    Chronicle(failBlock);
                    actNode.Add(new ThoughtNode(ThoughtNodeKind.Refusal) { Block = failBlock });
                });
                return;
            }

            // Both the reasoning and the reworded act stream into one preview box.
            var part = _previewSession.BeginAccumulatingPart(PreviewTitles.For(actionMind));
            string reasoning = await _thinkingExecutor.ExecuteItemReasoningAsync(
                action, item, node, _protagonist, _worldContext, preview: part.NextSegment())
                ?? "";
            if (string.IsNullOrWhiteSpace(reasoning)) reasoning = $"I could use {item.WithArticle()} to help with this.";
            string reworded = await _thinkingExecutor.ExecuteItemReformulationAsync(
                action, item, node, _protagonist, _worldContext, preview: part.NextSegment())
                ?? "";
            if (string.IsNullOrWhiteSpace(reworded)) reworded = action.DisplayText;

            // The chain's last link is the implement: a synthetic modus mentis carrying its name and
            // its usage level, whole. The hands had their say already, and it was a harder one — the
            // proficiency band decided whether this combination could happen at all.
            var combined = new ParsedNarrativeAction
            {
                ActionText                = reworded,
                DisplayText               = reworded,
                // A neutral phrasing for the outcome template, so it does not re-embed the styled one.
                NeutralActionText         = string.IsNullOrWhiteSpace(action.NeutralActionText)
                                                ? string.Empty : $"{action.NeutralActionText} using {item.WithArticle()}",
                ActionModusMentisId       = action.ActionModusMentisId,
                ActionModusMentis         = action.ActionModusMentis,
                CombinedActionModusMentis = new SyntheticItemModusMentis(item.ItemId, item.DisplayName, item.UsageLevel),
                ThinkingModusMentis       = action.ThinkingModusMentis,
                PreselectedOutcome        = action.PreselectedOutcome!,
                CombinedItem              = item,
                DifficultyLevel           = action.DifficultyLevel,
            };
            // Chain: the combined act (item) → reasoning (acting mind) → thinking → observation.
            var reasoningBlock = new NarrationBlock(NarrationBlockType.Thinking, actionMind, reasoning,
                Actions: new List<ParsedNarrativeAction> { combined }, ChainOrigin: action.ChainOrigin);
            combined.ChainOrigin = reasoningBlock;

            CommitOnContinue(part, () =>
            {
                Chronicle(reasoningBlock);
                actNode.Add(new ThoughtNode(ThoughtNodeKind.Thought) { Block = reasoningBlock })
                       .Add(new ThoughtNode(ThoughtNodeKind.Act) { Action = combined });
            });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"NarrativeController: error during item combination: {ex.Message}");
            _previewSession.Reset();
        }
        finally
        {
            _loading = null;
        }
    }
}
