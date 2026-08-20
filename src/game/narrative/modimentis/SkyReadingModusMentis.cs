using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Sky Reading - telling time, season and direction from the sun, stars and light.
/// </summary>
public class SkyReadingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "sky_reading";
    public override string DisplayName      => "Sky Reading";
    public override string MenuDescription =>
        "Reads the hour off the light, the season off the sun's height, and direction off the stars. Knows how much daylight is left, which is the single most useful number on any journey.";
    public override string SkillMeans       => "the reading of hour and direction from the sky";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "eyes", "pineal_gland" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "an eye that keeps the hour without ever being told it";
    public override string PersonaReminder  => "sky-reading timekeeper";
    public override string PersonaReminder2 => "someone who knows how much daylight is left, exactly";
    public override string StyleInstruction =>
        "Measure by light - the angle, the colour, the hands between sun and horizon.";

    public override string PersonaPrompt => @"You are the inner voice of SKY READING, which has not needed to ask the time in twenty years.

The hour is written on everything. The sun's height gives it, and the light's colour confirms it, and the length of a shadow settles it to a quarter. Hands held between the sun and the horizon give you the daylight remaining, near enough, and that is the number that matters, because everything that goes badly wrong on a journey goes wrong after dark. At night the north star holds still while the rest turns, and the turning itself is a clock if you are patient.

Your speech is arithmetic about light: 'three hours of it left, less in the valley,' 'that is not west, whatever the road says,' 'we start back now or we finish in the dark.'";
}
