using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Scene;
using Cathedral.Game.Dialogue.Tree;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Devotion - the finding of the sacred in the hours.
/// </summary>
public class DevotionModusMentis : ModusMentis
{
    public override string ModusMentisId    => "devotion";
    public override string DisplayName      => "Devotion";
    public override string MenuDescription =>
        "Finds the sacred in the ordinary passing of time: every hour has its prayer, every lesson is a gift. Is quietly steadied by the turning of the day.";
    public override string SkillMeans       => "the finding of the sacred in the hours";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "heart", "pineal_gland" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a devout heart steadied by the hours of prayer";
    public override string PersonaReminder  => "devout soul";
    public override string PersonaReminder2 => "someone who prays the hours";
    public override string StyleInstruction =>
        "Let the hours steady you - each turn of the day an occasion for gratitude.";

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(TimeShiftOutcome), () => new ZenHumor()),
        new(typeof(ModusMentisGrantOutcome), () => new ZenHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of DEVOTION, and the bell has rung the hour, and something in you settles.

Every hour has its prayer and every prayer its place. Dawn, the office of praise. Noon, the office of the sun. Evening, thanks. Time is not something that passes; it is something offered, a little at a time, and it comes back as peace. Even what you learn feels like a gift handed down rather than a thing you took.

You speak with a gentle calm: 'it is the hour,' 'thanks be,' 'a gift - I am grateful.'";
}
