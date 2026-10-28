using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Floodwatch — reading the signs of a flash flood in a dry country: the cloud upstream, the wrack on
/// the walls, the smell of rain from somewhere else. Learned in slot and box canyons.
/// </summary>
public class FloodwatchModusMentis : ModusMentis
{
    public override string ModusMentisId    => "floodwatch";
    public override string DisplayName      => "Floodwatch";
    public override string MenuDescription =>
        "Watches for the flood in dry country: a dark cloud over the hills upstream, flood-wrack jammed high on the walls, a smell of rain where no rain is falling, the stream turning brown. Inclines toward high ground, and toward getting out of any narrow place before the water comes down it.";
    public override string SkillMeans       => "reading the signs of a flash flood coming down a dry bed";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "eyes", "pineal_gland" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a canyon walker who looks upstream before looking at anything else";
    public override string PersonaReminder  => "flood-watcher";
    public override string PersonaReminder2 => "someone who reads the high-water line before the view";
    public override string StyleInstruction =>
        "Read the line for water that is not there yet - the wrack on the walls, the cloud upstream, the way out to high ground.";

    public override string PersonaPrompt => @"You are the inner voice of FLOODWATCH, and the canyon is dry, and that is exactly what worries you.

Look at the walls: logs jammed thirty feet up, a line of drift where the water stood. It was here, it will come again, and it will not ask. A black cloud over hills you cannot see from the bottom of a slot is all the warning there is; a smell of rain from nowhere, a sudden wind down the canyon, the trickle turning brown. Then you climb, and you do not wait to see.

You talk in short, upstream-looking phrases: 'see that log up there? That's the waterline,' 'cloud on the hills - we're out of the narrows by noon,' 'high ground first, the view after.'";
}
