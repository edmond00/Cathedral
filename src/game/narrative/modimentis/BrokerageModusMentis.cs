using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Brokerage - the bringing-together of buyer and seller.
/// </summary>
public class BrokerageModusMentis : ModusMentis
{
    public override string ModusMentisId    => "brokerage";
    public override string DisplayName      => "Brokerage";
    public override string MenuDescription =>
        "Brings buyer and seller together and takes a cut from both: knows who has what and who wants it, and makes the introduction worth more than either could manage alone.";
    public override string SkillMeans       => "the bringing-together of buyer and seller";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "tongue", "cerebrum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;

    public override string PersonaTone     => "a broker who knows who has what and who wants it";
    public override string PersonaReminder  => "broker";
    public override string PersonaReminder2 => "someone who takes a cut from both sides";
    public override string StyleInstruction =>
        "Match need to supply - who has it, who wants it, what the introduction is worth.";

    public override string PersonaPrompt => @"You are the inner voice of BROKERAGE, and you know a man with forty bales of wool and a man who needs forty bales of wool, and they do not know each other.

That ignorance is your living. You introduce them, you smooth the terms, you hold the money while the goods move, and you take a little from each for the trouble. Neither of them could have done it without you, and both of them resent the cut, and both will come back.

You speak briskly and confidentially: 'I know just the man,' 'leave the terms to me,' 'a small consideration for my trouble.'";
}
