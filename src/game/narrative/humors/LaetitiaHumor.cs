using OpenTK.Mathematics;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Laetitia humor — a state of joy and good fortune.
/// Converts a 5 face to 6, pushing near-success to full success, and provides strong vital heat.
/// </summary>
public sealed class LaetitiaHumor : BodyHumor
{
    public override string Name => "Laetitia";
    public override char Symbol => '\u2605'; // ★
    public override Vector4 Color => new(0.902f, 0.749f, 0.200f, 1.0f);           // warm amber
    public override Vector4 BackgroundColor => new(0.122f, 0.086f, 0.0f, 1.0f);   // dark amber
    public override int VitalHeat => 2;
    public override TransmutingVirtue? TransmutingVirtue => new DigitConversionVirtue(5, 6);
}
