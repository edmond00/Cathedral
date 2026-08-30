using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative.Rules;

/// <summary>
/// Blocks an illegal action when a witness is at <b>effective-Visual</b> proximity — close enough that
/// the character would be caught in the act.
///
/// Discreteness of the chosen modus mentis downgrades the witness's proximity by one step
/// (see <see cref="ProximityModel"/>): a discrete modus mentis turns a same-area (Visual) witness into
/// effective-Audio, so it may act — risking only a caught-red-handed confrontation if the action fails.
/// A non-discrete modus mentis keeps the raw Visual level and is blocked outright here.
///
/// The refusal reason is phrased in the first person so it can be re-expressed in the modus mentis's
/// voice as the [IMPOSSIBLE] narration.
/// </summary>
public class IllegalActionVisualWitnessRule : IActionRule
{
    public ActionRuleResult Check(ActionRuleContext ctx)
    {
        if (!ctx.IsIllegalAction) return ActionRuleResult.Pass();

        // Raw proximity, not effective: this is the "may I act at all?" question, and a discrete
        // modus mentis may always attempt — that permission is what discreteness buys. What it does
        // NOT buy is safety, since failing in front of somebody still costs the full price
        // (see ProximityModel). Only a non-discrete skill is stopped from trying.
        if (ctx.WitnessContext.Type != WitnessType.Visual) return ActionRuleResult.Pass();
        if (ctx.ActionModusMentis?.ActsDiscretely == true) return ActionRuleResult.Pass();

        var witnessName = ctx.WitnessContext.Witness?.DisplayName ?? "someone";
        return ActionRuleResult.Fail($"{witnessName} is right here — I would be caught red-handed.");
    }
}
