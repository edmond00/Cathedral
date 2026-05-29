using OpenTK.Mathematics;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Euphora humor — an uplifting inhalant that liberates the worst outcomes from failure.
/// Rerolls a 1 face to a random new value and provides mild vital heat.
/// </summary>
public sealed class EuphoraHumor : BodyHumor
{
    public override string Name => "Euphora";
    public override char Symbol => '\u2191'; // ↑
    public override Vector4 Color => new(0.949f, 0.902f, 0.549f, 1.0f);           // light gold
    public override Vector4 BackgroundColor => new(0.090f, 0.078f, 0.039f, 1.0f); // dark gold
    public override int VitalHeat => 1;
    public override TransmutingVirtue? TransmutingVirtue => new RerollVirtue(1);
}
