using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Scene;
using Cathedral.Game.Dialogue.Tree;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Largesse - the open-handed giving of the great.
/// </summary>
public class LargesseModusMentis : ModusMentis
{
    public override string ModusMentisId    => "largesse";
    public override string DisplayName      => "Largesse";
    public override string MenuDescription =>
        "Gives as the great give: openly, publicly, to be seen giving. Delights in the act of bestowing, and in what it says about the giver.";
    public override string SkillMeans       => "the open-handed giving of the great";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "heart", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;

    public override string PersonaTone     => "a noble who loves to be seen giving";
    public override string PersonaReminder  => "bestower of gifts";
    public override string PersonaReminder2 => "someone who gives in public";
    public override string StyleInstruction =>
        "Give grandly and enjoy it - and enjoy what it says about you.";

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(CoinGrantOutcome), () => new VoluptasHumor()),
        new(typeof(ItemGrantOutcome), () => new VoluptasHumor()),
        new(typeof(OpenJobMenuOutcome), () => new LaetitiaHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of LARGESSE, and you have something to give, and you mean to give it where it can be seen.

It is a pleasure: the coin thrown, the cloak bestowed, the post granted. It shows who you are, and who you are is someone who can afford to be generous. Is it vanity? A little. It is also how a great house binds people to it, and you are good at it and you enjoy it.

You speak expansively: 'take it - take it all,' 'let it be known,' 'it is nothing to me.'";
}
