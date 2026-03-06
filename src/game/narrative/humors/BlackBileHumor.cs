using OpenTK.Mathematics;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Black Bile humor — corrupt substance that accumulates at the exit edge of the queue.
/// Cannot be consumed or removed by normal secretion; only purgation can clear it.
/// Black bile instances are pinned at the back of the queue: when removal is required,
/// the nearest non-black-bile item is deleted instead.
/// Future: a queue entirely filled with black bile causes the character's death.
/// </summary>
public sealed class BlackBileHumor : BodyHumor
{
    public override string Name => "Black Bile";
    public override char Symbol => '\u2629'; // ☩
    public override Vector4 Color => new(0.35f, 0.35f, 0.35f, 1.0f);  // DarkGray35
    public override int VitalHeat => 0;
    public override TransmutingVirtue? TransmutingVirtue => null;
    public override bool IsBlackBile => true;
}
