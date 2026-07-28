using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Narrative.Routines;

/// <summary>
/// Turns one narration session into learned routines. A session is not a single chain: it produces a
/// routine every time the player reaches something a routine can end on, so an afternoon spent
/// talking to two neighbours is remembered as two routines, one per neighbour, rather than one chain
/// that only replays if you want to speak to both.
///
/// Recording rules:
/// <list type="bullet">
/// <item>The session's start location + time become every emitted routine's binding.</item>
/// <item>A successful recordable verb is appended to the working chain.</item>
/// <item>A successful <i>unrecordable</i> verb is normally skipped — the chain closes over it and
///   recording continues — unless its effects cannot be reproduced by a replay
///   (<see cref="Verb.BreaksRoutineRecording"/>), in which case the session stops there.</item>
/// <item>A step that hands off to another phase (fight/dialogue) terminates the chain that contains
///   it: a routine is emitted immediately for it, and the session keeps recording afterwards.</item>
/// <item>Failed verbs never reach the success hook, so they are naturally ignored.</item>
/// <item>Whatever is still open when narration ends is emitted as a final routine.</item>
/// </list>
///
/// <para><b>How an emitted routine is built.</b> Routines always replay from the session's entry
/// point, so a routine is a prefix, never a tail: it is the movement steps recorded so far (they are
/// what put the player where the terminus happened) plus the terminus itself. Everything else is
/// dropped, which is what lets the second neighbour's routine leave out the first neighbour. That
/// pruning is safe because a step's own requirements — item, modus mentis, acting member — travel
/// with it as <see cref="RoutineConstraint"/>s and are re-checked at replay; only scene state a
/// dropped step created could be missed, and that surfaces as a routine greyed out in the menu
/// rather than as a wrong replay.</para>
/// </summary>
public class RoutineRecorder
{
    private readonly Protagonist _protagonist;
    private readonly int _locationId;
    private readonly TimePeriod _startTime;

    // The chain still open: every recordable step since the last terminus, in order.
    private readonly List<RoutineStep> _open = new();
    // The movement subsequence of the session — the prefix every emitted routine starts with.
    private readonly List<RoutineStep> _path = new();
    // Signatures of what has already been saved, so a session that repeats itself (or repeats an
    // older routine) does not fill the queue with copies. Pre-seeded from the existing queue, hence
    // the separate count of what THIS session produced.
    private readonly HashSet<string> _emitted = new();
    // Steps appended to the open chain since the last emission — see EmitOpenChain.
    private int _stepsSinceEmit;

    private bool _recording = true;

    public RoutineRecorder(Protagonist protagonist, int locationId, TimePeriod startTime)
    {
        _protagonist = protagonist;
        _locationId  = locationId;
        _startTime   = startTime;

        // An identical routine already in the queue counts as emitted — re-walking a routine you
        // already know should not duplicate it.
        foreach (var existing in protagonist.RecordedRoutines)
            _emitted.Add(Signature(existing.LocationId, existing.StartTime, existing.Steps));
    }

    /// <summary>
    /// Called after a verb has SUCCEEDED, with the reports it is about to apply (they carry the
    /// <see cref="RoutineChainEffect"/> this class reasons about). Returns silently once recording
    /// has stopped for the session.
    /// </summary>
    public void OnVerbSucceeded(ParsedNarrativeAction action, Scene.Scene scene, PoV povBeforeMove,
        PartyMember actingMember, bool itemConsumed, IReadOnlyList<OutcomeReport> reports)
    {
        if (!_recording) return;

        var verb   = action.Verb;
        var target = action.PreselectedOutcome.Target;
        reports ??= Array.Empty<OutcomeReport>();

        // No target means nothing stable to record or to reason about — close the session.
        if (target == null) { EmitOpenChain(); Stop("action had no target"); return; }

        if (!verb.CanRecordAsRoutine(scene, povBeforeMove, target, actingMember))
        {
            if (verb.BreaksRoutineRecording(scene, povBeforeMove, target, reports))
            {
                EmitOpenChain();
                Stop($"'{verb.VerbId}' cannot be left out of a routine");
            }
            else
            {
                Console.WriteLine($"RoutineRecorder: skipped unrecordable '{verb.VerbId}' — chain still open " +
                                  $"({_open.Count} step(s))");
            }
            return;
        }

        var targetRef = verb.RoutineTarget(scene, povBeforeMove, target);
        if (targetRef == null) { EmitOpenChain(); Stop($"'{verb.VerbId}' produced no stable target"); return; }

        var step = new RoutineStep
        {
            VerbId           = verb.VerbId,
            Target           = targetRef,
            Verbatim         = verb.Verbatim(scene, povBeforeMove, target),
            // Named here, while the scene context is still live: the label is what the player reads
            // in the routines menu, so it must resolve pronouns and variants to concrete names now.
            Label            = verb.RoutineLabel(scene, povBeforeMove, target, action.PreselectedOutcome.VerbView),
            TriggeredPhase   = verb.RoutineTriggeredPhase(scene, povBeforeMove, target),
            VariantKey       = verb.RoutineVariantKey(action.PreselectedOutcome.VerbView) ?? "",
            Constraints      = BuildConstraints(action, actingMember, itemConsumed),
            MovesPointOfView = reports.Any(r => r.RoutineChainEffect == RoutineChainEffect.Movement),
        };

        // A phase hand-off ends the chain it belongs to: emit path + this step as a routine of its
        // own and leave the working chain untouched, so later steps build on the same prefix.
        if (step.TriggeredPhase is RoutinePhaseKind.Fight or RoutinePhaseKind.Dialogue)
        {
            Emit(_path.Append(step));
            // A hand-off that also relocated the player joins the prefix afterwards (never before —
            // it would then appear twice in the routine just emitted). No verb does both today; when
            // one does, later steps must still know how the player got where they are.
            if (step.MovesPointOfView) _path.Add(step);
            return;
        }

        _open.Add(step);
        _stepsSinceEmit++;
        if (step.MovesPointOfView) _path.Add(step);
    }

