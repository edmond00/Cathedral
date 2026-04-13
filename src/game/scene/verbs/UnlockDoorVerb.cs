using System;
using Cathedral.Game.Narrative;
using Cathedral.Game.Scene.Building;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Unlocks a locked <see cref="DoorPointOfInterest"/> and immediately passes through it.
/// Only possible from the front side when the door is locked.
/// Combines "unlock" and "enter" into a single action because forcing a door
/// requires committing to crossing the threshold.
/// </summary>
public class UnlockDoorVerb : Verb
{
    public override string VerbId      => "unlock_door";
    public override string DisplayName => "Unlock";

    /// <summary>Forcing open a locked door without a key is illegal.</summary>
    public override bool IsLegal => false;

    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
    {
        if (target is not DoorPointOfInterest door) return false;
        return pov.Where.Id == door.FrontArea.Id && door.DoorState == DoorState.Locked;
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"unlock and open {target.DisplayName.ToLowerInvariant()}";

    public override void Execute(Scene scene, PoV pov, Protagonist actor, Element target)
    {
        if (target is not DoorPointOfInterest door)
            throw new InvalidOperationException("UnlockDoorVerb target must be a DoorPointOfInterest");

        door.DoorState = DoorState.Unlocked;
        pov.Where      = door.BackArea;
        pov.Focus      = null;
        scene.StateChanges.Capture(door);

        Console.WriteLine($"UnlockDoorVerb: Unlocked {door.DisplayName}, moved to {door.BackArea.DisplayName}");
    }
}
