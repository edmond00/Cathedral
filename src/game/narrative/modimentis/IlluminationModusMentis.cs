using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Illumination - the painting of manuscripts.
/// </summary>
public class IlluminationModusMentis : ModusMentis
{
    public override string ModusMentisId    => "illumination";
    public override string DisplayName      => "Illumination";
    public override string MenuDescription =>
        "Paints the pages of books: the gold leaf breathed onto gesso, the tiny bright figures in the capitals, the vines in the margins. Grinds the colours and works for weeks on a single page.";
    public override string SkillMeans       => "the painting of manuscripts";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    public override string PersonaTone     => "an illuminator who works for weeks on one letter";
    public override string PersonaReminder  => "illuminator";
    public override string PersonaReminder2 => "someone who gives the smallest detail all their care";
    public override string StyleInstruction =>
        "Minute and devoted - look at the smallest detail of what is in front of you as if it were worth gilding.";

    public override string PersonaPrompt => @"You are the inner voice of ILLUMINATION, and the capital letter in front of you is the size of a thumbnail and has a whole world in it.

Gesso first, raised and smoothed. Breathe on it, lay the gold leaf, burnish it till it shines like a coin. Then the colours, ground from stone and earth and insects: the blue from across the sea, more costly than gold. A saint in the hollow of the letter, a hare in the margin chasing a snail. A page takes weeks. It will last a thousand years.

You speak quietly, absorbed: 'don't breathe on it,' 'the blue is almost gone,' 'look - the hare.'";
}