    /// <summary>Called when the narration phase ends; saves whatever chain is still open.</summary>
    public void FinalizeAtNarrationEnd()
    {
        EmitOpenChain();
        _recording = false;
    }

    // ── Emission ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Emits the still-open chain, if it has anything left to say. The test is whether a step was
    /// recorded since the last emission: with nothing new the chain is just the prefix of a routine
    /// already saved (the walk up to the conversation), while a single new step makes it its own
    /// piece of work — following a track to the vegetable beds after speaking to the plowman is a
    /// "go to the beds" routine, not a leftover.
    /// </summary>
    private void EmitOpenChain()
    {
        if (_open.Count == 0 || _stepsSinceEmit == 0) return;
        Emit(_open);
        _open.Clear();
    }

    private void Emit(IEnumerable<RoutineStep> steps)
    {
        // Cloned: a movement prefix is shared by every routine built on it, but each routine owns
        // its own step instances.
        var list = steps.Select(s => s.Clone()).ToList();
        if (list.Count == 0) return;

        // Accounted for either way: a chain that was saved, and one that turned out to be a routine
        // already known, both leave nothing new behind for the end-of-session emission.
        _stepsSinceEmit = 0;

        string signature = Signature(_locationId, _startTime, list);
        if (!_emitted.Add(signature))
        {
            Console.WriteLine($"RoutineRecorder: skipped duplicate routine '{list[^1].DisplayLabel}'");
            return;
        }

        var routine = new Routine
        {
            LocationId = _locationId,
            StartTime  = _startTime,
            Name       = list[^1].DisplayLabel,
            Steps      = list,
        };

        _protagonist.RecordRoutine(routine);
        Console.WriteLine($"RoutineRecorder: recorded routine '{routine.Name}' " +
                          $"({routine.Steps.Count} steps) at location {routine.LocationId} / {routine.StartTime}");
    }

    private void Stop(string reason)
    {
        _recording = false;
        _open.Clear();
        Console.WriteLine($"RoutineRecorder: recording stopped — {reason}");
    }

    /// <summary>
    /// Identity of a routine for duplicate detection: where and when it replays, and the ordered
    /// verb+target pairs it walks. Labels and constraints are deliberately excluded — the same walk
    /// carrying a different tool is still the same routine.
    /// </summary>
    private static string Signature(int locationId, TimePeriod startTime, IEnumerable<RoutineStep> steps)
        => $"{locationId}|{startTime}|" + string.Join(
               ">", steps.Select(s => $"{s.VerbId}:{s.Target?.Key}:{s.VariantKey}"));

    private static List<RoutineConstraint> BuildConstraints(ParsedNarrativeAction action,
        PartyMember actingMember, bool itemConsumed)
    {
        var constraints = new List<RoutineConstraint>
        {
            new ActingMemberConstraint(actingMember.DisplayName),
        };

        if (!string.IsNullOrEmpty(action.ActionModusMentisId))
            constraints.Add(new ModusMentisConstraint(action.ActionModusMentisId));

        if (itemConsumed && action.CombinedItem != null)
            constraints.Add(new ItemConstraint(action.CombinedItem.ItemId, action.CombinedItem.DisplayName));

        return constraints;
    }
}
