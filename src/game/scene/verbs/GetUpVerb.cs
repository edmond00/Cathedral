using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// GET UP — the only action available in the Get-Up scene.
/// Accessible from any observation in the scene regardless of target.
/// Difficulty is always 1 (overridden in the action executor; no critic malus).
/// On success: queues a <see cref="GetUpTransitionOutcome"/> that signals world travel.
/// On failure: no penalty, no damage — the scene loops back for another attempt.
/// </summary>
public sealed class GetUpVerb : Verb
{
    public override string VerbId         => "get_up";
    public override string DisplayName    => "GET UP";
    public override int    BaseDifficulty => 1;

    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
        => scene.Phase == NarrationPhase.GetUp;

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => "get up and continue your travel";

    public override IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, Protagonist actor, Element target)
        => new List<OutcomeReport> { new GetUpTransitionOutcome() };

    public override IReadOnlyList<OutcomeReport> FailureReports(Scene scene, PoV pov, Protagonist actor, Element target)
        => System.Array.Empty<OutcomeReport>();
}
