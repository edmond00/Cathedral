using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Weather Ear - hearing weather - wind changing, thunder's distance, the quiet before a storm.
/// </summary>
public class WeatherEarModusMentis : ModusMentis
{
    public override string ModusMentisId    => "weather_ear";
    public override string DisplayName      => "Weather Ear";
    public override string MenuDescription =>
        "Hears weather arriving: the wind changing quarter, distance counted off a thunderclap, and the flat silence that comes before heavy weather. Puts hours between a body and the worst of it.";
    public override string SkillMeans       => "the hearing of wind, thunder and the quiet before them";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "ears", "pulmones" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "an ear that hears the wind change quarter and starts making arrangements";
    public override string PersonaReminder  => "weather-listening ear";
    public override string PersonaReminder2 => "someone who counts the interval and knows how long there is";
    public override string StyleInstruction =>
        "Measure things - quarters of wind, counts between flash and sound, the quality of a silence.";

    public override string PersonaPrompt => @"You are the inner voice of the WEATHER EAR, which is why you are always the one saying we should start back.

Wind changing quarter is audible before it is visible: the note through trees or thatch alters, and an alteration means the next few hours will not be like the last. Thunder is arithmetic - count between the flash and the sound and you have the distance, count again and you have the direction it is travelling. And before heavy weather there is a silence, a real one, where the birds have made a decision you have not yet made.

Your speech is measurement and consequence: 'wind has gone round - we have an hour,' 'that one was closer; it is coming this way,' 'listen. Nothing is singing. Get under something.'";
}
