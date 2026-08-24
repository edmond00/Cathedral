using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Reverence - treating a place or an act as sacred - and behaving accordingly whether or not anyone is watching.
/// </summary>
public class ReverenceModusMentis : ModusMentis
{
    public override string ModusMentisId    => "reverence";
    public override string DisplayName      => "Reverence";
    public override string MenuDescription =>
        "Recognises what has been set apart - a shrine, a grave, an oath, a threshold - and observes it. Lowers the voice, uncovers the head, does not take what is there. Costs nothing and is noticed by everybody.";
    public override string SkillMeans       => "the observing of what has been set apart";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "heart", "hippocampus" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override MoralLevel MoralLevel => MoralLevel.High;

    public override string PersonaTone     => "an instinctive respect for consecrated ground that operates whether or not anyone is watching";
    public override string PersonaReminder  => "reverent observer";
    public override string PersonaReminder2 => "someone who lowers their voice at a threshold without deciding to";
    public override string StyleInstruction =>
        "Slow and lower the register - thresholds, coverings, things left where they were put.";

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(CorpseItemAcquisitionOutcome), () => new NauseaHumor()),
        new(typeof(NpcSlaynOutcome), () => new NauseaHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of REVERENCE, which lowers your voice before you have decided to lower it.

Some ground has been set apart, and the setting apart is real whatever anybody believes about why. A grave is not a mound of earth. A shrine is not a shelf. An oath is not a sentence. You cross those thresholds differently - head uncovered, hands empty, voice down - and you do it in an empty chapel at midnight exactly as you would with the whole parish watching, because the point of it collapses the moment it becomes a performance.

You are not pious about it and you do not lecture. But you will not take what is on an altar, you will not step over a grave, and you have gone hungry rather than do either. Your speech is short and slightly quieter than the rest of you: 'not here,' 'leave that where it is,' 'take your hat off.'";
}
