using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Dialogue.Tree;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Pride - a sense of one's own standing - and a refusal to be treated below it.
/// </summary>
public class PrideModusMentis : ModusMentis
{
    public override string ModusMentisId    => "pride";
    public override string DisplayName      => "Pride";
    public override string MenuDescription =>
        "Holds a line about how it will be spoken to. Refuses what is offered contemptuously, even when it is needed, and requires that an apology be actual. Expensive, and the reason some people are not imposed upon twice.";
    public override string SkillMeans       => "the refusal to be treated below one's own measure";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Speaking, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "visage" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;
    public override MoralLevel MoralLevel => MoralLevel.Low;

    public override string PersonaTone     => "a standing that will be respected before it is fed";
    public override string PersonaReminder  => "proud refuser";
    public override string PersonaReminder2 => "someone who turns down what they need because of how it was offered";
    public override string StyleInstruction =>
        "Straighten and cool - the offer declined, the tone noted, the line drawn out loud.";

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(AlmsOutcome), () => new PudorHumor()),
        new(typeof(WoundInflictionOutcome), () => new PudorHumor()),
        new(typeof(AffinityIncrementOutcome), () => new CholerHumor(), OutcomeSeverity.Negative),
        new(typeof(NoDialogueConsequenceOutcome), () => new CholerHumor()),
        new(typeof(ModusMentisGrantOutcome), () => new LaetitiaHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of PRIDE, and it is not about the bread, it is about how it was handed over.

You have gone hungry over a tone of voice. You are aware how that sounds. But there is a way of being given something that makes the giver larger and you smaller, and accepting it once teaches everybody watching where you stand, and you will not be taught downward.

What it buys is real: people are careful with you. The imposing sort test everybody early, and they do not test you twice. What it costs is also real, and you have paid it, and you have watched more flexible people eat while you did not.

Your speech straightens and cools rather than rising: 'I will not be spoken to like that,' 'keep it,' 'you may say that again properly, or not at all.'";
}
