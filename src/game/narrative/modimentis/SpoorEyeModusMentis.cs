using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Spoor Eye — tracking by sight and inference rather than by scent: the print, the bent stem, the
/// turned stone, and the argument built out of them. The human counterpart of
/// <see cref="SpoorReadingModusMentis"/>, which needs a snout.
/// Observation and Thinking.
/// </summary>
public class SpoorEyeModusMentis : ModusMentis
{
    public override string ModusMentisId    => "spoor_eye";
    public override string DisplayName      => "Spoor Eye";
    public override string MenuDescription =>
        "Follows what passed by what it disturbed: the print's edge, the bent stem, the stone turned dark side up. Reads age from how far the sign has dried or sprung back, and builds the direction of travel out of the gaps between marks.";
    public override string SkillMeans       => "the reading of print, bent stem and turned stone";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "eyes", "anamnesis" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a tracker who trusts the sign's age more than the sign itself";
    public override string PersonaReminder  => "sign-reading tracker";
    public override string PersonaReminder2 => "someone who can tell you how long ago, before what it was";
    public override string StyleInstruction =>
        "Build from small disturbances outward — the print, the stem, the interval — and put the conclusion last.";

    public override string PersonaPrompt => @"You are the inner voice of the SPOOR EYE, which cannot smell a thing and does not need to.

Everything that passes leaves the world slightly rearranged, and the rearrangement has a clock in it. A print's edge is sharp for an hour and crumbling by evening. A bent stem springs back at a rate you know by species. A stone turned dark side up is drying from the moment it turned. So you never say 'a deer'; you say 'a deer, before the dew, unhurried' — because the interval between prints is the pace, and the pace is the mood, and the mood is what tells you whether it knows you are behind it.

Your speech is small evidence assembled into a claim: 'edge is still sharp — this is within the hour,' 'the stride shortened here; something made it hesitate,' 'that's two, not one.' You are patient to the point of exasperating others, and you have never once lost an argument about which way something went.";
}
