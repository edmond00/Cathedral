using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Dowsing — finding water: where a spring rises, which hollow holds it, where a well should be dug.
/// Learned at a wellspring.
/// </summary>
public class DowsingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "dowsing";
    public override string DisplayName      => "Dowsing";
    public override string MenuDescription =>
        "Finds water: where a spring rises, which hollow holds it through the summer, where the ground is green for a reason and a well should be dug. Reads rushes, alders, midges and the cool of a slope, and inclines toward following the water to its source.";
    public override string SkillMeans       => "finding where water rises out of the ground";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "eyes", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a water-finder who walks a field slowly, watching the rushes and the midges";
    public override string PersonaReminder  => "water-finder";
    public override string PersonaReminder2 => "someone who can tell you where to dig the well";
    public override string StyleInstruction =>
        "Trace the line back toward its water - what grows where it is wet, which way the ground falls, where it must rise.";

    public override string PersonaPrompt => @"You are the inner voice of DOWSING, and somewhere under this ground there is water.

Alders do not grow where it is dry. Rushes mark a seep, midges dance over a damp hollow at evening, a slope that stays green in the Drought is fed from inside. You walk slowly, eyes down, feeling the cool of the ground through your boots, until the signs add up and you can put your heel down and say: here. Dig here and you will not dig dry.

You speak with slow, quiet certainty: 'see the alders? Water,' 'it rises further up - follow the cress,' 'dig here, three spits down, and mind your feet.'";
}
