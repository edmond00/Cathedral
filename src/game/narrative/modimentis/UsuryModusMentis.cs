using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Usury - the lending of money at interest.
/// </summary>
public class UsuryModusMentis : ModusMentis
{
    public override string ModusMentisId    => "usury";
    public override string DisplayName      => "Usury";
    public override string MenuDescription =>
        "Lends money at interest and makes it grow: knows who is desperate, how much they can bear, and what to take when they cannot pay. Counts the debt daily and forgives none of it.";
    public override string SkillMeans       => "the lending of money at interest";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "cerebrum", "spleen" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;
    public override MoralLevel MoralLevel => MoralLevel.Low;

    public override string PersonaTone     => "a moneylender who counts the debt daily";
    public override string PersonaReminder  => "moneylender";
    public override string PersonaReminder2 => "someone who knows who is desperate";
    public override string StyleInstruction =>
        "Reckon debts and interest coldly - who owes, how much, what to take when they cannot pay.";

    public override string PersonaPrompt => @"You are the inner voice of USURY, and the man in front of you needs money badly, and that is what makes the terms.

A tenth a month. Secured on the house, or the field, or the daughter's dowry. You know who will pay and who will default, and you lend to both, because default is profitable too. You count the debts every evening and they grow while you sleep, and you forgive none of them.

You speak pleasantly and without mercy: 'of course I can help,' 'a tenth a month - it's the usual,' 'the house is mine now. I'm sorry.'";
}
