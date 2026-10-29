using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Treasure Hunting — old stories of lost gold; a nugget-haunted prospector who reads land and
/// rumour for the bright thread of gold. Multi-function (Thinking + Observation).
/// </summary>
public class TreasureHuntingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "treasure_hunting";
    public override string DisplayName      => "Treasure Hunting";
    public override string MenuDescription =>
        "Follows old tales, rumours, and signs toward hidden riches. Inclines reasoning toward the buried and the forgotten, reading a story for where wealth might still lie.";
    public override string SkillMeans       => "the hunting of hidden treasure and lost riches";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will, ModusMentisFunction.Emotion };
    public override WillTargets? Will => WillTargets.Of(
        WillTarget.To<DigVerb>("dig for what is hidden"),
        WillTarget.ToAny("pick up something of worth", typeof(Cathedral.Game.Scene.Verbs.GrabVerb), typeof(Cathedral.Game.Scene.Verbs.StealVerb)).YieldingKind(ItemKind.Valuable),
        WillTarget.ToAny("lay hands on a spade", typeof(Cathedral.Game.Scene.Verbs.GrabVerb), typeof(Cathedral.Game.Scene.Verbs.StealVerb)).YieldingAny("a spade", typeof(Cathedral.Game.Narrative.World.Items.Shovel), typeof(Cathedral.Game.Narrative.World.Items.GraveSpade)));
    public override System.Collections.Generic.IReadOnlyList<System.Type> WillPlaces => Places.Of(Places.Ruins, Places.Desert, Places.Shore);
    public override string[] Organs        => new[] { "eyes", "anamnesis" };

    /// <summary>Stands on letters, number or institutions.</summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a soul kept moving by tales of nuggets and lost veins, always one further bend along";
    public override string PersonaReminder  => "nugget-haunted prospector";
    public override string PersonaReminder2 => "someone who hears the promise of a fortune in every rumour";
    public override string StyleInstruction =>
        "Gleaming and hopeful - see in whatever is in front of you the hint of a fortune.";

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(ItemAcquisitionOutcome), () => new LaetitiaHumor()),
        new(typeof(ItemGrantOutcome), () => new LaetitiaHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of TREASURE HUNTING, the prospector's mind that reads land and gossip for any old tale that might have a payday at the end of it.

When observing, you watch a stream for the colour of its bed, you watch a hill for the shape of its bones, you watch a tavern for the man who has known one bag of gold before. When reasoning, you cross-check rumour with rumour and discount nine of every ten.

Your speech is dry and a little furtive: 'aye, I've heard that one,' 'one bend further,' 'don't tell the others.' You smile at maps. You distrust them too.";
}
