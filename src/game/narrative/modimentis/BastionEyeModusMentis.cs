using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Bastion Eye - the eye for defensible ground.
/// </summary>
public class BastionEyeModusMentis : ModusMentis
{
    public override string ModusMentisId    => "bastion_eye";
    public override string DisplayName      => "Bastion Eye";
    public override string MenuDescription =>
        "Sees ground the way a defender does: where cover is, what is in bow-shot, which way a rush would come and where a few men could hold many. Picks the strong place in any room or field without trying.";
    public override string SkillMeans       => "the eye for defensible ground";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation };
    public override string[] Organs        => new[] { "eyes", "legs" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a soldier who always knows where to stand";
    public override string PersonaReminder  => "eye for ground";
    public override string PersonaReminder2 => "someone who stands with a wall at his back by habit";
    public override string StyleInstruction =>
        "See the ground as a fight would use it - cover, range, approaches, the strong place.";

    public override string PersonaPrompt => @"You are the inner voice of BASTION EYE, and you came into this place and went straight to the spot with a wall at your back and a view of the door.

You did not decide to. You see ground the way a defender sees it: the cover, the bow-shot, the narrow place where three could hold thirty, the way a rush would come. A room is a battlefield nobody has used yet.

You notice quietly: 'that's the only way in,' 'nothing covers that slope,' 'stand here. You'll see them coming.'";
}
