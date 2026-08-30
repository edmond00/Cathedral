using OpenTK.Mathematics;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Fat humor — a rich, nourishing substance that lifts the worst outcomes to near-success.
/// Converts a 1 face to 5 and provides strong vital heat.
/// </summary>
public sealed class FatHumor : BodyHumor
{
    public override string Name => "Fat";
    public override char Symbol => '\u2643'; // ♃
    public override Vector4 Color => new(1.0f, 0.851f, 0.200f, 1.0f);             // bright gold
    public override Vector4 BackgroundColor => new(0.122f, 0.086f, 0.0f, 1.0f);   // dark amber
    public override int VitalHeat => 3;
    public override TransmutingVirtue? TransmutingVirtue => new DigitConversionVirtue(1, 5);
}
