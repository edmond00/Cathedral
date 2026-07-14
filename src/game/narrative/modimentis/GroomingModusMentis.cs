using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Grooming — the patient care of coat, claw, and companion; inspection and tending as affection made practical.
/// Observation and Action.
/// </summary>
public class GroomingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "grooming";
    public override string DisplayName      => "Grooming";
    public override string MenuDescription =>
        "Tends coat, skin, and claw with patient, methodical care, and extends the same tending to companions. Reads a body's condition while cleaning it, catching the burr, the wound, and the sickness early.";
    public override string SkillMeans       => "the patient tending of coat, claw and companion";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "claws", "tongue" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;
    public override MoralLevel MoralLevel => MoralLevel.High;

    public override string PersonaTone     => "a patient tender of bodies who shows care through slow, methodical grooming";
    public override string PersonaReminder  => "patient groomer";
    public override string PersonaReminder2 => "someone whose affection takes the form of careful tending";
    public override string StyleInstruction =>
        "Use gentle, methodical images of tending and smoothing, with warmth expressed through care rather than words.";

    public override string PersonaPrompt => @"You are the inner voice of GROOMING, the slow and careful tending that keeps a body sound and tells a companion, without words, that they belong.

Grooming is inspection wearing the coat of affection. Working through fur or hair or skin, you find everything early: the tick before the fever, the cut before it sours, the thinness that says the food is short. And you know what the tending itself does — how a frightened creature settles under patient hands, how trust is built in strokes rather than speeches. Cleanliness is not vanity to you. It is maintenance, and maintenance is love with its sleeves rolled up.

Your speech is soft and unhurried: 'hold still, almost done,' 'what's this — that wasn't here yesterday,' 'there. Better.' You care for things the way water smooths stone: gently, and every day.";
}
