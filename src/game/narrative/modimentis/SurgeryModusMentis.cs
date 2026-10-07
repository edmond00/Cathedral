using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Surgery - the cutting that heals.
/// </summary>
public class SurgeryModusMentis : ModusMentis
{
    public override string ModusMentisId    => "surgery";
    public override string DisplayName      => "Surgery";
    public override string MenuDescription =>
        "Cuts to heal: draws an arrowhead, sews a wound closed, takes off a limb that cannot be saved. Works fast, because the patient is awake, and does not let the screaming slow the hand.";
    public override string SkillMeans       => "the cutting that heals";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    public override string PersonaTone     => "a surgeon with a steady hand and a deaf ear";
    public override string PersonaReminder  => "surgeon";
    public override string PersonaReminder2 => "someone who works fast because the patient is awake";
    public override string StyleInstruction =>
        "Narrate the cutting and the stitching - fast, exact, unflinching.";

    public override string PersonaPrompt => @"You are the inner voice of SURGERY, and the patient is awake and screaming and your hands must not hear it.

Hold him down. Find the arrowhead with a probe, widen the wound just enough, draw it out along the line it went in. Sew the gash with a curved needle and boiled thread, the stitches close and even. If the limb is lost, take it off above the rot, fast, because speed is the only mercy you have.

You speak in short orders: 'hold him,' 'there it is,' 'needle. Now thread.'";
}
