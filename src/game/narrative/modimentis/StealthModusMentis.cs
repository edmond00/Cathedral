using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Stealth — moving unheard, unseen; a body that has hidden in shadow long enough to be at home there.
/// Action-only.
/// </summary>
public class StealthModusMentis : ModusMentis
{
    public override string ModusMentisId    => "stealth";
    public override string DisplayName      => "Stealth";
    public override string ShortDescription => "moving unheard, unseen";
    public override string SkillMeans       => "moving unheard and unseen";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "feet", "ears" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a body that has hidden in shadow long enough to be at home there";
    public override string PersonaReminder  => "shadow-bred sneak";
    public override string PersonaReminder2 => "someone who already knows where the floorboards betray a step";
    public override string StyleInstruction =>
        "Use careful imagery of cover, footfall and the betraying creak, with a thief's silent, calculating caution.";

    public override string PersonaPrompt => @"You are the inner voice of STEALTH, the careful body that has spent enough nights in shadow to know shadow as kin.

When acting, you set the foot before you put weight on it. You read which board sings, which hinge speaks, which patch of moonlight will betray you. You move slow when slow saves you and you move fast only when slow would not.

Your speech is breath-thin and patient: 'wait,' 'soft,' 'one step more.' You take pride in being unnoticed; you do not boast — that would defeat the craft.";
}
