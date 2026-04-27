using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Scene.Building;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Passes through an unlocked <see cref="DoorPointOfInterest"/> (front → back),
/// or exits back through it from the rear regardless of lock state.
///
/// Lock semantics summary:
///   Front side (FrontArea):  only available when door is <see cref="DoorState.Unlocked"/>.
///   Back side  (BackArea):   always available — locked doors cannot trap you inside.
/// </summary>
public class OpenDoorVerb : Verb
{
    public override string VerbId         => "open_door";
    public override string DisplayName    => "Open";
    public override int    BaseDifficulty => 1;

    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
    {
        if (target is not DoorPointOfInterest door) return false;

        // Front → back: only when unlocked
        if (pov.Where.Id == door.FrontArea.Id && door.DoorState == DoorState.Unlocked) return true;

        // Back → front: always (locked doors cannot trap you inside)
        if (pov.Where.Id == door.BackArea.Id) return true;

        return false;
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
    {
        if (target is not DoorPointOfInterest door) return "open the door";

        return pov.Where.Id == door.FrontArea.Id
            ? $"open {door.DisplayName.ToLowerInvariant()} and step through"
            : $"exit back through {door.DisplayName.ToLowerInvariant()}";
    }

    public override IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, Protagonist actor, Element target)
    {
        if (target is not DoorPointOfInterest door) return System.Array.Empty<OutcomeReport>();
        var destination = pov.Where.Id == door.FrontArea.Id ? door.BackArea : door.FrontArea;
        return new[] { new AreaMoveOutcome(destination) };
    }
}
