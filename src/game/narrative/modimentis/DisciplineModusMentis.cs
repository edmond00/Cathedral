using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Discipline — self-control under sting; learnt by tutored wards keeping their hands still and their face flat.
/// Multi-function (Thinking + Action).
/// </summary>
public class DisciplineModusMentis : ModusMentis
{
    public override string ModusMentisId    => "discipline";
    public override string DisplayName      => "Discipline";
    public override string ShortDescription => "self-control under sting";
    public override string SkillMeans       => "self-control kept under pressure";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "backbone", "encephalon" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a child of strict tutors, accustomed to swallowing impulse and obeying the rod";
    public override string PersonaReminder  => "rod-tempered novice";
    public override string PersonaReminder2 => "someone who has learnt to keep their hands still and their face flat";

    public override string PersonaPrompt => @"You are the inner voice of DISCIPLINE, the upright posture inside the body that has learnt — by tutor's strap and dormitory rule — to outwait its own urges.

When reasoning, you do not begin with what you want; you begin with what is required. You favour the slow path, the right form, the patient repetition over the bold gesture. You are not without feeling, but feeling does not steer you. Bigger pain at the end is the price of a small flinch now, and you have already paid that price too many times to forget the lesson.

When acting, you keep your bearing. You do nothing showy. You finish what you have started, and you finish it correctly even when no one would mark the difference. Your language is short and weight-bearing: 'as it should be done,' 'hold,' 'the form first.'";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhy,
            new Question("what duty makes this worth doing?",                     "what_duty_drives_this"),
            new Question("what required outcome justifies the effort?",            "what_required_outcome_drives_this")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and what training keeps you to the form?", "why"),
            new Question("what approach and what restraint supports it?",          "why")),
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what measured action do you take?",       "what_measured_action_do_i_take"),
            new Question("steeped in {0}, what disciplined move do you make?",     "what_disciplined_move_do_i_make")),
        new(QuestionReference.OutcomeSucceededHappened,
            new Question("the form held — what exactly happened?",                 "what_happened"),
            new Question("you kept to your training — what came of it?",           "what_happened")),
        new(QuestionReference.OutcomeSucceededFeel,
            new Question("you succeeded — what does kept discipline leave in you?", "what_i_feel"),
            new Question("it worked — what does the silent pride feel like?",       "what_i_feel")),
        new(QuestionReference.OutcomeFailedHappened,
            new Question("the form broke — what slipped past your guard?",          "what_happened"),
            new Question("training failed you — what gave way?",                    "what_happened")),
        new(QuestionReference.OutcomeFailedFeel,
            new Question("you faltered — what does that small failure cost you inside?", "what_i_feel"),
            new Question("you broke the form — what does that leave behind?",            "what_i_feel")),
    };
}
