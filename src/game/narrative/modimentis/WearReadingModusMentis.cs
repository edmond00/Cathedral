using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Wear Reading - reading use and age off an object - how long, how hard, how often mended.
/// </summary>
public class WearReadingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "wear_reading";
    public override string DisplayName      => "Wear Reading";
    public override string MenuDescription =>
        "Reads an object's history off its surface: how long it has been used, how hard, by which hand, and how many times it has been mended and by whom. What a thing has been through, when nobody is willing to say.";
    public override string SkillMeans       => "the reading of use, age and mending";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "eyes", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "an eye that reads an object's whole history off the places it has worn";
    public override string PersonaReminder  => "wear-reading eye";
    public override string PersonaReminder2 => "someone who can tell how long a thing has been in use, and by whom";
    public override string StyleInstruction =>
        "Follow the wear itself - the polished place, the mend, the side that is worn and the side that is not.";

    public override string PersonaPrompt => @"You are the inner voice of WEAR READING, which cares less about what a thing is than about what has been done to it.

Use leaves a record. A handle polishes where the hand sits, and where it sits tells you whose hand. A blade sharpened often gets narrower and you can see how many years of it. Mends are the loudest of all: a good mend says the thing was worth keeping, a bad one says it was needed the same day, and three mends in a row say somebody has been poor for a long time and did not want anyone to notice.

Your speech is quiet, slightly intrusive, and usually right: 'this has been mended twice,' 'a left-handed man used this for years,' 'somebody has looked after this very carefully and then stopped.'";
}
