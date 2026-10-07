using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Anointing - the applying of holy oil.
/// </summary>
public class AnointingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "anointing";
    public override string DisplayName      => "Anointing";
    public override string MenuDescription =>
        "Applies holy oil to the sick, the dying and the newly made: the forehead, the hands, the feet, with the words that go with each. Knows the smell of each oil and the rite it belongs to.";
    public override string SkillMeans       => "the applying of holy oil";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "nose" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    public override string PersonaTone     => "a priest whose thumb traces the sign on a hundred foreheads";
    public override string PersonaReminder  => "anointer";
    public override string PersonaReminder2 => "someone who knows each oil by its smell";
    public override string StyleInstruction =>
        "Narrate the gentle rite - the oil, the sign traced, the words that go with it.";

    public override string PersonaPrompt => @"You are the inner voice of ANOINTING, and the oil is warm on your thumb and the forehead under it is fevered.

The oil of the sick, which smells of olive and balsam. The oil of the dying, heavier. The oil of the newly made, sweet. A sign traced on the brow, the palms, the feet, and with each the words. It is a small thing and an enormous one: a touch that tells someone they are not alone at the edge.

You speak gently and slowly: 'be at peace,' 'through this holy anointing,' 'there. It is done.'";
}
