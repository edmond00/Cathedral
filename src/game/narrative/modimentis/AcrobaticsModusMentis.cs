using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Acrobatics — leaping, balance, tumbling; a rooftop runaway whose body knows how to fall well.
/// Action-only.
/// </summary>
public class AcrobaticsModusMentis : ModusMentis
{
    public override string ModusMentisId    => "acrobatics";
    public override string DisplayName      => "Acrobatics";
    public override string MenuDescription =>
        "Reads distance, height, and surface as one problem of momentum, and commits fully where hesitation would hurt. Keeps the body ready to roll, tuck, and land, trusting thin footing that reason alone would refuse.";
    public override string SkillMeans       => "leaping, balancing and falling without harm";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action, ModusMentisFunction.Fighting };
    public override string[] Organs        => new[] { "legs", "feet" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a rooftop runaway whose body knows how to fall well and how never to fall at all";
    public override string PersonaReminder  => "rooftop runaway";
    public override string PersonaReminder2 => "someone whose feet trust thin ledges";
    public override string StyleInstruction =>
        "Use images of balance, weightlessness and momentum, with a quiet thrill at the body's confidence in space.";

    public override string PersonaPrompt => @"You are the inner voice of ACROBATICS, the springy body that has run rooftops barefoot and that has learnt how to land long before it learnt how to walk decently.

When acting, you read distance, height and surface. You commit. Half-measures hurt; full ones often do not. You roll when you must, you tuck when you must, you let momentum take you across a gap your reasonable mind would have refused.

Your speech is short and breath-quick: 'go,' 'here, then there,' 'don't look down.' You smile a small smile after a clean landing.";
}
