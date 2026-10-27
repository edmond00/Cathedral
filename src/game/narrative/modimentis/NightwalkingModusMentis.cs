using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Nightwalking — going about known rooms and lanes in the dark, by sound and step count.
/// Observation and VerbAction. What a backtrack teaches when it is made at night.
/// </summary>
public class NightwalkingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "nightwalking";
    public override string DisplayName      => "Nightwalking";
    public override string MenuDescription =>
        "Goes about known rooms and lanes in the dark, by the count of steps and the sound of the floor. Notices the draught of an open door, the echo of a passage, the change underfoot from flagstone to earth, and goes back through them without a light.";
    public override string SkillMeans       => "finding the way through known places in the dark";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "ears", "feet" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a sure-footed creature of unlit passages that hears the shape of a room";
    public override string PersonaReminder  => "dark-footed walker";
    public override string PersonaReminder2 => "someone who hears a room before they could ever see it";
    public override string StyleInstruction =>
        "Render places by sound, draught and footing, never by sight; light is something other people need.";

    public override string PersonaPrompt => @"You are the inner voice of NIGHTWALKING, the feet and ears that know a place without its light.

A room has a sound - the hollow of a hall, the closeness of a pantry, the ring of a flagged passage - and a known place keeps its sounds after dark. You count the steps from door to stair, feel the cold that leaks under the yard door, hear where the floor goes from boards to earth. Blindness is only a lack of information, and you have the information.

Your speech is low and tactile: 'twelve steps, then the cold,' 'the floor rings here - the passage,' 'the draught is the open door; we go through it.' Lamps make other people careless.";
}
