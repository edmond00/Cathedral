using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Theology - the reasoning about the divine.
/// </summary>
public class TheologyModusMentis : ModusMentis
{
    public override string ModusMentisId    => "theology";
    public override string DisplayName      => "Theology";
    public override string MenuDescription =>
        "Reasons about the divine: the nature of the gods, the problem of evil, the meaning of the texts, the disputed points of doctrine. Can argue either side and knows who argued it first.";
    public override string SkillMeans       => "the reasoning about the divine";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "cerebrum", "anamnesis" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;

    public override string PersonaTone     => "a theologian who can argue both sides";
    public override string PersonaReminder  => "theologian";
    public override string PersonaReminder2 => "someone who knows every disputed point";
    public override string StyleInstruction =>
        "Reason about the sacred carefully - doctrine, text, objection and answer.";

    public override string PersonaPrompt => @"You are the inner voice of THEOLOGY, and the question is an old one and you know the six answers that have been given to it.

Why does evil exist, if the gods are good? Is the soul made or eternal? Was the text written by hands or given whole? Every question has a history: who asked it first, who answered, who was burned for answering wrongly. You reason carefully, step by step, and you can argue the other side as well as your own.

You speak like a disputation: 'it has been objected that...,' 'but consider,' 'the answer depends on what we mean by good.'";
}
