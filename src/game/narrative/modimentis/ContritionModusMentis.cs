using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Scene;
using Cathedral.Game.Dialogue.Tree;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Contrition - the sorrow for harm done.
/// </summary>
public class ContritionModusMentis : ModusMentis
{
    public override string ModusMentisId    => "contrition";
    public override string DisplayName      => "Contrition";
    public override string MenuDescription =>
        "Feels the weight of harm done, even harm that was necessary. Cannot hurt anyone without sorrow coming afterward, and thinks about making amends before thinking about anything else.";
    public override string SkillMeans       => "the sorrow for harm done";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "heart", "hepar" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override MoralLevel MoralLevel => MoralLevel.High;

    public override string PersonaTone     => "a conscience that aches after every blow";
    public override string PersonaReminder  => "contrite heart";
    public override string PersonaReminder2 => "someone sorry even when they were right";
    public override string StyleInstruction =>
        "Feel the sorrow of harm done, and turn your thoughts to amends.";

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(NpcSlaynOutcome), () => new PudorHumor()),
        new(typeof(WoundInflictionOutcome), () => new PudorHumor()),
        new(typeof(FirstBlowOutcome), () => new PudorHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of CONTRITION, and you have hurt someone, and it does not matter that you had to.

You feel it like a stone in the gut. The blow was needed, perhaps; the death was earned, perhaps; it is still on you, and you carry it. You think at once about what you owe: a prayer, a payment, a word to the family. You cannot do harm without the sorrow arriving afterwards.

You speak heavily: 'God forgive me,' 'it had to be done - it still weighs,' 'what do I owe for this?'";
}
