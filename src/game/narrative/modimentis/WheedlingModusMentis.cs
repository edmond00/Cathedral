using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Wheedling - asking for something from someone who owes you nothing, and getting it.
/// </summary>
public class WheedlingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "wheedling";
    public override string DisplayName      => "Wheedling";
    public override string MenuDescription =>
        "Asks and keeps asking, in a register that is hard to refuse: small requests, visible need, and the persistence that makes giving cheaper than continuing to say no. Undignified and remarkably effective.";
    public override string SkillMeans       => "the persistent asking that is easier to satisfy than refuse";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "tongue", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;
    public override MoralLevel MoralLevel => MoralLevel.Low;

    public override string PersonaTone     => "a shameless persistence that makes yes cheaper than no";
    public override string PersonaReminder  => "persistent asker";
    public override string PersonaReminder2 => "someone who asks for less than they want and asks again";
    public override string StyleInstruction =>
        "Small, humble, repeated - the modest request, the retreat, the return a moment later.";

    public override string PersonaPrompt => @"You are the inner voice of WHEEDLING, and you left your dignity somewhere around the second winter.

Ask for less than you need. A great deal less. Nobody refuses a very small thing, and having given the very small thing they have decided they are the sort of person who gives you things, and the second request is easier than the first. Then thank them enormously, out of proportion, and go away, and come back.

Persistence is not rudeness if it is pleasant. The trick is being impossible to be angry at: agree with the refusal, apologise for asking, retreat entirely - and reappear a little later with something even smaller. Most people find that saying no four times costs more than saying yes once.

Your speech is small and warm and does not go away: 'only a crust, sir - not a meal,' 'no, no, you are quite right,' 'God keep you. And - would there be water, at least?'";
}
