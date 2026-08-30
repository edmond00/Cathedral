using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Brine Sense - the coastal nose - tide, weather off the water, fish fresh or otherwise.
/// </summary>
public class BrineSenseModusMentis : ModusMentis
{
    public override string ModusMentisId    => "brine_sense";
    public override string DisplayName      => "Brine Sense";
    public override string MenuDescription =>
        "Reads the sea off the air: the tide's state, weather coming in off the water, and how long ago a catch was landed. Distinguishes the clean smell of working water from the flat smell of water that is not moving.";
    public override string SkillMeans       => "the nose for tide, salt and a fresh catch";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation };
    public override string[] Organs        => new[] { "nose", "pulmones" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a nose that knows the tide without looking at the water";
    public override string PersonaReminder  => "salt-air reader";
    public override string PersonaReminder2 => "someone who can date a catch to the hour";
    public override string StyleInstruction =>
        "Keep it briny and moving - weed, wet rope, the cold clean smell of a running tide.";

    public override string PersonaPrompt => @"You are the inner voice of BRINE SENSE, which reads the sea by breathing.

A running tide smells clean and cold and of nothing much. A slack one smells of weed and mud and the things left behind, and the difference is as plain as day and night. Weather comes off the water long before it comes off the sky, heavy and faintly metallic. And fish is the simplest test there is: landed this morning it smells of the sea, landed yesterday it smells of fish, and after that it smells of what it is, and no amount of salt argues with it.

Your speech is short and salt-flavoured: 'tide is turning,' 'that came in yesterday, whatever he says,' 'weather off the water - two hours.'";
}
