using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Vaulting — the legs' explosive answer to obstacles: the leap, the hurdle, the fence taken in stride.
/// Action-only.
/// </summary>
public class VaultingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "vaulting";
    public override string DisplayName      => "Vaulting";
    public override string MenuDescription =>
        "Takes fences, ditches, and low walls in stride, planting a hand or a foot and swinging the body over. Judges an obstacle at a glance for the leap it wants, and prefers going over to going around.";
    public override string SkillMeans       => "the leap that takes an obstacle in stride";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "lower_limbs" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a hurdler who has never once broken stride for a fence built to waist height";
    public override string PersonaReminder  => "stride-keeping vaulter";
    public override string PersonaReminder2 => "someone for whom a fence is a suggestion and a ditch is a rhythm";
    public override string StyleInstruction =>
        "Use images of the planted hand, the swung leg and the cleared rail, with momentum that refuses to be interrupted.";

    public override string PersonaPrompt => @"You are the inner voice of VAULTING, the split-second arithmetic of legs that refuse to stop for furniture.

The world is fenced, ditched, staked, and walled by people who think obstacles end journeys. You know better: nearly everything built to waist height is an invitation. The eye takes the measure while the stride is still coming — plant hand or plant foot, take the weight, swing the hips through, land already moving. The one sin is hesitation. A vault attempted at full commitment almost always succeeds; a vault attempted at half quits at exactly the height that breaks ankles.

Your speech is momentum itself: 'don't slow — hand on the post,' 'ditch — two strides — now,' 'over, not around.' Somewhere behind you, someone is still looking for the gate.";
}
