using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cathedral.Audio;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Preview;
using Cathedral.Game.Narrative.Tree;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Building;

namespace Cathedral.Game;

// The steps a mind takes before any act: observing an element, thinking about one, thinking an act
// over again, and speaking of one to a companion.
public partial class NarrativeController
{
    // ── What a click has asked for, while its popup is open ──────────────────

    private abstract record Pending;
    /// <summary>Observe in <paramref name="Area"/>: one of a kind, or the element <paramref name="Named"/>.</summary>
    private sealed record ObservePending(Area Area, ElementKind Kind, NarrativeAnchor? Named, ThoughtNode? Container) : Pending;
    private sealed record ThinkPending(ThoughtNode Element) : Pending;
    /// <summary>Speak of an element: first the speaking mind is asked for, then whom to address.</summary>
    private sealed record SpeakPending(ThoughtNode Element, ModusMentis? Mind, IReadOnlyList<PartyMember> Listeners) : Pending;
    /// <summary>An act clicked: Execute, Use Tool or Reconsider.</summary>
    private sealed record ActPending(ThoughtNode Act) : Pending;
    private sealed record ReconsiderPending(ThoughtNode Act) : Pending;
    private sealed record ToolPending(ThoughtNode Act) : Pending;

    private Pending? _pending;

    /// <summary>
    /// Routes the modus mentis chosen in the popup to the step it was asked for. Shared by the mouse
    /// and the script driver's <c>choose</c>.
    /// </summary>
    private void ApplyModusMentisSelection(ModusMentis chosen)
    {
        var pending = _pending;
        _pending = null;
        Console.WriteLine($"NarrativeController: selected modus mentis {chosen.DisplayName}");

        // A modus mentis this body can no longer carry is still offered and refused here rather than
        // withheld from the popup — see BrokenModusMentis. This is the one place every faculty the
        // player chooses passes through, so the guard is written once.
        if (_active.IsModusMentisBroken(chosen))
        {
            var (faculty, under) = pending switch
            {
                ObservePending o   => (NeutralNarration.BrokenFaculty.Observation, o.Container),
                SpeakPending s     => (NeutralNarration.BrokenFaculty.Speech, s.Element),
                ThinkPending t     => (NeutralNarration.BrokenFaculty.Thinking, t.Element),
                ReconsiderPending r => (NeutralNarration.BrokenFaculty.Thinking, r.Act.ElementNode),
                _                  => (NeutralNarration.BrokenFaculty.Thinking, (ThoughtNode?)null),
            };
            _ = RefuseBrokenModusMentisAsync(chosen, faculty, under);
            return;
        }

        switch (pending)
        {
            case ObservePending observe:
                // Observing needs presence: the member goes there first, by ways the party knows.
                WalkTo(observe.Area, then: () =>
                {
                    _loading = Exploring ? Config.LoadingMessages.GeneratingObservations : Config.LoadingMessages.Remembering;
                    _ = ObserveAsync(observe, chosen);
                });
                break;

            case ThinkPending think:
                _loading = Config.LoadingMessages.ThinkingDeeply;
                _ = ThinkAsync(think.Element, chosen);
                break;

            case ReconsiderPending reconsider:
                _loading = Config.LoadingMessages.ThinkingDeeply;
                _ = ReconsiderAsync(chosen, reconsider.Act);
                break;

            case SpeakPending speak:
                // The speaking mind is chosen: now, whom to address.
                _pending = speak with { Mind = chosen };
                _choicePopup.Show(PopupAnchor(), speak.Listeners.Select(l => l.DisplayName).ToList(), "Who do you address?");
                break;
        }
    }

    // ── Observing ────────────────────────────────────────────────────────────

