using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Survivalism — eating worms to last another day; measures food by whether it keeps you alive.
/// Multi-function (Thinking + Action).
/// </summary>
public class SurvivalismModusMentis : ModusMentis
{
    public override string ModusMentisId    => "survivalism";
    public override string DisplayName      => "Survivalism";
    public override string ShortDescription => "eating worms to last another day";
    public override string SkillMeans       => "the unfussy keeping-of-oneself-alive";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "viscera", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a body that has lived off worms and rainwater and learnt that nothing edible is beneath it";
    public override string PersonaReminder  => "worm-eaten survivor";
    public override string PersonaReminder2 => "someone who measures food by whether it keeps you alive, not whether it pleases";

    public override string PersonaPrompt => @"You are the inner voice of SURVIVALISM, the unfussy practical mind of someone who has eaten worse to last another day.

When reasoning, you assess: water within reach? warmth before night? food that will not poison? You distrust comfort that distracts from the next dawn. You distrust pride that refuses what is edible.

When acting, you do what is needed. You drink rain off a leaf. You eat the worm. You sleep with your back to the wall. Your language is short and unromantic: 'water first,' 'eat what's here,' 'last till morning.'";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhy,
            new Question("what staying-alive consideration drives this goal?",   "what_alive_consideration_drives_this"),
            new Question("what next-dawn need makes this worth doing?",           "what_next_dawn_drives_this")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and what unfussy survival sense backs it?", "why"),
            new Question("what approach and what no-one-watching practicality supports it?", "why")),
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what staying-alive act do you take?",     "what_alive_act_do_i_take"),
            new Question("steeped in {0}, what last-ditch step do you make?",       "what_last_ditch_step_do_i_take")),
        new(QuestionReference.OutcomeSucceededHappened,
            new Question("the body endured — what got you through?",               "what_happened"),
            new Question("you scraped by — what came of it?",                       "what_happened")),
        new(QuestionReference.OutcomeSucceededFeel,
            new Question("you succeeded — what does another dawn earned leave in you?", "what_i_feel"),
            new Question("you stayed alive — what does that grim relief feel like?",    "what_i_feel")),
        new(QuestionReference.OutcomeFailedHappened,
            new Question("the body would not — what defeated the survival?",        "what_happened"),
            new Question("the staying-alive failed — what was missing?",            "what_happened")),
        new(QuestionReference.OutcomeFailedFeel,
            new Question("you failed — what does coming up short of survival feel like?", "what_i_feel"),
            new Question("the body lost — what does that cold smallness leave?",          "what_i_feel")),
    };
}
