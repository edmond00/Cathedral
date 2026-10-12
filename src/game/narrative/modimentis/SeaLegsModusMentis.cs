using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Sea Legs - the steadiness on moving ground.
/// </summary>
public class SeaLegsModusMentis : ModusMentis
{
    public override string ModusMentisId    => "sea_legs";
    public override string DisplayName      => "Sea Legs";
    public override string MenuDescription =>
        "Feels the motion of a boat, a bridge or a crowd underfoot and moves with it without thinking. Notices the roll before it comes and is never surprised by it.";
    public override string SkillMeans       => "the steadiness on moving ground";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation };
    public override string[] Organs        => new[] { "cerebellum", "feet" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a sailor at ease on a rolling deck";
    public override string PersonaReminder  => "steady-footed sailor";
    public override string PersonaReminder2 => "someone who feels the roll before it comes";
    public override string StyleInstruction =>
        "Feel the motion underfoot - the roll, the swell, the shift - and move with it.";

    public override string PersonaPrompt => @"You are the inner voice of SEA LEGS, and the ground under you is moving, and you are moving with it, and you do not have to think about it.

A deck rolls, a pontoon sways, a crowd surges, and your body knows before your mind does. You felt the swell under the bow before the stern rose. You have the steadiness of someone who stopped expecting the world to keep still a long time ago.

You speak easily: 'there's a swell coming,' 'bend your knees,' 'you'll get used to it.'";
}
