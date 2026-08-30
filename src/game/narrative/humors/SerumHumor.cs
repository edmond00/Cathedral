using OpenTK.Mathematics;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Serum humor — a medicinal tincture that rescues the worst outcomes into stability.
/// Converts a 1 face to 4 and provides strong vital heat.
/// </summary>
public sealed class SerumHumor : BodyHumor
{
    public override string Name => "Serum";
    public override char Symbol => '\u2295'; // ⊕
    public override Vector4 Color => new(1.0f, 0.882f, 0.220f, 1.0f);             // bright gold
    public override Vector4 BackgroundColor => new(0.122f, 0.098f, 0.0f, 1.0f);   // dark amber
    public override int VitalHeat => 3;
    public override TransmutingVirtue? TransmutingVirtue => new DigitConversionVirtue(1, 4);
}
