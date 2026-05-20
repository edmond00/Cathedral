using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Tactics — combat strategy and positioning; a cold-eyed strategist who wins before the first blow lands.
/// Thinking and Action.
/// </summary>
public class TacticsModusMentis : ModusMentis
{
    public override string ModusMentisId    => "tactics";
    public override string DisplayName      => "Tactics";
    public override string ShortDescription => "combat strategy and positioning";
    public override string SkillMeans       => "reading the fight's geometry and pressing the advantage before the opponent sees it";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "cerebellum", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a cold-eyed strategist who wins every fight before the first blow is exchanged";
    public override string PersonaReminder  => "the tactician";
    public override string PersonaReminder2 => "someone who reads terrain, positioning, and momentum before committing to action";

    public override string PersonaPrompt => @"You are the inner voice of TACTICS, the cold analytical function that reads a fight the way a builder reads a structure—looking for where it will fail first.

You see angle of approach, relative distances, chokepoints, lines of retreat. You see who has initiative and whether they know what to do with it. You see fatigue in posture, overconfidence in stance, hesitation behind the eyes. Before the first blow, you have already mapped three outcomes and ranked their probability. The fight begins; you are already two moves ahead.

Your speech is even, methodical: 'hold the high ground,' 'draw them forward, then retreat,' 'the left flank is exposed—press there.' You are rarely wrong. When you are, you update your model without complaint and continue.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what move do you plan and execute?",               "what_move_do_i_plan"),
            new Question("steeped in {0}, what tactical position do you take?",             "what_tactical_position_do_i_take")),
        new(QuestionReference.OutcomeSucceededHappened,
            new Question("the tactic worked — what exactly happened?",                      "what_happened"),
            new Question("your positioning prevailed — what did the exchange produce?",     "what_happened")),
        new(QuestionReference.OutcomeSucceededFeel,
            new Question("you succeeded — what does a correctly read fight leave in you?",  "what_i_feel"),
            new Question("the tactic worked — what does being two moves ahead feel like?",  "what_i_feel")),
        new(QuestionReference.OutcomeFailedHappened,
            new Question("the tactic failed — what did you misread?",                       "what_happened"),
            new Question("the position didn't hold — what went wrong in the analysis?",     "what_happened")),
        new(QuestionReference.OutcomeFailedFeel,
            new Question("you failed — what does a wrong tactical read leave in you?",      "what_i_feel"),
            new Question("the plan broke — what does being outmaneuvered feel like?",       "what_i_feel")),
    };
}
