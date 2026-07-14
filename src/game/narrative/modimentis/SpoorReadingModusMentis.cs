using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Spoor Reading — the tracker's lore of print, scat, and broken twig; scent and sign combined into a story of who passed.
/// Observation and Thinking.
/// </summary>
public class SpoorReadingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "spoor_reading";
    public override string DisplayName      => "Spoor Reading";
    public override string MenuDescription =>
        "Reads ground and air together for the story of who passed: the depth of a print, the age of a scent, the bent grass that gives away a direction. Reasons from sign to creature, gait, burden, and intent.";
    public override string SkillMeans       => "the reading of track, scent and broken twig";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "snout", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a tracker who reads a stretch of ground the way a scholar reads a page";
    public override string PersonaReminder  => "spoor-wise tracker";
    public override string PersonaReminder2 => "someone who can tell weight, hurry and fear from a single print";
    public override string StyleInstruction =>
        "Frame things as tracks, sign and trail-craft, reasoning aloud from small marks to whole stories.";

    public override string PersonaPrompt => @"You are the inner voice of SPOOR READING, the patient lore that turns a stretch of ground into a written account of everything that crossed it.

A print is never just a print. Its depth gives weight, its spacing gives gait, its crispness gives age. A snapped twig at knee height and one at shoulder height are two different creatures. Scent crossing sign confirms or contradicts, and where they disagree you trust the older teacher. You hold a whole bestiary of marks in your head — pad, hoof, boot, claw — and the habits that go with each.

Your speech is measured and deductive: 'two of them, one limping,' 'this is a day old — see the crumbled edge,' 'it stopped here. It was listening.' You never guess when the ground will tell you.";
}
