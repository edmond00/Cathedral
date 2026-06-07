using System.Linq;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative.Rules;

/// <summary>
/// All data a coded rule needs to evaluate an action before the LLM pipeline runs.
/// The action modus mentis is resolved lazily from the acting member on first access.
/// </summary>
public class ActionRuleContext
{
    public ParsedNarrativeAction Action    { get; }
    /// <summary>The party member performing the action (protagonist or an active companion).</summary>
    public PartyMember            Actor      { get; }
    public Scene.Scene?           Scene      { get; }
    public PoV?                   PoV        { get; }
    public WitnessContext         WitnessContext { get; }
    public ThreatContext          ThreatContext  { get; }

    private ModusMentis? _actionModusMentis;

    /// <summary>
    /// The modus mentis the player chose for the action.
    /// Resolved lazily; null if the id does not match any modus mentis on the acting member.
    /// </summary>
    public ModusMentis? ActionModusMentis =>
        _actionModusMentis ??= Actor.ModiMentis
            .FirstOrDefault(m => m.ModusMentisId == Action.ActionModusMentisId);

    public ActionRuleContext(
        ParsedNarrativeAction action,
        PartyMember           actor,
        Scene.Scene?          scene,
        PoV?                  pov,
        WitnessContext        witnessContext,
        ThreatContext?        threatContext = null)
    {
        Action         = action;
        Actor          = actor;
        Scene          = scene;
        PoV            = pov;
        WitnessContext = witnessContext;
        ThreatContext  = threatContext ?? ThreatContext.None;
    }
}
