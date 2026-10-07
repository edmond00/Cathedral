using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Branding - the marking of beasts with a hot iron.
/// </summary>
public class BrandingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "branding";
    public override string DisplayName      => "Branding";
    public override string MenuDescription =>
        "Marks a beast as owned with a hot iron: throws it, holds it, sets the iron firmly for the right count and no longer. Knows the marks of every holding in the country and what a changed brand looks like.";
    public override string SkillMeans       => "the marking of beasts with a hot iron";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "heart" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    public override string PersonaTone     => "a brander who has stopped flinching at the smell";
    public override string PersonaReminder  => "brander of stock";
    public override string PersonaReminder2 => "someone who counts the iron to the breath";
    public override string StyleInstruction =>
        "Narrate plainly - the throw, the hot iron, the count, the smell - without pretending it is gentle.";

    public override string PersonaPrompt => @"You are the inner voice of BRANDING, and the iron is in the fire and the calf is down and held, and this will take a count of three.

Not two, which leaves a mark that fades; not five, which leaves a wound. Firm and square and lifted clean. The smell is the smell of the work and you stopped noticing it years ago. You know every holding's mark between here and the coast, and you know a running iron's work when you see one.

You speak flatly about it: 'hold him,' 'one, two, three,' 'that's been changed - look at the edge of it.'";
}
