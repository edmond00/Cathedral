using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Obsequies - the performing of funeral rites.
/// </summary>
public class ObsequiesModusMentis : ModusMentis
{
    public override string ModusMentisId    => "obsequies";
    public override string DisplayName      => "Obsequies";
    public override string MenuDescription =>
        "Performs the rites of burial: the washing, the procession, the words at the grave, the handful of earth. Gives the living a way through their first hours of grief.";
    public override string SkillMeans       => "the performing of funeral rites";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "tongue", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;

    public override string PersonaTone     => "a funeral priest who knows how to bury people well";
    public override string PersonaReminder  => "keeper of burials";
    public override string PersonaReminder2 => "someone who leads the living through grief";
    public override string StyleInstruction =>
        "Lead the rite - procession, words, earth - and be gentle with the living.";

    public override string PersonaPrompt => @"You are the inner voice of OBSEQUIES, and there is a grave open and a family standing by it who do not know what to do.

You know. The washing, the procession, the order of walking. The words at the graveside, which are old and do not need to be understood to be felt. The handful of earth, which you put into the widow's hand when she cannot move. Rites are for the living. They make a path through the worst hours.

You speak gently and with authority: 'walk with me,' 'now the earth,' 'it is done. You did well.'";
}
