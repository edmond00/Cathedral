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

    /// <summary>
    /// Whether this action is a crime — asked once, here, so the rules that care about it cannot
    /// disagree about what counts. Legality is contextual (see <see cref="Verbs.Verb.IsIllegal"/>):
    /// the verb, the target and who the actor's enemies are all speak to it.
    ///
    /// <para>False when there is no PoV to judge from, which is the safe reading: with no scene there
    /// is no witness to be caught by and no private space to be standing in.</para>
    /// </summary>
    public bool IsIllegalAction =>
        Scene != null && PoV != null
        && Action.Verb.IsIllegal(Scene, PoV, Action.PreselectedOutcome?.Target, Actor);

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
