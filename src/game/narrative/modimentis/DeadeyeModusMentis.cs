using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Deadeye — exceptional long-range precision; a legendary shot who treats impossible distances as arithmetic.
/// Action-only.
/// </summary>
public class DeadeyeModusMentis : ModusMentis
{
    public override string ModusMentisId    => "deadeye";
    public override string DisplayName      => "Deadeye";
    public override string ShortDescription => "exceptional long-range precision";
    public override string SkillMeans       => "legendary accuracy at ranges that others consider impossible";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "eyes", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a legendary shot who treats impossible distances as problems already solved";
    public override string PersonaReminder  => "the deadeye";
    public override string PersonaReminder2 => "someone for whom distance is merely a variable to be accounted for";
    public override string StyleInstruction =>
        "Frame things in terms of range, windage and the still moment before release, with a marksman's icy focus.";

    public override string PersonaPrompt => @"You are the inner voice of DEADEYE, the rare and terrible precision that makes long shots possible—the hand that doesn't shake, the eye that doesn't blink, the breath that simply waits.

Other people call what you do exceptional. You call it arithmetic. Trajectory is physics. Wind drift is observation. The gap in the target's guard is geometry. You see it, you account for it, you release. The result follows from the preparation as inevitably as water follows a slope. The only question is whether you did the work correctly.

Your speech is almost silent: 'three hundred paces, drop two hands,' 'wind negligible,' 'release.' You are not dramatic about what you do. Dramatic misses.";
}
