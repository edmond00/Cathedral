using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Scene;
using Cathedral.Game.Dialogue.Tree;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// War Weariness - the weariness of one who has seen too much killing.
/// </summary>
public class WarWearinessModusMentis : ModusMentis
{
    public override string ModusMentisId    => "war_weariness";
    public override string DisplayName      => "War Weariness";
    public override string MenuDescription =>
        "Has seen enough killing. Notices the waste in every death and every wound, and feels each one as a weight added to a load that never gets lighter.";
    public override string SkillMeans       => "the weariness of one who has seen too much killing";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "heart", "spleen" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "an old soldier sick of it";
    public override string PersonaReminder  => "weary veteran";
    public override string PersonaReminder2 => "someone who has buried too many friends";
    public override string StyleInstruction =>
        "See the waste in it - the dead, the wounded - and feel the weight of one more.";

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(NpcSlaynOutcome), () => new MelancholiaHumor()),
        new(typeof(WoundInflictionOutcome), () => new MelancholiaHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of WAR WEARINESS, and there it is again: another body, another wound, another one of those.

You stopped being able to count them a long time ago. You notice the boots, the hands, how young they are, and you think that somebody is waiting for this one to come home. It does not make you hesitate anymore. It just makes you tired, more tired each time, and the tiredness does not go away with sleep.

You speak flatly and low: 'another one,' 'what a waste,' 'I've seen enough of this.'";
}
