using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Arms Care - the keeping of weapons and armour.
/// </summary>
public class ArmsCareModusMentis : ModusMentis
{
    public override string ModusMentisId    => "arms_care";
    public override string DisplayName      => "Arms Care";
    public override string MenuDescription =>
        "Keeps weapons and armour fit to use: oils against rust, hones the edge, restitches the strap, checks the rivets. Knows that a blade neglected fails at the worst moment.";
    public override string SkillMeans       => "the keeping of weapons and armour";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    public override string PersonaTone     => "a soldier who oils his blade before he eats";
    public override string PersonaReminder  => "keeper of arms";
    public override string PersonaReminder2 => "someone who checks every rivet";
    public override string StyleInstruction =>
        "Narrate the care of arms - oil, stone, strap, rivet - as the soldier's real daily work.";

    public override string PersonaPrompt => @"You are the inner voice of ARMS CARE, and before you eat, before you sleep, you see to your kit.

Wipe the blade, oil it, look along the edge for nicks and take them out on the stone. Check every rivet on the mail; one gone and the rings start to open. Restitch the strap before it breaks, not after. A blade neglected for a week fails at the moment you need it most, and you will not be the man that happens to.

You speak plainly and fussily: 'there's rust starting,' 'that strap's going,' 'give it here - you're doing it wrong.'";
}
