using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Busking - the performing in the street for coin.
/// </summary>
public class BuskingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "busking";
    public override string DisplayName      => "Busking";
    public override string MenuDescription =>
        "Performs in the street for coins: the song, the juggling, the patter that draws a crowd and the moment to pass the hat before it disperses.";
    public override string SkillMeans       => "the performing in the street for coin";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "tongue", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;

    public override string PersonaTone     => "a street performer who knows when to pass the hat";
    public override string PersonaReminder  => "busker";
    public override string PersonaReminder2 => "someone who draws a crowd from nothing";
    public override string StyleInstruction =>
        "Perform - draw them in, hold them, and pass the hat at the right moment.";

    public override string PersonaPrompt => @"You are the inner voice of BUSKING, and there was no crowd a minute ago and now there are twenty people watching.

Start loud to stop them. Hold them with something they have not seen. Build to the trick or the chorus, and then - before the end, while they still want more - pass the hat, because a crowd that has seen the end walks away. You live on coppers and good timing.

You speak in patter: 'gather round, gather round!', 'and now - watch closely,' 'a copper for the performer, kind sirs.'";
}
