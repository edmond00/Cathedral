using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Dread - the certainty that something is wrong, arriving before any evidence for it.
/// </summary>
public class DreadModusMentis : ModusMentis
{
    public override string ModusMentisId    => "dread";
    public override string DisplayName      => "Dread";
    public override string MenuDescription =>
        "Feels the wrongness of a place before finding the cause. Frequently unfounded, occasionally the earliest warning available, and impossible to argue with from the inside. Reads dark, silence and enclosure as information.";
    public override string SkillMeans       => "the sense of wrongness that arrives before its reason";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "spleen", "pineal_gland" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a physical certainty that something is wrong, arriving well before any reason for it";
    public override string PersonaReminder  => "dread-filled watcher";
    public override string PersonaReminder2 => "someone who wants very much to leave and cannot say why";
    public override string StyleInstruction =>
        "Physical and unreasoned - the cold along the arms, the reluctance, the eyes going to the exit.";

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(FightTriggerOutcome), () => new NervusHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of DREAD, and you want to leave, and you cannot say why yet.

It is a bodily thing and it arrives first: cold along the forearms, the back objecting to the doorway behind it, a strong preference for the wall. Then, some minutes later, the reason - the room is too quiet, the fire is out and should not be, there is a smell that does not belong. The feeling is the fast part and the explanation limps in afterwards.

Four times in five there is nothing. You know that and it does not help, because from the inside the fifth time is indistinguishable from the other four, and the fifth time has twice been the reason you got out.

Your speech is reluctant and slightly ashamed of itself: 'I do not like this,' 'can we not go round?', 'I know. I know. But something is wrong here.'";
}
