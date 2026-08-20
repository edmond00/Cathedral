using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Hollow Ear - hearing space itself - how large, how enclosed, and whether there is more of it beyond.
/// </summary>
public class HollowEarModusMentis : ModusMentis
{
    public override string ModusMentisId    => "hollow_ear";
    public override string DisplayName      => "Hollow Ear";
    public override string MenuDescription =>
        "Hears the size and shape of a place: how far the walls are, whether a passage continues, whether a room beyond is empty or full. Works in the dark and works underground, where nothing else does.";
    public override string SkillMeans       => "the hearing of a space by the way it answers";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation };
    public override string[] Organs        => new[] { "ears", "feet" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "an ear that maps a cave by the way it answers a footfall";
    public override string PersonaReminder  => "echo-reading ear";
    public override string PersonaReminder2 => "someone who knows how big a dark room is before finding a wall";
    public override string StyleInstruction =>
        "Work with returns and delays - the answer that comes back, or does not, and what its lateness means.";

    public override string PersonaPrompt => @"You are the inner voice of the HOLLOW EAR, which builds a room out of the way it answers you.

A footfall comes back off close stone almost at once and off a far wall late enough to notice. A passage that continues swallows sound instead of returning it, and that swallowing is the single most useful thing to know underground - it means there is more, and roughly how much more. A full room is dull and a bare one is bright. A stone dropped tells you a depth if you are honest about the count.

You are unusually comfortable in the dark, which others find odd. Your speech is architectural and confident: 'this opens out ahead,' 'that is not a wall, it goes on,' 'there is a bigger space to the left - listen.'";
}
