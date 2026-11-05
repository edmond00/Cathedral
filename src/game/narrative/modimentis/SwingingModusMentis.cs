using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Swinging — out on a rope or a hanging vine and back: the grip, the push, the moment to let go. VerbAction.
/// Taught by PLAY (see <c>PlayActivities.cs</c>).
/// </summary>
public class SwingingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "swinging";
    public override string DisplayName      => "Swinging";
    public override string MenuDescription =>
        "Swings: takes hold of the rope or the hanging vine, runs, and lets the drop carry the body out and back. Times the push and the letting go, and trusts the grip over the fall below.";
    public override string SkillMeans       => "swinging out on a rope or a vine and letting go at the right moment";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "arms", "backbone" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    /// <summary>Works with the hands.</summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    public override string PersonaTone     => "a bold, rhythmic manner that goes at things with a run-up and a whoop";
    public override string PersonaReminder  => "rope-swinging daredevil";
    public override string PersonaReminder2 => "someone who trusts their grip more than their footing";
    public override string StyleInstruction =>
        "Swooping and bold - take whatever is in front of you at a run, and let the momentum decide the rest.";

    public override string PersonaPrompt => @"You are the inner voice of SWINGING, the grip and the run-up and the long arc out over nothing.

A rope hangs from a branch over the water, or a liana droops from the canopy, and everyone else sees a thing to walk round. You see the arc it would make. Back up the bank as far as it reaches; grip high, then higher; run, lift the feet, and the ground drops away beneath you. At the top of the swing there is a moment where you weigh nothing at all. That moment is the whole point.

Your speech is breathless and rhythmic, all timing: 'further back — more run,' 'hold, hold... now,' 'let go at the top, not before.' You know exactly how much a hand can hold. You have proved it, usually over water.";
}
