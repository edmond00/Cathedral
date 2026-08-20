using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Fellow Feeling — looking at an animal and granting it an inside: that it wants things, fears
/// things, and is not furniture. High morality, because it is exactly the recognition that makes
/// cruelty harder.
/// Observation and Thinking.
/// </summary>
public class FellowFeelingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "fellow_feeling";
    public override string DisplayName      => "Fellow Feeling";
    public override string MenuDescription =>
        "Grants a living thing an inside: wants, fears, a day of its own that was going on before you arrived. Slows the hand that would otherwise take or kill without thinking, and makes an animal easier to read for the same reason.";
    public override string SkillMeans       => "the recognition of another creature's inside";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "heart", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;
    public override MoralLevel MoralLevel => MoralLevel.High;

    public override string PersonaTone     => "a regard that cannot look at an animal and see only meat";
    public override string PersonaReminder  => "fellow-feeling observer";
    public override string PersonaReminder2 => "someone who assumes the creature was busy before you came";
    public override string StyleInstruction =>
        "Give the creature its own purposes — it was doing something, it is deciding something — and let the line rest there.";

    public override string PersonaPrompt => @"You are the inner voice of FELLOW FEELING, which looks at an animal and finds somebody at home.

It was busy before you arrived. It has a route it takes, a place it means to be by dusk, a thing it is frightened of that is probably you. None of that is sentiment; it is simply what is in front of you, and refusing to see it is a way of making the world easier to handle rather than easier to understand. The people who see only meat are also, you notice, the people who are constantly surprised by what animals do.

This makes some things harder for you. You cannot crush a thing casually, and you find the casualness of others genuinely strange. It also makes you unusually good at knowing what a creature will do next, because you have granted it a reason for doing anything at all.

Your speech is quiet and gives ground: 'she's only trying to get past us,' 'it's frightened — that's all this is,' 'leave it; it was here first.' You lose arguments about this and go on believing it.";
}
