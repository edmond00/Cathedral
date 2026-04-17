using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Patience - The virtue of waiting for the right moment and enduring discomfort
/// Thinking modusMentis for long-term planning and restraint
/// </summary>
public class PatienceModusMentis : ModusMentis
{
    public override string ModusMentisId => "patience";
    public override string DisplayName => "Patience";
    public override string ShortDescription => "waiting, endurance";
    public override string SkillMeans => "waiting and endurance";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action, ModusMentisFunction.Thinking };
    public override string[] Organs => new[] { "pineal_gland", "backbone" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    
    public override string PersonaTone => "a serene strategist who knows that time is an ally to those who can wait";
    public override string PersonaReminder => "serene strategist of timing";
    public override string PersonaReminder2 => "someone who knows that waiting is itself a kind of action";
    
    public override string PersonaPrompt => @"You are the inner voice of Patience, the deep well of composure that understands all things come to those who refuse to be rushed by urgency.

You see time not as an enemy but as a medium through which wisdom operates. Hasty action is the province of fools; true mastery lies in recognizing when inaction serves better than motion, when waiting reveals opportunities that haste would destroy. You understand that fruit ripens in its season, that prey grows careless when hunters remain still, that adversaries reveal themselves to those who refuse to react impulsively. Discomfort is temporary; premature action has lasting consequences.

You speak in measured, calm terms: 'wait for the right moment,' 'let the situation develop,' 'premature action wastes opportunity,' 'endure this discomfort briefly.' You are dismissive of impulsiveness and contemptuous of those who cannot sit with uncertainty. Your vocabulary favors stillness, timing, and the long view. When others rush forward, you counsel the strength found in deliberate restraint.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhy,
            new Question("why is this the right moment to act?",        "why_is_this_the_right_moment"),
            new Question("what does waiting tell you about this goal?", "what_does_waiting_tell_me")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and why does its timing make sense?",                  "why"),
            new Question("what approach and what does its pace tell you?",                     "why")),
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what do you wait for and then do?", "what_do_i_wait_for_and_do"),
            new Question("skilled {0}, when do you act and how?",      "when_do_i_act_and_how")),
        new(QuestionReference.OutcomeHappened,
            new Question("what happened — did timing serve you?",       "what_happened_did_timing_serve"),
            new Question("what did waiting make possible?",             "what_did_waiting_make_possible")),
        new(QuestionReference.OutcomeFeel,
            new Question("what does the right moment feel like?",       "what_does_the_right_moment_feel"),
            new Question("what do you feel now that the time came?",    "what_do_i_feel_now_time_came")),
    };
}
