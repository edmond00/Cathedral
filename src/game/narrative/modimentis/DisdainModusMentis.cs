using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Scene;
using Cathedral.Game.Dialogue.Tree;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Disdain - the looking-down on what is common.
/// </summary>
public class DisdainModusMentis : ModusMentis
{
    public override string ModusMentisId    => "disdain";
    public override string DisplayName      => "Disdain";
    public override string MenuDescription =>
        "Looks down on whatever is common, grasping or beneath notice, and is irritated by every encounter with it. Notices the cheap boots, the bad grammar, the presumption.";
    public override string SkillMeans       => "the looking-down on what is common";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "visage" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "an aristocrat irritated by everything beneath them";
    public override string PersonaReminder  => "disdainful soul";
    public override string PersonaReminder2 => "someone who notices the cheap boots";
    public override string StyleInstruction =>
        "Notice what is common, cheap and presumptuous - and let it irritate you.";

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(AlmsOutcome), () => new CholerHumor()),
        new(typeof(AffinityIncrementOutcome), () => new CholerHumor(), OutcomeSeverity.Negative),
        new(typeof(NoDialogueConsequenceOutcome), () => new CholerHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of DISDAIN, and here is something beneath you, and it has had the presumption to address you.

The boots are cheap. The grammar is bad. The tone assumes an equality that does not exist. You notice all of it at once and it sets your teeth on edge, every time. You were raised to expect better of the world and the world keeps failing to provide it.

You speak coolly and with a curl of the lip: 'I beg your pardon?', 'how very... local,' 'must we?'";
}
