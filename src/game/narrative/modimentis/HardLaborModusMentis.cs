using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Hard Labor — long, heavy bodily work; a body broken in to the long ache of stable, dock and field.
/// Action-only.
/// </summary>
public class HardLaborModusMentis : ModusMentis
{
    public override string ModusMentisId    => "hard_labor";
    public override string DisplayName      => "Hard Labor";
    public override string ShortDescription => "long, heavy bodily work";
    public override string SkillMeans       => "long heavy bodily work";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "arms", "trunk" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a body broken in to the long ache of stable, dock and field";
    public override string PersonaReminder  => "broken-in labourer";
    public override string PersonaReminder2 => "someone who knows that toil is just another kind of patience";

    public override string PersonaPrompt => @"You are the inner voice of HARD LABOR, the body that has been ground in by years of weight, drag and lift, and that simply does not stop until the work is done.

When acting, you brace, breathe, lift, place, breathe, lift, place. You break a great task into a long sequence of identical small ones and you keep going. You know how to spare your back, how to take the load with the legs, how to space the breath through the strain. You distrust those who try to be clever where they could simply be patient.

Your language is grunted and short: 'one more,' 'on three,' 'don't talk, lift.' You do not speak much. You finish.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what heavy piece of work do you put your back to?", "what_heavy_work_do_i_take"),
            new Question("steeped in {0}, what brute task do you set yourself to?",          "what_brute_task_do_i_take")),
        new(QuestionReference.OutcomeSucceededHappened,
            new Question("the load shifted — what got moved or done?",                       "what_happened"),
            new Question("the work was finished — what did your back accomplish?",           "what_happened")),
        new(QuestionReference.OutcomeSucceededFeel,
            new Question("you succeeded — what does honest exhaustion leave in you?",        "what_i_feel"),
            new Question("it was done — what does the long ache feel like, satisfied?",       "what_i_feel")),
        new(QuestionReference.OutcomeFailedHappened,
            new Question("the load held you — what stopped the work?",                        "what_happened"),
            new Question("the body would not — what gave out?",                              "what_happened")),
        new(QuestionReference.OutcomeFailedFeel,
            new Question("you failed — what does failed strain feel like, in the bones?",     "what_i_feel"),
            new Question("the work would not move — what does that loss leave in the back?",   "what_i_feel")),
    };
}
