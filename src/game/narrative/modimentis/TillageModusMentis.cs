using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Tillage — ploughing, breaking and turning soil; the straight furrow and the ready ground.
/// Will + VerbAction.
/// </summary>
public class TillageModusMentis : ModusMentis
{
    public override string ModusMentisId    => "tillage";
    public override string DisplayName      => "Tillage";
    public override string MenuDescription =>
        "Reads ground through the resistance it offers, sorting clay from loam by the pull and judging when soil is ready to break. Sets the body to steady, repetitive labour and holds a line straight across long work without flagging.";
    public override string SkillMeans       => "the breaking and turning of soil";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will, ModusMentisFunction.Action };
    public override WillTargets? Will => WillTargets.Of(
        WillTarget.To<DigVerb>("break the ground"),
        WillTarget.ToAny("lay hands on a spade", typeof(Cathedral.Game.Scene.Verbs.GrabVerb), typeof(Cathedral.Game.Scene.Verbs.StealVerb)).YieldingAny("a spade", typeof(Cathedral.Game.Narrative.World.Items.Shovel), typeof(Cathedral.Game.Narrative.World.Items.GraveSpade)));
    public override System.Collections.Generic.IReadOnlyList<System.Type> WillPlaces => Places.Of(Places.Fields);
    public override string[] Organs        => new[] { "arms", "backbone" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a plowman who can lay a furrow straight across a strip without lifting his eyes from the ox";
    public override string PersonaReminder  => "soil-breaker";
    public override string PersonaReminder2 => "someone who understands the ground through work";
    public override string StyleInstruction =>
        "Dogged and patient - judge whatever is in front of you by the long, straight work it would take.";

    public override string PersonaPrompt => @"You are the inner voice of TILLAGE, the labour that breaks the sleeping ground and lays it open for the seed.

When acting, you set the depth, keep the line straight, and feel the soil turn through the handles — heavy and wet, or dry and crumbling. You spare the beast and yourself by working the strip in an even rhythm, headland to headland. You know clay from loam by the pull, and stone by the jar up your arms. Your language is plain and dogged: 'hold the line,' 'mind the beast,' 'one more furrow before we rest.' The work is long, and you outlast it.";
}
