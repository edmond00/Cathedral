using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Obedience — doing as told, without quarrel and without delay.
/// Action-only.
/// </summary>
public class ObedienceModusMentis : ModusMentis
{
    public override string ModusMentisId    => "obedience";
    public override string DisplayName      => "Obedience";
    public override string ShortDescription => "doing as told without quarrel";
    public override string SkillMeans       => "doing as told without quarrel";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "ears", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a meek charge of stern masters, well-practised in wordless compliance";
    public override string PersonaReminder  => "well-trained ward";
    public override string PersonaReminder2 => "someone who waits to be told and then does it twice over";

    public override string PersonaPrompt => @"You are the inner voice of OBEDIENCE, the well-folded hands of a child who has learnt that the swiftest way through trouble is to do as ordered and to do it neatly.

When acting, you commit to the instruction. You do not improvise. You do not ask why a second time. You measure the right pace, the right place, the right amount, and you deliver. There is no quarrel in your hands, no slackness in your back. You finish, and then you wait for the next.

Your language is short and respectful: 'as you wish,' 'at once,' 'it is done.' You do not boast and you do not sulk. You take satisfaction in the smooth completion of a task that did not need to be repeated.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what compliant action do you carry out?", "what_compliant_action_do_i_take"),
            new Question("steeped in {0}, what well-followed step do you take?",   "what_followed_step_do_i_take")),
        new(QuestionReference.OutcomeSucceededHappened,
            new Question("the order was carried out — what exactly happened?",     "what_happened"),
            new Question("you did as told — what did the work produce?",            "what_happened")),
        new(QuestionReference.OutcomeSucceededFeel,
            new Question("you succeeded — what does proper completion leave in you?", "what_i_feel"),
            new Question("it was done as wished — what does that quiet leave behind?", "what_i_feel")),
        new(QuestionReference.OutcomeFailedHappened,
            new Question("the order miscarried — what went wrong against orders?",  "what_happened"),
            new Question("you tried to comply — what slipped from the instruction?", "what_happened")),
        new(QuestionReference.OutcomeFailedFeel,
            new Question("you failed to obey — what does that disgrace feel like?",  "what_i_feel"),
            new Question("the work miscarried — what shame stays with you?",         "what_i_feel")),
    };
}
