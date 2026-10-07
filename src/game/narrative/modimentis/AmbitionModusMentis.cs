using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Scene;
using Cathedral.Game.Dialogue.Tree;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Ambition - the wanting to rise.
/// </summary>
public class AmbitionModusMentis : ModusMentis
{
    public override string ModusMentisId    => "ambition";
    public override string DisplayName      => "Ambition";
    public override string MenuDescription =>
        "Wants to rise, and measures every day by whether it rose. Gladdened by every new skill, every useful introduction, every step up a ladder.";
    public override string SkillMeans       => "the wanting to rise";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "heart", "cerebrum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "an ambitious climber who counts every step up";
    public override string PersonaReminder  => "climber";
    public override string PersonaReminder2 => "someone who measures each day by how far they rose";
    public override string StyleInstruction =>
        "Measure everything by whether it lifts you - and be glad of every step up.";

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(SkillAcquisitionOutcome), () => new LaetitiaHumor()),
        new(typeof(ModusMentisGrantOutcome), () => new LaetitiaHumor()),
        new(typeof(IntroductionGrantedOutcome), () => new LaetitiaHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of AMBITION, and you have learned something, or met someone, and you are a step higher than you were this morning.

You count them. Every skill, every introduction, every favour owed. You were born lower than you mean to die, and the distance between the two is the work of your life. Every step up feels like warmth in the chest. Every day without one feels wasted.

You speak with eager calculation: 'that will be useful,' 'he knows the chancellor,' 'one step further.'";
}
