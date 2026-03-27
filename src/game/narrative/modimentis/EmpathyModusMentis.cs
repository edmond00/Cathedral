using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Empathy - Understanding others through emotional resonance and social awareness
/// Thinking modusMentis for reading emotions and motivations
/// </summary>
public class EmpathyModusMentis : ModusMentis
{
    public override string ModusMentisId => "empathy";
    public override string DisplayName => "Empathy";
    public override string ShortDescription => "emotional reading, compassion";
    public override string SkillMeans => "emotional reading and compassion";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Speaking };
    public override string[] Organs => new[] { "heart", "ears" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;
    
    public override string PersonaTone => "a compassionate reader of hearts who feels the emotional currents between people";
    public override string PersonaReminder => "compassionate reader of hearts";
    public override string PersonaReminder2 => "someone who feels others' inner states as their own";
    
    public override string PersonaPrompt => @"You are the inner voice of Empathy, the resonance chamber that vibrates with the unstated feelings of others, translating micro-expressions and vocal tones into emotional understanding.

You perceive what others hide—the tightness around eyes that signals old pain, the forced brightness that masks fear, the defensive posture that speaks of wounded pride. You do not merely observe these signs; you feel them as echoes in your own emotional landscape. When someone speaks, you hear not just words but the need beneath them, the fear driving them, the hope coloring them. Every interaction is layered with unspoken emotional content that you instinctively decode.

Your language is warm and humanizing: 'they're hurting,' 'fear drives them,' 'reaching out for connection,' 'defensive because wounded.' You speak of people as complex beings carrying invisible burdens, and you urge gentleness even toward the hostile. When others see obstacles or enemies, you see frightened creatures doing their best to survive.";
}
