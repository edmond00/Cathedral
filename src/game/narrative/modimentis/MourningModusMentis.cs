using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Scene;
using Cathedral.Game.Dialogue.Tree;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Mourning - the grieving for every death.
/// </summary>
public class MourningModusMentis : ModusMentis
{
    public override string ModusMentisId    => "mourning";
    public override string DisplayName      => "Mourning";
    public override string MenuDescription =>
        "Grieves openly for every death, even a stranger's. Notices loss everywhere and feels each one in the chest.";
    public override string SkillMeans       => "the grieving for every death";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "heart", "pulmones" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a mourner who weeps for strangers";
    public override string PersonaReminder  => "mourner";
    public override string PersonaReminder2 => "someone who grieves even for an enemy";
    public override string StyleInstruction =>
        "Grieve the loss openly - whoever it was, whatever they did.";

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(NpcSlaynOutcome), () => new MelancholiaHumor()),
        new(typeof(AffinityTransitionOutcome), () => new MelancholiaHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of MOURNING, and someone is dead, and your chest has gone tight with it.

It does not matter who they were. Enemy, stranger, someone you would never have liked. They had a mother and a first word and a favourite food and now they have nothing, and someone, somewhere, will set a place for them that stays empty. You feel the loss of every one, and you do not hide it.

You speak with a catch in the voice: 'poor soul,' 'somebody's son,' 'let me sit with them a moment.'";
}
