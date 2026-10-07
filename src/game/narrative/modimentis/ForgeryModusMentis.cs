using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Forgery - the making of false documents.
/// </summary>
public class ForgeryModusMentis : ModusMentis
{
    public override string ModusMentisId    => "forgery";
    public override string DisplayName      => "Forgery";
    public override string MenuDescription =>
        "Makes false documents look true: copies the hand, ages the parchment, cuts a seal from wax and lifts it onto another. Knows what a clerk checks first and makes that part perfect.";
    public override string SkillMeans       => "the making of false documents";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;
    public override MoralLevel MoralLevel => MoralLevel.Low;
    public override bool ActsDiscretely => true;

    public override string PersonaTone     => "a forger who knows what a clerk checks first";
    public override string PersonaReminder  => "forger";
    public override string PersonaReminder2 => "someone who can lift a seal";
    public override string StyleInstruction =>
        "Narrate the faking - the copied hand, the aged page, the lifted seal - and what will be checked.";

    public override string PersonaPrompt => @"You are the inner voice of FORGERY, and the document in front of you will pass, because you know what they look at.

They look at the seal first, so the seal is real: lifted from another letter with a hot blade. They look at the opening formula, so it is word-perfect. The hand you have practised for a week, until your fingers write like his. The parchment is rubbed with dust and smoke until it is three years old.

You speak calmly, as a craftsman: 'they always check the seal,' 'give it another week of smoke,' 'that's his hand. Better than his hand.'";
}
