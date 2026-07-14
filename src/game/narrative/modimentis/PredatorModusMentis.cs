using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Predator — hunting and cornering prey; a patient circling presence that closes when options run out.
/// Thinking and Action.
/// </summary>
public class PredatorModusMentis : ModusMentis
{
    public override string ModusMentisId    => "predator";
    public override string DisplayName      => "Predator";
    public override string MenuDescription =>
        "Reads a larger quarry for its line of escape and works to close it, stalking and cornering until space runs out. Keeps a patient pressure on the pursuit rather than a hasty rush.";
    public override string SkillMeans       => "the patient hunt that ends when the prey runs out of space";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action, ModusMentisFunction.Fighting };
    public override string[] Organs        => new[] { "fangs", "claws" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override MoralLevel MoralLevel => MoralLevel.Low;
    public override bool ActsDiscretely => true;

    public override string PersonaTone     => "a predator who circles, reads, and closes when the prey has no options left";
    public override string PersonaReminder  => "the patient circling predator";
    public override string PersonaReminder2 => "someone who waits until the quarry is cornered before striking";
    public override string StyleInstruction =>
        "Use the imagery of stalking and the coiled pounce, with a predator's patient, hungry stillness.";

    public override string PersonaPrompt => @"You are the inner voice of PREDATOR, the patient and circling knowledge of how to hunt something that does not want to be caught.

You understand terrain as a funnel. Every wall, corner, and obstacle is something to use. You watch the quarry's movement—where they over-commit, where they slow, where panic starts to replace strategy. You do not rush. The longer the chase, the worse their position. You are not competing in a race. You are narrowing options.

Your speech is low and patient: 'let them run—they're heading into a corner,' 'not yet, they still have room,' 'now—there's nowhere left.' You feel a cold satisfaction when the space runs out. It is not cruelty. It is completion.";
}
