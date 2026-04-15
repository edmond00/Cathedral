using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Abstract action that can modify the active party member (inventory),
/// the <see cref="PoV"/> (changing area/focus), or the <see cref="Scene"/> state (unlocking, etc.).
/// Verbs are registered in the global <see cref="VerbRegistry"/> and filtered per scene.
/// </summary>
public abstract class Verb
{
    /// <summary>Unique identifier for this verb type (e.g. "move", "grab").</summary>
    public abstract string VerbId { get; }

    /// <summary>Human-readable display name (e.g. "Move", "Grab").</summary>
    public abstract string DisplayName { get; }

    /// <summary>
    /// Whether executing this verb is a legal action.
    /// Override to false for verbs that constitute crimes (stealing, trespassing, attacking innocents).
    /// Combined with <see cref="Scene.Area.IsPrivate"/> to determine full legality.
    /// </summary>
    public virtual bool IsLegal => true;

    /// <summary>
    /// Whether this verb is valid to use when an enemy is nearby (same area).
    /// When false, the LLM critic asks whether the enemy gets an opportunity attack.
    /// Override to true for combat verbs (attack, slay, reconcile, appease).
    /// </summary>
    public virtual bool CanBeUsedUnderThreat => false;

    /// <summary>
    /// Returns whether this verb can be executed right now given the scene state,
    /// point of view, target element, and (optionally) the acting party member.
    /// </summary>
    public abstract bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null);

    /// <summary>
    /// Returns a natural-language string describing the action or its outcome.
    /// This is sent to the thinking LLM instance that needs to choose a goal/outcome.
    /// Can be dynamically written (e.g. "grab the {item name}").
    /// </summary>
    public abstract string Verbatim(Scene scene, PoV pov, Element target);

    /// <summary>
    /// Executes the verb, modifying the scene state, party member, and/or PoV.
    /// </summary>
    public abstract void Execute(Scene scene, PoV pov, Protagonist actor, Element target);
}
