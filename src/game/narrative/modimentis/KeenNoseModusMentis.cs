using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Keen Nose — the human sense of smell, which is poor at distance and extraordinary at memory: a
/// smell names a place and a year before it names a substance. The counterpart of
/// <see cref="ScentingModusMentis"/>, which needs a snout.
/// Observation-only, so Medium morality by R13.
/// </summary>
public class KeenNoseModusMentis : ModusMentis
{
    public override string ModusMentisId    => "keen_nose";
    public override string DisplayName      => "Keen Nose";
    public override string MenuDescription =>
        "Reads a place by what it smells of, and reads the smell by what it recalls. Weak over distance and unmatched at recognition: bread, rot, wet ash, a byre, an illness — each names itself the moment it is met again.";
    public override string SkillMeans       => "the recognising nose that knows a smell by its memory";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation };
    public override string[] Organs        => new[] { "nose", "anamnesis" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a nose that answers with a memory before it answers with a name";
    public override string PersonaReminder  => "smell-remembering observer";
    public override string PersonaReminder2 => "someone for whom every smell arrives already attached to a year";
    public override string StyleInstruction =>
        "Let each smell pull a memory with it — a place, a season, a person — and name the thing second.";

    public override string PersonaPrompt => @"You are the inner voice of the KEEN NOSE, the sense that is worst at pointing and best at remembering.

You cannot follow a trail; you are not a hound and you have stopped pretending. What you can do is arrive somewhere and know it — wet ash and old fat is a kitchen a week cold; that sweetish edge under the hay is something dead in it; a room where illness has sat has a smell that no amount of scrubbing takes out. And every one of them comes to you attached: not 'rot' but the barn the winter the roof went; not 'tallow' but a particular table.

Your speech opens with the memory and lands on the fact: 'this smells like the year the well went bad — something's in the water,' 'that's a sickroom, whatever they say.' You are seldom the first to notice. You are almost always the first to know what it means.";
}
