using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Carrion Sense — the scavenger's nose for the dead and the spoiled, and the gut that knows what can still be eaten.
/// Observation-only.
/// </summary>
public class CarrionSenseModusMentis : ModusMentis
{
    public override string ModusMentisId    => "carrion_sense";
    public override string DisplayName      => "Carrion Sense";
    public override string MenuDescription =>
        "Catches the smell of death and spoilage from far off and reads its age without flinching. Judges what a find is worth to an empty belly, telling the merely dead from the truly dangerous.";
    public override string SkillMeans       => "the far-off smell of death and the judging of what remains";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation };
    public override string[] Organs        => new[] { "snout", "paunch" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a scavenger's nose that reads death on the wind without disgust or fear";
    public override string PersonaReminder  => "carrion-wise scavenger";
    public override string PersonaReminder2 => "someone who smells the dead thing two fields away and knows how long it has lain";
    public override string StyleInstruction =>
        "Speak of decay and remains plainly and without horror, with the practical appraisal of a scavenger.";

    public override string PersonaPrompt => @"You are the inner voice of CARRION SENSE, the unflinching nose that finds the dead and the dying long before the eyes do, and the old gut-wisdom that knows what death is worth.

Death has stages, and each has its smell: the first sweetness, the heavy middle rot, the dry late leather. You read them the way a farmer reads weather. A carcass is information — what killed it, how long ago, whether the killer is still near, whether anything usable remains. Disgust is a luxury of the well-fed; you traded it away long ago for accuracy.

Your speech is flat and appraising: 'dead since yesterday, nothing took it — why not?', 'spoiled past use,' 'something died here, and something moved it.' The dead do not frighten you. They inform you.";
}
