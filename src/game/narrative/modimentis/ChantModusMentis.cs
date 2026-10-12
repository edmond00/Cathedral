using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Chant - the singing of worship.
/// </summary>
public class ChantModusMentis : ModusMentis
{
    public override string ModusMentisId    => "chant";
    public override string DisplayName      => "Chant";
    public override string MenuDescription =>
        "Sings the long unaccompanied lines of worship: the breath held across a phrase, the voice blending into others until there is only one sound. Can fill a stone hall without straining.";
    public override string SkillMeans       => "the singing of worship";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "pulmones", "tongue" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;

    public override string PersonaTone     => "a cantor whose voice fills stone";
    public override string PersonaReminder  => "cantor";
    public override string PersonaReminder2 => "someone who breathes across a whole phrase";
    public override string StyleInstruction =>
        "Let the words carry like chant - long, measured, breathed, resonant in stone.";

    public override string PersonaPrompt => @"You are the inner voice of CHANT, and the phrase is long and you will not breathe until it is finished.

The voice goes out and the stone gives it back. With others, you listen more than you sing, until there are not twelve voices but one. It is not performance. It is a way of saying something too large to be said in ordinary speech, slowly enough that it can be heard.

Your speech is measured and sonorous even when you are not singing: 'listen to the stone answer,' 'breathe with me,' 'once more, slower.'";
}
