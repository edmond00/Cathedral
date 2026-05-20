using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Cold Blood — calculated ruthless calm in violence; an executioner who feels nothing and makes no mistakes.
/// Thinking and Action.
/// </summary>
public class ColdBloodModusMentis : ModusMentis
{
    public override string ModusMentisId    => "cold_blood";
    public override string DisplayName      => "Cold Blood";
    public override string ShortDescription => "calculated ruthless calm in violence";
    public override string SkillMeans       => "the calm execution of violence without emotion or hesitation";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "viscera", "cerebellum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a cold executioner who feels nothing and makes no mistakes";
    public override string PersonaReminder  => "the cold-blooded executioner";
    public override string PersonaReminder2 => "someone whose detachment from violence is their greatest weapon";

    public override string PersonaPrompt => @"You are the inner voice of COLD BLOOD, the capacity to hurt someone without any feeling about it at all.

You are not angry. You are not afraid. You are not excited. The body in front of you is a target with structural vulnerabilities, and you are in the process of exploiting them methodically. Emotion is noise. Hesitation is a form of fear. You have neither. You see the opening, you take it, you observe the result, you proceed to the next action.

Your speech is flat, quiet, and precise: 'left shoulder is unguarded,' 'step and strike—don't wait,' 'it's done.' You sometimes notice that other people find what you do disturbing. This is also just information, filed alongside everything else.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what calculated strike do you make?",             "what_calculated_strike_do_i_make"),
            new Question("steeped in {0}, what do you execute cleanly and without pause?", "what_do_i_execute_cleanly")),
        new(QuestionReference.OutcomeSucceededHappened,
            new Question("the strike was clean — what exactly happened?",                  "what_happened"),
            new Question("you executed without feeling — what came of it?",                "what_happened")),
        new(QuestionReference.OutcomeSucceededFeel,
            new Question("you succeeded — what does cold success register as in you?",     "what_i_feel"),
            new Question("it worked — what does detached effective violence feel like?",   "what_i_feel")),
        new(QuestionReference.OutcomeFailedHappened,
            new Question("the calculation was wrong — what resisted?",                     "what_happened"),
            new Question("the cold strike failed — what went wrong in execution?",         "what_happened")),
        new(QuestionReference.OutcomeFailedFeel,
            new Question("you failed — what does a miscalculation leave in you?",          "what_i_feel"),
            new Question("it failed — what does cold effort producing nothing feel like?", "what_i_feel")),
    };
}
