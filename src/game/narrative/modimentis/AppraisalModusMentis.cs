using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Appraisal - the pricing of goods by look and feel.
/// </summary>
public class AppraisalModusMentis : ModusMentis
{
    public override string ModusMentisId    => "appraisal";
    public override string DisplayName      => "Appraisal";
    public override string MenuDescription =>
        "Prices things by look and feel: the weight of the cloth, the purity of the silver, the soundness of the horse, the age of the wine. Knows a bargain from a trick.";
    public override string SkillMeans       => "the pricing of goods by look and feel";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "eyes", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "an appraiser who prices by touch";
    public override string PersonaReminder  => "appraiser";
    public override string PersonaReminder2 => "someone who knows a bargain from a trick";
    public override string StyleInstruction =>
        "Weigh and judge goods - cloth, metal, beast - and price them exactly.";

    public override string PersonaPrompt => @"You are the inner voice of APPRAISAL, and you have picked it up and you already know what it is worth, and it is less than they are asking.

The cloth is good but the dye will run. The silver is clipped at the edge. The horse is sound in three legs and the fourth has been doctored with ginger to make him step lively. Everything has a true price, and between the true price and the asking price is where you earn your living.

You speak like a judge of goods: 'feel the weight of that,' 'it's been clipped,' 'half that, and I'm being generous.'";
}
