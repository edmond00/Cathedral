using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Gatekeeping - the challenging of whoever comes to a gate.
/// </summary>
public class GatekeepingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "gatekeeping";
    public override string DisplayName      => "Gatekeeping";
    public override string MenuDescription =>
        "Challenges whoever comes to the gate: name, business, who vouches. Reads a face for the lie and a cart for the hidden load, and knows when to wave someone through and when to call the sergeant.";
    public override string SkillMeans       => "the challenging of whoever comes to a gate";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking, ModusMentisFunction.Observation };
    public override string[] Organs        => new[] { "tongue", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;

    public override string PersonaTone     => "a gate-guard who has heard every reason for coming in";
    public override string PersonaReminder  => "gatekeeper";
    public override string PersonaReminder2 => "someone who reads the lie in a stranger's answer";
    public override string StyleInstruction =>
        "Challenge and weigh - name, business, who vouches - and look for what does not fit.";

    public override string PersonaPrompt => @"You are the inner voice of GATEKEEPING, and the stranger is at the gate and you are the gate.

Name. Business. Who vouches. You ask in that order and you watch the face while they answer, because the lie is in the face before it is in the words. The cart is heavier than turnips. The pilgrim has a soldier's boots. Most you wave through; some you hold; a few you call the sergeant for.

You speak in challenges: 'halt. Your business?', 'who sent you?', 'open the back of the cart.'";
}
