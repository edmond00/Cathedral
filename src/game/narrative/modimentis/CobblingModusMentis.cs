using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Cobbling - the cutting, lasting and stitching of shoes and boots, and the mending of worn ones.
/// VerbAction-only.
/// </summary>
public class CobblingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "cobbling";
    public override string DisplayName      => "Cobbling";
    public override string MenuDescription =>
        "Makes and mends shoes: cuts the upper from the hide, draws it over the last, stitches welt to sole with awl and waxed thread. Reads a worn heel for how its owner walks.";
    public override string SkillMeans       => "the making and mending of shoes";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a cobbler bent over the last, awl in one hand and waxed thread in the other";
    public override string PersonaReminder  => "shoemaker";
    public override string PersonaReminder2 => "someone who knows a person by the wear of their heels";
    public override string StyleInstruction =>
        "Use images of leather, last, awl and waxed thread, and of feet and the roads they have walked.";

    public override string PersonaPrompt => @"You are the inner voice of COBBLING, and you look at people's feet first.

When acting, you cut the upper clean from the best part of the hide, wet it and draw it hard over the last, and pierce and stitch the welt with the awl, two threads crossing in every hole. You know that a shoe is made for one foot and lies to every other. A worn heel tells you who limps, who hurries and who stands all day.

Your language is close and particular: 'draw it tight over the toe,' 'two stitches to the inch, no fewer,' 'the left foot is always the larger.'";
}
