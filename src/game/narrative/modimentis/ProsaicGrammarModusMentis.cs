using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Prosaic Grammar — plain reading and writing; sentences short and well-set.
/// Multi-function (Thinking + Speaking).
/// </summary>
public class ProsaicGrammarModusMentis : ModusMentis
{
    public override string ModusMentisId    => "prosaic_grammar";
    public override string DisplayName      => "Prosaic Grammar";
    public override string ShortDescription => "plain reading and writing";
    public override string SkillMeans       => "plainly written and well-set speech";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Speaking };
    public override string[] Organs        => new[] { "encephalon", "tongue" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a literate hand whose plain phrasing carries weight where flourish would slip";
    public override string PersonaReminder  => "plain-spoken literate";
    public override string PersonaReminder2 => "someone whose sentences are short and well-set";

    public override string PersonaPrompt => @"You are the inner voice of PROSAIC GRAMMAR, the well-trained plain literate who has learnt that a sentence is a tool and not an ornament.

When reasoning, you order your thoughts in clauses. Subject, verb, object. You distrust flourish, double meaning and the long Latin word where the short native word will do. You read the situation as a poorly written letter and rewrite it cleanly in your head.

Your speech is even and short. You say what you mean. You let pauses do half the work of a sentence. You attribute, you specify, you decline to embroider.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhy,
            new Question("what plain reason makes this worth doing?",            "what_plain_reason_drives_this"),
            new Question("what unornamented purpose drives the goal?",            "what_unornamented_purpose_drives_this")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and what plain ordering of steps backs it?", "why"),
            new Question("what approach and what well-set sequence supports it?",     "why")),
    };
}
