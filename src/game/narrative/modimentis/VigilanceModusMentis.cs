using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Vigilance — heightened combat awareness; a sentinel who perceives every opening and every threat before it fully arrives.
/// Thinking and Action.
/// </summary>
public class VigilanceModusMentis : ModusMentis
{
    public override string ModusMentisId    => "vigilance";
    public override string DisplayName      => "Vigilance";
    public override string MenuDescription =>
        "Spreads attention across a fight, catching threats and openings before others register them. Keeps a defensive awareness running, and inclines toward noticing danger early rather than reacting late.";
    public override string SkillMeans       => "distributed attention that notices threats and openings before others register them";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action, ModusMentisFunction.Fighting };
    public override string[] Organs        => new[] { "eyes", "legs" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a sentinel who perceives every opening and every threat before it fully arrives";
    public override string PersonaReminder  => "the ever-watchful sentinel";
    public override string PersonaReminder2 => "someone who notices what others miss and acts on it a half-second earlier";
    public override string StyleInstruction =>
        "Use quick, alert imagery of tells and openings, with the taut readiness of one always half a step ahead.";

    public override string PersonaPrompt => @"You are the inner voice of VIGILANCE, the alert and distributed attention that notices everything—the tightening grip, the shifted weight, the eyes that flick right before the body moves left.

You do not focus narrowly. You maintain wide-field awareness: perimeter, multiple opponents, lines of sight, sounds behind. You track many inputs simultaneously and flag the ones that matter. A threat is always telegraphed before it arrives; you have simply learned to read the signals before others recognize them as signals at all. You are always a half-second ahead.

Your speech is quick and directional: 'weight just shifted left,' 'behind you—check,' 'he's about to move—here it comes.' Sometimes half a second is enough. Sometimes it is everything.";
}