    /// <summary>
    /// <paramref name="mind"/> observes what the click asked for, standing in its area: one element
    /// of the kind, of the mind's own choosing among those the member has not observed and does not
    /// remember — or the named element — or declines, once. A point either way.
    /// </summary>
    private async Task ObserveAsync(ObservePending what, ModusMentis mind)
    {
        try
        {
            _previewSession.Reset();
            var owner = Mind;
            var area = what.Area;

            List<NarrativeAnchor> pool;
            object about;
            if (what.Named != null)
            {
                pool  = new List<NarrativeAnchor> { what.Named };
                about = ElementKinds.ElementOf(what.Named)!;
            }
            else
            {
                pool = Unobserved(area).Where(u => u.Kind == what.Kind && !u.Known).Select(u => u.Anchor).ToList();
                // A script that named one of them has made the mind's choice for it.
                if (_cliObservePin is { } pinned && pool.Contains(pinned)) pool = new List<NarrativeAnchor> { pinned };
                _cliObservePin = null;
                // Confronted, a member looks only at whoever confronts them.
                if (ConfrontationOf(owner) is { } confrontation)
                    pool = pool.Where(a => IsConfronter(a, confrontation)).ToList();
                about = (area.Id, what.Kind);
            }
            if (pool.Count == 0) { _loading = null; return; }

            var result = await _observationController.ObserveAsync(
                pool, what.Named != null, mind, _active, NodeForArea(area), (_scene, owner.Pov), _locationId,
                mayDecline: !owner.HasDeclined(mind, about),
                preview: _previewSession, commit: Commit);

            _loading = null;
            // No text at all: nothing was observed and nothing is owed.
            if (result != null) SpendPoint();

            void Commit(ObservationResult observed)
            {
                Chronicle(observed.Block);
                _ambianceEngine?.PlaySoundEffect(SoundEffectType.NarrativeReveal);

                if (observed.Observed == null)
                {
                    // The declining is spent: asked again about the same thing, this mind attends.
                    owner.Decline(mind, about);
                    var refusal = new ThoughtNode(ThoughtNodeKind.Refusal) { Block = observed.Block };
                    if (what.Container != null) what.Container.Add(refusal);
                    else owner.Strays.Add((area, refusal));
                    return;
                }

                Root(owner, observed.Observed, area, observed.Block, what.Container);
                if (!Exploring) return;
                // What an observation takes in is remembered with the place: a way seen is on the
                // map, a thing or a person is named on the next visit.
                AreaVisits.See(_scene, area, observed.Observed);
                ElementMemory.Learn(_scene, observed.Observed);
            }
        }
        catch (Exception ex)
        {
            Fail("observing", ex);
        }
    }

    // ── Thinking ─────────────────────────────────────────────────────────────

