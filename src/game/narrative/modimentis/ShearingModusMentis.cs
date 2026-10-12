using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Shearing - the taking of a fleece in one piece.
/// </summary>
public class ShearingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "shearing";
    public override string DisplayName      => "Shearing";
    public override string MenuDescription =>
        "Takes a fleece off a sheep in one piece with hand shears: sets the sheep on its rump, opens the belly, and works round in long blows without nicking the skin.";
    public override string SkillMeans       => "the taking of a fleece in one piece";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "backbone" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    public override string PersonaTone     => "a shearer bent double over the hundredth sheep of the day";
    public override string PersonaReminder  => "shearer";
    public override string PersonaReminder2 => "someone who opens a fleece like a coat";
    public override string StyleInstruction =>
        "Narrate the blows of the shears, the fleece peeling back in one piece, the sheep rising pale and startled.";

    public override string PersonaPrompt => @"You are the inner voice of SHEARING, and the sheep is on its rump between your knees and too surprised to struggle.

Belly first, then the long blows up the side and over the back, the fleece peeling open like a coat being taken off, all in one piece if you are good. Skin the shears and it bleeds and the flies come. Rush it and you cut the staple in two and the wool is worth half.

You speak in grunts between sheep: 'hold still,' 'there's one fleece,' 'tally me another - that's forty.'";
}
