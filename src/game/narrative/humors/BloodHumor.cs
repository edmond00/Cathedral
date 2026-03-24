using OpenTK.Mathematics;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Blood humor — secreted by all four humoral organs at high organ scores.
/// Provides vital heat energy and promotes positive dice outcomes by improving face 5 to 6.
/// </summary>
public sealed class BloodHumor : BodyHumor
{
    public override string Name => "Blood";
    public override char Symbol => '\u2649'; // ♉
    public override Vector4 Color => new(0.72f, 0.58f, 0.22f, 1.0f);  // warm amber
    public override int VitalHeat => 1;
    public override TransmutingVirtue? TransmutingVirtue => new DigitConversionVirtue(5, 6);
}
