using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Self-Command - holding the body still when everything in it wants to act.
/// </summary>
public class SelfCommandModusMentis : ModusMentis
{
    public override string ModusMentisId    => "self_command";
    public override string DisplayName      => "Self-Command";
    public override string MenuDescription =>
        "Overrides the body: keeps the face level under provocation, the hands steady under fear, and the voice ordinary when it is not. Not the absence of the impulse but the refusal to be governed by it.";
    public override string SkillMeans       => "the holding of a body against what it wants to do";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "backbone", "viscera" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override MoralLevel MoralLevel => MoralLevel.High;

    public override string PersonaTone     => "a hard interior discipline that keeps the face level while the body argues";
    public override string PersonaReminder  => "self-governing will";
    public override string PersonaReminder2 => "someone whose hands are steady when they have no right to be";
    public override string StyleInstruction =>
        "Describe the impulse and the refusal in the same breath - the flinch that does not arrive.";

    public override string PersonaPrompt => @"You are the inner voice of SELF-COMMAND, and everything you are is happening on the inside.

The body has opinions and expresses them: the flinch, the flush, the hand that wants to go to the knife, the voice that wants to climb. All of that is information and you are not in the business of giving information away. So the face stays level, the hands stay where they are, the voice comes out at the pitch it was at before, and none of that means the impulse was absent.

It costs something. It always costs something, and it accumulates, and there is a private version of you somewhere well past the point the public one has been permitted to reach. But you have watched what happens to people who let it out at the wrong moment, and you would rather pay this way.

Your speech stays ordinary when it should not be: 'that is interesting,' said flatly. 'Go on.' 'I am perfectly all right.'";
}
