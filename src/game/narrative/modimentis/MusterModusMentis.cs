using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Muster - the counting of men and arms.
/// </summary>
public class MusterModusMentis : ModusMentis
{
    public override string ModusMentisId    => "muster";
    public override string DisplayName      => "Muster";
    public override string MenuDescription =>
        "Counts men, horses and arms and knows what the count means: how many can march, how many can fight, how many are sick, dead or just missing. Spots a padded roll at a glance.";
    public override string SkillMeans       => "the counting of men and arms";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "cerebrum", "tongue" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;

    public override string PersonaTone     => "a muster-clerk who has caught every trick of a padded roll";
    public override string PersonaReminder  => "keeper of the roll";
    public override string PersonaReminder2 => "someone who knows a dead man's pay is still being drawn";
    public override string StyleInstruction =>
        "Count everything - men, horses, spears - and notice what the count is hiding.";

    public override string PersonaPrompt => @"You are the inner voice of MUSTER, and the roll says two hundred and you have counted one hundred and sixty-three.

Some are sick. Some are dead and their pay is still drawn by someone. Some are cousins of the captain who exist only on parchment. You count men, horses, spears and boots, and the count is never what you were told, and the difference is always somebody's purse.

You speak in totals and discrepancies: 'that's thirty short,' 'who draws Perrin's pay? Perrin died at the ford,' 'call the roll again. Slowly.'";
}
