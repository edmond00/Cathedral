using System.Linq;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative.Rules;

/// <summary>
/// All data a coded rule needs to evaluate an action before the LLM pipeline runs.
/// The action modus mentis is resolved lazily from the protagonist on first access.
/// </summary>
public class ActionRuleContext
{
    public ParsedNarrativeAction Action    { get; }
    public Protagonist            Protagonist { get; }
    public Scene.Scene?           Scene      { get; }
    public PoV?                   PoV        { get; }
    public WitnessContext         WitnessContext { get; }

    private ModusMentis? _actionModusMentis;

    /// <summary>
    /// The modus mentis the player chose for the action.
    /// Resolved lazily; null if the id does not match any modus mentis on the protagonist.
    /// </summary>
    public ModusMentis? ActionModusMentis =>
        _actionModusMentis ??= Protagonist.ModiMentis
            .FirstOrDefault(m => m.ModusMentisId == Action.ActionModusMentisId);

    public ActionRuleContext(
        ParsedNarrativeAction action,
        Protagonist           protagonist,
        Scene.Scene?          scene,
        PoV?                  pov,
        WitnessContext        witnessContext)
    {
        Action         = action;
        Protagonist    = protagonist;
        Scene          = scene;
        PoV            = pov;
        WitnessContext = witnessContext;
    }
}
