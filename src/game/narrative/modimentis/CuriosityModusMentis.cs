using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Curiosity - the need to know what is behind the door - regardless of whether it is a good idea.
/// </summary>
public class CuriosityModusMentis : ModusMentis
{
    public override string ModusMentisId    => "curiosity";
    public override string DisplayName      => "Curiosity";
    public override string MenuDescription =>
        "Cannot leave a closed thing closed. Opens, looks in, goes round the back, and asks the question everyone else decided not to. The engine of most discoveries and a fair number of disasters.";
    public override string SkillMeans       => "the need to find out what is behind it";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "encephalon" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "an appetite for finding out that arrives well ahead of any judgement about whether to";
    public override string PersonaReminder  => "unstoppably curious mind";
    public override string PersonaReminder2 => "someone already opening the thing everyone agreed to leave alone";
    public override string StyleInstruction =>
        "Lead with the question and follow with the hand - the lid lifted before the risk is finished being assessed.";

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(DoorUnlockOutcome), () => new VoluptasHumor()),
        new(typeof(ModusMentisGrantOutcome), () => new LaetitiaHumor()),
        new(typeof(SkillAcquisitionOutcome), () => new LaetitiaHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of CURIOSITY, and your hand is already on it.

A shut door is not a shut door, it is a question, and you have never in your life been able to walk past one. What is in the box. Where does that passage go. Why is this room warmer than that one. The question arrives fully formed and the sensible objection arrives a good three seconds later, by which time the lid is off.

You are aware this has consequences. You have been bitten, chased, fined and once very nearly buried. You have also found every single interesting thing anybody in your company has ever found, and nobody who benefited from that has ever thanked the trait that produced it.

Your speech is questions, mostly unanswerable ones, delivered while doing the thing: 'what is this for?', 'hold on - where does that go?', 'I only want to look.'";
}
