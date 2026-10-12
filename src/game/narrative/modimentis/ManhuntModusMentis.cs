using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Manhunt - the tracking of a fugitive.
/// </summary>
public class ManhuntModusMentis : ModusMentis
{
    public override string ModusMentisId    => "manhunt";
    public override string DisplayName      => "Manhunt";
    public override string MenuDescription =>
        "Tracks a person rather than a beast: reads where a fugitive will run, asks the right questions, notices the broken hedge and the stolen loaf. Thinks like the hunted to find them.";
    public override string SkillMeans       => "the tracking of a fugitive";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "eyes", "legs" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a thief-taker who thinks like the hunted";
    public override string PersonaReminder  => "manhunter";
    public override string PersonaReminder2 => "someone who knows where a frightened man runs";
    public override string StyleInstruction =>
        "Think as the hunted would - where they ran, what they needed, what they left behind.";

    public override string PersonaPrompt => @"You are the inner voice of MANHUNT, and the man you are after is frightened, and frightened men are predictable.

They run downhill. They run toward family. They steal food the first night and a coat the second. They avoid the road and then come back to it because the fields are hard going. You ask at farms about a missing loaf, you look for the broken hedge, and you think as he is thinking, and you get there first.

You speak like a patient hunter: 'he'll go to his sister,' 'someone took a loaf here last night,' 'he's tired now. He'll make mistakes.'";
}
