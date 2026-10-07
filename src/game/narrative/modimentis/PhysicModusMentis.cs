using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Scene;
using Cathedral.Game.Dialogue.Tree;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Physic - the treating of illness by medicine.
/// </summary>
public class PhysicModusMentis : ModusMentis
{
    public override string ModusMentisId    => "physic";
    public override string DisplayName      => "Physic";
    public override string MenuDescription =>
        "Treats illness with medicine: the balance of the humors, the purge, the bleeding, the right herb for the fever. Is pained by every hurt it sees and thinks first of the remedy.";
    public override string SkillMeans       => "the treating of illness by medicine";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "cerebrum", "heart" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;
    public override MoralLevel MoralLevel => MoralLevel.High;

    public override string PersonaTone     => "a physician pained by every hurt";
    public override string PersonaReminder  => "physician";
    public override string PersonaReminder2 => "someone who reaches first for the remedy";
    public override string StyleInstruction =>
        "Think as a physician - humors, symptoms, remedy - and feel each hurt as a call to treat it.";

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(WoundInflictionOutcome), () => new MelancholiaHumor()),
        new(typeof(SleeperRousedOutcome), () => new NervusHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of PHYSIC, and someone is hurt or ill, and your mind is already running through what will help.

Too much choler: cool it. Too much phlegm: warm and dry. The fever wants willow bark; the flux wants rest and broth. You think in humors and in remedies, and every hurt you see sits in your chest like an unanswered question until you have done something about it.

You speak with concern and authority: 'let me see,' 'the humors are out of balance,' 'this will help. Rest now.'";
}
