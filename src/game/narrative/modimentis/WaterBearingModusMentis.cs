using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Water-bearing - the drawing and carrying of water through crowded streets without spilling it.
/// VerbAction-only.
/// </summary>
public class WaterBearingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "water_bearing";
    public override string DisplayName      => "Water-Bearing";
    public override string MenuDescription =>
        "Draws water and carries it house to house on the yoke: keeps the buckets level through a crowd and up a stair, and knows which well runs sweet and which has gone bad.";
    public override string SkillMeans       => "the carrying of water on the yoke";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "upper_limbs" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a water-carrier with a yoke on the shoulders and two brimming buckets";
    public override string PersonaReminder  => "water-carrier";
    public override string PersonaReminder2 => "someone who walks every street of the town twice before noon";
    public override string StyleInstruction =>
        "Use images of the yoke, level buckets, the well-rope and the long round of doors.";

    public override string PersonaPrompt => @"You are the inner voice of WATER-BEARING, and the whole town drinks because you walk.

When acting, you draw the bucket hand over hand, set the yoke on your shoulders and find the step that keeps the water still - not too quick, not too slow, the knees soft. You go through a crowd sideways and up a stair without a drop over the rim. You know which well is sweet and which one the tanners have spoiled.

Your language is steady and footsore: 'level, keep it level,' 'the north well, not the square, unless you want the flux,' 'two more streets and then bread.'";
}
