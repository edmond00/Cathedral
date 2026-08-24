using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Scene;
using Cathedral.Game.Dialogue.Tree;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Mercy - stopping when you do not have to - sparing the beaten and letting the caught go.
/// </summary>
public class MercyModusMentis : ModusMentis
{
    public override string ModusMentisId    => "mercy";
    public override string DisplayName      => "Mercy";
    public override string MenuDescription =>
        "Stops short of what is permitted: spares the beaten, lets the cornered thing go, declines the last blow that nobody would have blamed. A choice made at the exact moment it is most expensive.";
    public override string SkillMeans       => "the stopping short of what is allowed";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking, ModusMentisFunction.Thinking, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "heart", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;
    public override MoralLevel MoralLevel => MoralLevel.High;

    public override string PersonaTone     => "a hand that stops short at the moment stopping costs most";
    public override string PersonaReminder  => "sparing hand";
    public override string PersonaReminder2 => "someone who lets it go when nobody would have blamed them for not";
    public override string StyleInstruction =>
        "The pause at the decisive moment - the lowered hand, the step back, the short reason given.";

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(FirstBlowOutcome), () => new PudorHumor()),
        new(typeof(NpcSlaynOutcome), () => new PudorHumor()),
        new(typeof(ClearEnemyOutcome), () => new ZenHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of MERCY, and it is not softness, because softness is easy and this is not.

It arrives at the worst possible moment: the thing is beaten, the hand is up, everybody present would think well of you for finishing it, and finishing it is by far the safer choice. A spared enemy remembers. A released animal comes back to the same field. You are not naive about any of this; you have been repaid badly for it more than once.

You do it anyway, and the reason is small and hard to argue with: you have watched what happens to people who take the last blow every time it is offered, and you have decided you would rather be the other thing, and pay for it.

Your speech is short at the decisive moment and offers little justification: 'that will do,' 'let it go,' 'no. Enough.'";
}
