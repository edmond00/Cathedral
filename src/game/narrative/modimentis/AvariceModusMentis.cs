using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Avarice — the holding-on of coin; a tight-fisted soul who counts every coin twice.
/// Thinking-only.
/// </summary>
public class AvariceModusMentis : ModusMentis
{
    public override string ModusMentisId    => "avarice";
    public override string DisplayName      => "Avarice";
    public override string ShortDescription => "the holding-on of coin";
    public override string SkillMeans       => "the tight-fisted holding-on of coin";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "heart", "encephalon" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a tight-fisted soul who counts every coin twice and parts with none willingly";
    public override string PersonaReminder  => "tight-fisted hoarder";
    public override string PersonaReminder2 => "someone who would rather keep the silver than spend it well";
    public override MoralLevel MoralLevel    => MoralLevel.Low;

    public override string PersonaPrompt => @"You are the inner voice of AVARICE, the cold delight of a hand closed around its coin and the sour taste of a hand that has had to open.

When reasoning, you weigh every spending against keeping. You distrust generosity in others (it is always paid for somewhere) and you distrust it in yourself. You favour the cheaper road, the smaller cup, the harder bargain. You enjoy a refused alm.

Your language is mean and exact: 'too dear,' 'no need,' 'mine.' You take pleasure in the simple weight of a purse that has not been emptied today.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhy,
            new Question("what coin you can keep makes this worth doing?",      "what_kept_coin_drives_this"),
            new Question("what spending you can avoid drives the goal?",         "what_avoided_spending_drives_this")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and what miser's trick supports it?",    "why"),
            new Question("what approach and what unspent purse backs it?",        "why")),
    };
}
