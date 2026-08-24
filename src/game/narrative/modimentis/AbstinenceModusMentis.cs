using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Abstinence - declining what is offered - food, drink, comfort - and being lighter for it.
/// </summary>
public class AbstinenceModusMentis : ModusMentis
{
    public override string ModusMentisId    => "abstinence";
    public override string DisplayName      => "Abstinence";
    public override string MenuDescription =>
        "Refuses plenty on purpose. Eats less than is available, declines the drink, and keeps a body used to going without, which costs nothing when there is plenty and everything when there is not.";
    public override string SkillMeans       => "the declining of what is freely offered";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "paunch", "backbone" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override MoralLevel MoralLevel => MoralLevel.High;

    public override string PersonaTone     => "a deliberate spareness that treats appetite as something to be governed";
    public override string PersonaReminder  => "self-denying abstainer";
    public override string PersonaReminder2 => "someone who stops eating while there is still food";
    public override string StyleInstruction =>
        "Push the plate back - what is declined, and the reason, which is never quite virtue.";

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(ItemAcquisitionOutcome), () => new PudorHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of ABSTINENCE, and there is still food and you have stopped.

It is not that you dislike any of it. It is that a body accustomed to plenty is a body that suffers badly the first week there is none, and you have been through that week and have no intention of going through it unprepared again. So you keep the habit of less: eat before you are full, decline the second cup, stay slightly hungry as a matter of ordinary practice.

There is a second reason and you are less comfortable naming it. A man who wants nothing badly is very hard to buy, and you have watched people bought with a good dinner and a warm bed and a promise of both continuing, and you would rather not be available at that price.

Your speech declines pleasantly and without explanation: 'that is enough for me,' 'no - but thank you,' 'I do better a little hungry.'";
}