    /// <summary>
    /// <paramref name="thinkingMind"/> thinks about an observed element, wherever it now is: goal,
    /// means, reasoning, doubt, willingness, act — as many tries as the mind's readiness to doubt
    /// allows (<see cref="ThinkingExecutor.GenerateThinkingAsync"/>). Every text of it becomes a node
    /// under the element, each from the last, and the act it comes to hangs from the last thought. One
    /// point for the whole chain.
    /// </summary>
    private async Task ThinkAsync(ThoughtNode element, ModusMentis thinkingMind)
    {
        try
        {
            _previewSession.Reset();
            var owner = Mind;

            // Verb gates read mutable world state (affinity, item presence, …) — re-expand every verb
            // list right before the goal choice so it can only offer what is possible now.
            RefreshSceneVerbs();
            if (Locate(element) is not { } at) { _loading = null; return; }
            Console.WriteLine($"NarrativeController: {thinkingMind.DisplayName} thinks about '{at.Anchor.DisplayName}' in {at.Where.DisplayName}");

            // In the childhood reminescence REMEMBER may only be carried out by the reminescence
            // modus mentis; thinking and observing may use any.
            var actionMinds = _active.GetActionModiMentis();
            if (_scene.Phase == NarrationPhase.ChildhoodReminescence)
                actionMinds = actionMinds.Where(m => m.ModusMentisId == "childhood_reminescence").ToList();

            var response = await _thinkingExecutor.GenerateThinkingAsync(
                thinkingMind,
                at.Anchor,
                NodeForArea(at.Where),
                actionMinds,
                _protagonist,
                _worldContext,
                _locationId,
                _active,
                // The scene and the point of view are what the coded choice rules judge from — whether
                // a goal on offer is a crime — and an act is judged where it would be done.
                _scene,
                new PoV(at.Where, Period),
                isReminescence: _scene.Phase == NarrationPhase.ChildhoodReminescence,
                autoSuccess: InOpeningPhase,
                preview: _previewSession,
                proposed: owner.Proposals,
                cancellationToken: CancellationToken.None)
                ?? throw new InvalidOperationException("thinking returned nothing");

            // One block per segment, in order: the thinking mind's thoughts, and each refusal in the
            // voice that refused. Without iterative thinking that is one thought, or a thought and
            // its refusal. The acts belong to the last thought.
            var segments = response.Segments.Count > 0
                ? response.Segments
                : new List<ThinkingSegment> { new(thinkingMind, response.ReasoningText, IsThought: true) };
            var lastThought = segments.LastOrDefault(seg => seg.IsThought);
            var blocks = segments.Select(seg => new NarrationBlock(
                Type: seg.IsThought ? NarrationBlockType.Thinking : NarrationBlockType.Outcome,
                ModusMentis: seg.ModusMentis,
                Text: seg.Text,
                Actions: response.Actions.Count > 0 && ReferenceEquals(seg, lastThought) ? response.Actions : null,
                // The observation is the root of the chain the dice read.
                ChainOrigin: seg.IsThought ? element.Block : null)).ToList();
            var thinkingBlock = lastThought == null ? null : blocks[segments.IndexOf(lastThought)];
            foreach (var action in response.Actions) action.ChainOrigin = thinkingBlock;

            void Commit()
            {
                // An act is proposed the moment its symbol is on the tree, which is here — so this is
                // where it stops being offered again. A goal the acting mind refused reaches no symbol
                // and is deliberately not recorded: the refusal was that mind's, and asking again with
                // another is the move that should work.
                foreach (var action in response.Actions)
                    owner.Proposals.Propose(action.PreselectedOutcome);

                var tail = element;
                ThoughtNode? last = null;
                for (int i = 0; i < segments.Count; i++)
                {
                    tail = tail.Add(new ThoughtNode(segments[i].IsThought ? ThoughtNodeKind.Thought : ThoughtNodeKind.Refusal)
                                    { Block = blocks[i] });
                    if (ReferenceEquals(segments[i], lastThought)) last = tail;
                    Chronicle(blocks[i]);
                }
                foreach (var action in response.Actions)
                    last!.Add(new ThoughtNode(ThoughtNodeKind.Act) { Action = action });
                _ambianceEngine?.PlaySoundEffect(SoundEffectType.NarrativeReveal);
            }
            CommitOnContinue(response.PreviewLastPart, Commit);

            _loading = null;
            SpendPoint();
            Console.WriteLine($"NarrativeController: thinking done — {response.Actions.Count} act(s), {owner.Noetic} point(s) left");
            if (DebugMode.IsActive && response.Actions.Count > 0)
                DebugMode.PrintAvailableActions(response.Actions);
        }
        catch (Exception ex)
        {
            Fail("thinking", ex);
        }
    }

    /// <summary>
    /// Attaches <paramref name="commit"/> to the last preview part and completes it only once it is
    /// attached (so a fast CONTINUE can never fire a commit-less part) — or commits at once when
    /// nothing was previewed.
    /// </summary>
    private void CommitOnContinue(PreviewPart? lastPart, Action commit)
    {
        if (lastPart != null)
        {
            lastPart.AttachCommit(commit);
            lastPart.MarkComplete();
            _previewSession.EndProduction();
        }
        else
        {
            commit();
            _previewSession.Reset();
        }
    }

    // ── Reconsidering ────────────────────────────────────────────────────────

