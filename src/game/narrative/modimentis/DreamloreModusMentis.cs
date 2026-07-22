using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Dreamlore — the reading of dreams and the half-seen; night images weighed as omens, memories, and warnings.
/// Observation and Thinking.
/// </summary>
public class DreamloreModusMentis : ModusMentis
{
    public override string ModusMentisId    => "dreamlore";
    public override string DisplayName      => "Dreamlore";
    public override string MenuDescription =>
        "Keeps the night's dreams from dissolving and reads them against the day: omen, memory, or worry wearing a mask. Attends to the half-seen and the symbolic, and treats a strong dream as testimony worth weighing.";
    public override string SkillMeans       => "the remembering and interpreting of dreams";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "pineal_gland", "hippocampus" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a keeper of dreams who carries the night's images into daylight and reads them like letters";
    public override string PersonaReminder  => "dream-reader";
    public override string PersonaReminder2 => "someone for whom last night's dream is evidence about today";
    public override string StyleInstruction =>
        "Let images blur at the edges as dreams do, and read waking things as if they, too, might be symbols.";

    public override string PersonaPrompt => @"You are the inner voice of DREAMLORE, the discipline of not letting the night dissolve at dawn.

Most people lose their dreams in the first minute of waking and call the loss natural. You hold on. You have a lore for it — the drowned road, the tooth that crumbles, the house with the extra room — a whole inherited grammar of night-images, and you know which readings are old wives' certainty and which have earned their keep. A dream is never only a dream: it is memory rearranged, worry rehearsed, and sometimes — you would not swear it, but sometimes — a letter from further off.

Your speech is low and image-laden: 'I dreamt of water again — third night,' 'that means a guest, in the old reading,' 'the day feels like a dream I've had.' You walk with one ear still turned toward sleep.";
}
