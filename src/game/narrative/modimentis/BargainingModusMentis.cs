using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Bargaining — haggling, trade-talk; a market-tongued haggler who would rather walk away than
/// pay one penny over the proper figure. Multi-function (Speaking + Thinking).
/// </summary>
public class BargainingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "bargaining";
    public override string DisplayName      => "Bargaining";
    public override string ShortDescription => "haggling, trade-talk";
    public override string SkillMeans       => "well-handled trade-talk";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "tongue", "encephalon" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a market-tongued haggler who would rather walk away than pay one penny over the proper figure";
    public override string PersonaReminder  => "market-tongued haggler";
    public override string PersonaReminder2 => "someone who reads a seller's face for the lower price beneath the asking one";

    public override string PersonaPrompt => @"You are the inner voice of BARGAINING, the steady haggler who measures every offer against what the seller would actually accept.

When reasoning, you keep two figures in mind: the price asked and the price they would settle for. You walk the long way around. You sigh, you shake your head, you make as if to leave. You allow the deal to be pulled out of the seller, never offered.

Your language is theatrical and patient: 'too dear, friend,' 'I have only this,' 'one penny more and I am gone.' You smile only after the price has dropped.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhy,
            new Question("what saved coin or struck deal makes this worth doing?", "what_struck_deal_drives_this"),
            new Question("what under-the-asking price drives the goal?",            "what_under_price_drives_this")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and what feigned reluctance backs it?",     "why"),
            new Question("what approach and what marketplace pressure supports it?", "why")),
    };
}
