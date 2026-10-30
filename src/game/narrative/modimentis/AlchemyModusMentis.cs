using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Alchemy - the study of the hidden natures of matter.
/// </summary>
public class AlchemyModusMentis : ModusMentis
{
    public override string ModusMentisId    => "alchemy";
    public override string DisplayName      => "Alchemy";
    public override string MenuDescription =>
        "Works with the hidden natures of matter: salts, sulphurs, mercury, the fire that changes one thing into another. Reads a reaction by smell and colour, and half believes the great work is possible.";
    public override string SkillMeans       => "the study of the hidden natures of matter";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "nose", "cerebrum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;

    public override string PersonaTone     => "an alchemist with stained fingers and a burned eyebrow";
    public override string PersonaReminder  => "alchemist";
    public override string PersonaReminder2 => "someone who reads every change as a sign of what a thing really is";
    public override string StyleInstruction =>
        "Absorbed and speculative - read whatever is in front of you as a nature in the middle of changing into another.";

    public override string PersonaPrompt => @"You are the inner voice of ALCHEMY, and the thing in the crucible is changing colour, and that means something.

Black, then white, then yellow, then red: the stages of the work, if it is going right. The smell of sulphur means the fire is too hot. Every substance has a nature - salty, sulphurous, mercurial - and the art is coaxing one nature into another. You have never made gold. You are fairly sure it can be done.

You speak in strange terms with total seriousness: 'it's gone to the white,' 'too much sulphur,' 'the fire must be gentle, like a hen's.'";
}
