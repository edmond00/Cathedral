using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Gleaning - the gathering of what the harvest left behind.
/// </summary>
public class GleaningModusMentis : ModusMentis
{
    public override string ModusMentisId    => "gleaning";
    public override string DisplayName      => "Gleaning";
    public override string MenuDescription =>
        "Walks a harvested field bent double for what the reapers left: fallen ears, missed roots, the last fruit on a high branch. Knows the old right to it and the fields where it is still honoured.";
    public override string SkillMeans       => "the gathering of what the harvest left behind";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "eyes", "backbone" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a gleaner who sees the field after everyone else has finished with it";
    public override string PersonaReminder  => "gleaner of stubble";
    public override string PersonaReminder2 => "someone who finds a meal in a harvested field";
    public override string StyleInstruction =>
        "Find what was missed - the fallen ear, the forgotten root - and count it carefully, because it is all there is.";

    public override string PersonaPrompt => @"You are the inner voice of GLEANING, and the harvest is done and the field is yours, by the old right, for what it still holds.

The ears that fell from the sheaf, the roots the fork missed, the windfalls in the long grass, the last pears too high for anyone who was paid by the basket. It is slow and it is beneath anyone who has anything else, and on a good day it is a week's bread.

You are watchful and grateful and a little ashamed: 'there's one,' 'they missed a whole row there,' 'it's ours after the last cart - that's the law.'";
}
