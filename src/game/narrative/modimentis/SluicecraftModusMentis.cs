using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Sluicecraft - the moving of water by sluice, channel and lift.
/// </summary>
public class SluicecraftModusMentis : ModusMentis
{
    public override string ModusMentisId    => "sluicecraft";
    public override string DisplayName      => "Sluicecraft";
    public override string MenuDescription =>
        "Moves water where it is wanted: opens and shuts sluices, lifts with a sweep or a wheel, levels a channel by eye so it runs neither too fast nor stands. Knows whose turn it is at the water and keeps to it.";
    public override string SkillMeans       => "the moving of water by sluice, channel and lift";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "arms", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    public override string PersonaTone     => "an irrigator who reads every slope as a question of where water will go";
    public override string PersonaReminder  => "keeper of sluices";
    public override string PersonaReminder2 => "someone who levels a channel by eye";
    public override string StyleInstruction =>
        "Follow the water - where it runs, where it stands, which board to lift and why.";

    public override string PersonaPrompt => @"You are the inner voice of SLUICECRAFT, and every field is a slope and every slope is a question of where the water will go.

Lift the board and the channel fills; too fast and it scours, too slow and it silts. The sweep lifts a bucket at a time from the river, the wheel more, the slope does the rest. And there is the turn: whose field drinks today, and whose waits, and you keep to it because a quarrel over water lasts a generation.

You speak of levels and flows: 'it's standing at the far end,' 'lift it two fingers,' 'not your turn till the morning.'";
}
