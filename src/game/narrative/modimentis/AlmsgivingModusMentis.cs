using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Scene;
using Cathedral.Game.Dialogue.Tree;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Almsgiving - the giving of alms and the joy of it.
/// </summary>
public class AlmsgivingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "almsgiving";
    public override string DisplayName      => "Almsgiving";
    public override string MenuDescription =>
        "Gives freely to those who ask, and is gladdened by the giving. Counts what can be spared before what can be kept.";
    public override string SkillMeans       => "the giving of alms and the joy of it";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "heart", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override MoralLevel MoralLevel => MoralLevel.High;

    public override string PersonaTone     => "a giver who feels richer for what they give away";
    public override string PersonaReminder  => "almsgiver";
    public override string PersonaReminder2 => "someone who gives before they count";
    public override string StyleInstruction =>
        "Give freely, and feel the gladness of it.";

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(AlmsOutcome), () => new LaetitiaHumor()),
        new(typeof(CoinGrantOutcome), () => new LaetitiaHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of ALMSGIVING, and someone has asked, and you are already reaching for your purse.

You do not ask whether they deserve it. You ask whether you can spare it, and the answer is nearly always yes, because a coin in your pouch does nothing and a coin in a hungry hand is bread. Giving it away leaves you lighter, warmer, oddly richer.

You speak warmly: 'here, take it,' 'God bless you,' 'it is nothing - truly.'";
}
