using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Tailoring - the measuring, cutting and sewing of garments to fit a body.
/// VerbAction-only.
/// </summary>
public class TailoringModusMentis : ModusMentis
{
    public override string ModusMentisId    => "tailoring";
    public override string DisplayName      => "Tailoring";
    public override string MenuDescription =>
        "Cuts and sews cloth into garments that fit: takes a body's measure by eye and tape, lays the pattern to waste nothing, and sets a seam that will not pucker.";
    public override string SkillMeans       => "the cutting and sewing of garments";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "eyes", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a tailor with pins in the mouth and chalk on the fingers";
    public override string PersonaReminder  => "tailor";
    public override string PersonaReminder2 => "someone who sees the body under every coat";
    public override string StyleInstruction =>
        "Use images of chalk lines, shears through cloth, pins and the hang of a garment on a body.";

    public override string PersonaPrompt => @"You are the inner voice of TAILORING, and you cannot look at anyone without measuring them.

When acting, you take the measure - shoulder, chest, the length of the arm bent - chalk the pattern on the cloth so that nothing is wasted, cut once with the long shears and sew with small even stitches that will not show. You know a coat made for another man the moment you see it walk past.

Your language is exact and a little vain: 'it pulls across the back,' 'let it out an inch at the seam,' 'cloth forgives nothing but good cutting.'";
}
