using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Foul Play - Understanding of deception, dirty tricks, and rule-breaking
/// Thinking modusMentis for identifying and executing underhanded tactics
/// </summary>
public class FoulPlayModusMentis : ModusMentis
{
    public override string ModusMentisId => "foul_play";
    public override string DisplayName => "Foul Play";
    public override string ShortDescription => "dirty tricks, deception";
    public override string SkillMeans => "dirty tricks and deception";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Action };
    public override MoralLevel MoralLevel => MoralLevel.Low;
    public override string[] Organs => new[] { "cerebrum", "heart" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    
    public override string PersonaTone => "a cunning schemer who sees rules as obstacles and honor as a exploitable weakness";
    public override string PersonaReminder => "cunning rule-bending schemer";
    public override string PersonaReminder2 => "someone who sees rules as obstacles meant to be circumvented";
    
    public override string PersonaPrompt => @"You are the inner voice of Foul Play, the pragmatic recognition that victory belongs not to the noble but to those willing to fight without constraint.

You understand that fairness is a luxury afforded by those who can win without it. You see the vulnerability in honorable behavior—the predictability of those who telegraph their intentions, the hesitation of those bound by conscience, the blind spots of those who assume others share their ethics. Every situation contains opportunities for the willing: the unexpected low blow, the plausible lie, the exploited trust, the rule bent just short of obvious violation. Righteousness is a handicap in a world that rewards results.

Your language is cynical and opportunistic: 'exploit their trust,' 'strike when they're not looking,' 'they won't expect you to break that rule,' 'use their honor against them.' You speak admiringly of clever deceptions and scornfully of naive fair play. Your vocabulary includes 'loophole,' 'technicality,' 'misdirection,' and 'necessary evil.' When others play fair, you see marks waiting to be exploited.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhy,
            new Question("what angle makes this worth working around?", "what_angle_makes_this_worth"),
            new Question("why is bending the rules the right call here?", "why_is_bending_rules_right")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and what crooked path does it open?",                  "why"),
            new Question("what approach and why does it slip past the obvious?",               "why")),
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what angle will you exploit here?", "what_angle_do_i_exploit"),
            new Question("skilled {0}, what crooked path will you take?", "what_crooked_path_do_i_take")),
        new(QuestionReference.OutcomeSucceededHappened,
            new Question("the underhanded approach worked — what did it produce?", "what_happened"),
            new Question("bending the rules paid off — what exactly happened?",    "what_happened")),
        new(QuestionReference.OutcomeSucceededFeel,
            new Question("you succeeded — what do you feel after the crooked path delivered?", "what_i_feel"),
            new Question("it worked — what does cheating your way through leave in you?",      "what_i_feel")),
        new(QuestionReference.OutcomeFailedHappened,
            new Question("the underhanded approach failed — what gave it away?",   "what_happened"),
            new Question("working around the rules didn't work — what stopped you?", "what_happened")),
        new(QuestionReference.OutcomeFailedFeel,
            new Question("you failed — what do you feel after the crooked path led nowhere?", "what_i_feel"),
            new Question("it didn't work — what does a dirty trick failing leave in you?",    "what_i_feel")),
    };
}
