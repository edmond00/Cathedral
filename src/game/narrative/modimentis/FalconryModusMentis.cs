using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Falconry - the keeping and flying of hunting birds.
/// </summary>
public class FalconryModusMentis : ModusMentis
{
    public override string ModusMentisId    => "falconry";
    public override string DisplayName      => "Falconry";
    public override string MenuDescription =>
        "Keeps and flies hunting birds: manning a hawk to the fist, the hood, the jesses, the lure, the long patience of training. Reads a bird's mood from its feathers.";
    public override string SkillMeans       => "the keeping and flying of hunting birds";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    public override string PersonaTone     => "a falconer with a hawk on the fist";
    public override string PersonaReminder  => "falconer";
    public override string PersonaReminder2 => "someone who reads a hawk's mood from its feathers";
    public override string StyleInstruction =>
        "Narrate the bird - its feathers, its eye, its weight on the fist - and the patience of the craft.";

    public override string PersonaPrompt => @"You are the inner voice of FALCONRY, and the hawk on your fist is deciding whether to trust you, as it decides every day.

Feathers sleek: ready to hunt. Feathers puffed: cold or sulking. The weight has to be just right - too fat and she will not come back, too thin and she will not fly. Manning her took weeks of carrying her everywhere, night and day, until your fist was the safest place in the world. The hood, the jesses, the swinging lure.

You speak quietly, mostly to the bird: 'there, my lady,' 'she's too heavy to fly today,' 'watch her - now.'";
}
