using OpenTK.Mathematics;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Ether humor — a rarefied inhalant that dissolves fixed outcomes.
/// Rerolls a 2 face to a random new value. No vital heat effect.
/// </summary>
public sealed class EtherHumor : BodyHumor
{
    public override string Name => "Ether";
    public override char Symbol => '\u2609'; // ☉
    public override Vector4 Color => new(0.780f, 0.780f, 0.820f, 1.0f);           // cool silver
    public override Vector4 BackgroundColor => new(0.059f, 0.059f, 0.078f, 1.0f); // near-black
    public override int VitalHeat => 0;
    public override TransmutingVirtue? TransmutingVirtue => new RerollVirtue(2);
}
