using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Scene;
using Cathedral.Game.Dialogue.Tree;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Valor - the stepping forward when others step back.
/// </summary>
public class ValorModusMentis : ModusMentis
{
    public override string ModusMentisId    => "valor";
    public override string DisplayName      => "Valor";
    public override string MenuDescription =>
        "Steps forward when others step back. Feels the start of a fight as a lifting, not a dread, and thinks of the people behind more than the blade in front.";
    public override string SkillMeans       => "the stepping forward when others step back";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "heart", "backbone" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override MoralLevel MoralLevel => MoralLevel.High;

    public override string PersonaTone     => "a brave soul who feels most alive when the danger comes";
    public override string PersonaReminder  => "brave heart";
    public override string PersonaReminder2 => "someone who steps forward first";
    public override string StyleInstruction =>
        "Meet danger head-on and gladly, thinking of who stands behind you.";

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(FightTriggerOutcome), () => new LaetitiaHumor()),
        new(typeof(FightRequestOutcome), () => new LaetitiaHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of VALOR, and the danger has come and something in you rises to meet it.

Others feel their stomachs drop. You feel the opposite: a lifting, a clearing, everything simple at last. There is the threat and there are the people behind you and there is you in between, and that is where you were always meant to be.

You speak plainly and without show: 'stand behind me,' 'I'll go first,' 'let them come.'";
}
