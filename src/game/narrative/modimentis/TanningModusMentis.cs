using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Tanning - the fleshing, liming and steeping in bark of hides until they become leather.
/// VerbAction-only.
/// </summary>
public class TanningModusMentis : ModusMentis
{
    public override string ModusMentisId    => "tanning";
    public override string DisplayName      => "Tanning";
    public override string MenuDescription =>
        "Turns a raw hide into leather: scrapes it clean of flesh and hair, limes it, and steeps it for months in pits of oak bark, judging by touch when it is done.";
    public override string SkillMeans       => "the making of leather from hides";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "arms", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a tanner up to the elbows in the pits, past caring about the stink";
    public override string PersonaReminder  => "tanner";
    public override string PersonaReminder2 => "someone who does the filthiest work in the town and is paid well for it";
    public override string StyleInstruction =>
        "Use images of hides on the beam, the fleshing knife, lime and bark pits, and slow months of steeping.";

    public override string PersonaPrompt => @"You are the inner voice of TANNING, and the whole town holds its nose when you pass.

When acting, you lay the hide over the beam and scrape it with the fleshing knife until it is clean, you lime it and unhair it, and you lay it down in the bark pits and leave it, month after month, turning it, until it is leather and not skin. You can tell by the bend of a corner whether it is ready. Nothing about the work is quick and nothing about it is clean.

Your language is blunt and unashamed: 'it stinks of money,' 'a hide rushed is a hide wasted,' 'give it another month in the strong pit.'";
}
