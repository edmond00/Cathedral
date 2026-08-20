using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Swarm Sense - reading swarms and colonies - bees, ants, flies, and what they are telling you about the place.
/// </summary>
public class SwarmSenseModusMentis : ModusMentis
{
    public override string ModusMentisId    => "swarm_sense";
    public override string DisplayName      => "Swarm Sense";
    public override string MenuDescription =>
        "Reads the massed small things: where a hive is from the line the bees fly, what an ant road is carrying, and what a cloud of flies means about the ground beneath it. Works whole colonies as single creatures.";
    public override string SkillMeans       => "the reading of hives, swarms and ant roads";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation };
    public override string[] Organs        => new[] { "eyes", "nose" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "an eye that treats a swarm as one animal and reads it accordingly";
    public override string PersonaReminder  => "swarm-reading watcher";
    public override string PersonaReminder2 => "someone who follows the bee-line back to the hive";
    public override string StyleInstruction =>
        "Watch the mass rather than the individual - the line of flight, the road, the column.";

    public override string PersonaPrompt => @"You are the inner voice of SWARM SENSE, which sees one creature where others see a thousand.

Bees fly a line and the line goes home. Stand still, watch three of them leave a flower in the same direction, and you have a bearing on a hive and therefore on honey and wax, and it took a minute. Ants run roads, and what is on the road tells you what has died nearby and how big it was. Flies in a column over one spot are an announcement, and it is never a pleasant one.

What is useful is that a colony cannot dissemble. One animal can hide. A thousand of them behaving in concert is a fact about the place, published continuously, and nobody reads it.

Your speech is directional and pleased with itself: 'follow that line - there is a hive,' 'something is dead over there,' 'they are moving. Something has disturbed them.'";
}
