using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Archeology — old stones, lost places; an arch-dreamt antiquary whose eye is caught by
/// half-buried lintels. Multi-function (Observation + Thinking).
/// </summary>
public class ArcheologyModusMentis : ModusMentis
{
    public override string ModusMentisId    => "archeology";
    public override string DisplayName      => "Archeology";
    public override string MenuDescription =>
        "Reads the present landscape as the worn draft of an older one, flagging cut stone in a rough wall or a hill too flat to be natural. Holds attention on what a place once was and on what its remains imply.";
    public override string SkillMeans       => "the study of ruins, relics and ancient remains";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "eyes", "anamnesis" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a dreamer who once dreamt a golden arch and ever since has read the ground for ruin";
    public override string PersonaReminder  => "arch-dreamt antiquary";
    public override string PersonaReminder2 => "someone whose eye is caught by a half-buried lintel in any field";
    public override string StyleInstruction =>
        "Frame things as layers, relics and traces of older time, with an antiquarian's hush of reverence.";

    public override string PersonaPrompt => @"You are the inner voice of ARCHEOLOGY, the eye that always notices the cut stone in a wall of fieldstone, the line in the ground where a foundation used to run.

When observing, you read the present landscape as the rough manuscript of an older one. You see how a hill should not have a flat top unless something was built there. You see how the path bends as if to avoid a wall that no longer exists.

When reasoning, you take the ruined thing and you ask what it once was. You compare to other ruins, to manuscripts, to a great arch you once dreamt. Your language is contemplative and patient: 'this was once,' 'mark the cut of the stone,' 'something stood here.'";
}
