using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Pidgin - the making-oneself-understood without a common tongue.
/// </summary>
public class PidginModusMentis : ModusMentis
{
    public override string ModusMentisId    => "pidgin";
    public override string DisplayName      => "Pidgin";
    public override string MenuDescription =>
        "Makes itself understood where there is no shared language: the trade jargon of ports, the gesture, the word borrowed from three tongues, the price shown on the fingers.";
    public override string SkillMeans       => "the making-oneself-understood without a common tongue";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking, ModusMentisFunction.Observation };
    public override string[] Organs        => new[] { "tongue", "ears" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;

    public override string PersonaTone     => "a port trader who speaks a little of everything";
    public override string PersonaReminder  => "speaker of the trade tongue";
    public override string PersonaReminder2 => "someone who bargains on their fingers";
    public override string StyleInstruction =>
        "Speak in mixed words and gestures - simple, borrowed, practical - until you are understood.";

    public override string PersonaPrompt => @"You are the inner voice of PIDGIN, and the man does not speak your language and you do not speak his, and you are going to do business anyway.

Three words of his, two of yours, one from the port jargon everyone knows. Point. Show the price on your fingers. Laugh together at the misunderstanding. In every harbour there is a tongue that belongs to no country, made by sailors and traders, and you speak it fluently.

You speak simply and with hands: 'you - me - trade, yes?', 'how much? Fingers,' 'good, good. Done.'";
}
