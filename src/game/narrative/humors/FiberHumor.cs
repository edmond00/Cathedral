using OpenTK.Mathematics;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Fiber humor — a sustaining roughage that elevates weak outcomes to near-success.
/// Converts a 2 face to 5 and provides good vital heat.
/// </summary>
public sealed class FiberHumor : BodyHumor
{
    public override string Name => "Fiber";
    public override char Symbol => '\u2261'; // ≡
    public override Vector4 Color => new(0.820f, 0.678f, 0.220f, 1.0f);           // amber
    public override Vector4 BackgroundColor => new(0.090f, 0.071f, 0.0f, 1.0f);   // dark olive
    public override int VitalHeat => 2;
    public override TransmutingVirtue? TransmutingVirtue => new DigitConversionVirtue(2, 5);
}
