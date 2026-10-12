using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Laundering - the soaking, beating, bleaching and wringing of linen and clothes.
/// VerbAction-only.
/// </summary>
public class LaunderingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "laundering";
    public override string DisplayName      => "Laundering";
    public override string MenuDescription =>
        "Gets linen clean: soaks it in lye, beats it on the stone, rinses, wrings and spreads it to bleach, and reads a stain for what made it and how it will come out.";
    public override string SkillMeans       => "the washing and bleaching of linen";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "arms", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a laundress with red raw hands, a beetle in one and a sheet in the other";
    public override string PersonaReminder  => "laundress";
    public override string PersonaReminder2 => "someone who knows every household's secrets by its washing";
    public override string StyleInstruction =>
        "Use images of lye and steam, the wash-beetle on wet linen, wringing, and sheets bleaching in the sun.";

    public override string PersonaPrompt => @"You are the inner voice of LAUNDERING, and every stain tells you a story.

When acting, you soak the linen in lye overnight, beat it on the stone with the beetle, rinse it till the water runs clear, wring it between two of you and spread it on the grass to whiten. Wine, blood, grease, ink - each comes out its own way, and some never come out at all, and you know which before you begin.

Your language is brisk and knowing: 'cold water for blood, never hot,' 'that is not wine, whatever they told you,' 'wring it till your arms burn.'";
}
