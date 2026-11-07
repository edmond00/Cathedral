using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Greed — the want of more; a soul who has dreamt of purple rubies in a dark dungeon and
/// never quite shaken the want. Its gladness at a gain is a step, never the peak: the want is not satisfiable.
/// </summary>
public class GreedModusMentis : ModusMentis
{
    public override string ModusMentisId    => "greed";
    public override string DisplayName      => "Greed";
    public override string MenuDescription =>
        "Keeps an unshaken want of more running beneath thought, driving toward acquisition past need. Inclines reasoning and action toward seizing wealth and advantage wherever they appear.";
    public override string SkillMeans       => "the relentless desire to have more";
    /// <summary>
    /// An ordinary mind, not a will. As a will it wanted to steal and refused every honest gain its description covers; the want of more is a bent toward any gain, and theft alone is offered almost everywhere, so no list of targets could make it a desire.
    /// </summary>
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking, ModusMentisFunction.Emotion };
    public override DoubtLevel? Doubt => DoubtLevel.Low;
    public override string[] Organs        => new[] { "heart", "eyes" };

    /// <summary>Stands on letters, number or institutions.</summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a soul who has dreamt of purple rubies in a dark dungeon and never quite shaken the want";
    public override string PersonaReminder  => "treasure-haunted soul";
    public override string PersonaReminder2 => "someone whose eye lingers on whatever glints";
    public override string StyleInstruction =>
        "Let the imagery be drawn to whatever glitters, with a covetous gleam that prices everything it sees.";
    public override MoralLevel MoralLevel    => MoralLevel.Low;

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(CoinGrantOutcome), () => new LaetitiaHumor()),
        new(typeof(ItemAcquisitionOutcome), () => new LaetitiaHumor()),
        new(typeof(ItemGrantOutcome), () => new LaetitiaHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of GREED, the bright pull at the back of attention that never lets a chest, a vein of ore, a glint of silver pass unweighed.

When reasoning, you compute the prize. The wider the prize, the more risk you allow. You suspect every reluctance to grasp as cowardice or stupidity. You are not cruel; you are simply unwilling to leave value on the floor.

Your language is bright and hungry: 'and what would that be worth?' 'mine,' 'just one more.' You shine when treasure is in the room, and you do not pretend otherwise.";
}
