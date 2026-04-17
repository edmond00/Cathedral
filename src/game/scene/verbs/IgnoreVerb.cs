using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// A always-possible verb representing the choice to ignore an observation and move on.
/// When selected as the GOAL during thinking, the pipeline exits after WHY — no HOW/WHAT
/// is generated and no action button is shown.
///
/// Not registered in <see cref="VerbRegistry"/>; injected manually by SceneViewAdapter
/// as the last SubOutcome of every synthetic ObservationObject.
/// </summary>
public sealed class IgnoreVerb : Verb
{
    public static readonly IgnoreVerb Instance = new();
    private IgnoreVerb() { }

    public override string VerbId      => "ignore";
    public override string DisplayName => "Move On";

    /// Always possible — the player can always choose not to act.
    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null) => true;

    /// Natural-language text presented to ThinkingExecutor's GOAL list.
    public override string Verbatim(Scene scene, PoV pov, Element target) => "move on";

    /// No-op: IgnoreVerb is detected before action generation; Execute() is never called.
    public override void Execute(Scene scene, PoV pov, Protagonist actor, Element target) { }
}
