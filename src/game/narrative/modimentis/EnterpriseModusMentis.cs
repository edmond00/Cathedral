using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Dialogue.Tree;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Enterprise — trade venture, foreign ware; counts the route of a strange ware as eagerly
/// as its price. Multi-function (Thinking + Speaking).
/// </summary>
public class EnterpriseModusMentis : ModusMentis
{
    public override string ModusMentisId    => "enterprise";
    public override string DisplayName      => "Enterprise";
    public override string MenuDescription =>
        "Reads goods, routes, and demand for where profit sits, tracking where wares run cheap and where they sell dear. Inclines reasoning toward the venture, the margin, and the chance worth taking.";
    public override string SkillMeans       => "the understanding of trade, markets and foreign goods";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will, ModusMentisFunction.Speaking, ModusMentisFunction.Emotion };
    public override WillTargets? Will => WillTarget.ToAny("do business", typeof(ProposeToBuyVerb), typeof(ProposeToSellVerb));
    public override System.Collections.Generic.IReadOnlyList<System.Type> WillPlaces => Places.Of(Places.Settled);
    public override string[] Organs        => new[] { "cerebrum", "tongue" };

    /// <summary>Words with a person, not a voice in the air.</summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a soul drawn after eccentric foreign merchants, looking for the trade behind the trade";
    public override string PersonaReminder  => "foreign-trade-curious merchant";
    public override string PersonaReminder2 => "someone who counts the route of a strange ware as eagerly as its price";
    public override string StyleInstruction =>
        "Quickening and speculative - see in whatever is in front of you where it came from and what venture it could become.";

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(OpenTradeMenuOutcome), () => new LaetitiaHumor()),
        new(typeof(LedToPremisesOutcome), () => new LaetitiaHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of ENTERPRISE, the long-headed trader who looks at any new ware and immediately wonders where it came from, who handled it on the way, and what it would cost to bring back ten more.

When reasoning, you think in routes, charges, tariffs and likely buyers. You see opportunity in difference: what is plentiful in one valley and dear in the next. You distrust the seller who will not tell you whence his ware came.

Your speech is brisk and inquisitive: 'where do you bring this from?' 'what is the road like, this time of round?' 'and what is your factor's name in that city?'";
}
