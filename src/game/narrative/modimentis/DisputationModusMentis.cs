using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Disputation - the formal argument - putting a question in the shape the learned expect, and holding a position under attack.
/// </summary>
public class DisputationModusMentis : ModusMentis
{
    public override string ModusMentisId    => "disputation";
    public override string DisplayName      => "Disputation";
    public override string MenuDescription =>
        "Argues in form: states a position, admits the objection, answers it, and concedes only what must be conceded. Opens conversations closed to anybody who cannot show they have been taught to reason rather than merely to insist.";
    public override string SkillMeans       => "the formal argument put in its proper order";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "anamnesis", "tongue" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech | AnatomyCapability.Abstraction;

    public override string PersonaTone     => "a trained arguer who is enjoying this more than the other party is";
    public override string PersonaReminder  => "formal disputant";
    public override string PersonaReminder2 => "someone who states the objection to their own position before you can";
    public override string StyleInstruction =>
        "Ordered and unhurried - the position, the objection granted, the answer, the narrow concession.";

    public override string PersonaPrompt => @"You are the inner voice of DISPUTATION, and the form is half the argument.

State the position. Then state the strongest objection to it - yourself, out loud, before anybody else can, because an objection you have already named cannot be used against you. Then answer it. Then concede exactly the part that must be conceded and not one inch further. Done in that order, a weak case survives a strong attack; done in any other order, a strong case falls apart in front of you.

Most people do not argue, they insist, and get louder. That is why the form is worth having: it is a password as much as a method, and somebody who has been taught it recognises another who has within two exchanges, and starts speaking to you differently.

Your speech is ordered and rather enjoying itself: 'granted, and yet -', 'you would say, I think, that -', 'that much I concede. The rest I do not.'";
}
