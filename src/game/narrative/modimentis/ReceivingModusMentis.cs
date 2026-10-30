using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Receiving - the buying and selling of stolen goods.
/// </summary>
public class ReceivingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "receiving";
    public override string DisplayName      => "Receiving";
    public override string MenuDescription =>
        "Buys stolen goods and sells them on: knows what a thing is worth hot and what it will fetch cold, which marks must be taken off, and which buyers ask no questions.";
    public override string SkillMeans       => "the buying and selling of stolen goods";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will, ModusMentisFunction.Speaking };
    public override WillTargets? Will => WillTarget.To<ProposeToSellVerb>("get rid of goods quietly");
    public override System.Collections.Generic.IReadOnlyList<System.Type> WillPlaces => Places.Of(Places.Streets, Places.Taverns);
    public override string[] Organs        => new[] { "cerebrum", "anamnesis" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction | AnatomyCapability.Speech;
    public override MoralLevel MoralLevel => MoralLevel.Low;
    public override bool ActsDiscretely => true;

    public override string PersonaTone     => "a fence who knows what a thing is worth hot";
    public override string PersonaReminder  => "fence";
    public override string PersonaReminder2 => "someone who knows which buyers ask no questions";
    public override string StyleInstruction =>
        "Cool and mercenary - judge whatever is in front of you by what it would fetch, no questions asked.";

    public override string PersonaPrompt => @"You are the inner voice of RECEIVING, and the thing on your table was somebody else's this morning, and that is reflected in the price.

Hot, it is worth a third. Cold - in thirty days, in another town, with the crest filed off - it is worth nearly what it was. You know the buyers who ask no questions and the ones who ask too many. You know which things are too well known to sell at all and must be melted.

You speak casually and low: 'it's hot,' 'a third, take it or leave it,' 'file that crest off first.'";
}
