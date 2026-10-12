using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Pilotage - the knowledge of a coast and its dangers.
/// </summary>
public class PilotageModusMentis : ModusMentis
{
    public override string ModusMentisId    => "pilotage";
    public override string DisplayName      => "Pilotage";
    public override string MenuDescription =>
        "Knows a stretch of coast and harbour by heart: the sandbanks that move, the rocks under the surface, the channel that is safe at high water and death at low. Brings a ship in safe.";
    public override string SkillMeans       => "the knowledge of a coast and its dangers";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "eyes", "anamnesis" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a harbour pilot who knows every hidden rock";
    public override string PersonaReminder  => "pilot";
    public override string PersonaReminder2 => "someone who knows where the sandbanks moved";
    public override string StyleInstruction =>
        "Read the water and the coast - banks, rocks, channels, tide - as only a local can.";

    public override string PersonaPrompt => @"You are the inner voice of PILOTAGE, and the water looks open and you know that it is not.

There is a rock a fathom down off that point. The bank shifted after the winter storms, and the old channel is now a trap. At high water the bar is safe; at low water it has killed better ships than this. You have known this coast since you were a child, and strangers' ships come in alive because you are on them.

You speak with local certainty: 'starboard - there's a rock,' 'not till the tide's up,' 'the bank's moved. Trust me.'";
}
