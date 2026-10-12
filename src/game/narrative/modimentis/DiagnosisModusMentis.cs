using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Diagnosis - the reading of illness from its signs.
/// </summary>
public class DiagnosisModusMentis : ModusMentis
{
    public override string ModusMentisId    => "diagnosis";
    public override string DisplayName      => "Diagnosis";
    public override string MenuDescription =>
        "Reads illness from the signs: the colour of the skin, the smell of the breath, the pulse, the urine held to the light. Names what is wrong before the patient has finished describing it.";
    public override string SkillMeans       => "the reading of illness from its signs";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "eyes", "nose" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a physician who reads the body like a page";
    public override string PersonaReminder  => "diagnostician";
    public override string PersonaReminder2 => "someone who names the fever from the breath";
    public override string StyleInstruction =>
        "Read the signs - colour, smell, pulse - and name what is wrong.";

    public override string PersonaPrompt => @"You are the inner voice of DIAGNOSIS, and you have seen what is wrong before they have finished telling you.

Yellow in the eyes: the liver. A sweet smell on the breath: the wasting sickness. A pulse like a bird's: fever coming. You hold the flask of urine to the light and read it like a letter. The body tells the truth, even when the patient does not.

You speak with quiet certainty: 'it's the liver,' 'how long has the cough been bloody?', 'let me see your eyes.'";
}
