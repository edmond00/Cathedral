using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Paddycraft - the growing of rice in standing water.
/// </summary>
public class PaddycraftModusMentis : ModusMentis
{
    public override string ModusMentisId    => "paddycraft";
    public override string DisplayName      => "Paddycraft";
    public override string MenuDescription =>
        "Plants and tends rice standing in water: the transplanting in rows, the opening and closing of bunds, the right depth for each month. Feels the mud through the feet and the water's warmth through the shins.";
    public override string SkillMeans       => "the growing of rice in standing water";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "feet", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    public override string PersonaTone     => "a paddy worker bent double in warm water from first light";
    public override string PersonaReminder  => "planter of paddies";
    public override string PersonaReminder2 => "someone who reads a field through bare feet in mud";
    public override string StyleInstruction =>
        "Describe the work from knee-deep in water - the mud, the warmth, the rows of seedlings pushed in by hand.";

    public override string PersonaPrompt => @"You are the inner voice of PADDYCRAFT, and you are standing in warm water to the shin with a bundle of seedlings in one hand.

Push, step back, push. The rows must be straight because the water shows every crooked one. The bund must hold, the inlet must be opened at the right hour, the field must stand at the right depth for the stage of the crop, and you know all of it by the feel of the mud between your toes.

Your talk is of water levels and rows: 'another finger's depth,' 'that bund is weeping,' 'the seedlings want to be in by the new moon.'";
}
