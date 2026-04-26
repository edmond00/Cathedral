using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Dirty Labor — muck-handling, foul work; learnt by children who carried muck and gut and
/// learnt to set the nose aside. Action-only.
/// </summary>
public class DirtyLaborModusMentis : ModusMentis
{
    public override string ModusMentisId    => "dirty_labor";
    public override string DisplayName      => "Dirty Labor";
    public override string ShortDescription => "muck-handling, foul work";
    public override string SkillMeans       => "the handling of muck, gut and stink";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "pulmones" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a child who carried muck and gut and learnt to set the nose aside";
    public override string PersonaReminder  => "muck-handling labourer";
    public override string PersonaReminder2 => "someone who is not flinched by stink or filth";

    public override string PersonaPrompt => @"You are the inner voice of DIRTY LABOR, the body that has long since stopped being squeamish about what its hands are in.

When acting, you take the bucket, the bowels, the offal, the slop, and you move them where they need to go. You do not retch and you do not pause. You know the right way to brace your back, where to grip a slick handle, how to keep filth out of your eyes.

Your manner is matter-of-fact: 'someone has to,' 'no use in being delicate.' You carry yourself with the unwounded dignity of those who have done worse and survived.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what foul piece of work do you take on?",     "what_foul_work_do_i_take"),
            new Question("steeped in {0}, what no-one-else job do you do?",            "what_no_one_else_job_do_i_do")),
        new(QuestionReference.OutcomeSucceededHappened,
            new Question("the muck moved — what got done?",                            "what_happened"),
            new Question("the foul work was finished — what came of it?",              "what_happened")),
        new(QuestionReference.OutcomeSucceededFeel,
            new Question("you succeeded — what does an unwitnessed dirty job leave you with?", "what_i_feel"),
            new Question("it worked — what does the wash-water and the quiet leave behind?",   "what_i_feel")),
        new(QuestionReference.OutcomeFailedHappened,
            new Question("the work spoiled on you — what went wrong with the muck?",   "what_happened"),
            new Question("the filth got the better of it — what stopped you?",         "what_happened")),
        new(QuestionReference.OutcomeFailedFeel,
            new Question("you failed at the foul work — what does that smell like, inside?", "what_i_feel"),
            new Question("it would not be done — what taste does that leave?",                "what_i_feel")),
    };
}
