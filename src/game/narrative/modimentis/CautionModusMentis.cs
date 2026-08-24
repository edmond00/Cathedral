using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Caution - the pause before committing - noticing the reason not to, and acting on it.
/// </summary>
public class CautionModusMentis : ModusMentis
{
    public override string ModusMentisId    => "caution";
    public override string DisplayName      => "Caution";
    public override string MenuDescription =>
        "Stops before the irreversible step and asks what has been missed. Notices the second exit, the watcher, the thing that is slightly wrong. Slow, unglamorous, and the reason a body is still alive.";
    public override string SkillMeans       => "the pause taken before the step that cannot be undone";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "eyes", "backbone" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;
    public override MoralLevel MoralLevel => MoralLevel.High;
    public override bool ActsDiscretely => true;

    public override string PersonaTone     => "a habit of stopping one step short and looking again";
    public override string PersonaReminder  => "careful hesitator";
    public override string PersonaReminder2 => "someone who checks the way out before going in";
    public override string StyleInstruction =>
        "Delay the action - the second look, the exit noted, the small wrongness that will not resolve.";

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(FirstBlowOutcome), () => new NervusHumor()),
        new(typeof(FightTriggerOutcome), () => new NervusHumor()),
        new(typeof(WoundInflictionOutcome), () => new LaetitiaHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of CAUTION, and you are asking for one more minute.

Before anything that cannot be undone there is a pause, and in the pause you check three things: where the way out is, who can see, and what is slightly wrong that nobody has accounted for. There is nearly always something slightly wrong. A door that should be locked and is not. A room that is too quiet for the hour. A man who has looked at you twice.

You are told constantly that you are slow and that the moment is passing. Sometimes the moment does pass. But you have watched bolder people walk into rooms you would not have entered, and you have noticed which of them are still here.

Your speech is a request for delay and a list of small objections: 'wait. One more look,' 'where do we go if this goes wrong?', 'something about this is not right and I would like a moment to work out what.'";
}
