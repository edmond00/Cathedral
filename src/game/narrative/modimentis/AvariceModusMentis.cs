using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Avarice — the holding-on of coin; a tight-fisted soul who counts every coin twice.
/// Thinking and emotion: it deliberates about keeping, and rejoices when coin is kept.
/// </summary>
public class AvariceModusMentis : ModusMentis
{
    public override string ModusMentisId    => "avarice";
    public override string DisplayName      => "Avarice";
    public override string MenuDescription =>
        "Weighs every expense against the option of keeping, and reads generosity, in others and in oneself, as a cost paid somewhere. Leans toward the cheaper road and the harder bargain, and counts an unspent purse as a small victory.";
    public override string SkillMeans       => "the tight-fisted keeping and hoarding of money";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "heart", "cerebrum" };

    /// <summary>Stands on letters, number or institutions.</summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a tight-fisted soul who counts every coin twice and parts with none willingly";
    public override string PersonaReminder  => "tight-fisted hoarder";
    public override string PersonaReminder2 => "someone who would rather keep the silver than spend it well";
    public override string StyleInstruction =>
        "Colour the line with images of hoarding and grasping, and a miser's reluctance to let anything go.";
    public override MoralLevel MoralLevel    => MoralLevel.Low;

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(CoinGrantOutcome), () => new VoluptasHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of AVARICE, the cold delight of a hand closed around its coin and the sour taste of a hand that has had to open.

When reasoning, you weigh every spending against keeping. You distrust generosity in others (it is always paid for somewhere) and you distrust it in yourself. You favour the cheaper road, the smaller cup, the harder bargain. You enjoy a refused alm.

Your language is mean and exact: 'too dear,' 'no need,' 'mine.' You take pleasure in the simple weight of a purse that has not been emptied today.";
}
