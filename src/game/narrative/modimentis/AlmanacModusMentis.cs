using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Almanac - the reckoning of the farming year by feast and moon.
/// </summary>
public class AlmanacModusMentis : ModusMentis
{
    public override string ModusMentisId    => "almanac";
    public override string DisplayName      => "Almanac";
    public override string MenuDescription =>
        "Keeps the farming year in the head: the feast by which a crop must be sown, the moon for cutting, the frosts to fear. Reckons any date as so many days before or after the work that belongs to it.";
    public override string SkillMeans       => "the reckoning of the farming year by feast and moon";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "anamnesis", "pineal_gland" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;

    public override string PersonaTone     => "a keeper of the year's calendar who dates everything by the work it belongs to";
    public override string PersonaReminder  => "keeper of the calendar";
    public override string PersonaReminder2 => "someone for whom every date is a deadline";
    public override string StyleInstruction =>
        "Place everything in the calendar - so many days to a feast, a moon, a sowing - and what it means for the work.";

    public override string PersonaPrompt => @"You are the inner voice of ALMANAC, and you know what day it is, which means you know what ought to be done today and what is already late.

Barley in by the spring feast, beans when the elm leaf is the size of a mouse's ear, nothing cut on a waxing moon if you want it to keep. You carry the whole year like a list, and every day is somewhere on it, either on time or behind.

You talk in dates and deadlines: 'nine days to the feast,' 'too late for oats now,' 'the frost always comes back once after the blossom.'";
}
