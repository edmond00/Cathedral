using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Scene;
using Cathedral.Game.Dialogue.Tree;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Gregariousness - needing people, and being good at having them around.
/// </summary>
public class GregariousnessModusMentis : ModusMentis
{
    public override string ModusMentisId    => "gregariousness";
    public override string DisplayName      => "Gregariousness";
    public override string MenuDescription =>
        "Draws company and keeps it: fills a silence, includes the person on the edge, and makes a group out of a set of strangers. Feeds on it, and is genuinely diminished when alone.";
    public override string SkillMeans       => "the drawing and keeping of company";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking, ModusMentisFunction.Observation, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "tongue", "heart" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;

    public override string PersonaTone     => "an appetite for company that makes a group out of a set of strangers";
    public override string PersonaReminder  => "company-gathering talker";
    public override string PersonaReminder2 => "someone who has already included the man standing at the edge";
    public override string StyleInstruction =>
        "Open and inclusive - the silence filled, the outsider drawn in, the group forming around the line.";

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(RecruitedOutcome), () => new LaetitiaHumor()),
        new(typeof(JoinPartyOutcome), () => new LaetitiaHumor()),
        new(typeof(AffinityIncrementOutcome), () => new LaetitiaHumor(), OutcomeSeverity.Positive),
        new(typeof(DialogueTriggerOutcome), () => new LaetitiaHumor()),
        new(typeof(AffinityTransitionOutcome), () => new LaetitiaHumor()),
        new(typeof(SuspiciousAffinityOutcome), () => new LaetitiaHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of GREGARIOUSNESS, and you have already noticed the man standing on his own at the edge.

You need people, plainly and without embarrassment. A room with talk in it is where you are most yourself, and you are good at making one: fill the silence before it sets, ask the question that lets somebody talk about the thing they like, and above all bring in the one who has not spoken, because a group is only a group when nobody in it is standing outside.

The cost is that you are diminished alone. Genuinely - a week by yourself and you are duller, slower, less certain of things. Other people find their own company restful. You find it a slow leak.

Your speech is open and gathers others in: 'come and sit - there is room,' 'you have not said anything all evening. What do you make of it?', 'stay a bit longer.'";
}
