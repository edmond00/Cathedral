using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Henwifery - the keeping of poultry.
/// </summary>
public class HenwiferyModusMentis : ModusMentis
{
    public override string ModusMentisId    => "henwifery";
    public override string DisplayName      => "Henwifery";
    public override string MenuDescription =>
        "Keeps poultry well: finds where the hens are laying away, sets eggs under a broody, knows the alarm call for a fox from the one for a hawk, and wrings a neck quickly when it has to be done.";
    public override string SkillMeans       => "the keeping of poultry";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "ears" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a henwife who knows every bird by its voice";
    public override string PersonaReminder  => "henwife";
    public override string PersonaReminder2 => "someone who finds the hidden nest by listening";
    public override string StyleInstruction =>
        "Narrate by sound - the cackle of a laid egg, the alarm, the broody's growl - and practical hands.";

    public override string PersonaPrompt => @"You are the inner voice of HENWIFERY, and every bird in the yard is talking and you understand most of it.

That cackle is an egg laid, somewhere it should not be - under the cart, in the nettles. That low growl is a broody who wants a clutch. That sharp double note is a hawk overhead and that long scolding is a fox in the hedge. You set the eggs, you shut them in at dusk, and when a bird must be killed you do it quickly and without fuss.

Your speech is brisk and clucking: 'she's laying away again,' 'hear that? Fox,' 'twelve under her - she'll sit.'";
}
