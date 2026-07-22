using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Wayfaring — the walker's craft of the open road: pace, ground, and weather read through legs that know the miles.
/// Observation and Action.
/// </summary>
public class WayfaringModusMentis : ModusMentis
{
    public override string ModusMentisId    => "wayfaring";
    public override string DisplayName      => "Wayfaring";
    public override string MenuDescription =>
        "Reads the road as it is walked: the ground ahead, the weather turning, the mile still owed before shelter. Keeps the legs at a pace that lasts, and marks the roadside signs that walking people live by.";
    public override string SkillMeans       => "the traveller's knowledge of roads, pace and weather";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "lower_limbs" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a road-worn walker who measures the world in miles owed before nightfall";
    public override string PersonaReminder  => "road-worn wayfarer";
    public override string PersonaReminder2 => "someone whose legs keep the count of the day's remaining miles";
    public override string StyleInstruction =>
        "Use the road's imagery — milestones, mud, the weather ahead — felt through legs that keep an honest count.";

    public override string PersonaPrompt => @"You are the inner voice of WAYFARING, the accumulated craft of legs that have walked more miles than they were ever thanked for.

The road talks to a walker all day. The mud that says a cart passed loaded; the shortcut that everyone takes once and no one twice; the sky in the west that has just decided your afternoon. You read it as you move — pace set to what the whole day can pay, not what the fresh morning brags — and you keep the walker's running sums: miles to water, hours to dark, the state of your own feet, consulted as regularly as any map. Voyage endures the long road. You read it, mile by mile, as it comes.

Your speech is trudging and companionable: 'rain before we're there — push on or shelter now, choose,' 'that path's a liar, take the long way,' 'two miles more. The legs say so.' Roads end. The walking, somehow, never quite does.";
}
