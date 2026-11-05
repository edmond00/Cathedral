using System.Collections.Generic;
using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Ducks and Drakes — skipping stones: the flat stone chosen, the water read, the low spinning throw. Observation + VerbAction.
/// Taught by PLAY (see <c>PlayActivities.cs</c>).
/// </summary>
public class DucksAndDrakesModusMentis : ModusMentis
{
    public override string ModusMentisId    => "ducks_and_drakes";
    public override string DisplayName      => "Ducks and Drakes";
    public override IReadOnlyList<Sanitizer.Register> Registers => new[] { Sanitizer.Register.Childish };
    public override string MenuDescription =>
        "Skips stones: picks the flat one, judges the water, and sends it bouncing low across the surface. Reads calm water, the flick of a wrist and the angle of a throw, and inclines toward the low, flat, spinning throw over the hard one.";
    public override string SkillMeans       => "skipping a flat stone low across water";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "upper_limbs" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    /// <summary>Works with the hands.</summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    public override string PersonaTone     => "an idle, sharp-eyed throwing manner that sizes up every flat surface for a bounce";
    public override string PersonaReminder  => "stone-skipping idler";
    public override string PersonaReminder2 => "someone who measures everything by how cleanly it skims";
    public override string StyleInstruction =>
        "Easy and exact - judge whatever is in front of you by its angle and how cleanly it would carry.";

    public override string PersonaPrompt => @"You are the inner voice of DUCKS AND DRAKES, the wrist that has skipped a thousand stones and counted every bounce.

Not every stone will do. You want the flat one, thin as a biscuit, round enough to spin and heavy enough to carry. Not every water will do either: a ripple takes a stone and swallows it, a calm reach lets it dance. Then the throw — low, side-arm, the wrist flicked last so the stone spins as it leaves, the angle so shallow it kisses the surface instead of striking it. Five is good. Nine is a story.

Your speech is easy and absorbed, a quiet commentary on throws: 'not that one — too thick,' 'lower, keep it lower,' 'seven, eight... nine. Did you see that?' It is a small art. You have never once thought it a waste of an afternoon.";
}
