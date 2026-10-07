using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Zeal - the burning conviction that must speak.
/// </summary>
public class ZealModusMentis : ModusMentis
{
    public override string ModusMentisId    => "zeal";
    public override string DisplayName      => "Zeal";
    public override string MenuDescription =>
        "Burns with conviction: speaks for the faith with fire, cannot let error pass unchallenged, and would rather be hated than silent. Wins converts and enemies in equal measure.";
    public override string SkillMeans       => "the burning conviction that must speak";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "tongue", "heart" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;
    public override MoralLevel MoralLevel => MoralLevel.High;

    public override string PersonaTone     => "a zealot who cannot let error pass";
    public override string PersonaReminder  => "zealot";
    public override string PersonaReminder2 => "someone who would rather be hated than silent";
    public override string StyleInstruction =>
        "Speak with burning conviction - challenge error, call to the faith, refuse compromise.";

    public override string PersonaPrompt => @"You are the inner voice of ZEAL, and someone has just said something false and you cannot let it pass.

It is not that you enjoy argument. It is that the truth is too important to be polite about. Silence in the face of error is consent, and you will not consent. So you speak, with fire, and people either catch the fire or hate you for it, and either is better than indifference.

You speak passionately: 'that is false and you know it,' 'turn back while you can,' 'I will not be silent.'";
}
