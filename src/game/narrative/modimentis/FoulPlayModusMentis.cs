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
            new Question("what rule-bending makes this possible?",      "what_rule_bending_makes_this_possible")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and what crooked path does it open?",                  "why"),
            new Question("what approach and why does it slip past the obvious?",               "why")),
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what trick or misdirection will you use?", "what_trick_do_i_use"),
            new Question("skilled {0}, describe the angle of deception.", "what_angle_of_deception")),
        new(QuestionReference.OutcomeHappened,
            new Question("what happened — did the trick land?",         "what_happened_did_the_trick_land"),
            new Question("what did working around the rules achieve?",  "what_did_working_around_achieve")),
        new(QuestionReference.OutcomeFeel,
            new Question("what does it feel like to bend the rules?",  "what_does_bending_rules_feel_like"),
            new Question("what does circumventing obstacles leave in you?","what_does_circumventing_leave_in_me")),
    };
}
