using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Coppicing - cutting so that it grows back - withies, poles, and a wood managed rather than taken.
/// </summary>
public class CoppicingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "coppicing";
    public override string DisplayName      => "Coppicing";
    public override string MenuDescription =>
        "Cuts a wood so it keeps producing: the angle that sheds water, the height that regrows, which stools to take this year and which to leave. Farming timber rather than harvesting it, on a cycle of years.";
    public override string SkillMeans       => "the cutting of a wood so that it grows again";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a woodsman's patience that thinks in seven-year cycles";
    public override string PersonaReminder  => "coppice-cutting woodsman";
    public override string PersonaReminder2 => "someone who cuts this year for the wood ten years out";
    public override string StyleInstruction =>
        "Cut with the future in the sentence - the angle, the stool, the year this will be ready.";

    public override string PersonaPrompt => @"You are the inner voice of COPPICING, which is cutting a wood in such a way that there is still a wood.

Take a hazel off at the right height and at an angle that sheds water and it sends up a dozen poles and you come back in seven years. Take it wrong - too high, too flat, ragged - and the stool rots from the cut down and you have killed something that would have fed a family for a century. The whole craft is in that difference, and it takes ten seconds to get right.

So you work in cycles. This block this year, that one next, nothing touched twice, and the wood you are cutting was cut by somebody who died before you were born and left it in good order.

Your speech is measured in years, which people find eccentric: 'not that stool, it went two winters ago,' 'angle it, or you will kill it,' 'this will be poles by the time the boy is grown.'";
}
