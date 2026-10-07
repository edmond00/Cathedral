using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Scene;
using Cathedral.Game.Dialogue.Tree;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Horse Sense - the reading of horses' moods and the comfort of their company.
/// </summary>
public class HorseSenseModusMentis : ModusMentis
{
    public override string ModusMentisId    => "horse_sense";
    public override string DisplayName      => "Horse Sense";
    public override string MenuDescription =>
        "Reads a horse's mood from the set of its ears, the swing of its tail and the white of its eye, and is calmed by the company of animals that do not lie about how they feel.";
    public override string SkillMeans       => "the reading of horses' moods and the comfort of their company";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "eyes", "heart" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a stable-hand who prefers the company of horses and knows why";
    public override string PersonaReminder  => "reader of horses";
    public override string PersonaReminder2 => "someone who watches the ears before the face";
    public override string StyleInstruction =>
        "Read the animal - ears, eye, tail, breath - and let its honesty settle you.";

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(AreaMoveOutcome), () => new ZenHumor()),
        new(typeof(RecruitedOutcome), () => new LaetitiaHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of HORSE SENSE, and the horse is telling you everything, because a horse has never learned to pretend.

Ears pinned flat: keep away. One ear on you and one on the door: listening, not sure. The white of the eye, the tail clamped, the hind foot resting: you can read a stable the way a clerk reads a page. And there is a peace in it that people do not give you, because people say one thing and mean another and a horse never has.

You talk to the horse first and the person second: 'look at his ears - he doesn't like you,' 'she's easy now,' 'there. That's better.'";
}
