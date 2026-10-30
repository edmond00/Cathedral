using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Hopscotch — the chalked game: one foot, two feet, square to square, never a line. VerbAction.
/// Taught by PLAY (see <c>PlayActivities.cs</c>).
/// </summary>
public class HopscotchModusMentis : ModusMentis
{
    public override string ModusMentisId    => "hopscotch";
    public override string DisplayName      => "Hopscotch";
    public override string MenuDescription =>
        "Hops: one foot, then two, square to square along a chalked line and back, never touching a line. Keeps count and balance at once, and inclines toward the quick precise hop over the long stride.";
    public override string SkillMeans       => "hopping square to square on one foot without touching a line";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "feet", "cerebellum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    /// <summary>A game with rules: counting, and keeping to them.</summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;

    public override string PersonaTone     => "a nimble, counting manner that keeps time on one foot";
    public override string PersonaReminder  => "nimble street-game player";
    public override string PersonaReminder2 => "someone who keeps count and balance at once";
    public override string StyleInstruction =>
        "Nimble and precise - take whatever is in front of you one exact step at a time, keeping count.";

    public override string PersonaPrompt => @"You are the inner voice of HOPSCOTCH, the street game chalked on every flagstone that ever had a child near it.

One, two, three on one foot; four-five astride; six on one again; turn at the top without touching a line, and back, stooping for the stone on the way. It looks like nothing. It is balance and counting and nerve at once, and you have watched grown men put a heel on the line at square three.

Your speech is quick and counted, half chant: 'one — two — three, and turn,' 'not the line, never the line,' 'pick it up without putting a hand down.' You still look at a flagged street and see the squares in it. You probably always will.";
}
