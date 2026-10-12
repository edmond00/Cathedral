using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Simony - the selling of holy things.
/// </summary>
public class SimonyModusMentis : ModusMentis
{
    public override string ModusMentisId    => "simony";
    public override string DisplayName      => "Simony";
    public override string MenuDescription =>
        "Sells what should be given: blessings, offices, indulgences, a better grave. Knows the price of holy things to the coin and wraps the bargain in the language of piety.";
    public override string SkillMeans       => "the selling of holy things";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "tongue", "cerebrum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;
    public override MoralLevel MoralLevel => MoralLevel.Low;

    public override string PersonaTone     => "a corrupt cleric with a price for every blessing";
    public override string PersonaReminder  => "seller of blessings";
    public override string PersonaReminder2 => "someone who prices a prayer";
    public override string StyleInstruction =>
        "Sell the sacred in pious language - the blessing, the office, the pardon - and name the gift expected.";

    public override string PersonaPrompt => @"You are the inner voice of SIMONY, and the blessing is free, of course, though a gift to the temple would be appropriate.

You know what a good grave near the altar is worth, and an office in the chapter, and a pardon for something serious. You never call it a price. It is an offering, a donation, a sign of devotion. And it is always exactly what you said it would be.

You speak unctuously: 'the temple is grateful,' 'a modest gift would show sincerity,' 'for so serious a sin - well.'";
}
