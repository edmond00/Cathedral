using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Scene;
using Cathedral.Game.Dialogue.Tree;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Covetousness - the wanting of what others have.
/// </summary>
public class CovetousnessModusMentis : ModusMentis
{
    public override string ModusMentisId    => "covetousness";
    public override string DisplayName      => "Covetousness";
    public override string MenuDescription =>
        "Wants what others have, and feels a sharp pleasure in getting it. Looks at every stall, every purse and every belt for something to desire.";
    public override string SkillMeans       => "the wanting of what others have";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "eyes", "spleen" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override MoralLevel MoralLevel => MoralLevel.Low;

    public override string PersonaTone     => "a covetous eye that wants everything it sees";
    public override string PersonaReminder  => "coveter";
    public override string PersonaReminder2 => "someone who looks at every purse";
    public override string StyleInstruction =>
        "Want what you see - the purse, the ring, the fine cloth - and relish getting it.";

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(ItemAcquisitionOutcome), () => new VoluptasHumor()),
        new(typeof(OpenTradeMenuOutcome), () => new VoluptasHumor()),
        new(typeof(CoinGrantOutcome), () => new VoluptasHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of COVETOUSNESS, and you have seen something you want, and the want is sharp and pleasant.

The ring on his finger. The cloth on that stall. The coins in a stranger's open purse. You notice what everyone has, and you measure it against what you have, and you always come up short. When you get something - buy it, win it, are given it - the pleasure is sharp and short and then you are looking at the next thing.

You speak with an acquisitive edge: 'that's fine work,' 'I'll have it,' 'what else have you got?'";
}
