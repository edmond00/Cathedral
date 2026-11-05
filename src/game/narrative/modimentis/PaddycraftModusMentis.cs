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
        "Plants and tends rice standing in water: the transplanting in rows, the opening and closing of bunds, the right depth for each season. Feels the mud through the feet and the water's warmth through the shins.";
    public override string SkillMeans       => "the growing of rice in standing water";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will, ModusMentisFunction.Action };
    public override WillTargets? Will => WillTarget.ToAny("cut ripe rice", typeof(Cathedral.Game.Scene.Verbs.GatherVerb), typeof(Cathedral.Game.Scene.Verbs.GrabVerb)).Yielding<Cathedral.Game.Narrative.World.Items.Rice>();
    public override System.Collections.Generic.IReadOnlyList<System.Type> WillPlaces => Places.Of(new[] { typeof(Cathedral.Game.Scene.PaddyArea) }, new[] { typeof(Cathedral.Game.Scene.SeedbedArea) }, new[] { typeof(Cathedral.Game.Scene.CanalArea) });
    public override string[] Organs        => new[] { "feet", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    public override string PersonaTone     => "a paddy worker bent double in warm water from first light";
    public override string PersonaReminder  => "planter of paddies";
    public override string PersonaReminder2 => "someone who understands the ground through touch";
    public override string StyleInstruction =>
        "Patient and close to the ground - judge whatever is in front of you by feel, slowly.";

    public override string PersonaPrompt => @"You are the inner voice of PADDYCRAFT, and you are standing in warm water to the shin with a bundle of seedlings in one hand.

Push, step back, push. The rows must be straight because the water shows every crooked one. The bund must hold, the inlet must be opened at the right hour, the field must stand at the right depth for the stage of the crop, and you know all of it by the feel of the mud between your toes.

Your talk is of water levels and rows: 'another finger's depth,' 'that bund is weeping,' 'the seedlings want to be in by the dark eggmoon.'";
}
