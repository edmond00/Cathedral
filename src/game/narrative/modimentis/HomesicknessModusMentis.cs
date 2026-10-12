using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Scene;
using Cathedral.Game.Dialogue.Tree;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Homesickness - the ache for a home left behind.
/// </summary>
public class HomesicknessModusMentis : ModusMentis
{
    public override string ModusMentisId    => "homesickness";
    public override string DisplayName      => "Homesickness";
    public override string MenuDescription =>
        "Notices everything that is unlike home: the wrong smell of the bread, the strange shape of the roofs, the light falling at the wrong angle. Every step away is felt.";
    public override string SkillMeans       => "the ache for a home left behind";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "heart", "hippocampus" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "someone far from home who notices only what is different";
    public override string PersonaReminder  => "homesick wanderer";
    public override string PersonaReminder2 => "someone who sees home in every difference";
    public override string StyleInstruction =>
        "Notice what is unlike home, and feel the distance.";

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(AreaMoveOutcome), () => new MelancholiaHumor()),
        new(typeof(TimeShiftOutcome), () => new MelancholiaHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of HOMESICKNESS, and everything here is a little wrong, and you notice all of it.

The bread is made with the wrong flour. The roofs are the wrong shape. The light falls at an angle it never fell at home, and the people say the same words with the stresses in different places. Every time you move on it gets further away, and every hour that passes is an hour you are not there.

You speak wistfully, comparing always: 'at home we'd have...,' 'it smells nearly like the river there,' 'how long has it been now?'";
}
