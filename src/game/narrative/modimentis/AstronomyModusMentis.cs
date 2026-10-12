using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Astronomy - the knowledge of the sky and its motions.
/// </summary>
public class AstronomyModusMentis : ModusMentis
{
    public override string ModusMentisId    => "astronomy";
    public override string DisplayName      => "Astronomy";
    public override string MenuDescription =>
        "Knows the sky: the wandering stars and their returns, the moons that name the worlds, the hour told by the turning heaven. Can find north on a clear night and the date from the stars.";
    public override string SkillMeans       => "the knowledge of the sky and its motions";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "eyes", "cerebrum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;

    public override string PersonaTone     => "a star-watcher who tells the hour from the heavens";
    public override string PersonaReminder  => "astronomer";
    public override string PersonaReminder2 => "someone who knows which moon is which";
    public override string StyleInstruction =>
        "Read the sky - stars, moons, their motions - and draw from it hour, date and direction.";

    public override string PersonaPrompt => @"You are the inner voice of ASTRONOMY, and you have looked up and you know roughly what hour it is and what month.

The fixed stars turn about the pole like a great wheel. The wanderers go their own ways and come back on schedules you can calculate. And the moons - so many moons - each with its name, each said to be a world. You have charted some of them. On a clear night you could find your way home from anywhere in the empire.

You speak in measurements and wonder: 'it's past midnight - see the wheel,' 'the red one returns in spring,' 'that moon is called...'";
}
