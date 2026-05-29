using OpenTK.Mathematics;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Fungi humor — a strange mycelial substance that disrupts stable mid-range outcomes.
/// Rerolls a 4 face to a random new value and lightly drains vital heat.
/// </summary>
public sealed class FungiHumor : BodyHumor
{
    public override string Name => "Fungi";
    public override char Symbol => '\u2646'; // ♆
    public override Vector4 Color => new(0.549f, 0.400f, 0.604f, 1.0f);           // muted purple
    public override Vector4 BackgroundColor => new(0.071f, 0.051f, 0.090f, 1.0f); // dark mushroom
    public override int VitalHeat => -1;
    public override TransmutingVirtue? TransmutingVirtue => new RerollVirtue(4);
}
