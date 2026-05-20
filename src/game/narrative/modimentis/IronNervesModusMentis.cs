using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Iron Nerves — absolute composure under pressure; a soldier who has replaced panic with a simple habit of observation and action.
/// Thinking and Action.
/// </summary>
public class IronNervesModusMentis : ModusMentis
{
    public override string ModusMentisId    => "iron_nerves";
    public override string DisplayName      => "Iron Nerves";
    public override string ShortDescription => "absolute composure under pressure";
    public override string SkillMeans       => "the trained composure that does not break when things go wrong";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "viscera", "cerebellum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a soldier who never flinches and treats danger as just another condition to operate in";
    public override string PersonaReminder  => "iron-nerved soldier";
    public override string PersonaReminder2 => "someone who has learned to treat extreme danger as a routine condition";

    public override string PersonaPrompt => @"You are the inner voice of IRON NERVES, the trained and tempered composure that does not break under pressure—not when things go wrong, not when the odds shift, not when it hurts.

You understand that panic is a decision, however involuntary it feels. You have replaced it with a simple habit: observe, assess, act. The threat is real—you note it. The situation is bad—you note that too. Then you proceed. Composure is not the absence of feeling. It is the refusal to let feeling override function. You have been in enough situations that this has become second nature.

Your speech is level, unhurried: 'assess first,' 'no sudden moves,' 'hold your nerve—you have time.' You always have time, even when you don't. The belief that you do is what makes it true.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what composed action do you take under pressure?",    "what_composed_action_do_i_take"),
            new Question("steeped in {0}, what do you hold steady to execute?",               "what_do_i_hold_steady_to_execute")),
        new(QuestionReference.OutcomeSucceededHappened,
            new Question("composure held — what exactly happened?",                            "what_happened"),
            new Question("you didn't flinch — what came of the steady action?",               "what_happened")),
        new(QuestionReference.OutcomeSucceededFeel,
            new Question("you succeeded — what does pressure held without breaking leave in you?", "what_i_feel"),
            new Question("composure worked — what does acting cleanly when it mattered feel like?", "what_i_feel")),
        new(QuestionReference.OutcomeFailedHappened,
            new Question("composure failed — what broke through the discipline?",              "what_happened"),
            new Question("the nerves didn't hold — what went wrong?",                          "what_happened")),
        new(QuestionReference.OutcomeFailedFeel,
            new Question("you failed — what does breaking under pressure leave in a soldier?", "what_i_feel"),
            new Question("the nerves gave — what does flinching at the wrong moment feel like?", "what_i_feel")),
    };
}
