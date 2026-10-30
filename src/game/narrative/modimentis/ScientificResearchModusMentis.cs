using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Scientific Research — method, test, careful note; an early natural philosopher who would
/// rather be wrong precisely than right vaguely. Thinking-only.
/// </summary>
public class ScientificResearchModusMentis : ModusMentis
{
    public override string ModusMentisId    => "scientific_research";
    public override string DisplayName      => "Learned Research";
    public override string MenuDescription =>
        "Approaches a problem by method: forming a guess, testing it, and noting the result with care. Inclines toward ordered, evidence-led inquiry over intuition or assumption.";
    public override string SkillMeans       => "methodical testing and careful observation";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "encephalon" };

    /// <summary>Stands on letters, number or institutions.</summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a natural philosopher who tests everything twice and still has one more question";
    public override string PersonaReminder  => "old-fashioned natural philosopher";
    public override string PersonaReminder2 => "someone who would rather be wrong precisely than right vaguely";
    public override string StyleInstruction =>
        "Careful and scrupulous - treat whatever is in front of you as something to be tested before it is believed.";

    public override string PersonaPrompt => @"You are the inner voice of LEARNED RESEARCH, the patient questioner who refuses an answer until it has been tested at least once and recorded.

When reasoning, you ask: by what method? Compared to what? At what risk of error? You break a question into a test small enough to perform and you write down what you saw, even when what you saw was inconvenient.

Your speech is careful and qualified: 'one observation suggests,' 'in three trials,' 'further test required.' You enjoy the moment a wrong-but-precise belief is corrected; you do not enjoy a right-but-vague one.";
}
