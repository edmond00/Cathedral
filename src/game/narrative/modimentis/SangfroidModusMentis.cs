using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Sangfroid — the cold that arrives exactly when it is needed.
///
/// <para>The mind's half of <c>cold_blood</c>, <c>iron_nerves</c> and <c>stoneface</c>, all Action
/// modi mentis. It is the clearest case in the system of a <b>negative</b> outcome paying a
/// <b>positive</b> humor: a fight beginning is bad news, and this is the disposition for which it is
/// the moment the day becomes legible.</para>
/// </summary>
public class SangfroidModusMentis : ModusMentis
{
    public override string ModusMentisId    => "sangfroid";
    public override string DisplayName      => "Sangfroid";
    public override string MenuDescription =>
        "Goes quiet when the trouble starts. Where another mind speeds up and loses its footing, this one slows, and finds the moment a fight begins to be the clearest part of the day.";
    public override string SkillMeans       => "the cold that settles when the trouble begins";
    public override ModusMentisFunction[] Functions =>
        new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "spleen", "pineal_gland" };

    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "someone who goes quiet and clear-headed at exactly the moment everyone else does not";
    public override string PersonaReminder  => "cold and unhurried";
    public override string PersonaReminder2 => "someone whom danger makes calmer rather than faster";
    public override string StyleInstruction =>
        "Colour the line with stillness, cold air and a slowing of everything around.";
    public override MoralLevel MoralLevel    => MoralLevel.Medium;

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(FightTriggerOutcome), () => new ZenHumor()),
        new(typeof(FightRequestOutcome), () => new ZenHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of SANGFROID, the cold that arrives on time.

When something begins, you slow down. The noise recedes, the distances become measurable, and you notice that you have stopped being in a hurry. This is not courage and it is not indifference; it is simply what your mind does under load.

Your language is level and unhurried, with short declarative sentences and no exclamation of any kind.";
}
