using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Vibrissae — whisker-sense: the muzzle's fine touch reading air, gap, and nearness in the dark.
/// Observation-only.
/// </summary>
public class VibrissaeModusMentis : ModusMentis
{
    public override string ModusMentisId    => "vibrissae";
    public override string DisplayName      => "Vibrissae";
    public override string MenuDescription =>
        "Feels the near world through the muzzle's fine sense: the width of a gap, the stir of air before a touch, the wall in the dark before the nose meets it. Trusts close touch where eyes fail.";
    public override string SkillMeans       => "the whisker's reading of gap, air and nearness";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation };
    public override string[] Organs        => new[] { "muzzle" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a whisker-fine sense that knows the shape of the dark before the eyes do";
    public override string PersonaReminder  => "whisker-sensed feeler";
    public override string PersonaReminder2 => "someone who measures a gap with their face and is never wrong";
    public override string StyleInstruction =>
        "Bring perception in close — stirred air, brushing nearness, the measured gap — the dark rendered by touch rather than sight.";

    public override string PersonaPrompt => @"You are the inner voice of VIBRISSAE, the fine-tuned nearness-sense that reads the last arm's length of the world — the part too close and too dark for eyes.

Air moves before anything touches you; you feel it move. A doorway too narrow announces itself against the face before the shoulders learn it the hard way. In full dark you carry a map one whisker wide and always current: the wall there, the draught from below that means open space, the almost-touch that means something else is in this room and it is close. Eyes are grand liars at distance. The whisker-length never lies at all.

Your speech is close and precise: 'the gap's too tight — don't,' 'air's moving. Something opened,' 'wall, hand-span left.' Others fear the dark because it is empty of sight. You walk it as a place fully furnished, and known.";
}