    /// <summary>
    /// Whether an act on a tree may be reconsidered: a noetic point to spend, a thinking modus mentis
    /// to do it, and not in the opening phases, whose one act has one means.
    /// </summary>
    private bool CanReconsider()
        => !InOpeningPhase && Mind.Noetic > 0 && _active.GetThinkingModiMentis().Count > 0;

    /// <summary>
    /// Reconsider (<see cref="ThinkingExecutor.ReconsiderAsync"/>): <paramref name="thinkingMind"/>
    /// thinks over an act already proposed, and when it keeps it, gives it to another means able to
    /// carry it out on the click. The new act is a branch of its own under the element and the old one
    /// is superseded; when the mind wants no part of it, or no other means could, only the thought is
    /// added and the old act stands. One noetic point either way.
    /// </summary>
    private async Task ReconsiderAsync(ModusMentis thinkingMind, ThoughtNode previousNode)
    {
        try
        {
            var previous = previousNode.Action!;
            var element = previousNode.ElementNode;
            Console.WriteLine($"NarrativeController: reconsidering '{previous.ActionText}' with {thinkingMind.DisplayName}");
            _previewSession.Reset();
            var where = Locate(element)?.Where ?? _pov.Where;

            var response = await _thinkingExecutor.ReconsiderAsync(
                thinkingMind, previous, NodeForArea(where), _active.GetActionModiMentis(),
                _worldContext, _locationId, _active, _scene, new PoV(where, Period),
                action => ProbeActionRules(action, where), _previewSession, CancellationToken.None);

            var action = response.Actions.FirstOrDefault();
            var blocks = response.Segments.Select(seg => new NarrationBlock(
                Type: NarrationBlockType.Thinking,
                ModusMentis: seg.ModusMentis,
                Text: seg.Text,
                Actions: action != null && ReferenceEquals(seg, response.Segments[^1]) ? response.Actions : null,
                ChainOrigin: element.Block)).ToList();
            if (action != null) action.ChainOrigin = blocks[^1];

            void Commit()
            {
                var tail = element;
                foreach (var block in blocks)
                {
                    tail = tail.Add(new ThoughtNode(ThoughtNodeKind.Thought) { Block = block });
                    Chronicle(block);
                }
                if (action != null)
                {
                    // The same goal by another means: it stays proposed, under its new act.
                    previousNode.State = ActState.Superseded;
                    tail.Add(new ThoughtNode(ThoughtNodeKind.Act) { Action = action });
                }
                _ambianceEngine?.PlaySoundEffect(SoundEffectType.NarrativeReveal);
            }
            CommitOnContinue(response.PreviewLastPart, Commit);

            _loading = null;
            SpendPoint();
            Console.WriteLine(action != null
                ? $"NarrativeController: reconsidered — '{action.ActionText}' by {action.ActionModusMentis?.DisplayName} replaces it"
                : "NarrativeController: reconsidered — the act stands as it was");
            if (DebugMode.IsActive && action != null) DebugMode.PrintAvailableActions(response.Actions);
        }
        catch (Exception ex)
        {
            Fail("thinking", ex);
        }
    }

    // ── Speaking ─────────────────────────────────────────────────────────────

    /// <summary>
    /// The companions the played member may speak to: those standing in the same area or in one next
    /// to it. Speaking is across a room or a doorway, not across the place.
    /// </summary>
    private List<PartyMember> WithinSpeech()
        => _minds.Values.Where(m => !ReferenceEquals(m.Member, _active)
                                 && (m.Pov.Where.Id == _pov.Where.Id || AreNextTo(m.Pov.Where, _pov.Where)))
                 .Select(m => m.Member).ToList();

    /// <summary>Whether two areas are neighbours, or joined by a door, a stair or any other connector but a view.</summary>
    private bool AreNextTo(Area a, Area b)
        => (_scene.AreaGraph.TryGetValue(a.Id, out var fromA) && fromA.Contains(b.Id))
        || (_scene.AreaGraph.TryGetValue(b.Id, out var fromB) && fromB.Contains(a.Id))
        || a.PointsOfInterest.OfType<ConnectorPointOfInterest>()
            .Any(c => c is not LandscapePointOfInterest && c.Touches(a) && c.Touches(b));

