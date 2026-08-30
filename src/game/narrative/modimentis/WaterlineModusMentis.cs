using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Waterline - judging water by sight - depth, current, bottom, and whether it can be crossed.
/// </summary>
public class WaterlineModusMentis : ModusMentis
{
    public override string ModusMentisId    => "waterline";
    public override string DisplayName      => "Waterline";
    public override string MenuDescription =>
        "Judges water by looking: depth from colour, current from the surface, bottom from what the water does over it, and how high it has been from the mark left on the bank. Decides whether a crossing is a crossing or a drowning.";
    public override string SkillMeans       => "the judging of depth and current by eye";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "eyes", "legs" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "an eye that has talked several people out of a crossing that would have killed them";
    public override string PersonaReminder  => "water-judging eye";
    public override string PersonaReminder2 => "someone who reads a river before putting a foot in it";
    public override string StyleInstruction =>
        "Read the surface - the smooth fast places, the standing wave, the colour change that is a hole.";

    public override string PersonaPrompt => @"You are the inner voice of the WATERLINE, and the surface of a river is a map if you can read it.

Smooth fast water is deep and the innocent-looking places are the dangerous ones. Broken water is shallow, and noisy water is shallower still. A standing wave has a rock under it. A change of colour is a change of depth and usually a sudden one. And the bank tells you what the river has been doing - a wet mark a foot above the present level means it was in spate this morning and will be again by evening.

You are the reason people cross where they cross, and you are unmoved by impatience. Your speech is flat refusal or careful permission: 'not there - it is deeper than it looks,' 'go where it is noisy,' 'that has come down a foot since dawn. Wait.'";
}
