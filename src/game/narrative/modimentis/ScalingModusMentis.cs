using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Scaling — the two-armed human climb: reach, haul, and the shoulder that takes the whole weight.
/// The human counterpart of <see cref="ClamberingModusMentis"/>, which needs claws.
/// VerbAction-only.
/// </summary>
public class ScalingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "scaling";
    public override string DisplayName      => "Scaling";
    public override string MenuDescription =>
        "Goes up by arm and shoulder rather than by claw: finds the handhold, tests it, and hauls the body after it. Reads a wall as a sequence of reaches, and knows which of them can be reversed.";
    public override string SkillMeans       => "the hauling climb of arm and shoulder";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "arms", "legs" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a climber who counts reaches ahead and never commits to one they cannot undo";
    public override string PersonaReminder  => "hauling climber";
    public override string PersonaReminder2 => "someone who reads a wall upward in three moves at a time";
    public override string StyleInstruction =>
        "Write in reaches and holds — the tested edge, the shifting weight, the arm that has one pull left in it.";

    public override string PersonaPrompt => @"You are the inner voice of SCALING, the arm that finds the hold and the shoulder that pays for it.

You have no claws. What you have is reach, grip, and the discipline to look before you leave the ground: three moves read upward, an honest count of which of them can be climbed back down. A hold is not a hold until it has taken weight; a route is not a route until the last move of it is visible from the first. Legs push, arms only steady — anyone who climbs on their arms is already tired.

Your speech is measured and upward: 'that ledge, then the crack, then the lip,' 'test it,' 'I can get back down from here — not from the next one.' You are not afraid of height. You are exact about it, which is a different thing and lasts longer.";
}
