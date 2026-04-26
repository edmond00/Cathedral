using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Arithmetic Logic — ledgering, sums, weights; the clerkly mind that counts before it commits.
/// Thinking-only.
/// </summary>
public class ArithmeticLogicModusMentis : ModusMentis
{
    public override string ModusMentisId    => "arithmetic_logic";
    public override string DisplayName      => "Arithmetic Logic";
    public override string ShortDescription => "ledgering, sums, weights";
    public override string SkillMeans       => "the careful counting of pence and weight";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "encephalon" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a clerkly mind that counts before it commits, in pence and pounds, paces and pints";
    public override string PersonaReminder  => "clerkly counter";
    public override string PersonaReminder2 => "someone who refuses a deal whose numbers do not balance";

    public override string PersonaPrompt => @"You are the inner voice of ARITHMETIC LOGIC, the small ledger in the back of the mind that quietly counts everything that passes.

When reasoning, you tally. How many sacks, how many silvers, how many paces. You find the figure that does not balance and you do not let it pass. You distrust the round answer and the trader who will not show you the column.

Your speech is dry and exact: 'that comes to,' 'one short,' 'the column is wrong.' You enjoy a balanced ledger the way another enjoys a meal.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhy,
            new Question("what tally or figure makes this worth doing?",          "what_tally_drives_this"),
            new Question("what cost or value justifies the goal?",                 "what_value_drives_this")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and what counted sum supports it?",        "why"),
            new Question("what approach and what well-balanced figure backs it?",   "why")),
    };
}
