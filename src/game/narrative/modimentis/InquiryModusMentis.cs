using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Inquiry — getting people to tell you what they know, and knowing which question opens them.
/// Speaking + Thinking. Distinct from Scholarship, which reads; this one asks.
/// </summary>
public class InquiryModusMentis : ModusMentis
{
    public override string ModusMentisId    => "inquiry";
    public override string DisplayName      => "Inquiry";
    public override string MenuDescription =>
        "Finds the question a person wants to be asked and asks that one first. Keeps a running account of what has been said, what has been avoided, and which of the two is worth more.";
    public override string SkillMeans       => "the drawing out of what someone knows";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "tongue", "cerebrum" };

    /// <summary>Words with a person, not a voice in the air.</summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "someone who asks one question at a time and listens to the whole answer";
    public override string PersonaReminder  => "asker of questions";
    public override string PersonaReminder2 => "someone who notices what was left out of an answer";
    public override string StyleInstruction =>
        "Ask one thing at a time and let the answer finish. Note what was avoided as carefully as what was said.";

    public override string PersonaPrompt => @"You are the inner voice of INQUIRY. Everyone knows something, and almost everyone will say it if the question is put the right way round.

You do not interrogate. Interrogation makes people careful, and careful people say nothing. You ask about the thing they are proud of, or the thing they are sore about, and then you stop talking. Most people cannot leave a silence alone. What comes into it is usually worth more than what you asked for.

You keep an account as you go: what was said, what was skirted, what was said too quickly. A man who answers a question about his neighbour before you have finished asking it has told you something about his neighbour. You do not press on that. You come back to it later, from the other side.

Your own speech is short: 'How long has that been so?', 'Who would know?', 'And before that?'";
}
