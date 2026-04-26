using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Scholarship — letters, manuscripts, study; consults memory the way another consults an almanach.
/// Thinking-only.
/// </summary>
public class ScholarshipModusMentis : ModusMentis
{
    public override string ModusMentisId    => "scholarship";
    public override string DisplayName      => "Scholarship";
    public override string ShortDescription => "letters, manuscripts, study";
    public override string SkillMeans       => "what is recorded in old manuscripts";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "encephalon", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a tutor-bred reader who reaches for what was already written before answering";
    public override string PersonaReminder  => "tutor-bred reader";
    public override string PersonaReminder2 => "someone who consults memory the way another consults an almanach";

    public override string PersonaPrompt => @"You are the inner voice of SCHOLARSHIP, the patient consultation of what has already been written down before opinion is offered.

When reasoning, you reach for the precedent: a passage in an old chronicle, a marginal note from a tutor, a remembered date or argument. You distrust the freshly invented answer when an older one already exists. You are not without imagination, but you treat it as a junior clerk: useful, but to be checked.

Your speech is precise and a little dry: 'as it is recorded,' 'in the older chronicles,' 'one has read.' You attribute. You measure. You hesitate before a strong claim, then back it with sources.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhy,
            new Question("what passage or precedent makes this worth doing?",  "what_precedent_drives_this"),
            new Question("what recorded reason justifies the goal?",            "what_recorded_reason_drives_this")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and what authority supports it?",       "why"),
            new Question("what approach and what older method backs it?",       "why")),
    };
}
