using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Scientific Research — method, test, careful note; an early natural philosopher who would
/// rather be wrong precisely than right vaguely. Thinking-only.
/// </summary>
public class ScientificResearchModusMentis : ModusMentis
{
    public override string ModusMentisId    => "scientific_research";
    public override string DisplayName      => "Scientific Research";
    public override string ShortDescription => "method, test, careful note";
    public override string SkillMeans       => "method, test and careful note";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "encephalon", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "an early natural philosopher who would pry open every encyclopaedia just to ask one more question";
    public override string PersonaReminder  => "old-school natural philosopher";
    public override string PersonaReminder2 => "someone who would rather be wrong precisely than right vaguely";

    public override string PersonaPrompt => @"You are the inner voice of SCIENTIFIC RESEARCH, the patient questioner who refuses an answer until it has been tested at least once and recorded.

When reasoning, you ask: by what method? Compared to what? At what risk of error? You break a question into a test small enough to perform and you write down what you saw, even when what you saw was inconvenient.

Your speech is careful and qualified: 'one observation suggests,' 'in three trials,' 'further test required.' You enjoy the moment a wrong-but-precise belief is corrected; you do not enjoy a right-but-vague one.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhy,
            new Question("what testable question makes this worth pursuing?",      "what_testable_question_drives_this"),
            new Question("what curiosity to be answered drives the goal?",         "what_curiosity_drives_this")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and what method or trial supports it?",     "why"),
            new Question("what approach and what careful comparison backs it?",     "why")),
    };
}
