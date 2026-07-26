using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Scene.Reminescence;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// REMEMBER — the only action available in the childhood reminescence phase.
/// All state changes are expressed as <see cref="OutcomeReport"/> objects returned by
/// <see cref="SuccessReports"/>, which are applied in sequence by the caller.
/// </summary>
public class RememberVerb : Verb
{
    public override string VerbId         => "remember";
    public override string DisplayName    => "Remember";
    public override int    BaseDifficulty => 0;
    public override char?  DifficultyGlyphOverride => '○';

    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
    {
        if (scene.Phase != NarrationPhase.ChildhoodReminescence) return false;
        return target is FragmentPointOfInterest;
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
    {
        if (target is FragmentPointOfInterest frag)
            return $"try to remember what was this {frag.Fragment.Name} from my childhood";
        return "try to remember";
    }

    public override IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        if (target is not FragmentPointOfInterest fragmentPoi)
            throw new InvalidOperationException("RememberVerb target must be a FragmentPointOfInterest");

        var data    = fragmentPoi.Fragment;
        var outcome = data.Outcome;
        var origin  = scene.CurrentReminescenceId ?? "";
        var reports = new List<OutcomeReport>();

        // Skills — visible positive chips; Apply() grants a fresh level-1 instance.
        foreach (var skillType in outcome.SkillTypes)
        {
            var template = ModusMentisRegistry.Instance.GetModusMentis(skillType);
            if (template == null)
                throw new InvalidOperationException(
                    $"RememberVerb: modusMentis '{skillType.Name}' is not registered "
                    + "(it needs a public parameterless constructor to be auto-registered).");

            reports.Add(new SkillAcquisitionOutcome(template));
        }

        // Items — visible positive chips; Apply() calls protagonist.AcquireItem().
        foreach (var itemFactory in outcome.Items)
            reports.Add(new ItemGrantOutcome(itemFactory()));

        // Coins — visible positive chips; Apply() credits the shared party wallet.
        foreach (var (coinType, amount) in outcome.Coins)
            reports.Add(new CoinGrantOutcome(coinType, amount));

        // Internal: childhood history mutation (location + fragment record).
        reports.Add(new ChildhoodHistoryOutcome(origin, data.Name, data.Summary, data.ContextSummary, outcome.SetChildhoodLocation));

        // Internal: scene state capture for the consumed fragment.
        reports.Add(new StateCaptureOutcome(fragmentPoi));

        // Internal: phase transition request consumed by NarrativeController.
        reports.Add(new ReminescenceTransitionOutcome(origin, outcome.NextReminescenceId, data.Name));

        return reports;
    }
}
