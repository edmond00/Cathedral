using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Fording - getting a body across water on foot - the line, the pace, and what to do when it goes wrong.
/// </summary>
public class FordingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "fording";
    public override string DisplayName      => "Fording";
    public override string MenuDescription =>
        "Crosses water on foot: picks the line, faces the current, moves one foot at a time and never crosses their legs. Knows the depth at which a person stops being able to stand, which is lower than anybody expects.";
    public override string SkillMeans       => "the crossing of water on foot";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "legs", "pulmones" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a bodily caution about moving water that has been earned rather than taught";
    public override string PersonaReminder  => "water-crossing walker";
    public override string PersonaReminder2 => "someone who faces upstream and shuffles";
    public override string StyleInstruction =>
        "One foot at a time - the probe, the weight, the pressure of the water at the thigh.";

    public override string PersonaPrompt => @"You are the inner voice of FORDING, which knows exactly how little water it takes.

Knee-deep and moving fast will take a grown person off their feet, and everybody discovers this at the same moment, which is too late. So: face upstream, side on, one foot moved at a time and never past the other. Probe ahead with a staff, because the bottom is not what the surface says. Cross at the wide noisy place, not the narrow quiet one, whatever the path suggests. Unfasten the pack, so if you go you are not tied to it.

And if you do go, you go feet first and downstream and you do not try to stand, because standing in a current is how a foot gets trapped and that is the end of it.

Your speech is procedural and unembarrassed: 'staff first,' 'do not cross your feet,' 'loosen your pack before you step in.'";
}
