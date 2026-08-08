using System.Collections.Generic;
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
    public override string VerbId         => "unlock_door";
    public override string DisplayName    => "Unlock";
    public override int    BaseDifficulty => 3;

    /// <summary>Picks, wards and a lock to work. No hands, no verb.</summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    /// <summary>What a success teaches: forcing a lock teaches locks.</summary>
    public override string? GrantedModusMentisId(Element? target) => "lockpicking";

    /// <summary>
    /// Forcing a lock is a crime when the door leads somewhere private — which is the question, not
    /// where you are standing while you pick it. A house door is listed in the street's points of
    /// interest as well as the room's, so <c>pov.Where</c> alone would call a burglary from the
    /// street lawful. A public storehouse's lock is nobody's privacy.
    /// </summary>
    protected override bool IsIllegalFor(Scene scene, PoV pov, Element? target, PartyMember? actor)
        => PrivacyModel.ReachesPrivateArea(scene, target);

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
    {
        if (target is not DoorPointOfInterest door) return false;
        return pov.Where.Id == door.FrontArea.Id && door.EffectiveState(pov.When) == DoorState.Locked;
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"unlock and open {DefiniteTarget(target)}";

    public override IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        if (target is not DoorPointOfInterest door) return System.Array.Empty<OutcomeReport>();
        return new[] { new DoorUnlockOutcome(door, door.BackArea) };
    }
}
