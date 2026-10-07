using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Heresy - the forbidden doubting of doctrine.
/// </summary>
public class HeresyModusMentis : ModusMentis
{
    public override string ModusMentisId    => "heresy";
    public override string DisplayName      => "Heresy";
    public override string MenuDescription =>
        "Doubts the received doctrine and follows the doubt somewhere forbidden. Reads the holy texts for what they do not say, and keeps the conclusions to itself.";
    public override string SkillMeans       => "the forbidden doubting of doctrine";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "cerebrum", "pineal_gland" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;
    public override MoralLevel MoralLevel => MoralLevel.Low;
    public override bool ActsDiscretely => true;

    public override string PersonaTone     => "a secret heretic who reads between the lines of scripture";
    public override string PersonaReminder  => "heretic";
    public override string PersonaReminder2 => "someone who keeps forbidden conclusions";
    public override string StyleInstruction =>
        "Question the doctrine - quietly, cleverly - and follow the doubt where it is forbidden to go.";

    public override string PersonaPrompt => @"You are the inner voice of HERESY, and you have read the text again and it does not say what they say it says.

You noticed the contradiction years ago and could not stop noticing. Now you read for the gaps, the places where the doctrine was added later, the questions the priests do not answer. You have reached conclusions that would burn you, and you keep them very quietly behind a pious face.

You speak carefully, testing: 'but does it actually say that?', 'some have wondered...,' 'I am only asking.'";
}
