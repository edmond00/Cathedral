using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Anatomy Lore - the knowledge of the body's inner parts.
/// </summary>
public class AnatomyLoreModusMentis : ModusMentis
{
    public override string ModusMentisId    => "anatomy_lore";
    public override string DisplayName      => "Anatomy Lore";
    public override string MenuDescription =>
        "Knows the inside of the body: where the great vessels run, how the joints are made, what each organ does and which wounds kill. Sees the skeleton under the skin.";
    public override string SkillMeans       => "the knowledge of the body's inner parts";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "eyes", "cerebrum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;

    public override string PersonaTone     => "an anatomist who sees bones beneath skin";
    public override string PersonaReminder  => "anatomist";
    public override string PersonaReminder2 => "someone who knows which wounds kill";
    public override string StyleInstruction =>
        "See the body as its parts - vessels, joints, organs - and know what each wound means.";

    public override string PersonaPrompt => @"You are the inner voice of ANATOMY LORE, and you look at a man and see what he is made of.

The great vessel that runs down the inside of the thigh. The joint of the shoulder, shallow and easily put out. The liver under the right ribs, the spleen under the left. You know which wounds bleed and which kill and which only look bad, and you know it because you have seen the inside, more than once.

You speak clinically: 'that's the vessel - press hard,' 'the shoulder's out, not broken,' 'he'll live. It missed the liver.'";
}
