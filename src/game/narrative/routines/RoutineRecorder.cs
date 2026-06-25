using System;
using System.Collections.Generic;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Narrative.Routines;

/// <summary>
/// Accumulates recordable successful verbs into a routine during a single narration session, then
/// finalizes it onto the protagonist's queue. One recorder lives per narration session.
///
/// Recording rules (per spec):
/// <list type="bullet">
/// <item>The session's start location + time become the routine's binding.</item>
/// <item>A successful recordable verb is appended as a step.</item>
/// <item>A successful <i>unrecordable</i> verb stops recording: the steps so far become the routine.</item>
/// <item>A step that triggers a non-narration phase (fight/dialogue) is appended, then recording stops.</item>
/// <item>Failed verbs never reach the success hook, so they are naturally ignored.</item>
/// <item>If narration ends with an open routine, it is saved.</item>
/// </list>
/// </summary>
public class RoutineRecorder
{
    private readonly Protagonist _protagonist;
    private readonly int _locationId;
    private readonly TimePeriod _startTime;

    private readonly List<RoutineStep> _steps = new();
    private bool _recording = true;

    public RoutineRecorder(Protagonist protagonist, int locationId, TimePeriod startTime)
    {
        _protagonist = protagonist;
        _locationId  = locationId;
        _startTime   = startTime;
    }

    /// <summary>
    /// Called after a verb has SUCCEEDED and its reports were applied. Returns silently once recording
    /// has stopped for the session.
    /// </summary>
    public void OnVerbSucceeded(ParsedNarrativeAction action, Scene.Scene scene, PoV povBeforeMove,
        PartyMember actingMember, bool itemConsumed)
    {
        if (!_recording) return;

        var verb   = action.Verb;
        var target = action.PreselectedOutcome.Target;
        if (target == null) { Finalize(); return; }

        // An unrecordable success ends the chain — prior steps become the routine.
        if (!verb.CanRecordAsRoutine(scene, povBeforeMove, target, actingMember))
        {
            Finalize();
            return;
        }

        var targetRef = verb.RoutineTarget(scene, povBeforeMove, target);
        if (targetRef == null) { Finalize(); return; }

        var step = new RoutineStep
        {
            VerbId         = verb.VerbId,
            Target         = targetRef,
            Verbatim       = verb.Verbatim(scene, povBeforeMove, target),
            TriggeredPhase = verb.RoutineTriggeredPhase(scene, povBeforeMove, target),
            Constraints    = BuildConstraints(action, actingMember, itemConsumed),
        };
        _steps.Add(step);

        // Phases other than narration cannot be recorded — stop after this step.
        if (step.TriggeredPhase is RoutinePhaseKind.Fight or RoutinePhaseKind.Dialogue)
            Finalize();
    }

    /// <summary>Called when the narration phase ends; saves any still-open routine.</summary>
    public void FinalizeAtNarrationEnd() => Finalize();

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

    private void Finalize()
    {
        _recording = false;
        if (_steps.Count == 0) return;

        var routine = new Routine
        {
            LocationId = _locationId,
            StartTime  = _startTime,
            Name       = _steps[^1].Verbatim,
            Steps      = new List<RoutineStep>(_steps),
        };
        _steps.Clear();

        _protagonist.RecordRoutine(routine);
        Console.WriteLine($"RoutineRecorder: recorded routine '{routine.Name}' " +
                          $"({routine.Steps.Count} steps) at location {routine.LocationId} / {routine.StartTime}");
    }
}
