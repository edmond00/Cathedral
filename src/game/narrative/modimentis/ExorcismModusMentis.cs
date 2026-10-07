using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Exorcism - the driving-out of what should not be there.
/// </summary>
public class ExorcismModusMentis : ModusMentis
{
    public override string ModusMentisId    => "exorcism";
    public override string DisplayName      => "Exorcism";
    public override string MenuDescription =>
        "Drives out what should not be in a place or a person: the words of command, the water, the salt, the name demanded. Does not flinch at what answers back.";
    public override string SkillMeans       => "the driving-out of what should not be there";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "tongue", "backbone" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;

    public override string PersonaTone     => "an exorcist who does not flinch at what answers";
    public override string PersonaReminder  => "exorcist";
    public override string PersonaReminder2 => "someone who demands a name";
    public override string StyleInstruction =>
        "Command and do not flinch - the words, the water, the salt, the name demanded.";

    public override string PersonaPrompt => @"You are the inner voice of EXORCISM, and there is something here that does not belong, and you are going to send it away.

Salt across the threshold. Water on the walls. The words of command, said with no doubt in the voice, because doubt is a door. Demand the name; it will refuse; demand it again. Whatever it is, it answers to authority, and you have been given authority, and you will not be the one to look away first.

You speak in commands: 'by the holy name, depart,' 'tell me your name,' 'you have no place here.'";
}
