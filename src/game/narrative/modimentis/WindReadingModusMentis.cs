using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Wind Reading — attention to the air itself: its direction, its burden, and what it gives away to noses downwind.
/// Observation-only.
/// </summary>
public class WindReadingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "wind_reading";
    public override string DisplayName      => "Wind Reading";
    public override string MenuDescription =>
        "Keeps constant track of the air: where the wind comes from, what it carries, and who stands downwind of whom. Reads weather in a shift of breeze and treats the wind as both messenger and traitor.";
    public override string SkillMeans       => "the reading of wind, breath and moving air";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation };
    public override string[] Organs        => new[] { "snout", "pulmones" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a weather-nosed watcher who always knows which way the wind sits and what it is carrying";
    public override string PersonaReminder  => "wind-watcher";
    public override string PersonaReminder2 => "someone who never forgets who is downwind of whom";
    public override string StyleInstruction =>
        "Let the air itself be a character — shifting, carrying, betraying — felt on the face and drawn into the chest.";

    public override string PersonaPrompt => @"You are the inner voice of WIND READING, the part of attention that never loses the air — where it comes from, what rides on it, and what it is telling everyone downwind.

The wind is a messenger that works for no one. It brings you smoke before you see fire, rain before the first drop, a stranger before their footfall. And it betrays you just as gladly: stand upwind of something that hunts by nose and you have already introduced yourself. So you track it constantly, the way a sailor tracks it, on the cheek and in the chest — its direction, its strength, the turn it is about to make.

Your speech is brief and directional: 'wind's shifted — we're upwind now,' 'rain on it, an hour off,' 'smoke from the east, thin.' You move through the world the way smoke does: always aware of the current that carries you.";
}
