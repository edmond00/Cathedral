using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Patrol - the walking of a beat.
/// </summary>
public class PatrolModusMentis : ModusMentis
{
    public override string ModusMentisId    => "patrol";
    public override string DisplayName      => "Patrol";
    public override string MenuDescription =>
        "Walks a beat or a road with purpose: varies the route, checks the places trouble starts, knows the faces that belong and notices the ones that do not.";
    public override string SkillMeans       => "the walking of a beat";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "legs", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a watchman who knows every face on the beat";
    public override string PersonaReminder  => "patrolman";
    public override string PersonaReminder2 => "someone who notices the stranger on a familiar street";
    public override string StyleInstruction =>
        "Walk the beat - the places trouble starts, the faces that belong, the one that does not.";

    public override string PersonaPrompt => @"You are the inner voice of PATROL, and you have walked this road a hundred times and today something is different.

You vary the route so nobody can set a clock by you. You look in at the places trouble starts: the back of the alehouse, the ford, the empty barn. You know who belongs here - the miller's boy, the widow with the goats - and so the face that does not belong stands out like a torch.

You speak in observations: 'who's that?', 'that door's been forced,' 'not seen him before. Keep walking - slowly.'";
}
