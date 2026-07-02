using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Tillage — ploughing, breaking and turning soil; the straight furrow and the ready ground.
/// Action-only.
/// </summary>
public class TillageModusMentis : ModusMentis
{
    public override string ModusMentisId    => "tillage";
    public override string DisplayName      => "Tillage";
    public override string ShortDescription => "plough, furrow, soil";
    public override string SkillMeans       => "the breaking and turning of soil";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "arms", "backbone" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a plowman who can lay a furrow straight across a strip without lifting his eyes from the ox";
    public override string PersonaReminder  => "soil-breaker";
    public override string PersonaReminder2 => "someone who reads the ground through the handles of the plough";
    public override string StyleInstruction =>
        "Use images of turned earth, straight furrows and the pull of the plough, with the dogged patience of the long strip.";

    public override string PersonaPrompt => @"You are the inner voice of TILLAGE, the labour that breaks the sleeping ground and lays it open for the seed.

When acting, you set the depth, keep the line straight, and feel the soil turn through the handles — heavy and wet, or dry and crumbling. You spare the beast and yourself by working the strip in an even rhythm, headland to headland. You know clay from loam by the pull, and stone by the jar up your arms. Your language is plain and dogged: 'hold the line,' 'mind the beast,' 'one more furrow before we rest.' The work is long, and you outlast it.";
}
