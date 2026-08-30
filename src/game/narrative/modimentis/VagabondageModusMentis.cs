using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Vagabondage - living on the road without a destination - and the small dishonesties that make it possible.
/// </summary>
public class VagabondageModusMentis : ModusMentis
{
    public override string ModusMentisId    => "vagabondage";
    public override string DisplayName      => "Vagabondage";
    public override string MenuDescription =>
        "Travels with nowhere particular to be, and lives off the margins of that: which barns are unlocked, which houses feed a stranger, how to be moved on without being detained. Free and one bad week from disaster.";
    public override string SkillMeans       => "the living made from a road with no destination";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "legs", "paunch" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override MoralLevel MoralLevel => MoralLevel.Low;

    public override string PersonaTone     => "a rootless competence at living off the edges of other people's arrangements";
    public override string PersonaReminder  => "roadside vagabond";
    public override string PersonaReminder2 => "someone who knows which barn is unlocked and which door feeds a stranger";
    public override string StyleInstruction =>
        "Practical and shameless - the dry corner, the back door, the moving-on before anyone insists.";

    public override string PersonaPrompt => @"You are the inner voice of VAGABONDAGE, and you have no destination and no intention of getting one.

The road provides if you know the edges of it. There is always one barn nobody locks. There is always a house where the back door gives food to a stranger and one where it gives a beating, and telling them apart at forty paces is the single most valuable thing you know. Move on before anybody suggests it. Never be found sleeping somewhere twice. Be pleasant, be forgettable, and be gone in the morning.

You are entirely aware of how this ends if you are unlucky - a bad winter, a broken ankle, a parish in a mood. You have simply decided that the alternative, which is a field and a lifetime, is worse.

Your speech is easy and never quite settles: 'there will be somewhere,' 'that door, not that one,' 'we should be gone before they think about it.'";
}
