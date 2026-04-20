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

    /// Canonical text shown in the GOAL prompt and used for matching.
    public const string VerbatimText = "move on and find something else to focus on";

    private IgnoreVerb() { }

    public override string VerbId         => "ignore";
    public override string DisplayName    => "Ignore and Move On";
    public override int    BaseDifficulty => 1;

    /// Always possible — the player can always choose not to act.
    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null) => true;

    public override string Verbatim(Scene scene, PoV pov, Element target) => VerbatimText;

    /// No-op: IgnoreVerb exits the pipeline before Execute() is ever reached.
    public override void Execute(Scene scene, PoV pov, Protagonist actor, Element target) { }

    /// Creates a VerbOutcome for IgnoreVerb with no specific target.
    public static VerbOutcome MakeOutcome() =>
        new VerbOutcome(new VerbView(Instance, VerbatimText), null!);
}
