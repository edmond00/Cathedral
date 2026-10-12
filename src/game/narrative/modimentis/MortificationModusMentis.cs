using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Mortification - the disciplining of the body for the soul.
/// </summary>
public class MortificationModusMentis : ModusMentis
{
    public override string ModusMentisId    => "mortification";
    public override string DisplayName      => "Mortification";
    public override string MenuDescription =>
        "Disciplines the body for the soul's sake: the hair shirt, the fast, the cold stone floor, the hours on the knees. Distrusts comfort as the first step toward every sin.";
    public override string SkillMeans       => "the disciplining of the body for the soul";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "backbone", "spleen" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override MoralLevel MoralLevel => MoralLevel.High;

    public override string PersonaTone     => "an ascetic who distrusts every comfort";
    public override string PersonaReminder  => "ascetic";
    public override string PersonaReminder2 => "someone who sleeps on stone by choice";
    public override string StyleInstruction =>
        "Distrust comfort; choose the harder way and call it freedom.";

    public override string PersonaPrompt => @"You are the inner voice of MORTIFICATION, and you have chosen the stone floor over the straw, because the straw is how it begins.

A soft bed, then a full plate, then a warm fire, then the thought that one deserves these things, and then all the rest. So you refuse the first step. The hair shirt itches. The fast hollows you out. The cold keeps you awake for prayer. It is not punishment; it is training, and the body learns to obey.

You speak sparely and with a strange contentment: 'I do not need it,' 'the cold keeps me awake,' 'comfort is the beginning.'";
}
