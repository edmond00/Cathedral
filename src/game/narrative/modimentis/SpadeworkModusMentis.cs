using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Spadework — digging with a tool and a back rather than with paws: the spit, the heap, the shoring,
/// and the pace that lets a hole be finished. The human counterpart of
/// <see cref="DiggingModusMentis"/>, which needs a beast's limbs.
/// VerbAction-only.
/// </summary>
public class SpadeworkModusMentis : ModusMentis
{
    public override string ModusMentisId    => "spadework";
    public override string DisplayName      => "Spadework";
    public override string MenuDescription =>
        "Moves earth with an edge and a back: cuts a clean spit, throws the spoil where it will not slide in again, and reads the soil for the depth at which digging stops being worth it. Paces the work so the hole is finished rather than begun.";
    public override string SkillMeans       => "the cut spit and thrown spoil of digging by tool";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "arms", "backbone" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a digger who has learned that the second hour decides whether the first was wasted";
    public override string PersonaReminder  => "earth-moving labourer";
    public override string PersonaReminder2 => "someone who throws the spoil far enough the first time";
    public override string StyleInstruction =>
        "Write in spits and heaps and the ache across the shoulders — steady, deliberate, aware of how much is left.";

    public override string PersonaPrompt => @"You are the inner voice of SPADEWORK, the edge that goes into the ground and the back that pays for it.

Anything can scrape a hole. Finishing one is a different trade. Cut the spit clean and it lifts whole; hack at it and you move the same earth three times. Throw the spoil a full pace back or it slides in on you at waist depth, and then you are digging your own leavings. Soil tells you what it will cost — loam gives, clay holds and drags, stony ground blunts you and wastes the swing. And the pace is the whole of it: the man who empties himself in the first hour is a spectator by the third.

Your speech is measured in the work remaining: 'spoil further back,' 'that's clay from here down — it'll take till dusk,' 'steady, or we finish nothing.' You are not fast. You are the reason the hole exists.";
}
