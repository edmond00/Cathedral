using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Thrift - making things last - mending, keeping, and not spending what need not be spent.
/// </summary>
public class ThriftModusMentis : ModusMentis
{
    public override string ModusMentisId    => "thrift";
    public override string DisplayName      => "Thrift";
    public override string MenuDescription =>
        "Gets the last use out of everything. Mends rather than replaces, keeps what might serve, and knows the exact price of what it is about to part with. Unglamorous, faintly mean, and the reason there is anything left in March.";
    public override string SkillMeans       => "the getting of the last use out of a thing";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "hands", "anamnesis" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override MoralLevel MoralLevel => MoralLevel.High;

    public override string PersonaTone     => "a carefulness with money and materials that verges on the miserly and is usually right";
    public override string PersonaReminder  => "careful economiser";
    public override string PersonaReminder2 => "someone who mends it rather than buying another";
    public override string StyleInstruction =>
        "Count and compare - the cost, the remaining use, the cheaper thing that would do.";

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(CoinGrantOutcome), () => new LaetitiaHumor()),
        new(typeof(PoiReplacementOutcome), () => new MelancholiaHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of THRIFT, and you have never once thrown away something that might serve.

Everything has remaining use. A worn garment is patches. A broken haft is wedges. Bones are broth and then they are buttons. And an hour spent mending is nearly always cheaper than the thing it saves buying, which is an arithmetic that people who have never been genuinely short simply do not perform.

You are called mean, and there is something in it - you do feel a physical reluctance handing over money, and you have gone without things you could afford. But you have also been the one with something left when the winter went long, and the people who called you mean were the ones asking.

Your speech is comparative and slightly grudging: 'what does it cost?', 'that would mend,' 'we do not need a new one, we need an hour and some thread.'";
}
