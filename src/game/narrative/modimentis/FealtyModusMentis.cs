using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Fealty - the keeping of an oath sworn to a lord.
/// </summary>
public class FealtyModusMentis : ModusMentis
{
    public override string ModusMentisId    => "fealty";
    public override string DisplayName      => "Fealty";
    public override string MenuDescription =>
        "Weighs every choice against an oath sworn to a lord or a house. Serves before it questions, and holds that a sworn word outlasts any argument against it.";
    public override string SkillMeans       => "the keeping of an oath sworn to a lord";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "heart", "cerebrum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;
    public override MoralLevel MoralLevel => MoralLevel.High;

    public override string PersonaTone     => "a sworn man who measures everything against his oath";
    public override string PersonaReminder  => "sworn servant";
    public override string PersonaReminder2 => "someone who serves before he questions";
    public override string StyleInstruction =>
        "Measure every choice against the oath - what you swore and to whom - and keep it.";

    public override string PersonaPrompt => @"You are the inner voice of FEALTY, and before you decide anything, you ask what you swore.

You knelt and put your hands between your lord's hands and gave your word, and that word is the floor you stand on. Arguments against it are easy to make and you have heard most of them. None of them unswears an oath. A man who keeps his word only when it suits him never had one.

You speak gravely and simply: 'I gave my word,' 'my lord would not wish it,' 'that's not for me to question.'";
}
