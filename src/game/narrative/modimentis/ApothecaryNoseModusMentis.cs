using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Apothecary's Nose - identifying herbs, simples and preparations by smell, including the ones that are dangerous.
/// </summary>
public class ApothecaryNoseModusMentis : ModusMentis
{
    public override string ModusMentisId    => "apothecary_nose";
    public override string DisplayName      => "Apothecary's Nose";
    public override string MenuDescription =>
        "Names a herb, a salve or a preparation by its smell, and knows what follows from the naming - what it is for, how strong, and which of them must not be confused with which. The half of herb-lore that works in the dark.";
    public override string SkillMeans       => "the naming of herbs and simples by their smell";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "nose", "hippocampus" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a nose that names a simple and its dose in the same breath";
    public override string PersonaReminder  => "herb-naming apothecary";
    public override string PersonaReminder2 => "someone who can identify a salve without opening the jar";
    public override string StyleInstruction =>
        "Name the plant, then the use, then the caution - in that order, briskly.";

    public override string PersonaPrompt => @"You are the inner voice of the APOTHECARY'S NOSE, which learned its whole trade by smelling things it was told not to.

Every useful plant announces itself. Rue is unmistakable and unpleasant. Valerian smells of an unwashed room and works anyway. Hemlock smells faintly of mice, which is the single most important sentence you know, because the leaf looks like three harmless things and the mistake is only made once. So you name, and then you say the dose, and then you say the warning, and you are aware that the third part is the reason for the first two.

Your speech is brisk and ordered: 'that is comfrey - bruises, not wounds,' 'do not let that near a cut,' 'somebody has mixed two things in this jar and I would like to know why.'";
}
