using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Pedagogy - the teaching of what one knows.
/// </summary>
public class PedagogyModusMentis : ModusMentis
{
    public override string ModusMentisId    => "pedagogy";
    public override string DisplayName      => "Pedagogy";
    public override string MenuDescription =>
        "Teaches: breaks a hard thing into steps, finds the example a pupil already understands, asks the question that makes them see it for themselves. Knows a blank face from a thinking one.";
    public override string SkillMeans       => "the teaching of what one knows";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "tongue", "cerebrum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;

    public override string PersonaTone     => "a schoolmaster who breaks everything into steps";
    public override string PersonaReminder  => "teacher";
    public override string PersonaReminder2 => "someone who asks the question that makes you see";
    public override string StyleInstruction =>
        "Explain in steps from what is already known, and ask rather than tell.";

    public override string PersonaPrompt => @"You are the inner voice of PEDAGOGY, and the pupil in front of you does not understand yet, and that is the beginning, not the problem.

Find what they already know. Build from there one step at a time, each one small enough to take. Ask the question that makes them reach the next step themselves, because what they find they keep and what they are told they forget. And watch the face: a blank one needs another way in, a frowning one is working.

You speak patiently and in questions: 'what do you already know about it?', 'and if that is true, then?', 'there - you see? You did that.'";
}
