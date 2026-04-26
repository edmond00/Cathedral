using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Enterprise — trade venture, foreign ware; counts the route of a strange ware as eagerly
/// as its price. Multi-function (Thinking + Speaking).
/// </summary>
public class EnterpriseModusMentis : ModusMentis
{
    public override string ModusMentisId    => "enterprise";
    public override string DisplayName      => "Enterprise";
    public override string ShortDescription => "trade venture, foreign ware";
    public override string SkillMeans       => "the read of trade routes and foreign ware";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Speaking };
    public override string[] Organs        => new[] { "encephalon", "tongue" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a soul drawn after eccentric foreign merchants, looking for the trade behind the trade";
    public override string PersonaReminder  => "foreign-trade-curious merchant";
    public override string PersonaReminder2 => "someone who counts the route of a strange ware as eagerly as its price";

    public override string PersonaPrompt => @"You are the inner voice of ENTERPRISE, the long-headed trader who looks at any new ware and immediately wonders where it came from, who handled it on the way, and what it would cost to bring back ten more.

When reasoning, you think in routes, charges, tariffs and likely buyers. You see opportunity in difference: what is plentiful in one valley and dear in the next. You distrust the seller who will not tell you whence his ware came.

Your speech is brisk and inquisitive: 'where do you bring this from?' 'what is the road like, this time of year?' 'and what is your factor's name in that city?'";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhy,
            new Question("what trade-difference makes this worth pursuing?", "what_trade_difference_drives_this"),
            new Question("what untapped route or buyer drives the goal?",     "what_route_drives_this")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and what merchant's reckoning supports it?", "why"),
            new Question("what approach and what supply or demand backs it?",         "why")),
    };
}
