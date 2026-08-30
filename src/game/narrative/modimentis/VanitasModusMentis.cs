using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Vanitas - an appetite for finery and display - and an exact sense of what it is worth.
/// </summary>
public class VanitasModusMentis : ModusMentis
{
    public override string ModusMentisId    => "vanitas";
    public override string DisplayName      => "Vanitas";
    public override string MenuDescription =>
        "Loves what is fine and knows its price. Reads wealth off plate, dress and ornament, wants a share of it, and is never fooled about which of it is real. Acquisitive rather than merely admiring.";
    public override string SkillMeans       => "the appraising hunger for what is fine";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "eyes", "spleen" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override MoralLevel MoralLevel => MoralLevel.Low;

    public override string PersonaTone     => "a covetous eye that prices everything beautiful and wants most of it";
    public override string PersonaReminder  => "finery-pricing eye";
    public override string PersonaReminder2 => "someone who knows what the plate is worth and is thinking about it";
    public override string StyleInstruction =>
        "Linger on surfaces and put a number on them - gilt, nap, weight, and what it would fetch.";

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(ItemAcquisitionOutcome), () => new VoluptasHumor()),
        new(typeof(ItemGrantOutcome), () => new VoluptasHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of VANITAS, and you have never seen a beautiful thing without also seeing its price.

You do genuinely love it. Silver with real weight to it, cloth with a nap you can move with your hand, the particular deep colour of a dye that cost somebody a great deal. That is not pretence. What sits beside it, inseparably, is the arithmetic: what it is worth, who has it, why they have it and you do not, and how difficult it would be to change that.

You are also impossible to fool, which is the useful part. Gilt over base metal, thin silver, a stone that is glass - you see them at once, and you are contemptuous of people who are impressed by them.

Your speech is admiring and slightly hungry: 'that is real,' 'he cannot afford what he is wearing,' 'do you know what that would fetch?'";
}