    /// <summary>
    /// The played member speaks to <paramref name="companion"/> of an element they observed. What is
    /// said is a node on the speaker's tree, and plants the element on the listener's trees with the
    /// words as its root — the listener thinks about it from what they were told. It costs the
    /// speaker a point, and the view stays on the speaker: speaking is not switching.
    /// </summary>
    private async Task SpeakAsync(ThoughtNode element, ModusMentis speakingMind, PartyMember companion)
    {
        try
        {
            if (Locate(element) is not { } at) { _loading = null; return; }
            _previewSession.Reset();
            var speaker = Mind;
            var listener = _minds[companion];

            void Commit(NarrationBlock spoken)
            {
                Chronicle(spoken);
                element.Add(new ThoughtNode(ThoughtNodeKind.Speech) { Block = spoken, Note = $"to {companion.DisplayName}" });
                if (listener.NodeOf(element.Element!, element.SeenFrom) == null)
                    Root(listener, at.Anchor, at.Where, spoken);
                speaker.Noetic = Math.Max(0, speaker.Noetic - 1);
                _ambianceEngine?.PlaySoundEffect(SoundEffectType.NarrativeReveal);
            }

            var block = await _observationController.SpeakAsync(
                speakingMind, _active, companion.DisplayName, at.Anchor, _locationId, _previewSession, Commit);
            _loading = null;
            if (block == null)
                Console.Error.WriteLine("NarrativeController: speaking produced nothing");
        }
        catch (Exception ex)
        {
            Fail("speaking", ex);
        }
    }

    // ── A faculty the body can no longer carry ───────────────────────────────

    /// <summary>
    /// Narrates a modus mentis the body can no longer carry, in its own voice, and spends the noetic
    /// point the attempt cost. The observation / thinking / speaking counterpart of
    /// <c>BrokenModusMentisRule</c>, which does the same job for an action — see
    /// <see cref="BrokenModusMentis"/> for why this refuses rather than withholding. The refusal
    /// hangs under <paramref name="under"/>, or stands alone in the area when it is about no element.
    ///
    /// <para>It swallows its own exception rather than failing the narration. This is a refusal, not
    /// a step: taking the visit down because the account of a ruined arm could not be generated would
    /// turn a wound into a lost run.</para>
    /// </summary>
    private async Task RefuseBrokenModusMentisAsync(ModusMentis mind, NeutralNarration.BrokenFaculty faculty, ThoughtNode? under)
    {
        try
        {
            Console.WriteLine($"NarrativeController: '{mind.DisplayName}' is broken on this body "
                            + $"(effective level {_active.GetEffectiveModusMentisLevel(mind)}) — refusing {faculty}");
            var owner = Mind;
            var area = _pov.Where;

            _previewSession.Reset();
            var part = _previewSession.BeginPart(PreviewTitles.For(mind));
            string text = await _actionExecutor.OutcomeNarrator.NarrateBrokenModusMentisAsync(
                mind, _active, faculty, CancellationToken.None, preview: part.Sink);
            if (string.IsNullOrWhiteSpace(text))
                text = $"[IMPOSSIBLE] {BrokenModusMentis.NeutralFor(_active, mind, faculty)}";

            var block = new NarrationBlock(NarrationBlockType.Outcome, mind, text, Actions: null);
            CommitOnContinue(part, () =>
            {
                Chronicle(block);
                var refusal = new ThoughtNode(ThoughtNodeKind.Refusal) { Block = block };
                if (under != null) under.Add(refusal);
                else owner.Strays.Add((area, refusal));
            });
            SpendPoint();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"NarrativeController: error narrating broken modus mentis: {ex.Message}");
            _previewSession.Reset();
        }
        finally
        {
            _loading = null;
        }
    }
}
