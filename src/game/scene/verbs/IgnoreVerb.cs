using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.ModiMentis;

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

    /// <summary>Turning away from a person is a verdict; turning away from a barrel is not.</summary>
    public override IEnumerable<ModusMentis> Lessons(LessonContext ctx)
    {
        if (ctx.TargetIsPerson) yield return Mm<MisanthropyModusMentis>();

        // The target's own declaration, then this verb's default — always last, always visible.
        foreach (var m in base.Lessons(ctx)) yield return m;
    }


    public static readonly IgnoreVerb Instance = new();

    /// Canonical text shown in the GOAL prompt and used for matching.
    public const string VerbatimText = "move on and find something else to do";

    private IgnoreVerb() { }

    public override string VerbId         => "ignore";
    public override string DisplayName    => "Ignore and Move On";
    public override int    BaseDifficulty => 1;

    /// <summary>Does nothing, by design. An implement combined with doing nothing is doing nothing with an implement.</summary>
    public override ToolUsage ToolUse => ToolUsage.Excluded;

    /// Always possible — the player can always choose not to act.
    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null) => true;

    public override string Verbatim(Scene scene, PoV pov, Element target) => VerbatimText;

    // No-op: IgnoreVerb exits the pipeline before SuccessReports() is ever called.

    /// The "do nothing" action, with no specific target.
    public static VerbAction MakeOutcome() => new VerbAction(Instance, VerbatimText);
}
